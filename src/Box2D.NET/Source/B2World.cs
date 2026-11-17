// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using static Box2D.NET.B2Buffers;
using static Box2D.NET.B2Constants;

namespace Box2D.NET
{
    /// <summary>
    /// The world struct manages all physics entities, dynamic simulation,  and asynchronous queries.
    /// The world also contains efficient memory management facilities.
    /// </summary>
    public class B2World
    {
        public B2StackAllocator stack;

        /// <summary>
        /// Graph color contact solver for the SIMD width picked at world creation
        /// </summary>
        internal B2ContactSolverWide contactSolverWide;

        public B2BroadPhase broadPhase;
        public B2ConstraintGraph constraintGraph;

        /// <summary>
        /// The body id pool is used to allocate and recycle body ids. Body ids
        /// provide a stable identifier for users, but incur caches misses when used
        /// to access body data. Aligns with b2Body.
        /// </summary>
        public B2IdPool bodyIdPool;

        /// <summary>
        /// This is a sparse array that maps body ids to the body data
        /// stored in solver sets. As sims move within a set or across set.
        /// Indices come from id pool.
        /// </summary>
        public B2Array<B2Body> bodies;

        /// <summary>
        /// Provides free list for solver sets.
        /// </summary>
        public B2IdPool solverSetIdPool;

        /// <summary>
        /// Solvers sets allow sims to be stored in contiguous arrays. The first
        /// set is all static sims. The second set is active sims. The third set is disabled
        /// sims. The remaining sets are sleeping islands.
        /// </summary>
        public B2Array<B2SolverSet> solverSets;

        /// <summary>
        /// Used to create stable ids for joints
        /// </summary>
        public B2IdPool jointIdPool;

        /// <summary>
        /// This is a sparse array that maps joint ids to the joint data stored in the constraint graph
        /// or in the solver sets.
        /// </summary>
        public B2Array<B2Joint> joints;

        /// <summary>
        /// Used to create stable ids for contacts
        /// </summary>
        public B2IdPool contactIdPool;

        /// <summary>
        /// This is a sparse array that maps contact ids to the contact data stored in the constraint graph
        /// or in the solver sets.
        /// </summary>
        public B2Array<B2Contact> contacts;

        /// <summary>
        /// Used to create stable ids for islands
        /// </summary>
        public B2IdPool islandIdPool;

        /// <summary>
        /// Persistent islands
        /// </summary>
        public B2Array<B2Island> islands;

        public B2IdPool shapeIdPool;
        public B2IdPool chainIdPool;

        /// <summary>
        /// These are sparse arrays that point into the pools above
        /// </summary>
        public B2Array<B2Shape> shapes;
        public B2Array<B2ChainShape> chainShapes;

        /// <summary>
        /// Shape enlarged bounds (hot data split).
        /// </summary>
        public B2Array<B2AABB> fatAABBs;

        /// <summary>
        /// This is a dense array of sensor data.
        /// </summary>
        public B2Array<B2Sensor> sensors;

        /// <summary>
        /// Per thread storage
        /// </summary>
        public B2Array<B2TaskContext> taskContexts;
        public B2Array<B2SensorTaskContext> sensorTaskContexts;

        public B2Array<B2BodyMoveEvent> bodyMoveEvents;
        public B2Array<B2SensorBeginTouchEvent> sensorBeginEvents;
        public B2Array<B2ContactBeginTouchEvent> contactBeginEvents;

        /// <summary>
        /// End events are double buffered so that the user doesn't need to flush events
        /// </summary>
        public B2Array<B2SensorEndTouchEvent>[] sensorEndEvents = new B2Array<B2SensorEndTouchEvent>[2];
        public B2Array<B2ContactEndTouchEvent>[] contactEndEvents = new B2Array<B2ContactEndTouchEvent>[2];
        public int endEventArrayIndex;

        public B2Array<B2ContactHitEvent> contactHitEvents;
        public B2Array<B2JointEvent> jointEvents;

        // todo consider deferred waking and impulses to make it possible
        // to apply forces and impulses from multiple threads
        // impulses must be deferred because sleeping bodies have no velocity state
        // Problems:
        // - multiple forces applied to the same body from multiple threads
        // Deferred wake
        //b2BitSet bodyWakeSet;
        //b2ImpulseArray deferredImpulses;

        /// <summary>
        /// Used to track debug draw
        /// </summary>
        public B2BitSet debugBodySet;
        public B2BitSet debugJointSet;
        public B2BitSet debugContactSet;
        public B2BitSet debugIslandSet;

        /// <summary>
        /// Id that is incremented every time step
        /// </summary>
        public ulong stepIndex;

        /// <summary>
        /// Identify islands for splitting as follows:
        /// - I want to split islands so smaller islands can sleep
        /// - when a body comes to rest and its sleep timer trips, I can look at the island and flag it for splitting
        ///   if it has removed constraints
        /// - islands that have removed constraints must be put split first because I don't want to wake bodies incorrectly
        /// - otherwise I can use the awake islands that have bodies wanting to sleep as the splitting candidates
        /// - if no bodies want to sleep then there is no reason to perform island splitting
        /// </summary>
        public int splitIslandId;

