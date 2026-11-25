// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using NUnit.Framework;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MotionLockJoints;

namespace Box2D.NET.Test;

public class B2JointTest
{
    private struct MotionLockScene
    {
        public B2WorldId worldId;
        public B2BodyId groundId;
        public B2BodyId bodyId;
        public B2JointId jointId;
    }

    private static readonly Vector2 lockOrigin = new Vector2(1.0f, 4.0f);

    private static void EnsureSmall(float value, float tolerance)
    {
        Assert.That(MathF.Abs(value), Is.LessThanOrEqualTo(tolerance));
    }

    // A box under gravity locked to the ground. An anchor at the center of mass takes the decoupled path.
    private static MotionLockScene CreateMotionLockScene(B2MotionLockJointDef def, Vector2 localAnchor)
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new Vector2(0.0f, -10.0f);

        MotionLockScene s;
        s.worldId = b2CreateWorld(worldDef);

        B2BodyDef groundDef = b2DefaultBodyDef();
        s.groundId = b2CreateBody(s.worldId, groundDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = b2ToPos(lockOrigin);
        bodyDef.enableSleep = false;
        s.bodyId = b2CreateBody(s.worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(0.5f, 0.25f);
        b2CreatePolygonShape(s.bodyId, shapeDef, box);

        def.@base.bodyIdA = s.groundId;
        def.@base.bodyIdB = s.bodyId;
        def.@base.localFrameA.p = b2Add(lockOrigin, localAnchor);
        def.@base.localFrameB.p = localAnchor;
        s.jointId = b2CreateMotionLockJoint(s.worldId, def);
        return s;
    }

    private static void StepScene(B2WorldId worldId, int count)
    {
        for (int i = 0; i < count; ++i)
        {
            b2World_Step(worldId, 1.0f / 60.0f, 4);
        }
    }

    [Test]
    public void MotionLockJointApi()
    {
        B2MotionLockJointDef def = b2DefaultMotionLockJointDef();
        def.lockLinearY = true;
        MotionLockScene s = CreateMotionLockScene(def, Vector2Zero);

        Assert.That(b2Joint_GetType(s.jointId), Is.EqualTo(B2JointType.b2_motionLockJoint));
        Assert.That(b2MotionLockJoint_IsLinearLockEnabled(s.jointId, 0), Is.False);
        Assert.That(b2MotionLockJoint_IsLinearLockEnabled(s.jointId, 1), Is.True);

        b2MotionLockJoint_EnableLinearLock(s.jointId, 0, true);
        Assert.That(b2MotionLockJoint_IsLinearLockEnabled(s.jointId, 0), Is.True);

        b2MotionLockJoint_EnableLinearLock(s.jointId, 1, false);
        Assert.That(b2MotionLockJoint_IsLinearLockEnabled(s.jointId, 1), Is.False);

        StepScene(s.worldId, 10);

        b2DestroyJoint(s.jointId);
        StepScene(s.worldId, 1);

        b2DestroyWorld(s.worldId);
    }

    // Every combination of linear locks holds its axes against gravity and a push while the free axes move.
    [Test]
    public void MotionLockJointLinear()
    {
        Vector2 push = new Vector2(2.0f, 3.0f);

        for (int mask = 1; mask < 4; ++mask)
        {
            B2MotionLockJointDef def = b2DefaultMotionLockJointDef();
            def.lockLinearX = (mask & 1) != 0;
            def.lockLinearY = (mask & 2) != 0;

            MotionLockScene s = CreateMotionLockScene(def, Vector2Zero);
            b2Body_SetLinearVelocity(s.bodyId, push);
            StepScene(s.worldId, 60);

            Vector2 delta = b2SubPos(b2Body_GetPosition(s.bodyId), b2ToPos(lockOrigin));

            Assert.That(def.lockLinearX ? b2AbsFloat(delta.X) < 0.01f : b2AbsFloat(delta.X) > 1.0f, Is.True);
            Assert.That(def.lockLinearY ? b2AbsFloat(delta.Y) < 0.01f : b2AbsFloat(delta.Y) > 1.0f, Is.True);
            Assert.That(b2Joint_GetLinearSeparation(s.jointId), Is.LessThan(0.01f));

            b2DestroyWorld(s.worldId);
        }
    }

    // An anchor off the center of mass couples the rows. The point lock becomes a pendulum pivot.
    [Test]
    public void MotionLockJointCoupled()
    {
        B2MotionLockJointDef def = b2DefaultMotionLockJointDef();
        def.lockLinearX = true;
        def.lockLinearY = true;

        MotionLockScene s = CreateMotionLockScene(def, new Vector2(0.5f, 0.25f));
        StepScene(s.worldId, 120);

        Assert.That(b2Joint_GetLinearSeparation(s.jointId), Is.LessThan(0.01f));
        Assert.That(b2IsValidVec2(b2Body_GetLinearVelocity(s.bodyId)), Is.True);
        Assert.That(b2AbsFloat(b2Body_GetAngularVelocity(s.bodyId)), Is.GreaterThan(0.1f));

        b2MotionLockJoint_EnableLinearLock(s.jointId, 1, false);
        StepScene(s.worldId, 60);
        Assert.That(b2Joint_GetLinearSeparation(s.jointId), Is.LessThan(0.01f));

        b2DestroyWorld(s.worldId);
    }

    // A heavy circle resting on a light platform held by a point lock and fixed rotation.
    // The lock rows iterate with the contact, so the platform supports the circle like a static body.
    [Test]
    public void MotionLockJointPlatform()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = new Vector2(0.0f, -20.0f);
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef groundDef = b2DefaultBodyDef();
        B2BodyId groundId = b2CreateBody(worldId, groundDef);

        B2BodyDef platformDef = b2DefaultBodyDef();
        platformDef.type = B2BodyType.b2_dynamicBody;
        platformDef.enableSleep = false;
        platformDef.fixedRotation = true;
        B2BodyId platformId = b2CreateBody(worldId, platformDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(2.0f, 0.05f);
        b2CreatePolygonShape(platformId, shapeDef, box);

        B2MassData massData = b2Body_GetMassData(platformId);
        massData.rotationalInertia = massData.rotationalInertia / massData.mass;
        massData.mass = 1.0f;
        b2Body_SetMassData(platformId, massData);

        B2MotionLockJointDef lockDef = b2DefaultMotionLockJointDef();
        lockDef.@base.bodyIdA = groundId;
        lockDef.@base.bodyIdB = platformId;
        lockDef.lockLinearX = true;
        lockDef.lockLinearY = true;
        b2CreateMotionLockJoint(worldId, lockDef);

        B2BodyDef circleDef = b2DefaultBodyDef();
        circleDef.type = B2BodyType.b2_dynamicBody;
        circleDef.position = new B2Pos(0.0f, 0.55f);
        circleDef.enableSleep = false;
        B2BodyId circleId = b2CreateBody(worldId, circleDef);

        B2Circle circle = new B2Circle(Vector2Zero, 0.5f);
        b2CreateCircleShape(circleId, shapeDef, circle);

        massData = b2Body_GetMassData(circleId);
        massData.rotationalInertia = 10.0f * massData.rotationalInertia / massData.mass;
        massData.mass = 10.0f;
        b2Body_SetMassData(circleId, massData);

        for (int i = 0; i < 200; ++i)
        {
            b2World_Step(worldId, 1.0f / 50.0f, 4);
        }

        float gap = b2Body_GetPosition(circleId).Y - 0.55f;
        Assert.That(b2AbsFloat(gap), Is.LessThan(0.02f));
        Assert.That(b2Body_GetRotation(platformId).s, Is.EqualTo(0.0f));

        b2DestroyWorld(worldId);
    }

    // A revolute pivot off the center of mass plus a point lock at the center is redundant. The two
    // constraints share the load through iteration instead of forming one singular block.
    [Test]
    public void MotionLockJointRedundant()
    {
        B2MotionLockJointDef def = b2DefaultMotionLockJointDef();
        def.lockLinearX = true;
        def.lockLinearY = true;

        MotionLockScene s = CreateMotionLockScene(def, Vector2Zero);

        B2RevoluteJointDef revoluteDef = b2DefaultRevoluteJointDef();
        revoluteDef.@base.bodyIdA = s.groundId;
        revoluteDef.@base.bodyIdB = s.bodyId;
        revoluteDef.@base.localFrameA.p = new Vector2(lockOrigin.X - 0.5f, lockOrigin.Y);
        revoluteDef.@base.localFrameB.p = new Vector2(-0.5f, 0.0f);
        b2CreateRevoluteJoint(s.worldId, revoluteDef);

        StepScene(s.worldId, 120);

        Assert.That(b2Length(b2Body_GetLinearVelocity(s.bodyId)), Is.LessThan(0.1f));
        Assert.That(b2AbsFloat(b2Body_GetAngularVelocity(s.bodyId)), Is.LessThan(0.1f));
        Assert.That(b2Joint_GetLinearSeparation(s.jointId), Is.LessThan(0.01f));

        b2DestroyWorld(s.worldId);
    }

    // A box held still under gravity. The force at the anchor carries the weight.
    [Test]
    public void MotionLockJointForce()
    {
        B2Rot frameRotation = b2MakeRot(0.5f);

        for (int i = 0; i < 2; ++i)
        {
            B2MotionLockJointDef def = b2DefaultMotionLockJointDef();
            def.lockLinearX = true;
            def.lockLinearY = true;
            def.@base.localFrameA.q = frameRotation;
            def.@base.localFrameB.q = frameRotation;

            Vector2 anchor = i == 0 ? Vector2Zero : new Vector2(0.0f, 0.5f);
            MotionLockScene s = CreateMotionLockScene(def, anchor);
            StepScene(s.worldId, 120);

            float weight = 10.0f * b2Body_GetMass(s.bodyId);
            Vector2 force = b2Joint_GetConstraintForce(s.jointId);
            EnsureSmall(force.X, 0.02f * weight);
            EnsureSmall(force.Y - weight, 0.02f * weight);
            Assert.That(b2Joint_GetConstraintTorque(s.jointId), Is.EqualTo(0.0f));

            b2DestroyWorld(s.worldId);
        }
    }

    // Fixed rotation zeroes only the inverse inertia. The caller's inertia survives so unlocking restores it
    // without recomputing mass from the shapes.
    [Test]
    public void FixedRotationMassData()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = true;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);
        Assert.That(b2Body_IsFixedRotation(bodyId), Is.True);

