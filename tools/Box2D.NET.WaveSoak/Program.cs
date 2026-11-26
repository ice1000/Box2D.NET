// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

// Headless soak of the Continuous / Wave Drop sample, a 2D port of Box3D's mesh soak. Each generation
// drops 1024 small bodies on a wave shaped chain loop and steps until every body sleeps. A generation
// fails if a body falls through the chain or the pile is still awake at the step limit. Tunnels are
// rare, so the soak also counts bodies that sink into the wave past a few depth levels and can compare
// those counts against a baseline. Generations run in parallel, one single threaded world per thread.
// The printed seed reproduces a generation in the sample. The rough wave has bumps smaller than the
// bodies, so a body rests on many short segments at once.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Box2D.NET.Shared;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.Shared.WaveDrops;

namespace Box2D.NET.WaveSoak
{
    public static class Program
    {
        private const int MAX_REPORTED_FAILURES = 32;
        private const int MAX_THREADS = 64;
        private const int SHAPE_COUNT = (int)WaveDropShape.e_waveDropShapeCount;

        private enum FailureKind
        {
            e_none = -1,
            e_tunnel,
            e_awake,
        }

        private struct Failure(FailureKind kind, WaveDropShape shape, int generation, uint seed, int step, int index, B2Pos position)
        {
            public FailureKind kind = kind;
            public WaveDropShape shape = shape;
            public int generation = generation;
            public uint seed = seed;
            public int step = step;
            public int index = index;
            public B2Pos position = position;
        }

        private class ShapeStats
        {
            public int generationCount;
            public int tunnelCount;
            public int awakeCount;
            public int[] depthCounts = new int[WAVE_DROP_DEPTH_LEVEL_COUNT];

            public float maxDepth = -float.MaxValue;
            public uint maxSeed;
            public int maxGeneration = int.MaxValue;
            public int maxIndex = -1;
            public int maxStep;
        }

        private class Soak
        {
            public readonly object mutex = new object();
            public WaveDropSettings settings;
            public uint baseSeed;
            public int generationCount;
            public int nextGeneration;

            // -1 rotates through the shapes
            public int shapeFilter;

            public bool fixedSeed;
            public bool recycling;

            public int completedCount;
            public int failureCount;
            public long totalSteps;
            public int maxSteps;

            public ShapeStats[] stats = CreateStats();
            public Failure[] failures = new Failure[MAX_REPORTED_FAILURES];

            private static ShapeStats[] CreateStats()
            {
                ShapeStats[] stats = new ShapeStats[SHAPE_COUNT];
                for (int i = 0; i < SHAPE_COUNT; ++i)
                {
                    stats[i] = new ShapeStats();
                }

                return stats;
            }
        }

        private static string Format(FormattableString s) => FormattableString.Invariant(s);

        private static void RecordFailure(Soak soak, in Failure failure)
        {
            lock (soak.mutex)
            {
                if (soak.failureCount < MAX_REPORTED_FAILURES)
                {
                    soak.failures[soak.failureCount] = failure;
                }

                soak.failureCount += 1;
            }

            string kind = failure.kind == FailureKind.e_tunnel ? "TUNNEL" : "AWAKE";
            Console.WriteLine(Format($"{kind}: generation {failure.generation}, {g_waveDropShapeNames[(int)failure.shape]}, seed {failure.seed}, step {failure.step}, body ({failure.index / WAVE_DROP_GRID_COUNT}, {failure.index % WAVE_DROP_GRID_COUNT}) at ({failure.position.X:F2}, {failure.position.Y:F2})"));
        }

