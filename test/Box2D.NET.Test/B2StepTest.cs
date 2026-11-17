// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Threading;
using Box2D.NET.Shared;
using NUnit.Framework;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Shared.Determinism;

namespace Box2D.NET.Test;

// Port of test/test_step.c: the pull model task API (b2World_BeginStep / b2World_ContinueStep).
public class B2StepTest
{
    private const int EXPECTED_SLEEP_STEP = 299;
    private const uint EXPECTED_HASH = 0x465F3DF4;

    private const int MAX_BATCH_TASKS = B2Constants.B2_MAX_WORKERS + 2;

    private enum TaskOrder
    {
        e_forward,
        e_reverse,
    }

    // Runs every batch on the calling thread. Honoring the deferred flag holds the tree rebuild back until
    // right before the Continue that joins it, which catches a join list placed on the wrong batch.
    private class SerialHost
    {
        public TaskOrder order;
        public bool honorDeferred;
        public int batchCount;
        public int deferredBatch;
        public int joinBatch;
        public int deferredOnlyBatchCount;
        public int maxBatchSize;
        public int taskCount;
        public bool hasHeldTask;
        public B2Task heldTask;
    }

    private static void RunSerialBatch(SerialHost host, B2TaskBatch batch)
    {
        host.maxBatchSize = Math.Max(batch.count, host.maxBatchSize);

        int deferredCount = 0;
        for (int k = 0; k < batch.count; ++k)
        {
            int i = host.order == TaskOrder.e_forward ? k : batch.count - 1 - k;
            B2Task task = batch.tasks[i];
            if (task.deferred)
            {
                deferredCount += 1;
                host.deferredBatch = host.batchCount;
                if (host.honorDeferred)
                {
                    host.heldTask = task;
                    host.hasHeldTask = true;
                    continue;
                }
            }

            task.fcn(task.context);
        }

        if (deferredCount == batch.count)
        {
            host.deferredOnlyBatchCount += 1;
        }

        if (batch.joinCount > 0)
        {
            host.joinBatch = host.batchCount;
            if (host.hasHeldTask)
            {
                host.heldTask.fcn(host.heldTask.context);
                host.heldTask = default;
                host.hasHeldTask = false;
            }
        }

        host.batchCount += 1;
    }

    private static void SerialPhasedStep(SerialHost host, B2WorldId worldId, float timeStep, int subStepCount)
    {
        host.batchCount = 0;
        host.taskCount = 0;
        host.deferredBatch = -1;
        host.joinBatch = -1;
        host.heldTask = default;
        host.hasHeldTask = false;

        B2TaskBatch batch = b2World_BeginStep(worldId, timeStep, subStepCount);
        while (batch.count > 0)
        {
            Assert.That(batch.count, Is.LessThanOrEqualTo(MAX_BATCH_TASKS));
            Assert.That(batch.joinCount == 0 || batch.joinCount == 1, Is.True);
            for (int i = 0; i < batch.count; ++i)
            {
                B2TaskType type = batch.tasks[i].type;
                Assert.That(0 <= type && type < B2TaskType.b2_taskTypeCount, Is.True);
                Assert.That(batch.tasks[i].deferred == false || type == B2TaskType.b2_taskTreeRebuild, Is.True);
            }

            host.taskCount += batch.count;
            RunSerialBatch(host, batch);
            batch = b2World_ContinueStep(worldId);
        }

        Assert.That(host.hasHeldTask, Is.False);
        Assert.That(b2World_GetCounters(worldId).taskCount, Is.EqualTo(host.taskCount));
        Assert.That(b2World_GetCounters(worldId).enqueueCount, Is.EqualTo(0));
        Assert.That(host.deferredBatch == -1, Is.EqualTo(host.joinBatch == -1));
        Assert.That(host.deferredBatch < host.joinBatch || host.deferredBatch == -1, Is.True);
    }

    // A pool of threads draining a queue that only grows. Slots are never reused, so producers and
    // consumers need no reset handshake.
    private const int POOL_SLOT_CAPACITY = 1 << 18;

    private class Counter
    {
        public int value;
    }

    private struct PoolSlot
    {
        public b2TaskCallbackFcn fcn;
        public object context;
        public Counter counter;
    }