        B2MassData massData = new B2MassData(5.0f, new Vector2(0.0f, 0.0f), 2.0f);
        b2Body_SetMassData(bodyId, massData);

        EnsureSmall(b2Body_GetMass(bodyId) - 5.0f, 1e-6f);
        EnsureSmall(b2Body_GetRotationalInertia(bodyId) - 2.0f, 1e-6f);
        EnsureSmall(b2Body_GetMassData(bodyId).rotationalInertia - 2.0f, 1e-6f);

        b2Body_ApplyAngularImpulse(bodyId, 1.0f, true);
        Assert.That(b2Body_GetAngularVelocity(bodyId), Is.EqualTo(0.0f));

        b2Body_SetFixedRotation(bodyId, false);
        Assert.That(b2Body_IsFixedRotation(bodyId), Is.False);
        EnsureSmall(b2Body_GetRotationalInertia(bodyId) - 2.0f, 1e-6f);

        b2Body_ApplyAngularImpulse(bodyId, 1.0f, true);
        EnsureSmall(b2Body_GetAngularVelocity(bodyId) - 0.5f, 1e-6f);

        b2Body_SetFixedRotation(bodyId, true);
        Assert.That(b2Body_IsFixedRotation(bodyId), Is.True);
        Assert.That(b2Body_GetAngularVelocity(bodyId), Is.EqualTo(0.0f));

