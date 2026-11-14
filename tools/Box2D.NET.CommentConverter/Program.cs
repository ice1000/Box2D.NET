using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Box2D.NET.CommentConverter;

/// <summary>
/// Converts plain <c>//</c> and doxygen-style <c>///</c> comments that sit directly above
/// declarations into XML documentation comments wrapped in <c>&lt;summary&gt;</c>, so DocFX can pick them up.
/// Usage: <c>Box2D.NET.CommentConverter [folder]</c>. Without an argument it searches upward from the
/// current directory (and from the executable's directory) for <c>src/Box2D.NET</c>.
/// </summary>
public static class Program
{
    public const string FileFilter = "*.cs";
    public const string SummaryStart = "/// <summary>";
    public const string SummaryEnd = "/// </summary>";

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj" };

    // Optional modifiers followed by at least one access modifier that makes the member worth documenting.
    private static readonly Regex DeclarationRegex = new(
        @"^(?:(?:static|readonly|unsafe|partial|sealed|abstract|override|virtual|new|const|ref|extern|fixed|volatile|async|private)\s+)*(?:public|protected|internal)\b",
        RegexOptions.Compiled);

    // An enum member: `name`, `name = value`, optionally followed by a comma and a trailing comment.
    private static readonly Regex EnumMemberRegex = new(
        @"^[A-Za-z_]\w*\s*(?:=[^;{}]*?)?\s*,?\s*(?://.*)?$",
        RegexOptions.Compiled);

    // Leading `[Attribute(...)]` groups on a line.
    private static readonly Regex LeadingAttributesRegex = new(@"^(?:\[[^\]]*\]\s*)+", RegexOptions.Compiled);

    // A block that already contains XML doc tags is left alone (keeps the tool idempotent).
    private static readonly Regex XmlDocTagRegex = new(
        @"<\s*/?\s*(?:summary|remarks|inheritdoc|param|returns|typeparam|value|example|exception)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<int> Main(string[] args)
    {
        var folderPath = args.Length > 0 ? Path.GetFullPath(args[0]) : FindSourceFolder();
        if (folderPath is null || !Directory.Exists(folderPath))
        {
            Console.Error.WriteLine("Could not locate src/Box2D.NET. Pass the folder to process as the first argument.");
            return 1;
        }

        var files = Directory.GetFiles(folderPath, FileFilter, SearchOption.AllDirectories)
            .Where(f => !IsInExcludedDirectory(folderPath, f))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        Console.WriteLine($"Found {files.Count} C# files in the folder: {folderPath}");

        int changedFiles = 0;
        int convertedBlocks = 0;
        foreach (var filePath in files)
        {
            int blocks = await ProcessFileAsync(filePath);
            if (blocks > 0)
            {
                changedFiles++;
                convertedBlocks += blocks;
            }
        }

        Console.WriteLine($"*** Processing completed: {convertedBlocks} comment blocks converted in {changedFiles} files ***");

        return 0;
    }

    private static string FindSourceFolder()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "src", "Box2D.NET");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool IsInExcludedDirectory(string root, string filePath)
    {
        var relative = Path.GetRelativePath(root, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Take(segments.Length - 1).Any(ExcludedDirectories.Contains);
    }

    /// <summary>Converts one file in place. Returns the number of converted comment blocks.</summary>
    public static async Task<int> ProcessFileAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var encoding = new UTF8Encoding(hasBom);
        var content = encoding.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
        var (lines, endings) = SplitLines(content);

        int converted = ConvertLines(lines, endings);
        if (converted == 0)
        {
            return 0;
        }

        var output = new StringBuilder(content.Length + converted * 64);
        for (int i = 0; i < lines.Count; i++)
        {
            output.Append(lines[i]).Append(endings[i]);
        }

        await File.WriteAllTextAsync(filePath, output.ToString(), encoding);
        Console.WriteLine($"{filePath}: {converted} comment blocks converted");
        return converted;
    }

