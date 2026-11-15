// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Bit set provides fast operations on large arrays of bits.
    /// </summary>
    public struct B2BitSet
    {
        public ulong[] bits;
        public int blockCapacity;
        public int blockCount;
    }
}
