// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// World definition used to create a simulation world.
    /// Must be initialized using b2DefaultWorldDef().
    /// @ingroup world
    /// </summary>
    public struct B2WorldDef
    {
        /// <summary>
        /// Gravity vector. Box2D has no up-vector defined.
        /// </summary>
        public Vector2 gravity;

        /// <summary>
        /// Restitution speed threshold, usually in m/s. Collisions above this
        /// speed have restitution applied (will bounce).
        /// </summary>
        public float restitutionThreshold;

        /// <summary>
        /// Number of iterations of the restitution solver. More iterations can lead to less box spinning.
        /// @see B2_MAX_RESTITUTION_ITERATIONS
        /// </summary>
        public int restitutionIterations;

        /// <summary>
        /// Enable full contact propagation in the restitution solver. Expensive.
        /// </summary>
        public bool enableRestitutionPropagation;

        /// <summary>
        /// Threshold speed for hit events. Usually meters per second.
        /// </summary>
        public float hitEventThreshold;

        /// <summary>
        /// Contact stiffness. Cycles per second. Increasing this increases the speed of overlap recovery, but can introduce jitter.
        /// </summary>
        public float contactHertz;

        /// <summary>
        /// Contact bounciness. Non-dimensional. You can speed up overlap recovery by decreasing this with
        /// the trade-off that overlap resolution becomes more energetic.
        /// </summary>
        public float contactDampingRatio;

        /// <summary>
        /// This parameter controls how fast overlap is resolved and usually has units of meters per second. This only
        /// puts a cap on the resolution speed. The resolution speed is increased by increasing the hertz and/or
        /// decreasing the damping ratio.
        /// </summary>
        public float contactSpeed;

        /// <summary>
        /// Maximum linear speed. Usually meters per second.
        /// </summary>
        public float maximumLinearSpeed;

        /// <summary>
        /// Optional mixing callback for friction. The default uses sqrt(frictionA * frictionB).
        /// </summary>
        public b2FrictionCallbackFcn frictionCallbackFcn;

        /// <summary>
        /// Optional mixing callback for restitution. The default uses max(restitutionA, restitutionB).
        /// </summary>
        public b2RestitutionCallbackFcn restitutionCallbackFcn;

        /// <summary>
        /// Can bodies go to sleep to improve performance
        /// </summary>
        public bool enableSleep;

        /// <summary>
        /// Enable continuous collision
        /// </summary>
        public bool enableContinuous;
        
        /// <summary>
        /// Number of workers for multithreading. Box2D performs best when using performance cores and
        /// accessing a single L3 cache (uniform memory). Efficiency cores and SMT provide
        /// little benefit and may even harm performance.
        /// This is clamped to the range [1, B2_MAX_WORKERS].
        /// Using a value above 1 will turn on multithreading. If task callbacks are provided
        /// then Box2D will use the user provided task system. Otherwise Box2D will create threads and use
        /// an internal scheduler.
        /// </summary>
        public int workerCount;

        /// <summary>
        /// Function to spawn tasks
        /// </summary>
        public b2EnqueueTaskCallbackFcn enqueueTask;

        /// <summary>
        /// Function to finish a task
        /// </summary>
        public b2FinishTaskCallbackFcn finishTask;

        /// <summary>
        /// User context that is provided to enqueueTask and finishTask
        /// </summary>
        public object userTaskContext;

        /// <summary>
        /// User data
        /// </summary>
        public B2UserData userData;

        /// <summary>
        /// Optional initial capacities
        /// </summary>
        public B2Capacity capacity;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
