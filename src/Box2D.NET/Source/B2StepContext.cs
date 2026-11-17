// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    /// <summary>
    /// Context for a time step. Owned by the world and reset with Clear() each time step.
    /// </summary>
    public class B2StepContext // TODO: @ikpil, check struct or class
    {
        internal const int B2_STEP_TASK_CAPACITY = B2_MAX_WORKERS + 2;

        public B2StepPhase phase;
        public bool pairsUpdated;
        public bool treeStaged;
        public bool treeLaunched;
        public bool solveSkipped;
        public bool joinTree;
        public bool signalSemaphores;

        public int batchTaskBase;
        public int batchTaskCount;
        public int taskCount;
        public int enqueueCount;

        public readonly B2Task[] tasks = new B2Task[B2_STEP_TASK_CAPACITY];
        internal readonly B2StepTask[] stepTasks = CreateArray<B2StepTask>(B2_STEP_TASK_CAPACITY);

        internal readonly B2ParallelForShared parallelFor = new B2ParallelForShared();
        internal readonly B2ParallelForTask[] parallelForTasks = CreateArray<B2ParallelForTask>(B2_MAX_WORKERS);
        public readonly B2WorkerContext[] workerContexts = CreateArray<B2WorkerContext>(B2_MAX_WORKERS);

        // Created only when the world has no finish task function. Preserved across Clear().
        public B2Semaphore batchSemaphore;
        public B2Semaphore treeSemaphore;

        public ulong stepTicks;
        public ulong pairTicks;
        public ulong collideTicks;
        public ulong solveTicks;
        public ulong constraintTicks;
        public ulong transformTicks;
        public ulong bulletTicks;
        public ulong sensorTicks;

        public int awakeBodyCount;
        public int collideContactCount;

        public ArraySegment<B2SyncBlock> bodyBlocks;
        public ArraySegment<B2SyncBlock> contactBlocks;
        public ArraySegment<B2SyncBlock> jointBlocks;
        public ArraySegment<B2SyncBlock> graphBlocks;
        public ArraySegment<B2ContactConstraint> overflowContacts;

        /// <summary>
        /// time step
        /// </summary>
        public float dt;

        /// <summary>
        /// inverse time step (0 if dt == 0).
        /// </summary>
        public float inv_dt;

        /// <summary>
        /// sub-step
        /// </summary>
        public float h;
        public float inv_h;

        public int subStepCount;

        public B2Softness contactSoftness;
        public B2Softness staticSoftness;

        public float restitutionThreshold;
        public float maxLinearVelocity;

        public B2World world;
        public B2ConstraintGraph graph;

        /// <summary>
        /// shortcut to body states from awake set
        /// </summary>
        public B2BodyState[] states;

        /// <summary>
        /// shortcut to body sims from awake set
        /// </summary>
        public B2BodySim[] sims;

        /// <summary>
        /// array of all shape ids for shapes that have enlarged AABBs
        /// </summary>
        public int[] enlargedShapes;
        public int enlargedShapeCount;

        /// <summary>
        /// Array of bullet bodies that need continuous collision handling
        /// </summary>
        public ArraySegment<int> bulletBodies;
        public B2AtomicInt bulletBodyCount;

        public B2AtomicInt anyRestitution;

        /// <summary>
        /// Graph color spans for the narrow-phase, includes non-touching contacts as well.
        /// </summary>
        public readonly B2ContactCollideSpan[] collideSpans = new B2ContactCollideSpan[B2_GRAPH_COLOR_COUNT + 2];

        /// <summary>
        /// Flat view of the wide contact constraint array used by prepare and store.
        /// prepareSpans has activeColorCount + 1 entries, the last being a sentinel
        /// at wideContactCount. The wide constraints themselves are owned by
        /// world.contactSolverWide; colors[i].wideConstraintStart is each color's offset.
        /// </summary>
        public readonly B2ContactPrepareSpan[] contactPrepareSpans = new B2ContactPrepareSpan[B2_GRAPH_COLOR_COUNT + 1];
        public int wideContactCount;

        public readonly B2JointPrepareSpan[] jointPrepareSpans = new B2JointPrepareSpan[B2_GRAPH_COLOR_COUNT + 1];
        public int jointCount;

        public int activeColorCount;
        public int workerCount;

        public ArraySegment<B2SolverStage> stages;
        public int stageCount;
        public int orchestratorIndex;
        public bool enableWarmStarting;

        /// <summary>
        /// padding to prevent false sharing
        /// </summary>
        public B2FixedArray64<byte> padding1;

        /// <summary>
        /// This atomic is central to multi-threaded solver task synchronization.
        /// It prevents ABA problems by monotonically growing as the solver advances.
        /// This means a delayed worker thread will catch up without repeating already completed
        /// work (causing a race condition).
        /// sync index (16-bits) | stage type (16-bits)
        /// </summary>
        public B2AtomicU32 atomicSyncBits;

        /// <summary>
        /// padding to prevent false sharing
        /// </summary>
        public B2FixedArray64<byte> padding2;

        public B2AtomicInt orchestratorClaimed;

        /// <summary>
        /// padding to prevent false sharing
        /// </summary>
        public B2FixedArray64<byte> padding3;

        public B2AtomicInt pendingCount;
        public B2AtomicInt treePending;

        private static T[] CreateArray<T>(int count) where T : new()
        {
            T[] array = new T[count];
            for (int i = 0; i < count; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        /// <summary>
        /// Reset for a new time step. The semaphores and the preallocated arrays are kept.
        /// </summary>
        public void Clear()
        {
            phase = B2StepPhase.b2_stepIdle;
            pairsUpdated = false;
            treeStaged = false;
            treeLaunched = false;
            solveSkipped = false;
            joinTree = false;
            signalSemaphores = false;

            batchTaskBase = 0;
            batchTaskCount = 0;
            taskCount = 0;
            enqueueCount = 0;

            Array.Clear(tasks, 0, tasks.Length);
            foreach (B2StepTask stepTask in stepTasks)
            {
                stepTask.fcn = null;
                stepTask.context = null;
                stepTask.stepContext = null;
                stepTask.deferred = false;
            }

            parallelFor.callback = null;
            parallelFor.context = null;
            parallelFor.nextBlock = default;
            foreach (B2WorkerContext workerContext in workerContexts)
            {
                workerContext.Clear();
            }

            stepTicks = 0;
            pairTicks = 0;
            collideTicks = 0;
            solveTicks = 0;
            constraintTicks = 0;
            transformTicks = 0;
            bulletTicks = 0;
            sensorTicks = 0;

            awakeBodyCount = 0;
            collideContactCount = 0;

            bodyBlocks = default;
            contactBlocks = default;
            jointBlocks = default;
            graphBlocks = default;
            overflowContacts = default;

            dt = 0.0f;
            inv_dt = 0.0f;
            h = 0.0f;
            inv_h = 0.0f;
            subStepCount = 0;
            contactSoftness = default;
            staticSoftness = default;
            restitutionThreshold = 0.0f;
            maxLinearVelocity = 0.0f;
            world = null;
            graph = default;
            states = null;
            sims = null;
            enlargedShapes = null;
            enlargedShapeCount = 0;
            bulletBodies = default;
            bulletBodyCount = default;
            anyRestitution = default;
            Array.Clear(collideSpans, 0, collideSpans.Length);
            Array.Clear(contactPrepareSpans, 0, contactPrepareSpans.Length);
            wideContactCount = 0;
            Array.Clear(jointPrepareSpans, 0, jointPrepareSpans.Length);
            jointCount = 0;
            activeColorCount = 0;
            workerCount = 0;
            stages = default;
            stageCount = 0;
            orchestratorIndex = 0;
            enableWarmStarting = false;
            atomicSyncBits = default;
            orchestratorClaimed = default;
            pendingCount = default;
            treePending = default;
        }
    }
}