        b2DestroyWorld(worldId);
    }

    // Zero inverse inertia only stops impulses from changing the spin, so every path that writes angular
    // velocity directly has to drop it or the body spins forever.
    [Test]
    public void FixedRotationRejectsSpin()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = Vector2Zero;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeSquare(0.5f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = true;
        bodyDef.angularVelocity = 5.0f;
        B2BodyId dynamicId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(dynamicId, shapeDef, box);

        Assert.That(b2Body_GetAngularVelocity(dynamicId), Is.EqualTo(0.0f));

        b2Body_SetAngularVelocity(dynamicId, 3.0f);
        Assert.That(b2Body_GetAngularVelocity(dynamicId), Is.EqualTo(0.0f));

        bodyDef.type = B2BodyType.b2_kinematicBody;
        bodyDef.angularVelocity = 0.0f;
        bodyDef.position = new B2Pos(4.0f, 0.0f);
        B2BodyId kinematicId = b2CreateBody(worldId, bodyDef);
        b2CreatePolygonShape(kinematicId, shapeDef, box);

        B2WorldTransform target = new B2WorldTransform(new B2Pos(4.0f, 1.0f), b2MakeRot(1.0f));
        b2Body_SetTargetTransform(kinematicId, target, 1.0f / 60.0f, true);
        Assert.That(b2Body_GetAngularVelocity(kinematicId), Is.EqualTo(0.0f));
        Assert.That(b2Length(b2Body_GetLinearVelocity(kinematicId)), Is.GreaterThan(0.0f));

        b2Body_SetType(kinematicId, B2BodyType.b2_dynamicBody);
        Assert.That(b2Body_GetAngularVelocity(kinematicId), Is.EqualTo(0.0f));

        StepScene(worldId, 10);

        Assert.That(b2Body_GetRotation(dynamicId).s, Is.EqualTo(0.0f));
        Assert.That(b2Body_GetRotation(kinematicId).s, Is.EqualTo(0.0f));

        b2DestroyWorld(worldId);
    }

    // The body cannot turn toward the target rotation, so the target center must use the current one
    [Test]
    public void FixedRotationTargetTransform()
    {
        B2WorldDef worldDef = b2DefaultWorldDef();
        worldDef.gravity = Vector2Zero;
        B2WorldId worldId = b2CreateWorld(worldDef);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.fixedRotation = true;
        B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeOffsetBox(0.5f, 0.5f, new Vector2(1.0f, 0.0f), b2Rot_identity);
        b2CreatePolygonShape(bodyId, shapeDef, box);

        B2WorldTransform target = new B2WorldTransform(new B2Pos(2.0f, 3.0f), b2MakeRot(0.5f * B2_PI));
        b2Body_SetTargetTransform(bodyId, target, 1.0f / 60.0f, true);
        StepScene(worldId, 1);

        B2Pos p = b2Body_GetPosition(bodyId);
        EnsureSmall(p.X - 2.0f, 1.0e-4f);
        EnsureSmall(p.Y - 3.0f, 1.0e-4f);
        Assert.That(b2Body_GetRotation(bodyId).s, Is.EqualTo(0.0f));

        b2DestroyWorld(worldId);
    }
}