        public Vector2 gravity;
        public float hitEventThreshold;
        public float restitutionThreshold;
        public int restitutionIterations;
        public bool enableRestitutionPropagation;
        public float maxLinearSpeed;
        public float contactSpeed;
        public float contactHertz;
        public float contactDampingRatio;
        public float contactRecycleDistance;

        public b2FrictionCallbackFcn frictionCallbackFcn;
        public b2RestitutionCallbackFcn restitutionCallbackFcn;

        public ushort generation;

        public B2Profile profile;

        public B2Capacity maxCapacity;

        public b2PreSolveCallbackFcn preSolveCallbackFcn;
        public b2PreContinuousCallbackFcn preContinuousCallbackFcn;
        public object preSolveContext;

        public b2CustomFilterCallbackFcn customFilterCallbackFcn;
        public object customFilterContext;

        public int workerCount;
        public b2EnqueueTaskCallbackFcn enqueueTaskFcn;
        public b2FinishTaskCallbackFcn finishTaskFcn;
        public object userTaskContext;
        
        public B2Scheduler scheduler;
        public bool useScheduler;

        public B2UserData userData;

        /// <summary>
        /// Remember type step used for reporting forces and torques
        /// inverse sub-step
        /// </summary>
        public float inv_h;

        /// <summary>
        /// inverse full-step
        /// </summary>
        public float inv_dt;

        public ushort worldId;

        public bool enableSleep;
        public bool locked;
        public bool enableWarmStarting;
        public bool enableContinuous;
        public bool inUse;

        /// <summary>
        /// Context for a time step. Owned by the world and reset each time step.
        /// </summary>
        internal readonly B2StepContext stepContext = new B2StepContext();

        // Task handles for one batch in b2World_Step.
        internal readonly object[] stepTaskHandles = new object[B2StepContext.B2_STEP_TASK_CAPACITY];

        internal readonly System.ArraySegment<B2SyncBlock>[] graphColorBlocks = new System.ArraySegment<B2SyncBlock>[B2_GRAPH_COLOR_COUNT];
        internal readonly B2CrossContext crossContext = new B2CrossContext();

        public void Clear()
        {
            stack = null;
            contactSolverWide = null;
            broadPhase = null;

            bodyIdPool = null;

            bodies = new B2Array<B2Body>();

            solverSetIdPool = null;

            solverSets = new B2Array<B2SolverSet>();

            jointIdPool = null;

            joints = new B2Array<B2Joint>();

            contactIdPool = null;

            contacts = new B2Array<B2Contact>();

            islandIdPool = null;

            islands = new B2Array<B2Island>();

            shapeIdPool = null;
            chainIdPool = null;

            shapes = new B2Array<B2Shape>();
            chainShapes = new B2Array<B2ChainShape>();
            fatAABBs = new B2Array<B2AABB>();

            sensors = new B2Array<B2Sensor>();

            taskContexts = new B2Array<B2TaskContext>();
            sensorTaskContexts = new B2Array<B2SensorTaskContext>();

            bodyMoveEvents = new B2Array<B2BodyMoveEvent>();
            sensorBeginEvents = new B2Array<B2SensorBeginTouchEvent>();
            contactBeginEvents = new B2Array<B2ContactBeginTouchEvent>();

            sensorEndEvents[0] = new B2Array<B2SensorEndTouchEvent>();
            sensorEndEvents[1] = new B2Array<B2SensorEndTouchEvent>();
            contactEndEvents[0] = new B2Array<B2ContactEndTouchEvent>();
            contactEndEvents[1] = new B2Array<B2ContactEndTouchEvent>();
            endEventArrayIndex = 0;

            contactHitEvents = new B2Array<B2ContactHitEvent>();
            jointEvents = new B2Array<B2JointEvent>();

            // debugBodySet = null;
            // debugJointSet = null;
            // debugContactSet = null;

            stepIndex = 0;

            splitIslandId = B2_NULL_INDEX;

            gravity = new Vector2();
            hitEventThreshold = 0.0f;
            restitutionThreshold = 0.0f;
            restitutionIterations = 0;
            enableRestitutionPropagation = false;
            maxLinearSpeed = 0.0f;
            contactSpeed = 0.0f;
            contactHertz = 0.0f;
            contactDampingRatio = 0.0f;
            contactRecycleDistance = 0.0f; 

            frictionCallbackFcn = null;
            restitutionCallbackFcn = null;

            generation = 0;

            profile = new B2Profile();

            maxCapacity = new B2Capacity();

            preSolveCallbackFcn = null;
            preContinuousCallbackFcn = null;
            preSolveContext = null;

            customFilterCallbackFcn = null;
            customFilterContext = null;

            workerCount = 0;
            enqueueTaskFcn = null;
            finishTaskFcn = null;
            userTaskContext = null;
            scheduler = null;
            useScheduler = false;

            userData = B2UserData.Empty;

            inv_h = 0.0f;
            inv_dt = 0.0f;

            worldId = 0;

            enableSleep = false;
            locked = false;
            enableWarmStarting = false;
            enableContinuous = false;
            inUse = false;

            stepContext.Clear();
            System.Array.Clear(graphColorBlocks, 0, graphColorBlocks.Length);
            crossContext.world = null;
            crossContext.seeds = null;
            crossContext.staticSeedCount = 0;
            crossContext.crossMoveCount = 0;
            crossContext.dynamicMoveCount = 0;
        }
    }
}
