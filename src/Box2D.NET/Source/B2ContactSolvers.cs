// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Cores;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Solvers;
using static Box2D.NET.B2BitSets;

namespace Box2D.NET
{
    public static class B2ContactSolvers
    {
        /// <summary>
        /// contact separation for sub-stepping
        /// s = s0 + dot(cB + rB - cA - rA, normal)
        /// normal is held constant
        /// body positions c can translation and anchors r can rotate
        /// s(t) = s0 + dot(cB(t) + rB(t) - cA(t) - rA(t), normal)
        /// s(t) = s0 + dot(cB0 + dpB + rot(dqB, rB0) - cA0 - dpA - rot(dqA, rA0), normal)
        /// s(t) = s0 + dot(cB0 - cA0, normal) + dot(dpB - dpA + rot(dqB, rB0) - rot(dqA, rA0), normal)
        /// s_base = s0 + dot(cB0 - cA0, normal)
        /// </summary>
        internal static void b2PrepareContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_overflow_contact, "Prepare Overflow Contact", B2HexColor.b2_colorYellow, true);

            B2World world = context.world;
            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;

            int contactCount = color.contactSims.count;
            B2ContactSim[] contacts = color.contactSims.data;
            B2BodyState[] states = context.states;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            // Stiffer for static contacts to avoid bodies getting pushed through the ground
            B2Softness contactSoftness = context.contactSoftness;
            B2Softness staticSoftness = context.staticSoftness;

            float warmStartScale = world.enableWarmStarting ? 1.0f : 0.0f;
            bool anyRestitution = false;

            for (int i = 0; i < contactCount; ++i)
            {
                B2ContactSim contactSim = contacts[i];

                ref B2Manifold manifold = ref contactSim.manifold;
                int pointCount = manifold.pointCount;

                B2_ASSERT(0 < pointCount && pointCount <= 2);

                int indexA = b2DecodeAwakeIndex(contactSim.encodedBodySimA);
                int indexB = b2DecodeAwakeIndex(contactSim.encodedBodySimB);

#if DEBUG
                B2Body bodyA = bodies[contactSim.bodyIdA];
                B2_ASSERT(contactSim.encodedBodySimA == b2EncodeBodySimIndex(bodyA));

                B2Body bodyB = bodies[contactSim.bodyIdB];
                B2_ASSERT(contactSim.encodedBodySimB == b2EncodeBodySimIndex(bodyB));
#endif

                ref B2ContactConstraint constraint = ref constraints[i];

                // 0 is null
                constraint.indexA = indexA + 1;
                constraint.indexB = indexB + 1;
                constraint.normal = manifold.normal;
                constraint.friction = contactSim.friction;
                constraint.restitution = contactSim.restitution;
                constraint.rollingResistance = contactSim.rollingResistance;
                constraint.rollingImpulse = warmStartScale * manifold.rollingImpulse;
                constraint.tangentSpeed = contactSim.tangentSpeed;
                constraint.pointCount = pointCount;

                bool haveRestitution = contactSim.restitution > 0.0f;
                bool hitEvents = (contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0;
                bool sampleVelocity = haveRestitution || hitEvents;
                anyRestitution = anyRestitution || haveRestitution;

                Vector2 vA = Vector2Zero;
                float wA = 0.0f;
                Vector2 vB = Vector2Zero;
                float wB = 0.0f;

                if (sampleVelocity)
                {
                    if (indexA != B2_NULL_INDEX)
                    {
                        vA = states[indexA].linearVelocity;
                        wA = states[indexA].angularVelocity;
                    }

                    if (indexB != B2_NULL_INDEX)
                    {
                        vB = states[indexB].linearVelocity;
                        wB = states[indexB].angularVelocity;
                    }
                }

                float mA = contactSim.invMassA;
                float iA = contactSim.invIA;
                float mB = contactSim.invMassB;
                float iB = contactSim.invIB;

                if (indexA == B2_NULL_INDEX || indexB == B2_NULL_INDEX)
                {
                    constraint.softness = staticSoftness;
                }
                else
                {
                    constraint.softness = contactSoftness;
                }

                // copy mass into constraint to avoid cache misses during sub-stepping
                constraint.invMassA = mA;
                constraint.invIA = iA;
                constraint.invMassB = mB;
                constraint.invIB = iB;

                {
                    float k = iA + iB;
                    constraint.rollingMass = k > 0.0f ? 1.0f / k : 0.0f;
                }

                Vector2 normal = constraint.normal;
                Vector2 tangent = b2RightPerp(constraint.normal);

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ManifoldPoint mp = ref manifold.points[j];
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    cp.normalImpulse = warmStartScale * mp.normalImpulse;
                    cp.tangentImpulse = warmStartScale * mp.tangentImpulse;
                    cp.totalNormalImpulse = 0.0f;
                    cp.restitutionImpulse = 0.0f;

                    Vector2 rA = mp.anchorA;
                    Vector2 rB = mp.anchorB;

                    cp.anchorA = rA;
                    cp.anchorB = rB;
                    cp.baseSeparation = mp.separation - b2Dot(b2Sub(rB, rA), normal);

                    float rnA = b2Cross(rA, normal);
                    float rnB = b2Cross(rB, normal);
                    float kNormal = mA + mB + iA * rnA * rnA + iB * rnB * rnB;
                    cp.normalMass = kNormal > 0.0f ? 1.0f / kNormal : 0.0f;

                    float rtA = b2Cross(rA, tangent);
                    float rtB = b2Cross(rB, tangent);
                    float kTangent = mA + mB + iA * rtA * rtA + iB * rtB * rtB;
                    cp.tangentMass = kTangent > 0.0f ? 1.0f / kTangent : 0.0f;

                    // Only compute the normal velocity terms if needed.
                    if (sampleVelocity)
                    {
                        Vector2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                        Vector2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                        float vn = b2Dot(normal, b2Sub(vrB, vrA));

                        cp.relativeVelocity = vn;
                        mp.normalVelocity = vn;
                    }
                    else
                    {
                        cp.relativeVelocity = 0.0f;
                        mp.normalVelocity = 0.0f;
                    }
                }
            }

