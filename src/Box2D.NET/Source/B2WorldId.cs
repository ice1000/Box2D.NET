// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// World id references a world instance. This should be treated as an opaque handle.
    public readonly struct B2WorldId(ushort index1, ushort generation)
    {
        public readonly ushort index1 = index1;
        public readonly ushort generation = generation;
    }
}