        // Mirrors WaveDrop.Generate and the auto generate step loop. Returns the step count.
        private static int RunGeneration(Soak soak, int generation, uint seed, WaveDropShape shape, B2BodyId[] bodies,
            WaveDropDepth depth, out FailureKind failureKind)
        {
            failureKind = FailureKind.e_none;

            B2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.workerCount = 1;

            // The world registry is global and not thread safe
            B2WorldId worldId;
            lock (soak.mutex)
            {
                worldId = b2CreateWorld(worldDef);
            }

            if (soak.recycling == false)
            {
                b2World_SetContactRecycleDistance(worldId, 0.0f);
            }

            CreateWaveDropGround(worldId, soak.settings);

            WaveDropSettings settings = soak.settings;
            settings.shape = shape;
            CreateWaveDropBodies(worldId, settings, seed, bodies);
            ResetWaveDropDepth(depth, settings);

            float timeStep = 1.0f / 60.0f;
            int subStepCount = 4;
            int maxSteps = GetWaveDropStepLimit(shape);
            float tunnelHeight = GetWaveDropTunnelHeight();
            int step = 0;
            bool failed = false;

            while (failed == false)
            {
                b2World_Step(worldId, timeStep, subStepCount);
                UpdateWaveDropDepth(worldId, depth);
                step += 1;

                for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
                {
                    B2Pos center = b2Body_GetWorldCenter(bodies[i]);
                    if (center.Y < tunnelHeight)
                    {
                        RecordFailure(soak, new Failure(FailureKind.e_tunnel, shape, generation, seed, step, i, center));
                        failureKind = FailureKind.e_tunnel;
                        failed = true;
                        break;
                    }
                }

                if (b2World_GetBodyEvents(worldId).moveCount == 0)
                {
                    break;
                }

                if (failed == false && step > maxSteps)
                {
                    int awakeIndex = 0;
                    for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
                    {
                        if (b2Body_IsAwake(bodies[i]))
                        {
                            awakeIndex = i;
                            break;
                        }
                    }

                    B2Pos center = b2Body_GetWorldCenter(bodies[awakeIndex]);
                    RecordFailure(soak, new Failure(FailureKind.e_awake, shape, generation, seed, step, awakeIndex, center));
                    failureKind = FailureKind.e_awake;
                    failed = true;
                }
            }

            lock (soak.mutex)
            {
                b2DestroyWorld(worldId);
            }

            return step;
        }

        private static void SoakWorker(Soak soak)
        {
            B2BodyId[] bodies = new B2BodyId[WAVE_DROP_BODY_COUNT];
            WaveDropDepth depth = new WaveDropDepth();
            int[] depthCounts = new int[WAVE_DROP_DEPTH_LEVEL_COUNT];

            while (true)
            {
                int generation;
                lock (soak.mutex)
                {
                    generation = soak.nextGeneration;
                    soak.nextGeneration += 1;
                }

                if (generation >= soak.generationCount)
                {
                    break;
                }

                uint seed = soak.fixedSeed ? soak.baseSeed : GetWaveDropSeed(soak.baseSeed, generation);
                WaveDropShape shape = soak.shapeFilter >= 0 ? (WaveDropShape)soak.shapeFilter : (WaveDropShape)(generation % SHAPE_COUNT);

                int steps = RunGeneration(soak, generation, seed, shape, bodies, depth, out FailureKind failureKind);

                GetWaveDropDepthCounts(depth, depthCounts);

                int completed;
                lock (soak.mutex)
                {
                    soak.completedCount += 1;
                    soak.totalSteps += steps;
                    soak.maxSteps = b2MaxInt(soak.maxSteps, steps);

                    ShapeStats stats = soak.stats[(int)shape];
                    stats.generationCount += 1;
                    stats.tunnelCount += failureKind == FailureKind.e_tunnel ? 1 : 0;
                    stats.awakeCount += failureKind == FailureKind.e_awake ? 1 : 0;
                    for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
                    {
                        stats.depthCounts[i] += depthCounts[i];
                    }

                    // Ties go to the earlier generation so the report does not depend on thread timing
                    if (depth.maxDepth > stats.maxDepth || (depth.maxDepth == stats.maxDepth && generation < stats.maxGeneration))
                    {
                        stats.maxDepth = depth.maxDepth;
                        stats.maxSeed = seed;
                        stats.maxGeneration = generation;
                        stats.maxIndex = depth.maxIndex;
                        stats.maxStep = depth.maxStep;
                    }

                    completed = soak.completedCount;
                }

                if (completed % b2MaxInt(100, soak.generationCount / 10) == 0)
                {
                    Console.WriteLine($"{completed} generations, {completed * WAVE_DROP_BODY_COUNT} bodies");
                }
            }
        }

        // The rough wave gets its own names so its baseline rows never match a run on the broad wave
        private static string GetFilterName(int shapeFilter, bool rough)
        {
            if (shapeFilter < 0)
            {
                return rough ? "rough_all" : "all";
            }

            return rough ? "rough_" + g_waveDropShapeNames[shapeFilter] : g_waveDropShapeNames[shapeFilter];
        }

