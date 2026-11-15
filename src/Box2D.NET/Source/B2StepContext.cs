// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// <summary>
    /// Context for a time step. Recreated each time step.
    /// </summary>
    public class B2StepContext // TODO: @ikpil, check struct or class
    {
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
        public B2ContactCollideSpan[] collideSpans;

        /// <summary>
        /// Flat view of the wide contact constraint array used by prepare and store.
        /// prepareSpans has activeColorCount + 1 entries, the last being a sentinel
        /// at wideContactCount. The wide constraints themselves are owned by
        /// world.contactSolverWide; colors[i].wideConstraintStart is each color's offset.
        /// </summary>
        public B2ContactPrepareSpan[] contactPrepareSpans;
        public int wideContactCount;

        public B2JointPrepareSpan[] jointPrepareSpans;
        public int jointCount;

        public int activeColorCount;
        public int workerCount;

        public ArraySegment<B2SolverStage> stages;
        public int stageCount;
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

        /// <summary>
        /// Race flag claimed by whichever runner reaches b2SolverTask with workerIndex 0 first.
        /// The calling thread of b2World_Step also races for this slot so the orchestrator can
        /// always make progress, regardless of how the user's task system schedules tasks (out
        /// of order, fewer threads than workers, or synchronously inside enqueueTaskFcn). The
        /// loser of the race no-ops as workerIndex 0.
        /// </summary>
        public B2AtomicInt mainClaimed;

        /// <summary>
        /// padding to prevent false sharing
        /// </summary>
        public B2FixedArray64<byte> padding3;
    }
}
