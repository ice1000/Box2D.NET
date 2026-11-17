// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Semaphores;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2MathFunction;

namespace Box2D.NET
{
    /// <summary>
    /// Callback invoked by a staged parallel-for to process a range of items. May be called
    /// multiple times per worker: work is divided into blocks that workers claim
    /// atomically, so a worker that finishes early picks up the next unclaimed
    /// block instead of sitting idle. workerIndex is the worker identity and is
    /// stable across all invocations from the same worker, so it is safe to use as
    /// an index into per-worker state (e.g. world-&gt;taskContexts.data + workerIndex).
    /// </summary>
    public delegate void b2ParallelForCallback(int startIndex, int endIndex, int workerIndex, object context);

    // todo rename files to task.c and task.h
    public static class B2ParallelFors
    {
        private static void b2RunStepTask(object taskContext)
        {
            B2StepTask task = (B2StepTask)taskContext;
            B2StepContext stepContext = task.stepContext;

            task.fcn(task.context);

            if (task.deferred)
            {
                if (b2AtomicFetchAddInt(ref stepContext.treePending, -1) == 1 && stepContext.signalSemaphores)
                {
                    b2SignalSemaphore(ref stepContext.treeSemaphore);
                }
            }
            else
            {
                if (b2AtomicFetchAddInt(ref stepContext.pendingCount, -1) == 1 && stepContext.signalSemaphores)
                {
                    b2SignalSemaphore(ref stepContext.batchSemaphore);
                }
            }
        }

        // Cached delegate so adding a task does not allocate.
        private static readonly b2TaskCallbackFcn s_runStepTask = b2RunStepTask;
        private static readonly b2TaskCallbackFcn s_parallelForTrampoline = b2ParallelForTrampoline;

        internal static void b2ResetTaskBatch(B2StepContext stepContext)
        {
            stepContext.batchTaskBase = 1;
            stepContext.batchTaskCount = 0;
            stepContext.joinTree = false;
        }

        internal static void b2AddTask(B2StepContext stepContext, B2TaskType type, b2TaskCallbackFcn fcn, object context)
        {
            int index = stepContext.batchTaskBase + stepContext.batchTaskCount;
            B2_ASSERT(1 <= index && index < B2StepContext.B2_STEP_TASK_CAPACITY);

            B2StepTask stepTask = stepContext.stepTasks[index];
            stepTask.fcn = fcn;
            stepTask.context = context;
            stepTask.stepContext = stepContext;
            stepTask.deferred = false;

            ref B2Task task = ref stepContext.tasks[index];
            task.fcn = s_runStepTask;
            task.context = stepTask;
            task.type = type;
            task.deferred = false;

            stepContext.batchTaskCount += 1;
            stepContext.taskCount += 1;
        }

        internal static void b2StageDeferredTask(B2StepContext stepContext, B2TaskType type, b2TaskCallbackFcn fcn, object context)
        {
            B2_ASSERT(stepContext.treeStaged == false);

            B2StepTask stepTask = stepContext.stepTasks[0];
            stepTask.fcn = fcn;
            stepTask.context = context;
            stepTask.stepContext = stepContext;
            stepTask.deferred = true;

            ref B2Task task = ref stepContext.tasks[0];
            task.fcn = s_runStepTask;
            task.context = stepTask;
            task.type = type;
            task.deferred = true;

            stepContext.treeStaged = true;
            stepContext.taskCount += 1;
            b2AtomicStoreInt(ref stepContext.treePending, 1);
        }

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
        /// Divide [0, itemCount) into blocks and add up to world-&gt;workerCount tasks to the current
        /// batch. Each task loops, atomically claiming the next unclaimed block until the range is
        /// drained. minRange is the minimum block size. Block size grows once itemCount exceeds
        /// 8 * workerCount * minRange so block count stays bounded.
        /// </summary>
        internal static void b2StageParallelFor(B2World world, B2TaskType type, b2ParallelForCallback callback, int itemCount, int minRange, object context)
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

            B2StepContext stepContext = world.stepContext;
            B2ParallelForShared shared = stepContext.parallelFor;
            shared.blockCount = blockCount;
            shared.blockSize = blockSize;
            shared.itemCount = itemCount;
            shared.callback = callback;
            shared.context = context;
            b2AtomicStoreInt(ref shared.nextBlock, 0);

            for (int i = 0; i < taskCount; ++i)
            {
                B2ParallelForTask task = stepContext.parallelForTasks[i];
                task.shared = shared;
                task.workerIndex = i;
                b2AddTask(stepContext, type, s_parallelForTrampoline, task);
            }
        }
    }
}