        // A baseline row starts with the run that made it, so one file can hold several runs
        private static string GetBaselineKey(Soak soak)
        {
            return $"{GetFilterName(soak.shapeFilter, soak.settings.rough)},{soak.baseSeed},{soak.generationCount},";
        }

        private static string DepthLabel(float level) => level.ToString("G", CultureInfo.InvariantCulture);

        // Replaces the rows of this run and keeps the rest
        private static void WriteBaseline(Soak soak, string path)
        {
            string key = GetBaselineKey(soak);

            string[] oldLines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();

            StringBuilder sb = new StringBuilder();
            sb.Append("filter,base,generations,shape,tunnels,awake");
            for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
            {
                sb.Append(",depth").Append(DepthLabel(g_waveDropDepthLevels[i]));
            }

            sb.Append('\n');

            for (int i = 1; i < oldLines.Length; ++i)
            {
                string line = oldLines[i];
                if (line.Length > 0 && line.StartsWith(key, StringComparison.Ordinal) == false)
                {
                    sb.Append(line).Append('\n');
                }
            }

            for (int shape = 0; shape < SHAPE_COUNT; ++shape)
            {
                ShapeStats stats = soak.stats[shape];
                if (stats.generationCount == 0)
                {
                    continue;
                }

                sb.Append($"{GetFilterName(soak.shapeFilter, soak.settings.rough)},{soak.baseSeed},{soak.generationCount},{g_waveDropShapeNames[shape]},{stats.tunnelCount},{stats.awakeCount}");
                for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
                {
                    sb.Append(',').Append(stats.depthCounts[i]);
                }

                sb.Append('\n');
            }

            try
            {
                File.WriteAllText(path, sb.ToString());
            }
            catch (Exception)
            {
                Console.WriteLine($"cannot write {path}");
                return;
            }

            Console.WriteLine($"wrote {path}");
        }

        // Returns 0 when no count grew past its noise band, 1 when one did, 2 when the baseline has no rows for this run
        private static int CompareBaseline(Soak soak, string path)
        {
            if (File.Exists(path) == false)
            {
                Console.WriteLine($"cannot read {path}");
                return 2;
            }

            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                Console.WriteLine($"empty baseline {path}");
                return 2;
            }

            int result = 0;
            int changedCount = 0;
            int matchedCount = 0;
            string filterName = GetFilterName(soak.shapeFilter, soak.settings.rough);

            for (int lineIndex = 1; lineIndex < lines.Length; ++lineIndex)
            {
                string[] fields = lines[lineIndex].Split(',');
                if (fields.Length != 4 + 2 + WAVE_DROP_DEPTH_LEVEL_COUNT)
                {
                    continue;
                }

                if (uint.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out uint baseSeed) == false ||
                    int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int generations) == false)
                {
                    continue;
                }

                int[] counts = new int[2 + WAVE_DROP_DEPTH_LEVEL_COUNT];
                bool parsed = true;
                for (int i = 0; i < counts.Length; ++i)
                {
                    parsed = parsed && int.TryParse(fields[4 + i], NumberStyles.Integer, CultureInfo.InvariantCulture, out counts[i]);
                }

                if (parsed == false)
                {
                    continue;
                }

                if (fields[0] != filterName || baseSeed != soak.baseSeed || generations != soak.generationCount)
                {
                    continue;
                }

                string shapeName = fields[3];
                int shape = Array.IndexOf(g_waveDropShapeNames, shapeName);
                if (shape < 0)
                {
                    continue;
                }

                matchedCount += 1;

                ShapeStats stats = soak.stats[shape];
                int[] current = new int[2 + WAVE_DROP_DEPTH_LEVEL_COUNT];
                current[0] = stats.tunnelCount;
                current[1] = stats.awakeCount;
                for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
                {
                    current[2 + i] = stats.depthCounts[i];
                }

