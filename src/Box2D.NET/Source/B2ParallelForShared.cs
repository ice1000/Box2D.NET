// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Shared state for one staged parallel-for. Workers race on nextBlock to
    /// claim work, so a slow chunk can't strand the other threads.
    /// </summary>
    internal class B2ParallelForShared
    {
        public B2AtomicInt nextBlock;
        public int blockCount;
        public int blockSize;
        public int itemCount;
        public b2ParallelForCallback callback;
        public object context;
    }
}
