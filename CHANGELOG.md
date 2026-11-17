# Changelog

Box2D.NET.Fork Release 🎉

This is an AI-assisted C# fork of the original Box2D.Net for my own purposes.

I have come to the realization that (too late due to me being new to C#):

1. The original targets Unity users, which is on .net standard 2.1, an ancient version of C#;
2. Unity is [going to](https://discussions.unity.com/t/path-to-coreclr-2026-upgrade-guide/1714279) upgrade to CoreCLR and newer versions of C#, leaving fewer reasons to keep this project on an old language version;
3. The original does not aim to use the SIMD operations at all.

This fork does the following:

1. I will use the target C# version of that Unity forum post;
2. Use SIMD in the project as much as I see feasible (I don't have that much time either),
   especially rely on the behavior of recent versions of `System.Numerics`
   (yes, it seems to change over major versions);
3. Keep up with the original box2d development.

### Special Thanks
- [@erincatto](https://github.com/erincatto)
- [@ikpil](https://github.com/ikpil)

