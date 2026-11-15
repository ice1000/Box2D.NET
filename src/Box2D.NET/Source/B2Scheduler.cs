// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    public sealed class B2Scheduler
    {
        public B2Thread[] threads = new B2Thread[B2_MAX_WORKERS];
        public B2SchedulerWorkerContext[] workerContexts = new B2SchedulerWorkerContext[B2_MAX_WORKERS];

        /// <summary>
        /// total workers including main thread
        /// </summary>
        public int workerCount;

        /// <summary>
        /// threads created = workerCount - 1
        /// </summary>
        public int threadCount;

        public readonly B2SchedulerTask[] tasks = new B2SchedulerTask[B2_MAX_TASKS];
        public B2AtomicInt nextSlot;

        public B2Semaphore taskSemaphore;
        public B2AtomicInt sleeperCount;
        public B2AtomicInt shutdown;
    }
}