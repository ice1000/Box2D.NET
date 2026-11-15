// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    /// <summary>
    /// Callback invoked by b2ParallelFor to process a range of items. May be called
    /// multiple times per worker: work is divided into blocks that workers claim
    /// atomically, so a worker that finishes early picks up the next unclaimed
    /// block instead of sitting idle. workerIndex is the worker identity and is
    /// stable across all invocations from the same worker, so it is safe to use as
    /// an index into per-worker state (e.g. world-&gt;taskContexts.data + workerIndex).
    /// </summary>
    public delegate void b2ParallelForCallback(int startIndex, int endIndex, int workerIndex, object context);

    public static class B2ParallelFors
    {
        private static void b2ParallelForTrampoline(object taskContext)
        {
            B2ParallelForTask task = (B2ParallelForTask)taskContext;
            B2ParallelForShared shared = task.shared;
            int workerIndex = task.workerIndex;
            object context = shared.context;
            b2ParallelForCallback callback = shared.callback;

            int blockCount = shared.blockCount;
            int blockSize = shared.blockSize;
            int itemCount = shared.itemCount;

            while (true)
            {
                int blockIndex = b2AtomicFetchAddInt(ref shared.nextBlock, 1);
                if (blockIndex >= blockCount)
                {
                    break;
                }

                int start = blockIndex * blockSize;
                int end = start + blockSize;
                if (end > itemCount)
                {
                    end = itemCount;
                }

                callback(start, end, workerIndex, context);
            }
        }

        /// <summary>
        /// Divide [0, itemCount) into blocks and process them with cooperative claiming:
        /// up to world-&gt;workerCount tasks are enqueued, and each task loops, atomically
        /// claiming the next unclaimed block until the range is drained. Blocks the
        /// caller until all work is complete. minRange is the minimum block size; block
        /// size grows once itemCount exceeds 8 * workerCount * minRange so block count
        /// stays bounded.
        /// </summary>
        public static void b2ParallelFor(B2World world, b2ParallelForCallback callback, int itemCount, int minRange, object context)
        {
            if (itemCount <= 0)
            {
                return;
            }

            B2_ASSERT(minRange > 0);

            int workerCount = world.workerCount;
            B2_ASSERT(0 < workerCount && workerCount <= B2_MAX_WORKERS);

            // Target multiple blocks per worker to reduce thread stalls.
            // block size grows once items exceed maxBlockCount * minRange
            // so the block count stays bounded and per-block sync overhead stays low.
            int blocksPerWorker = 8;
            int maxBlockCount = blocksPerWorker * workerCount;

            int blockSize;
            int blockCount;
            if (itemCount <= minRange * maxBlockCount)
            {
                blockSize = minRange;
                blockCount = (itemCount + blockSize - 1) / blockSize;
            }
            else
            {
                blockSize = (itemCount + maxBlockCount - 1) / maxBlockCount;
                blockCount = (itemCount + blockSize - 1) / blockSize;
            }

            B2_ASSERT(blockCount >= 1);
            B2_ASSERT(blockSize * blockCount >= itemCount);

            // No point enqueueing more tasks than blocks.
            int taskCount = b2MinInt(workerCount, blockCount);

            // A single task would just run every block in order: call the callback directly
            // instead of going through the task system.
            if (taskCount == 1)
            {
                callback(0, itemCount, 0, context);
                return;
            }

            // Reuse the world's shared state unless a b2ParallelFor is already running on it.
            bool reuse = Interlocked.CompareExchange(ref world.parallelForInUse, 1, 0) == 0;
            B2ParallelForShared shared = reuse ? world.parallelForShared : new B2ParallelForShared();
            shared.blockCount = blockCount;
            shared.blockSize = blockSize;
            shared.itemCount = itemCount;
            shared.callback = callback;
            shared.context = context;
            b2AtomicStoreInt(ref shared.nextBlock, 0);

            B2ParallelForTask[] tasks = shared.tasks;
            object[] handles = shared.handles;
            try
            {
                for (int i = 0; i < taskCount; ++i)
                {
                    if (world.taskCount < B2_MAX_TASKS)
                    {
                        handles[i] = world.enqueueTaskFcn(b2ParallelForTrampoline, tasks[i], world.userTaskContext);
                        world.taskCount += 1;
                        world.activeTaskCount += handles[i] == null ? 0 : 1;
                    }
                    else
                    {
                        handles[i] = null;
                        b2ParallelForTrampoline(tasks[i]);
                    }
                }

                for (int i = 0; i < taskCount; ++i)
                {
                    if (handles[i] != null)
                    {
                        world.finishTaskFcn(handles[i], world.userTaskContext);
                        world.activeTaskCount -= 1;
                        handles[i] = null;
                    }
                }
            }
            finally
            {
                shared.callback = null;
                shared.context = null;
                if (reuse)
                {
                    Volatile.Write(ref world.parallelForInUse, 0);
                }
            }
        }
    }
}