            if (anyRestitution)
            {
                // Check if set first? Store per task context and OR?
                b2AtomicStoreInt(ref context.anyRestitution, 1);
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_overflow_contact);
        }

        internal static void b2WarmStartContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.warmstart_overflow_contact, "WarmStart Overflow Contact", B2HexColor.b2_colorDarkOrange, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            // This is a dummy state to represent a static body because static bodies don't have a solver body.
            B2BodyState dummyState = b2_identityBodyState;

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                ref B2BodyState stateA = ref indexA == B2_NULL_INDEX ? ref dummyState : ref states[indexA];
                ref B2BodyState stateB = ref indexB == B2_NULL_INDEX ? ref dummyState : ref states[indexB];

                Vector2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                Vector2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;

                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                // Stiffer for static contacts to avoid bodies getting pushed through the ground
                Vector2 normal = constraint.normal;
                Vector2 tangent = b2RightPerp(constraint.normal);
                int pointCount = constraint.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // fixed anchors
                    Vector2 rA = cp.anchorA;
                    Vector2 rB = cp.anchorB;

                    Vector2 P = b2Add(b2MulSV(cp.normalImpulse, normal), b2MulSV(cp.tangentImpulse, tangent));

                    cp.totalNormalImpulse += cp.normalImpulse;

                    wA -= iA * b2Cross(rA, P);
                    vA = b2MulAdd(vA, -mA, P);
                    wB += iB * b2Cross(rB, P);
                    vB = b2MulAdd(vB, mB, P);
                }

                wA -= iA * constraint.rollingImpulse;
                wB += iB * constraint.rollingImpulse;

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.warmstart_overflow_contact);
        }
        internal static void b2PushContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.push_contact, "Push Overflow Contact", B2HexColor.b2_colorAliceBlue, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float inv_h = context.inv_h;
            float contactSpeed = context.world.contactSpeed;

            // This is a dummy body to represent a static body since static bodies don't have a solver body.
            B2BodyState dummyState = b2_identityBodyState;

            Span<float> rnA = stackalloc float[2];
            Span<float> rnB = stackalloc float[2];
            Span<float> impulses = stackalloc float[2];

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                ref B2BodyState stateA = ref indexA == B2_NULL_INDEX ? ref dummyState : ref states[indexA];
                Vector2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Rot dqA = stateA.deltaRotation;

                ref B2BodyState stateB = ref indexB == B2_NULL_INDEX ? ref dummyState : ref states[indexB];
                Vector2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;
                B2Rot dqB = stateB.deltaRotation;

                Vector2 dp = b2Sub(stateB.deltaPosition, stateA.deltaPosition);

                Vector2 normal = constraint.normal;
                B2Softness softness = constraint.softness;
                int pointCount = constraint.pointCount;

                float vnc = b2Dot(b2Sub(vB, vA), normal);

                impulses[0] = 0.0f;
                impulses[1] = 0.0f;
                float normalImpulse = 0.0f;
                float angularA = 0.0f;
                float angularB = 0.0f;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // fixed anchor points
                    Vector2 rA = cp.anchorA;
                    Vector2 rB = cp.anchorB;

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    Vector2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                    float s = cp.baseSeparation + b2Dot(ds, normal);

                    float velocityBias;
                    float massScale;
                    float impulseScale;
                    if (s > 0.0f)
                    {
                        // speculative bias is positive
                        velocityBias = s * inv_h;
                        massScale = 1.0f;
                        impulseScale = 0.0f;
                    }
                    else
                    {
                        // overlap bias is negative
                        velocityBias = b2MaxFloat(softness.massScale * softness.biasRate * s, -contactSpeed);
                        massScale = softness.massScale;
                        impulseScale = softness.impulseScale;
                    }

                    rnA[j] = b2Cross(rA, normal);
                    rnB[j] = b2Cross(rB, normal);

                    // Relative normal velocity at contact.
                    float vn = vnc + wB * rnB[j] - wA * rnA[j];
                    for (int k = 0; k < j; ++k)
                    {
                        // Point 2 only depends on the impulse from point 1.
                        vn += (mA + mB + iA * rnA[k] * rnA[j] + iB * rnB[k] * rnB[j]) * impulses[k];
                    }

                    float impulse = b2MaxFloat(-cp.normalMass * (massScale * vn + velocityBias) - impulseScale * cp.normalImpulse,
                        -cp.normalImpulse);
                    cp.normalImpulse += impulse;
                    cp.totalNormalImpulse += impulse;

                    impulses[j] = impulse;

                    // These accumulators are not used until the loop is done.
                    normalImpulse += impulse;
                    angularA += impulse * rnA[j];
                    angularB += impulse * rnB[j];
                }

                Vector2 P = b2MulSV(normalImpulse, normal);
                vA = b2MulSub(vA, mA, P);
                wA -= iA * angularA;
                vB = b2MulAdd(vB, mB, P);
                wB += iB * angularB;

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.push_contact);
        }

        /// <summary>
        /// Solve contacts: normal, friction and rolling resistance.
        /// </summary>
        internal static void b2SolveContacts_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Overflow Contact", B2HexColor.b2_colorAliceBlue, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float inv_h = context.inv_h;

            // This is a dummy body to represent a static body since static bodies don't have a solver body.
            B2BodyState dummyState = b2_identityBodyState;

            Span<float> normalImpulses = stackalloc float[2];
            Span<float> tangentImpulses = stackalloc float[2];
            Span<float> rnA = stackalloc float[2];
            Span<float> rnB = stackalloc float[2];
            Span<float> rtA = stackalloc float[2];
            Span<float> rtB = stackalloc float[2];

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                ref B2BodyState stateA = ref indexA == B2_NULL_INDEX ? ref dummyState : ref states[indexA];
                Vector2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Rot dqA = stateA.deltaRotation;

                ref B2BodyState stateB = ref indexB == B2_NULL_INDEX ? ref dummyState : ref states[indexB];
                Vector2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;
                B2Rot dqB = stateB.deltaRotation;

                Vector2 dp = b2Sub(stateB.deltaPosition, stateA.deltaPosition);

                Vector2 normal = constraint.normal;
                Vector2 tangent = b2RightPerp(normal);
                float friction = constraint.friction;

                int pointCount = constraint.pointCount;
                float totalNormalImpulse = 0.0f;

                Vector2 dv = b2Sub(vB, vA);
                float vnc = b2Dot(dv, normal);

                // vt = dot(vrB - sB * tangent - (vrA + sA * tangent), tangent)
                //    = dot(vrB - vrA, tangent) - (sA + sB)
                float vtc = b2Dot(dv, tangent) - constraint.tangentSpeed;

                normalImpulses[0] = 0.0f;
                normalImpulses[1] = 0.0f;
                tangentImpulses[0] = 0.0f;
                tangentImpulses[1] = 0.0f;

                // Cache cross products
                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];
                    rnA[j] = b2Cross(cp.anchorA, normal);
                    rnB[j] = b2Cross(cp.anchorB, normal);
                    rtA[j] = b2Cross(cp.anchorA, tangent);
                    rtB[j] = b2Cross(cp.anchorB, tangent);
                }

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // Fixed anchor points.
                    Vector2 rA = cp.anchorA;
                    Vector2 rB = cp.anchorB;

                    // Compute current separation.
                    // This is subject to round-off error if the anchor is far from the body center of mass.
                    Vector2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                    float s = cp.baseSeparation + b2Dot(ds, normal);

                    // Speculative bias, zero when overlapped.
                    float velocityBias = s > 0.0f ? s * inv_h : 0.0f;

                    // Relative normal velocity at contact.
                    float vn = vnc + wB * rnB[j] - wA * rnA[j];
                    for (int k = 0; k < j; ++k)
                    {
                        // This allows getting point 2 normal velocity with only one dependency.
                        vn += (mA + mB + iA * rnA[k] * rnA[j] + iB * rnB[k] * rnB[j]) * normalImpulses[k];
                    }

                    float impulse = b2MaxFloat(-cp.normalMass * (vn + velocityBias), -cp.normalImpulse);
                    cp.normalImpulse += impulse;
                    cp.totalNormalImpulse += impulse;
                    normalImpulses[j] = impulse;

                    // This is used for rolling resistance
                    totalNormalImpulse += cp.normalImpulse;
                }

                // Rolling resistance
                float rollingImpulse;
                {
                    float Cdot = wB - wA;
                    for (int j = 0; j < pointCount; ++j)
                    {
                        Cdot += (iA * rnA[j] + iB * rnB[j]) * normalImpulses[j];
                    }

                    float lambda = constraint.rollingImpulse;
                    float maxLambda = constraint.rollingResistance * totalNormalImpulse;
                    constraint.rollingImpulse = b2ClampFloat(lambda - constraint.rollingMass * Cdot, -maxLambda, maxLambda);
                    rollingImpulse = constraint.rollingImpulse - lambda;
                }

                // Friction
                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    // relative tangent velocity at contact
                    float vt = vtc + wB * rtB[j] - wA * rtA[j] + (iA * rtA[j] + iB * rtB[j]) * rollingImpulse;
                    for (int k = 0; k < pointCount; ++k)
                    {
                        vt += (iA * rtA[j] * rnA[k] + iB * rtB[j] * rnB[k]) * normalImpulses[k];
                    }

                    for (int k = 0; k < j; ++k)
                    {
                        vt += (mA + mB + iA * rtA[k] * rtA[j] + iB * rtB[k] * rtB[j]) * tangentImpulses[k];
                    }

                    // clamp the accumulated force
                    float maxFriction = friction * cp.normalImpulse;
                    float impulse = b2ClampFloat(-cp.tangentMass * vt, -maxFriction - cp.tangentImpulse, maxFriction - cp.tangentImpulse);
                    cp.tangentImpulse += impulse;
                    tangentImpulses[j] = impulse;
                }

                {
                    float normalImpulse = 0.0f;
                    float tangentImpulse = 0.0f;
                    float angularA = rollingImpulse;
                    float angularB = rollingImpulse;
                    for (int j = 0; j < pointCount; ++j)
                    {
                        normalImpulse += normalImpulses[j];
                        tangentImpulse += tangentImpulses[j];
                        angularA += normalImpulses[j] * rnA[j] + tangentImpulses[j] * rtA[j];
                        angularB += normalImpulses[j] * rnB[j] + tangentImpulses[j] * rtB[j];
                    }

                    Vector2 P = b2Add(b2MulSV(normalImpulse, normal), b2MulSV(tangentImpulse, tangent));
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * angularA;
                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * angularB;
                }

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        internal static void b2ApplyRestitution_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.overflow_resitution, "Restitution Overflow", B2HexColor.b2_colorViolet, true);

            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2World world = context.world;
            B2SolverSet awakeSet = b2Array_Get(ref world.solverSets, (int)B2SolverSetType.b2_awakeSet);
            B2BodyState[] states = awakeSet.bodyStates.data;

            float threshold = world.restitutionThreshold;
            float inv_h = context.inv_h;
            bool propagate = world.enableRestitutionPropagation;

            B2BodyState dummyState = b2_identityBodyState;

            for (int i = 0; i < contactCount; ++i)
            {
                ref B2ContactConstraint constraint = ref constraints[i];
                if (propagate == false && constraint.restitution == 0.0f)
                {
                    continue;
                }

                float mA = constraint.invMassA;
                float iA = constraint.invIA;
                float mB = constraint.invMassB;
                float iB = constraint.invIB;

                int indexA = constraint.indexA - 1;
                int indexB = constraint.indexB - 1;

                ref B2BodyState stateA = ref indexA == B2_NULL_INDEX ? ref dummyState : ref states[indexA];
                Vector2 vA = stateA.linearVelocity;
                float wA = stateA.angularVelocity;
                B2Rot dqA = stateA.deltaRotation;

                ref B2BodyState stateB = ref indexB == B2_NULL_INDEX ? ref dummyState : ref states[indexB];
                Vector2 vB = stateB.linearVelocity;
                float wB = stateB.angularVelocity;
                B2Rot dqB = stateB.deltaRotation;

                Vector2 dp = b2Sub(stateB.deltaPosition, stateA.deltaPosition);

                Vector2 normal = constraint.normal;
                float restitution = constraint.restitution;
                int pointCount = constraint.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ContactConstraintPoint cp = ref constraint.points[j];

                    Vector2 rA = cp.anchorA;
                    Vector2 rB = cp.anchorB;

                    // The total normal impulse is 0 for speculative points.
                    float compressionImpulse = cp.totalNormalImpulse - cp.restitutionImpulse;
                    bool armed = restitution > 0.0f && cp.relativeVelocity < -threshold && compressionImpulse > 0.0f;

                    float velocityBias;
                    if (armed)
                    {
                        velocityBias = restitution * cp.relativeVelocity;
                    }
                    else
                    {
                        Vector2 ds = b2Add(dp, b2Sub(b2RotateVector(dqB, rB), b2RotateVector(dqA, rA)));
                        float s = cp.baseSeparation + b2Dot(ds, normal);

                        velocityBias = s > 0.0f ? s * inv_h : 0.0f;
                    }

                    Vector2 vrA = b2Add(vA, b2CrossSV(wA, rA));
                    Vector2 vrB = b2Add(vB, b2CrossSV(wB, rB));
                    float vn = b2Dot(b2Sub(vrB, vrA), normal);

                    float impulse = -cp.normalMass * (vn + velocityBias);

                    float newImpulse = b2MaxFloat(cp.normalImpulse + impulse, 0.0f);
                    impulse = newImpulse - cp.normalImpulse;

                    float approachImpulse = b2MinFloat(b2MaxFloat(-cp.normalMass * vn, 0.0f), b2MaxFloat(impulse, 0.0f));

                    if (armed)
                    {
                        // Poisson kinetic restitution guarantees no energy gain.
                        float allowance = restitution * (compressionImpulse + approachImpulse) - cp.restitutionImpulse;
                        impulse = b2MinFloat(impulse, approachImpulse + b2MaxFloat(allowance, 0.0f));
                    }

                    cp.normalImpulse += impulse;
                    cp.restitutionImpulse += impulse - approachImpulse;
                    cp.totalNormalImpulse += impulse;

                    Vector2 P = b2MulSV(impulse, normal);
                    vA = b2MulSub(vA, mA, P);
                    wA -= iA * b2Cross(rA, P);

                    vB = b2MulAdd(vB, mB, P);
                    wB += iB * b2Cross(rB, P);
                }

                if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateA.linearVelocity = vA;
                    stateA.angularVelocity = wA;
                }

                if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
                {
                    stateB.linearVelocity = vB;
                    stateB.angularVelocity = wB;
                }
            }

            b2TracyCZoneEnd(B2TracyCZone.overflow_resitution);
        }

        internal static void b2StoreImpulses_Overflow(B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            B2World world = context.world;
            ref B2ConstraintGraph graph = ref context.graph;
            ref B2GraphColor color = ref graph.colors[B2_OVERFLOW_INDEX];
            Span<B2ContactConstraint> constraints = color.overflowConstraints;
            int contactCount = color.contactSims.count;
            B2ContactSim[] contactSims = color.contactSims.data;
            B2TaskContext taskContext = world.taskContexts.data[0];
            ref B2BitSet hitEventBitSet = ref taskContext.hitEventBitSet;

            float negHitThreshold = -world.hitEventThreshold;
            bool hasHitEvents = taskContext.hasHitEvents;

            for (int i = 0; i < contactCount; ++i)
            {
                ref readonly B2ContactConstraint constraint = ref constraints[i];
                B2ContactSim contactSim = contactSims[i];
                ref B2Manifold manifold = ref contactSim.manifold;
                int pointCount = manifold.pointCount;

                for (int j = 0; j < pointCount; ++j)
                {
                    ref B2ManifoldPoint mp = ref manifold.points[j];
                    mp.normalImpulse = constraint.points[j].normalImpulse;
                    mp.tangentImpulse = constraint.points[j].tangentImpulse;
                    mp.totalNormalImpulse = constraint.points[j].totalNormalImpulse;
                }

                if ((contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0)
                {
                    for (int j = 0; j < contactSim.manifold.pointCount; ++j)
                    {
                        ref B2ManifoldPoint mp = ref manifold.points[j];

                        // Need to check total impulse because the point may be speculative and not colliding
                        if (mp.normalVelocity < negHitThreshold && mp.totalNormalImpulse > 0.0f)
                        {
                            b2SetBit(ref hitEventBitSet, contactSim.contactId);
                            hasHitEvents = true;
                            break;
                        }
                    }
                }

                manifold.rollingImpulse = constraint.rollingImpulse;
            }

            taskContext.hasHitEvents = hasHitEvents;

            b2TracyCZoneEnd(B2TracyCZone.store_impulses);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2AddW(Vector<float> a, Vector<float> b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2SubW(Vector<float> a, Vector<float> b)
        {
            return a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulW(Vector<float> a, Vector<float> b)
        {
            return a * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulAddW(Vector<float> a, Vector<float> b, Vector<float> c)
        {
            return (b * c) + a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MulSubW(Vector<float> a, Vector<float> b, Vector<float> c)
        {
            return a - (b * c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MinW(Vector<float> a, Vector<float> b)
        {
            return Vector.Min(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2MaxW(Vector<float> a, Vector<float> b)
        {
            return Vector.Max(a, b);
        }

        /// <summary>
        /// a = clamp(a, -b, b)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2SymClampW(Vector<float> a, Vector<float> b)
        {
            // a = clamp(a, -b, b)
            Vector<float> min = Vector.Min(a, b);
            return Vector.Max(-b, min);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2GreaterThanW(Vector<float> a, Vector<float> b)
        {
            return Vector.GreaterThan(a, b).As<int, float>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2EqualsW(Vector<float> a, Vector<float> b)
        {
            return Vector.Equals(a, b).As<int, float>();
        }

        /// <summary>
        /// component-wise returns mask ? b : a
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<float> b2BlendW(Vector<float> a, Vector<float> b, Vector<float> mask)
        {
            // component-wise returns mask ? b : a, bit by bit
            return Vector.ConditionalSelect(mask, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AllZeroW(Vector<float> a)
        {
            return Vector.EqualsAll(a, Vector<float>.Zero);
        }


        /// <summary>
        /// The Vector128 paths below are written to be bit-identical with the scalar fallback,
        /// including signed zero and NaN handling, so results stay deterministic across targets.
        /// Plain IEEE add/sub/mul/div are exact per lane, so they use the Vector4 operators directly.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2ZeroW()
        {
            return Vector4.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SplatW(float scalar)
        {
            return new Vector4(scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SetW(float a, float b, float c, float d)
        {
            return new Vector4(a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2NegW(in Vector4 a)
        {
            return Vector4.Negate(a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2AddW(in Vector4 a, in Vector4 b)
        {
            return a + b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SubW(in Vector4 a, in Vector4 b)
        {
            return a - b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulW(in Vector4 a, in Vector4 b)
        {
            return a * b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2DivW(in Vector4 a, in Vector4 b)
        {
            return a / b;
        }

        /// <summary>
        /// Separate multiply and add (no FMA) to match the scalar rounding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulAddW(in Vector4 a, in Vector4 b, in Vector4 c)
        {
            return a + b * c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MulSubW(in Vector4 a, in Vector4 b, in Vector4 c)
        {
            return a - b * c;
        }

        /// <summary>
        /// a &lt;= b ? a : b per lane. Vector4.Min is not used because its handling of
        /// equal signed zeros and NaN differs between runtimes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MinW(in Vector4 a, in Vector4 b)
        {
            return Vector4.ConditionalSelect(Vector4.LessThanOrEqual(a, b), a, b);
        }

        /// <summary>
        /// a &gt;= b ? a : b per lane
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2MaxW(in Vector4 a, in Vector4 b)
        {
            return Vector4.ConditionalSelect(Vector4.GreaterThanOrEqual(a, b), a, b);
        }

        /// <summary>
        /// a = clamp(a, -b, b)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SymClampW(in Vector4 a, in Vector4 b)
        {
            // a = clamp(a, -b, b)
            // Same as b2ClampFloat: a < lower ? lower : (a > upper ? upper : a).
            // Vector4.Clamp is not used because it follows IEEE min/max semantics.
            Vector4 lower = Vector4.Xor(b, Vector4.Create(-0.0f));
            Vector4 r = Vector4.ConditionalSelect(Vector4.GreaterThan(a, b), b, a);
            return Vector4.ConditionalSelect(Vector4.LessThan(a, lower), lower, r);
        }

        /// <summary>
        /// Masks are stored as all bits set (true) or all bits clear (false) per lane, like the C SSE2/NEON paths.
        /// They only feed b2AndW, b2BlendW and b2AnyTrueW, never arithmetic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2AndW(in Vector4 a, in Vector4 b)
        {
            return a & b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2GreaterThanW(in Vector4 a, in Vector4 b)
        {
            return Vector4.GreaterThan(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2EqualsW(in Vector4 a, in Vector4 b)
        {
            return Vector4.Equals(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2LessThanW(in Vector4 a, in Vector4 b)
        {
            return Vector4.LessThan(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AnyTrueW(in Vector4 mask)
        {
            return Vector128.ExtractMostSignificantBits(mask.AsVector128()) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool b2AllZeroW(in Vector4 a)
        {
            return Vector4.EqualsAll(a, Vector4.Zero);
        }

        /// <summary>
        /// component-wise returns mask ? b : a
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2BlendW(in Vector4 a, in Vector4 b, in Vector4 mask)
        {
            // component-wise returns mask ? b : a, bit by bit
            return Vector4.ConditionalSelect(mask, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2SoftMaskW(ReadOnlySpan<int> indexA, ReadOnlySpan<int> indexB)
        {
            // Integer lanes: Vector4 has no int API, so this stays on Vector128<int>.
            Vector128<int> zero = Vector128<int>.Zero;
            Vector128<int> mask = Vector128.Equals(Vector128.Create(indexA), zero) | Vector128.Equals(Vector128.Create(indexB), zero);
            return mask.AsSingle().AsVector4();
        }

        /// <summary>
        /// Read one lane straight from memory. A switch or Vector4 indexer with a variable index is much slower.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float b2GetLaneW(in Vector4 v, int laneIndex)
        {
            B2_ASSERT(0 <= laneIndex && laneIndex < B2_SIMD_WIDTH);
            return Unsafe.Add(ref Unsafe.As<Vector4, float>(ref Unsafe.AsRef(in v)), laneIndex);
        }

        /// <summary>
        /// a - b
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2SubVW(in B2Vec2W a, in B2Vec2W b)
        {
            return new B2Vec2W(b2SubW(a.X, b.X), b2SubW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2RightPerpW(in B2Vec2W a)
        {
            return new B2Vec2W(a.Y, b2NegW(a.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2DotW(in B2Vec2W a, in B2Vec2W b)
        {
            return b2AddW(b2MulW(a.X, b.X), b2MulW(a.Y, b.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 b2CrossW(in B2Vec2W a, in B2Vec2W b)
        {
            return b2SubW(b2MulW(a.X, b.Y), b2MulW(a.Y, b.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Vec2W b2RotateVectorW(in B2RotW q, in B2Vec2W v)
        {
            return new B2Vec2W(b2SubW(b2MulW(q.C, v.X), b2MulW(q.S, v.Y)), b2AddW(b2MulW(q.S, v.X), b2MulW(q.C, v.Y)));
        }


        /// <summary>
        /// This function allows hiding SIMD intrinsics in the source file to improve compilation performance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int b2GetWideContactConstraintByteCount()
        {
            return B2SizeOf<B2ContactConstraintWide>.Size;
        }
    }
}
