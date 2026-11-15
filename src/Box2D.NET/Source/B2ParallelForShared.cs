// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Shared state for one b2ParallelFor invocation. Workers race on nextBlock to
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

        // Preallocated per-worker task objects (task i has workerIndex i) and task handles,
        // so a b2ParallelFor call does not allocate.
        public readonly B2ParallelForTask[] tasks;
        public readonly object[] handles;

        public B2ParallelForShared()
        {
            tasks = new B2ParallelForTask[B2Constants.B2_MAX_WORKERS];
            handles = new object[B2Constants.B2_MAX_WORKERS];
            for (int i = 0; i < tasks.Length; ++i)
            {
                tasks[i] = new B2ParallelForTask
                {
                    shared = this,
                    workerIndex = i,
                };
            }
        }
    }
}