    private sealed class Pool : IDisposable
    {
        private readonly PoolSlot[] _slots = new PoolSlot[POOL_SLOT_CAPACITY];
        private readonly Thread[] _threads;
        private int _published;
        private int _claimed;
        private int _quit;

        public Pool(int threadCount)
        {
            _threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; ++i)
            {
                _threads[i] = new Thread(ThreadMain) { IsBackground = true, Name = "step_test" };
                _threads[i].Start();
            }
        }

        public bool RunOne()
        {
            int claimed = Volatile.Read(ref _claimed);
            int published = Volatile.Read(ref _published);
            if (claimed >= published)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _claimed, claimed + 1, claimed) != claimed)
            {
                return true;
            }

            PoolSlot slot = _slots[claimed];
            slot.fcn(slot.context);
            if (slot.counter != null)
            {
                Interlocked.Decrement(ref slot.counter.value);
            }

            return true;
        }

        private void ThreadMain()
        {
            while (Volatile.Read(ref _quit) == 0)
            {
                if (RunOne() == false)
                {
                    Thread.Yield();
                }
            }
        }

        // Single producer
        public void Push(b2TaskCallbackFcn fcn, object context, Counter counter)
        {
            int index = Volatile.Read(ref _published);
            Assert.That(index, Is.LessThan(POOL_SLOT_CAPACITY));
            _slots[index] = new PoolSlot { fcn = fcn, context = context, counter = counter };
            Volatile.Write(ref _published, index + 1);
        }

        public void HelpUntilZero(Counter counter)
        {
            while (Volatile.Read(ref counter.value) > 0)
            {
                if (RunOne() == false)
                {
                    Thread.SpinWait(1);
                }
            }
        }

        public void Dispose()
        {
            Volatile.Write(ref _quit, 1);
            foreach (Thread thread in _threads)
            {
                thread.Join();
            }
        }
    }

    // Pull host on the pool. The rebuild stays in flight across Continue calls until a batch joins it.
    private static void PooledPhasedStep(Pool pool, B2WorldId worldId, float timeStep, int subStepCount)
    {
        Counter batchCounter = new Counter();
        Counter treeCounter = new Counter();

        B2TaskBatch batch = b2World_BeginStep(worldId, timeStep, subStepCount);
        while (batch.count > 0)
        {
            int nonDeferred = 0;
            for (int i = 0; i < batch.count; ++i)
            {
                nonDeferred += batch.tasks[i].deferred ? 0 : 1;
            }

            Volatile.Write(ref batchCounter.value, nonDeferred);

            for (int i = 0; i < batch.count; ++i)
            {
                B2Task task = batch.tasks[i];
                if (task.deferred)
                {
                    Volatile.Write(ref treeCounter.value, 1);
                    pool.Push(task.fcn, task.context, treeCounter);
                }
                else
                {
                    pool.Push(task.fcn, task.context, batchCounter);
                }
            }

            pool.HelpUntilZero(batchCounter);

            if (batch.joinCount > 0)
            {
                pool.HelpUntilZero(treeCounter);
            }

            batch = b2World_ContinueStep(worldId);
        }

        Assert.That(Volatile.Read(ref treeCounter.value), Is.EqualTo(0));
    }

    private enum HostKind
    {
        e_syncHost,
        e_serialHost,
        e_pooledHost,
    }

    private struct HostConfig
    {
        public HostKind kind;
        public TaskOrder order;
        public bool honorDeferred;
        public int workerCount;
        public int threadCount;
    }

    private static void RunFallingHinges(HostConfig config)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = config.workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        using Pool pool = config.kind == HostKind.e_pooledHost ? new Pool(config.threadCount - 1) : null;

        SerialHost serialHost = new SerialHost { order = config.order, honorDeferred = config.honorDeferred };

        FallingHingeData data = CreateFallingHinges(worldId);

        float timeStep = 1.0f / 60.0f;
        int stepLimit = 1000;
        for (int i = 0; i < stepLimit; ++i)
        {
            int subStepCount = 4;
            if (config.kind == HostKind.e_serialHost)
            {
                SerialPhasedStep(serialHost, worldId, timeStep, subStepCount);
            }
            else if (config.kind == HostKind.e_pooledHost)
            {
                PooledPhasedStep(pool, worldId, timeStep, subStepCount);
            }
            else
            {
                b2World_Step(worldId, timeStep, subStepCount);
            }

            if (UpdateFallingHinges(worldId, ref data))
            {
                break;
            }
        }

        if (config.kind != HostKind.e_syncHost)
        {
            Assert.That(b2GetWorldFromId(worldId).scheduler, Is.Null);
        }

        Assert.That(serialHost.maxBatchSize, Is.LessThanOrEqualTo(config.workerCount + 2));

        b2DestroyWorld(worldId);

        string message = $"host={config.kind} order={config.order} deferred={config.honorDeferred} workers={config.workerCount} " +
                         $"threads={config.threadCount} sleepStep={data.sleepStep} hash=0x{data.hash:X8}";
        Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP), message);
        Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH), message);

        DestroyFallingHinges(ref data);
    }

    [Test]
    public void SerialPullTest()
    {
        int[] workerCounts = { 1, 4, 8 };
        foreach (int workerCount in workerCounts)
        {
            RunFallingHinges(new HostConfig { kind = HostKind.e_serialHost, order = TaskOrder.e_forward, workerCount = workerCount });
        }
    }

    // The first solver task to run must orchestrate whatever its slot, or a serial host strands a spinner
    [Test]
    public void ReverseOrderTest()
    {
        int[] workerCounts = { 4, 8 };
        foreach (int workerCount in workerCounts)
        {
            RunFallingHinges(new HostConfig { kind = HostKind.e_serialHost, order = TaskOrder.e_reverse, workerCount = workerCount });
        }
    }

    [Test]
    public void DeferredJoinTest()
    {
        int[] workerCounts = { 1, 4 };
        foreach (int workerCount in workerCounts)
        {
            HostConfig config = new HostConfig
            {
                kind = HostKind.e_serialHost, order = TaskOrder.e_forward, honorDeferred = true, workerCount = workerCount,
            };
            RunFallingHinges(config);

            config.order = TaskOrder.e_reverse;
            RunFallingHinges(config);
        }
    }

    [Test]
    public void PooledPullTest()
    {
        int[,] pairs = { { 1, 1 }, { 2, 2 }, { 4, 4 }, { 8, 8 }, { 8, 2 }, { 2, 8 } };
        for (int i = 0; i < pairs.GetLength(0); ++i)
        {
            RunFallingHinges(new HostConfig { kind = HostKind.e_pooledHost, workerCount = pairs[i, 0], threadCount = pairs[i, 1] });
        }
    }

    private static int enqueueOnlyCount;

    private static object EnqueueOnlyTask(b2TaskCallbackFcn task, object taskContext, object userContext)
    {
        enqueueOnlyCount += 1;
        Pool pool = (Pool)userContext;
        pool.Push(task, taskContext, null);
        return pool;
    }

    [Test]
    public void EnqueueOnlyTest()
    {
        int[] workerCounts = { 2, 4 };
        foreach (int workerCount in workerCounts)
        {
            using Pool pool = new Pool(3);

            B2WorldDef worldDef = b2DefaultWorldDef();
            worldDef.workerCount = workerCount;
            worldDef.enqueueTask = EnqueueOnlyTask;
            worldDef.finishTask = null;
            worldDef.userTaskContext = pool;
            B2WorldId worldId = b2CreateWorld(worldDef);

            FallingHingeData data = CreateFallingHinges(worldId);

            float timeStep = 1.0f / 60.0f;
            for (int step = 0; step < 1000; ++step)
            {
                enqueueOnlyCount = 0;
                b2World_Step(worldId, timeStep, 4);
                B2Counters counters = b2World_GetCounters(worldId);
                Assert.That(counters.enqueueCount, Is.EqualTo(enqueueOnlyCount));
                Assert.That(counters.enqueueCount, Is.LessThanOrEqualTo(counters.taskCount));

                if (UpdateFallingHinges(worldId, ref data))
                {
                    break;
                }
            }

            Assert.That(b2GetWorldFromId(worldId).scheduler, Is.Null);

            b2DestroyWorld(worldId);

            Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP));
            Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH));

            DestroyFallingHinges(ref data);
        }
    }

    [Test]
    public void AlternateTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 4;
        B2WorldId worldId = b2CreateWorld(worldDef);

        FallingHingeData data = CreateFallingHinges(worldId);
        SerialHost host = new SerialHost { order = TaskOrder.e_reverse, honorDeferred = true };

        float timeStep = 1.0f / 60.0f;
        for (int i = 0; i < 1000; ++i)
        {
            if ((i & 1) == 0)
            {
                b2World_Step(worldId, timeStep, 4);
            }
            else
            {
                SerialPhasedStep(host, worldId, timeStep, 4);
            }

            if (UpdateFallingHinges(worldId, ref data))
            {
                break;
            }
        }

        b2DestroyWorld(worldId);

        Assert.That(data.sleepStep, Is.EqualTo(EXPECTED_SLEEP_STEP));
        Assert.That(data.hash, Is.EqualTo(EXPECTED_HASH));

        DestroyFallingHinges(ref data);
    }

    [Test]
    public void EmptyWorldTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 4;
        B2WorldId worldId = b2CreateWorld(worldDef);

        SerialHost host = new SerialHost { order = TaskOrder.e_forward };
        SerialPhasedStep(host, worldId, 1.0f / 60.0f, 4);
        Assert.That(host.batchCount, Is.EqualTo(0));
        Assert.That(host.deferredBatch, Is.EqualTo(-1));

        b2World_Step(worldId, 1.0f / 60.0f, 4);
        SerialPhasedStep(host, worldId, 0.0f, 4);
        Assert.That(host.batchCount, Is.EqualTo(0));

        b2DestroyWorld(worldId);
    }

    private static B2BodyId CreateBall(B2WorldId worldId, B2Pos position)
    {
        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = position;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Circle circle = new B2Circle(new Vector2(0.0f, 0.0f), 0.5f);
        b2CreateCircleShape(bodyId, shapeDef, circle);
        return bodyId;
    }

    // The rebuild must join on the solve batch normally and on the collide batch when solve is skipped
    [Test]
    public void JoinPlacementTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 4;

        // A lone ball, no contacts. A zero step skips solve, so the collide batch is the rebuild alone and
        // nothing later can join it. It runs as a regular task.
        {
            B2WorldId worldId = b2CreateWorld(worldDef);
            B2BodyId bodyId = CreateBall(worldId, new B2Pos(0.0f, 4.0f));

            SerialHost host = new SerialHost { order = TaskOrder.e_forward, honorDeferred = true };
            SerialPhasedStep(host, worldId, 0.0f, 4);
            Assert.That(host.batchCount, Is.GreaterThan(0));
            Assert.That(host.deferredBatch, Is.EqualTo(-1));
            Assert.That(host.deferredOnlyBatchCount, Is.EqualTo(0));

            B2Pos p = b2Body_GetPosition(bodyId);
            Assert.That(p, Is.EqualTo(new B2Pos(0.0f, 4.0f)));

            b2DestroyWorld(worldId);
        }

        // A lone moving ball, no contacts. The collide batch is empty, so the rebuild rides with the solver
        // tasks as a regular task instead of running alone.
        {
            B2WorldId worldId = b2CreateWorld(worldDef);
            B2BodyId bodyId = CreateBall(worldId, new B2Pos(0.0f, 4.0f));
            b2Body_SetLinearVelocity(bodyId, new Vector2(10.0f, 0.0f));

            SerialHost host = new SerialHost { order = TaskOrder.e_forward, honorDeferred = true };
            for (int i = 0; i < 4; ++i)
            {
                SerialPhasedStep(host, worldId, 1.0f / 60.0f, 4);
                Assert.That(host.deferredBatch, Is.EqualTo(-1));
            }

            Assert.That(host.deferredOnlyBatchCount, Is.EqualTo(0));

            b2DestroyWorld(worldId);
        }

        // Touching balls on the ground with a zero step: the collide batch has contacts and the join
        {
            B2WorldId worldId = b2CreateWorld(worldDef);

            B2BodyDef groundDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(worldId, groundDef);
            B2ShapeDef groundShapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new Vector2(-20.0f, 0.0f), new Vector2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, groundShapeDef, segment);

            for (int i = 0; i < 8; ++i)
            {
                CreateBall(worldId, new B2Pos(-4.0f + 1.0f * i, 0.5f));
            }

            SerialHost host = new SerialHost { order = TaskOrder.e_forward, honorDeferred = true };
            SerialPhasedStep(host, worldId, 0.0f, 4);
            SerialPhasedStep(host, worldId, 0.0f, 4);

            // Moving step: the join lands on a later batch than the rebuild
            for (int i = 0; i < 4; ++i)
            {
                SerialPhasedStep(host, worldId, 1.0f / 60.0f, 4);
                if (host.deferredBatch >= 0)
                {
                    Assert.That(host.joinBatch, Is.GreaterThan(host.deferredBatch));
                }
            }

            b2DestroyWorld(worldId);
        }
    }

    // No contacts, a moving ball that keeps the rebuild staged, and a resting pair whose joint is destroyed.
    // The pair can only sleep after its island splits, so the solve batch carries the rebuild, the split and
    // the solver tasks together.
    [Test]
    public void SplitBatchTest()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = 4;
        worldDef.gravity = Vector2.Zero;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyId bodyA = CreateBall(worldId, new B2Pos(0.0f, 0.0f));
        B2BodyId bodyB = CreateBall(worldId, new B2Pos(2.0f, 0.0f));
        B2BodyId ballId = CreateBall(worldId, new B2Pos(0.0f, 20.0f));
        b2Body_SetLinearVelocity(ballId, new Vector2(10.0f, 0.0f));

        B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
        jointDef.@base.bodyIdA = bodyA;
        jointDef.@base.bodyIdB = bodyB;
        jointDef.@base.localFrameA.p = new Vector2(1.0f, 0.0f);
        jointDef.@base.localFrameB.p = new Vector2(-1.0f, 0.0f);
        B2JointId jointId = b2CreateRevoluteJoint(worldId, jointDef);

        SerialHost host = new SerialHost { order = TaskOrder.e_forward, honorDeferred = true };
        SerialPhasedStep(host, worldId, 1.0f / 60.0f, 4);

        b2DestroyJoint(jointId);

        int stepCount = 0;
        while (b2Body_IsAwake(bodyA) && stepCount < 120)
        {
            SerialPhasedStep(host, worldId, 1.0f / 60.0f, 4);
            stepCount += 1;
        }

        Assert.That(host.maxBatchSize, Is.EqualTo(worldDef.workerCount + 2));
        Assert.That(b2Body_IsAwake(bodyA), Is.False);
        Assert.That(b2Body_IsAwake(ballId), Is.True);

        b2DestroyWorld(worldId);
    }

    // A scene with bullets, a sensor, joints and sleeping islands, checked against the synchronous step
    // every step by state hash.
    private static B2WorldId CreateMixedScene(int workerCount)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.workerCount = workerCount;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);
        B2ShapeDef groundShapeDef = b2DefaultShapeDef();
        B2Segment segment = new B2Segment(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));
        b2CreateSegmentShape(groundId, groundShapeDef, segment);

        B2ShapeDef sensorDef = b2DefaultShapeDef();
        sensorDef.isSensor = true;
        sensorDef.enableSensorEvents = true;
        B2Polygon sensorBox = b2MakeOffsetBox(4.0f, 2.0f, new Vector2(10.0f, 2.0f), b2Rot_identity);
        b2CreatePolygonShape(groundId, sensorDef, sensorBox);

        B2Polygon box = b2MakeBox(0.5f, 0.5f);
        B2ShapeDef boxDef = b2DefaultShapeDef();
        boxDef.enableSensorEvents = true;
        boxDef.enableHitEvents = true;

        for (int row = 0; row < 10; ++row)
        {
            for (int col = 0; col < 10 - row; ++col)
            {
                B2BodyDef bodyDef = b2DefaultBodyDef();
                bodyDef.type = B2BodyType.b2_dynamicBody;
                bodyDef.position = new B2Pos(-5.0f + col * 1.05f + row * 0.525f, 0.5f + row * 1.0f);
                B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
                b2CreatePolygonShape(bodyId, boxDef, box);
            }
        }

        B2BodyId prevId = groundId;
        for (int i = 0; i < 6; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.position = new B2Pos(12.0f + i * 1.0f, 8.0f);
            B2BodyId linkId = b2CreateBody(worldId, bodyDef);
            B2Polygon link = b2MakeBox(0.5f, 0.125f);
            b2CreatePolygonShape(linkId, boxDef, link);

            B2RevoluteJointDef jointDef = b2DefaultRevoluteJointDef();
            jointDef.@base.bodyIdA = prevId;
            jointDef.@base.bodyIdB = linkId;
            jointDef.@base.localFrameA.p = prevId.index1 == groundId.index1 ? new Vector2(11.5f, 8.0f) : new Vector2(0.5f, 0.0f);
            jointDef.@base.localFrameB.p = new Vector2(-0.5f, 0.0f);
            b2CreateRevoluteJoint(worldId, jointDef);
            prevId = linkId;
        }

        for (int i = 0; i < 4; ++i)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;
            bodyDef.isBullet = true;
            bodyDef.position = new B2Pos(-30.0f, 1.0f + 2.0f * i);
            bodyDef.linearVelocity = new Vector2(200.0f, 0.0f);
            B2BodyId bulletId = b2CreateBody(worldId, bodyDef);
            B2ShapeDef bulletDef = b2DefaultShapeDef();
            bulletDef.enableSensorEvents = true;
            B2Circle circle = new B2Circle(new Vector2(0.0f, 0.0f), 0.1f);
            b2CreateCircleShape(bulletId, bulletDef, circle);
        }

        return worldId;
    }

    [Test]
    public void MixedSceneTest()
    {
        int[] workerCounts = { 1, 4, 8 };
        foreach (int workerCount in workerCounts)
        {
            B2WorldId syncId = CreateMixedScene(workerCount);
            B2WorldId serialId = CreateMixedScene(workerCount);
            B2WorldId pooledId = CreateMixedScene(workerCount);

            using Pool pool = new Pool(3);
            SerialHost host = new SerialHost { order = TaskOrder.e_reverse, honorDeferred = true };

            int sensorBeginTotal = 0;
            int hitTotal = 0;
            int maxBatchCount = 0;
            float timeStep = 1.0f / 60.0f;
            for (int i = 0; i < 400; ++i)
            {
                b2World_Step(syncId, timeStep, 4);
                SerialPhasedStep(host, serialId, timeStep, 4);
                PooledPhasedStep(pool, pooledId, timeStep, 4);

                ulong syncHash = b2World_GetStateHash(syncId);
                Assert.That(b2World_GetStateHash(serialId), Is.EqualTo(syncHash));
                Assert.That(b2World_GetStateHash(pooledId), Is.EqualTo(syncHash));

                B2SensorEvents syncSensors = b2World_GetSensorEvents(syncId);
                B2SensorEvents serialSensors = b2World_GetSensorEvents(serialId);
                Assert.That(serialSensors.beginCount, Is.EqualTo(syncSensors.beginCount));
                Assert.That(serialSensors.endCount, Is.EqualTo(syncSensors.endCount));

                B2ContactEvents syncContacts = b2World_GetContactEvents(syncId);
                B2ContactEvents serialContacts = b2World_GetContactEvents(serialId);
                Assert.That(serialContacts.beginCount, Is.EqualTo(syncContacts.beginCount));
                Assert.That(serialContacts.hitCount, Is.EqualTo(syncContacts.hitCount));

                B2BodyEvents syncBodies = b2World_GetBodyEvents(syncId);
                B2BodyEvents serialBodies = b2World_GetBodyEvents(serialId);
                Assert.That(serialBodies.moveCount, Is.EqualTo(syncBodies.moveCount));

                sensorBeginTotal += serialSensors.beginCount;
                hitTotal += serialContacts.hitCount;
                maxBatchCount = Math.Max(host.batchCount, maxBatchCount);
            }

            Assert.That(sensorBeginTotal, Is.GreaterThan(0));
            Assert.That(hitTotal, Is.GreaterThan(0));
            Assert.That(maxBatchCount, Is.EqualTo(7));
            Assert.That(b2World_GetAwakeBodyCount(serialId), Is.LessThan(55 + 6 + 4));

            Assert.That(b2World_GetAwakeBodyCount(serialId), Is.EqualTo(b2World_GetAwakeBodyCount(syncId)));
            Assert.That(b2World_GetCounters(serialId).taskCount, Is.GreaterThan(0));

            b2DestroyWorld(syncId);
            b2DestroyWorld(serialId);
            b2DestroyWorld(pooledId);
        }
    }
}