    /// <summary>
    /// Splits text into lines plus each line's own terminator ("\r\n", "\n" or "" for the last line),
    /// so files with mixed line endings are written back unchanged apart from the converted comments.
    /// </summary>
    public static (List<string> Lines, List<string> Endings) SplitLines(string content)
    {
        var lines = new List<string>();
        var endings = new List<string>();
        int start = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] != '\n')
            {
                continue;
            }

            bool crlf = i > start && content[i - 1] == '\r';
            lines.Add(content.Substring(start, i - start - (crlf ? 1 : 0)));
            endings.Add(crlf ? "\r\n" : "\n");
            start = i + 1;
        }

        lines.Add(content.Substring(start));
        endings.Add("");
        return (lines, endings);
    }

    /// <summary>
    /// Converts the comment blocks in <paramref name="lines"/> in place. When <paramref name="endings"/> is given,
    /// it is kept in step with <paramref name="lines"/>. Returns the number of converted blocks.
    /// </summary>
    public static int ConvertLines(List<string> lines, List<string> endings = null)
    {
        var insideEnum = ComputeInsideEnum(lines);
        var namespaceLineIndex = lines.FindIndex(line => line.TrimStart().StartsWith("namespace "));

        var blocks = ExtractCommentBlocks(lines)
            .Where(b => b.StartIndex > namespaceLineIndex) // skip license headers etc.
            .Where(b => IsFollowedByDocumentableDeclaration(lines, b, insideEnum))
            .Where(b => !AlreadyHasXmlDoc(lines, b))
            .Where(b => !LooksLikeCommentedOutCode(lines, b))
            .ToList();

        // Bottom-up so inserted summary lines don't shift the indexes of blocks still to process.
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            ConvertBlock(lines, endings, blocks[i]);
        }

        return blocks.Count;
    }

    public static bool IsCommentLine(string line) => line.TrimStart().StartsWith("//");

    public static List<CommentBlock> ExtractCommentBlocks(List<string> lines)
    {
        var blocks = new List<CommentBlock>();
        int i = 0;
        while (i < lines.Count)
        {
            if (!IsCommentLine(lines[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i + 1 < lines.Count && IsCommentLine(lines[i + 1]))
            {
                i++;
            }

            blocks.Add(new CommentBlock(start, i));
            i++;
        }

        return blocks;
    }

    private static bool IsFollowedByDocumentableDeclaration(List<string> lines, CommentBlock block, bool[] insideEnum)
    {
        // Skip attribute-only lines such as [MethodImpl(...)] between the comment and the declaration.
        int index = block.EndIndex + 1;
        while (index < lines.Count)
        {
            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith("[") && LeadingAttributesRegex.Replace(trimmed, "").Length == 0)
            {
                index++;
                continue;
            }

            break;
        }

        if (index >= lines.Count)
        {
            return false;
        }

        var target = LeadingAttributesRegex.Replace(lines[index].Trim(), "");
        if (target.Length == 0 || target.StartsWith("//") || target.StartsWith("#"))
        {
            return false;
        }

        if (DeclarationRegex.IsMatch(target))
        {
            return true;
        }

        return insideEnum[index] && EnumMemberRegex.IsMatch(target);
    }

    private static bool AlreadyHasXmlDoc(List<string> lines, CommentBlock block)
    {
        for (int i = block.StartIndex; i <= block.EndIndex; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("///") && XmlDocTagRegex.IsMatch(trimmed))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeCommentedOutCode(List<string> lines, CommentBlock block)
    {
        int nonEmpty = 0;
        int codeLike = 0;
        for (int i = block.StartIndex; i <= block.EndIndex; i++)
        {
            var text = GetCommentText(lines[i]).Trim();
            if (text.Length == 0)
            {
                continue;
            }

            nonEmpty++;
            if (text is "{" or "}")
            {
                return true;
            }

            if (text.EndsWith(";") || text.EndsWith("{") || text.EndsWith("}"))
            {
                codeLike++;
            }
        }

        return nonEmpty > 0 && codeLike * 2 >= nonEmpty;
    }

    private static void ConvertBlock(List<string> lines, List<string> endings, CommentBlock block)
    {
        if (endings is not null)
        {
            // Inserted lines reuse the terminator of the comment line they sit next to.
            endings.Insert(block.EndIndex + 1, endings[block.EndIndex]);
            endings.Insert(block.StartIndex, endings[block.StartIndex]);
        }

        var indentation = GetIndentation(lines[block.StartIndex]);

        for (int i = block.StartIndex; i <= block.EndIndex; i++)
        {
            var text = EscapeXml(GetCommentText(lines[i]));
            lines[i] = text.Length == 0
                ? $"{GetIndentation(lines[i])}///"
                : $"{GetIndentation(lines[i])}/// {text}";
        }

        lines.Insert(block.EndIndex + 1, $"{indentation}{SummaryEnd}");
        lines.Insert(block.StartIndex, $"{indentation}{SummaryStart}");
    }

    /// <summary>The text after the leading <c>//</c> or <c>///</c>, with one separating space removed.</summary>
    public static string GetCommentText(string line)
    {
        var trimmed = line.TrimStart();
        int slashes = trimmed.StartsWith("///") ? 3 : 2;
        var text = trimmed.Substring(slashes);
        if (text.StartsWith(" "))
        {
            text = text.Substring(1);
        }

        return text.TrimEnd();
    }

    public static string EscapeXml(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

    private static string GetIndentation(string line) => line.Substring(0, line.Length - line.TrimStart().Length);

    /// <summary>
    /// For each line, whether it starts inside the body of an enum. Tracks braces while ignoring
    /// comments, string literals and character literals.
    /// </summary>
    public static bool[] ComputeInsideEnum(List<string> lines)
    {
        var result = new bool[lines.Count];
        var stack = new Stack<bool>();
        var header = new StringBuilder();
        bool inBlockComment = false;

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            result[lineIndex] = stack.Count > 0 && stack.Peek();

            var line = lines[lineIndex];
            for (int c = 0; c < line.Length; c++)
            {
                char ch = line[c];
                char next = c + 1 < line.Length ? line[c + 1] : '\0';

                if (inBlockComment)
                {
                    if (ch == '*' && next == '/')
                    {
                        inBlockComment = false;
                        c++;
                    }

                    continue;
                }

                if (ch == '/' && next == '/')
                {
                    break;
                }

                if (ch == '/' && next == '*')
                {
                    inBlockComment = true;
                    c++;
                    continue;
                }

                if (ch == '"' || ch == '\'')
                {
                    c = SkipLiteral(line, c);
                    continue;
                }

                switch (ch)
                {
                    case '{':
                        stack.Push(Regex.IsMatch(header.ToString(), @"\benum\b"));
                        header.Clear();
                        break;
                    case '}':
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }

                        header.Clear();
                        break;
                    case ';':
                        header.Clear();
                        break;
                    default:
                        header.Append(ch);
                        break;
                }
            }

            header.Append(' ');
        }

        return result;
    }

    // Returns the index of the closing quote (or the end of the line).
    private static int SkipLiteral(string line, int start)
    {
        char quote = line[start];
        bool verbatim = start > 0 && line[start - 1] == '@';
        for (int i = start + 1; i < line.Length; i++)
        {
            if (!verbatim && line[i] == '\\')
            {
                i++;
                continue;
            }

            if (line[i] == quote)
            {
                if (verbatim && i + 1 < line.Length && line[i + 1] == quote)
                {
                    i++;
                    continue;
                }

                return i;
            }
        }

        return line.Length;
    }

    public record class CommentBlock(int StartIndex, int EndIndex)
    {
        public int Length => EndIndex - StartIndex + 1;
    }
}