                for (int i = 0; i < current.Length; ++i)
                {
                    if (current[i] == counts[i])
                    {
                        continue;
                    }

                    // Any collision change reshuffles the chaotic pile, so a count moves by about its square root
                    // even when nothing got worse. Flag growth past three of those. A count that was zero flags on any
                    // growth, since a good build never sinks that deep.
                    float band = 3.0f * MathF.Sqrt(counts[i]);
                    bool regressed = i < 2 ? current[i] > counts[i] : current[i] > counts[i] + band;

                    string column = i < 2 ? (i == 0 ? "tunnels" : "awake") : $"> {DepthLabel(g_waveDropDepthLevels[i - 2])} slop";

                    Console.WriteLine($"  {shapeName,-9} {column,-11} {counts[i]}>{current[i]}{(regressed ? "  GREW" : "")}");
                    changedCount += 1;
                    result = regressed ? 1 : result;
                }
            }

            if (matchedCount == 0)
            {
                Console.WriteLine($"{path} has no rows for {(soak.settings.rough ? "--rough " : "")}-s={GetFilterName(soak.shapeFilter, false)} -b={soak.baseSeed} -g={soak.generationCount}");
                return 2;
            }

            Console.WriteLine($"compared with {path}: {changedCount} changed, {(result == 0 ? "none grew past noise" : "GREW")}");
            return result;
        }

        private static string MatchValue(string arg, string shortName, string longName)
        {
            if (arg.StartsWith(shortName, StringComparison.Ordinal))
            {
                return arg.Substring(shortName.Length);
            }

            if (arg.StartsWith(longName, StringComparison.Ordinal))
            {
                return arg.Substring(longName.Length);
            }

            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "Usage: wave_soak [options]\n" +
                $"Drops {WAVE_DROP_BODY_COUNT} bodies per generation on the Wave Drop chain until they sleep.\n" +
                "Fails on a body below y = -2 (tunnel) or a pile awake past the step limit.\n" +
                "Counts bodies that sink into the wave deeper than each depth level.\n" +
                "  -g, --generations=<int>  generation count (default 1000)\n" +
                "  -t, --threads=<int>      worker threads, one world each (default 8)\n" +
                "  -s, --shape=<name>       box, capsule, rounded, circle or all (default box)\n" +
                "  -b, --base-seed=<uint>   base seed, each generation hashes its own seed from it (default random)\n" +
                "  --seed=<uint>            run every generation with this exact seed (replay)\n" +
                "  -a, --amplitude=<float>  wave amplitude (default 0.5, rough 0.015)\n" +
                "  --rough                  fine bumps smaller than the bodies\n" +
                "  -w, --write=<csv>        write the counts as a baseline, replacing only the rows of this run\n" +
                "  -c, --compare=<csv>      compare the counts with the baseline rows from the same wave, -s, -b and -g\n" +
                "  --no-recycling           disable contact recycling\n" +
                "Exit code is 0 when every generation passes and no count grew past noise, 1 otherwise,\n" +
                "2 for bad arguments or a baseline from a different run.");
        }

        public static int Main(string[] args)
        {
            int generationCount = 1000;
            int threadCount = 8;
            int shapeFilter = (int)WaveDropShape.e_waveDropBox;
            uint seed = (uint)b2GetTicks();
            bool fixedSeed = false;
            bool recycling = true;
            bool amplitudeSet = false;
            string writePath = null;
            string comparePath = null;
            WaveDropSettings settings = GetDefaultWaveDropSettings();

            foreach (string arg in args)
            {
                string value;

                if ((value = MatchValue(arg, "-g=", "--generations=")) != null)
                {
                    generationCount = int.Parse(value, CultureInfo.InvariantCulture);
                }
                else if ((value = MatchValue(arg, "-t=", "--threads=")) != null)
                {
                    threadCount = b2ClampInt(int.Parse(value, CultureInfo.InvariantCulture), 1, MAX_THREADS);
                }
                else if ((value = MatchValue(arg, "-s=", "--shape=")) != null)
                {
                    shapeFilter = Array.IndexOf(g_waveDropShapeNames, value);
                    if (value == "all")
                    {
                        shapeFilter = -1;
                    }
                    else if (shapeFilter < 0)
                    {
                        Console.WriteLine($"unknown shape: {value}");
                        return 2;
                    }
                }
                else if ((value = MatchValue(arg, "-seed=", "--seed=")) != null)
                {
                    seed = uint.Parse(value, CultureInfo.InvariantCulture);
                    fixedSeed = true;
                }
                else if ((value = MatchValue(arg, "-b=", "--base-seed=")) != null)
                {
                    seed = uint.Parse(value, CultureInfo.InvariantCulture);
                }
                else if ((value = MatchValue(arg, "-a=", "--amplitude=")) != null)
                {
                    settings.amplitude = float.Parse(value, CultureInfo.InvariantCulture);
                    amplitudeSet = true;
                }
                else if (arg == "--rough")
                {
                    settings.rough = true;
                }
                else if ((value = MatchValue(arg, "-w=", "--write=")) != null)
                {
                    writePath = value;
                }
                else if ((value = MatchValue(arg, "-c=", "--compare=")) != null)
                {
                    comparePath = value;
                }
                else if (arg == "--no-recycling")
                {
                    recycling = false;
                }
                else
                {
                    PrintUsage();
                    return 2;
                }
            }

            if (settings.rough && amplitudeSet == false)
            {
                settings.amplitude = GetWaveDropDefaultAmplitude(true);
            }

            if (fixedSeed && (writePath != null || comparePath != null))
            {
                Console.WriteLine("a baseline needs a base seed, not --seed");
                return 2;
            }

            Soak soak = new Soak();
            soak.settings = settings;
            soak.recycling = recycling;
            soak.baseSeed = seed;
            soak.fixedSeed = fixedSeed;
            soak.generationCount = generationCount;
            soak.shapeFilter = shapeFilter;

            Console.WriteLine($"wave soak: {generationCount} generations, {threadCount} threads, {(settings.rough ? "rough wave, " : "")}shape {GetFilterName(shapeFilter, false)}, {(fixedSeed ? "fixed" : "base")} seed {seed}");

            ulong ticks = b2GetTicks();

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < threadCount; ++i)
            {
                Thread thread = new Thread(() => SoakWorker(soak)) { Name = "soak" };
                thread.Start();
                threads.Add(thread);
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            float seconds = 0.001f * b2GetMilliseconds(ticks);
            double averageSteps = soak.completedCount > 0 ? (double)soak.totalSteps / soak.completedCount : 0.0;
            float slop = B2_LINEAR_SLOP;

            StringBuilder header = new StringBuilder();
            header.Append($"\n{"shape",-9} {"bodies",8} {"tunnels",8} {"awake",6}");
            for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
            {
                string label = $"> {DepthLabel(g_waveDropDepthLevels[i])} slop";
                header.Append($" {label,10}");
            }

            header.Append("   deepest");
            Console.WriteLine(header.ToString());

            for (int shape = 0; shape < SHAPE_COUNT; ++shape)
            {
                ShapeStats stats = soak.stats[shape];
                if (stats.generationCount == 0)
                {
                    continue;
                }

                StringBuilder row = new StringBuilder();
                row.Append($"{g_waveDropShapeNames[shape],-9} {stats.generationCount * WAVE_DROP_BODY_COUNT,8} {stats.tunnelCount,8} {stats.awakeCount,6}");
                for (int i = 0; i < WAVE_DROP_DEPTH_LEVEL_COUNT; ++i)
                {
                    row.Append($" {stats.depthCounts[i],10}");
                }

                if (stats.maxIndex >= 0)
                {
                    row.Append(Format($"   {stats.maxDepth / slop:F1} slop, seed {stats.maxSeed} body ({stats.maxIndex / WAVE_DROP_GRID_COUNT}, {stats.maxIndex % WAVE_DROP_GRID_COUNT}) step {stats.maxStep}"));
                }

                Console.WriteLine(row.ToString());
            }

            Console.WriteLine(Format($"\nsteps to sleep: average {averageSteps:F0}, max {soak.maxSteps}, time {seconds:F1} s"));

            int reported = b2MinInt(soak.failureCount, MAX_REPORTED_FAILURES);
            for (int i = 0; i < reported; ++i)
            {
                ref Failure f = ref soak.failures[i];
                Console.WriteLine($"  {(f.kind == FailureKind.e_tunnel ? "tunnel" : "awake")} {g_waveDropShapeNames[(int)f.shape]} seed {f.seed} body ({f.index / WAVE_DROP_GRID_COUNT}, {f.index % WAVE_DROP_GRID_COUNT})");
            }

            int exitCode = soak.failureCount == 0 ? 0 : 1;

            if (writePath != null)
            {
                WriteBaseline(soak, writePath);
            }

            if (comparePath != null)
            {
                int compareResult = CompareBaseline(soak, comparePath);
                exitCode = b2MaxInt(exitCode, compareResult);
            }

            Console.WriteLine(exitCode == 0 ? "PASS" : "FAIL");

            return exitCode;
        }
    }
}
