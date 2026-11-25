// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Solvers;

namespace Box2D.NET
{
    public static class B2MotionLockJoints
    {
        /**
         * @defgroup motion_lock_joint Motion Lock Joint
         * @brief Functions for the motion lock joint.
         * @{
         */
        /// <summary>
        /// Enable/disable a linear lock. The axis is 0 for x and 1 for y.
        /// </summary>
        public static void b2MotionLockJoint_EnableLinearLock(B2JointId jointId, int axis, bool flag)
        {
            if (B2_CHECK_INPUT(0 <= axis && axis < 2) == false) return;

            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_motionLockJoint);
            ref B2MotionLockJoint joint = ref @base.uj.motionLockJoint;
            if (flag != joint.lockLinear[axis])
            {
                joint.lockLinear[axis] = flag;
                if (axis == 0)
                {
                    joint.linearImpulse.X = 0.0f;
                }
                else
                {
                    joint.linearImpulse.Y = 0.0f;
                }
            }
        }

        /// <summary>
        /// Is a linear lock enabled? The axis is 0 for x and 1 for y.
        /// </summary>
        public static bool b2MotionLockJoint_IsLinearLockEnabled(B2JointId jointId, int axis)
        {
            if (B2_CHECK_INPUT(0 <= axis && axis < 2) == false) return false;

            B2JointSim @base = b2GetJointSimCheckType(jointId, B2JointType.b2_motionLockJoint);
            return @base.uj.motionLockJoint.lockLinear[axis];
        }

        /**@}*/

        internal static Vector2 b2GetMotionLockJointForce(B2World world, B2JointSim @base)
        {
            B2WorldTransform transformA = b2GetBodyTransform(world, @base.bodyIdA);
            Vector2 force = b2MulSV(world.inv_h, @base.uj.motionLockJoint.linearImpulse);
            force = b2RotateVector(@base.localFrameA.q, force);
            return b2RotateVector(transformA.q, force);
        }

        private static Vector2 b2GetMotionLockSeparation(in B2MotionLockJoint joint, in B2BodyState stateA, in B2BodyState stateB, Vector2 rA, Vector2 rB)
        {
            Vector2 dpA = b2Add(stateA.deltaPosition, b2Sub(rA, joint.frameA.p));
            Vector2 dpB = b2Add(stateB.deltaPosition, b2Sub(rB, joint.frameB.p));
            return b2Add(joint.separation, b2Sub(dpB, dpA));
        }

        internal static void b2PrepareMotionLockJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motionLockJoint);

            int idA = @base.bodyIdA;
            int idB = @base.bodyIdB;

            B2World world = context.world;

            B2Body bodyA = b2Array_Get(ref world.bodies, idA);
            B2Body bodyB = b2Array_Get(ref world.bodies, idB);

            B2_ASSERT(bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet || bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet);
            B2SolverSet setA = b2Array_Get(ref world.solverSets, bodyA.setIndex);
            B2SolverSet setB = b2Array_Get(ref world.solverSets, bodyB.setIndex);

            int localIndexA = bodyA.localIndex;
            int localIndexB = bodyB.localIndex;

            B2BodySim bodySimA = b2Array_Get(ref setA.bodySims, localIndexA);
            B2BodySim bodySimB = b2Array_Get(ref setB.bodySims, localIndexB);

            float iA = bodySimA.invInertia;
            float iB = bodySimB.invInertia;

            @base.invMassA = bodySimA.invMass;
            @base.invMassB = bodySimB.invMass;
            @base.invIA = iA;
            @base.invIB = iB;

            ref B2MotionLockJoint joint = ref @base.uj.motionLockJoint;
            joint.indexA = bodyA.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexA : B2_NULL_INDEX;
            joint.indexB = bodyB.setIndex == (int)B2SolverSetType.b2_awakeSet ? localIndexB : B2_NULL_INDEX;

            joint.frameA.q = b2MulRot(bodySimA.transform.q, @base.localFrameA.q);
            joint.frameA.p = b2RotateVector(bodySimA.transform.q, b2Sub(@base.localFrameA.p, bodySimA.localCenter));
            joint.frameB.q = b2MulRot(bodySimB.transform.q, @base.localFrameB.q);
            joint.frameB.p = b2RotateVector(bodySimB.transform.q, b2Sub(@base.localFrameB.p, bodySimB.localCenter));

            // The anchor difference is taken in world precision so a lock far from the center of body A keeps its accuracy
            // in double precision
            B2Pos posA = b2OffsetPos(bodySimA.center, joint.frameA.p);
            B2Pos posB = b2OffsetPos(bodySimB.center, joint.frameB.p);
            joint.separation = b2SubPos(posB, posA);

            bool anchorAtCenterB = joint.frameB.p.X == 0.0f && joint.frameB.p.Y == 0.0f;
            joint.diagonal = iA == 0.0f && (iB == 0.0f || anchorAtCenterB);

            if (context.enableWarmStarting == false)
            {
                joint.linearImpulse = Vector2Zero;
            }
        }

        internal static void b2WarmStartMotionLockJoint(B2JointSim @base, B2StepContext context)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motionLockJoint);

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            B2BodyState dummyState = b2_identityBodyState;

            ref readonly B2MotionLockJoint joint = ref @base.uj.motionLockJoint;
            ref B2BodyState stateA = ref joint.indexA == B2_NULL_INDEX ? ref dummyState : ref context.states[joint.indexA];
            ref B2BodyState stateB = ref joint.indexB == B2_NULL_INDEX ? ref dummyState : ref context.states[joint.indexB];

            Vector2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            Vector2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);
            Vector2 d = b2GetMotionLockSeparation(joint, stateA, stateB, rA, rB);

            B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
            Vector2 P = b2RotateVector(qA, joint.linearImpulse);

            float LA = b2Cross(b2Add(rA, d), P);
            float LB = b2Cross(rB, P);

            if (0 != (stateA.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateA.linearVelocity = b2MulSub(stateA.linearVelocity, mA, P);
                stateA.angularVelocity -= iA * LA;
            }

            if (0 != (stateB.flags & (uint)B2BodyFlags.b2_dynamicFlag))
            {
                stateB.linearVelocity = b2MulAdd(stateB.linearVelocity, mB, P);
                stateB.angularVelocity += iB * LB;
            }
        }

        internal static void b2SolveMotionLockJoint(B2JointSim @base, B2StepContext context, bool useBias)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motionLockJoint);

            ref B2MotionLockJoint joint = ref @base.uj.motionLockJoint;

            B2FixedArray2<int> axisIndices = new B2FixedArray2<int>();
            int count = 0;
            for (int i = 0; i < 2; ++i)
            {
                if (joint.lockLinear[i])
                {
                    axisIndices[count] = i;
                    count += 1;
                }
            }

            if (count == 0)
            {
                return;
            }

            float mA = @base.invMassA;
            float mB = @base.invMassB;
            float iA = @base.invIA;
            float iB = @base.invIB;

            B2BodyState dummyState = b2_identityBodyState;

            ref B2BodyState stateA = ref joint.indexA == B2_NULL_INDEX ? ref dummyState : ref context.states[joint.indexA];
            ref B2BodyState stateB = ref joint.indexB == B2_NULL_INDEX ? ref dummyState : ref context.states[joint.indexB];

            Vector2 vA = stateA.linearVelocity;
            float wA = stateA.angularVelocity;
            Vector2 vB = stateB.linearVelocity;
            float wB = stateB.angularVelocity;

            float massScale = 1.0f;
            float impulseScale = 0.0f;
            float biasRate = 0.0f;
            if (useBias)
            {
                massScale = @base.constraintSoftness.massScale;
                impulseScale = @base.constraintSoftness.impulseScale;
                biasRate = @base.constraintSoftness.biasRate;
            }

            Vector2 rA = b2RotateVector(stateA.deltaRotation, joint.frameA.p);
            Vector2 rB = b2RotateVector(stateB.deltaRotation, joint.frameB.p);
            Vector2 d = b2GetMotionLockSeparation(joint, stateA, stateB, rA, rB);
            Vector2 rAd = b2Add(rA, d);

            B2Rot qA = b2MulRot(stateA.deltaRotation, joint.frameA.q);
            B2FixedArray2<Vector2> allAxes = new B2FixedArray2<Vector2>(b2Rot_GetXAxis(qA), b2Rot_GetYAxis(qA));

            Vector2 vRel = b2Sub(b2Add(vB, b2CrossSV(wB, rB)), b2Add(vA, b2CrossSV(wA, rAd)));

            B2FixedArray2<Vector2> axes = new B2FixedArray2<Vector2>();
            B2FixedArray2<float> sA = new B2FixedArray2<float>();
            B2FixedArray2<float> sB = new B2FixedArray2<float>();
            B2FixedArray2<float> rhs = new B2FixedArray2<float>();
            B2FixedArray2<float> oldImpulses = new B2FixedArray2<float>(joint.linearImpulse.X, joint.linearImpulse.Y);
            B2FixedArray2<float> impulses = new B2FixedArray2<float>(0.0f, 0.0f);

            for (int i = 0; i < count; ++i)
            {
                Vector2 axis = allAxes[axisIndices[i]];
                axes[i] = axis;
                sA[i] = b2Cross(rAd, axis);
                sB[i] = b2Cross(rB, axis);
                rhs[i] = b2Dot(vRel, axis) + biasRate * b2Dot(d, axis);
            }

            float m = mA + mB;

            if (joint.diagonal)
            {
                float invM = m > 0.0f ? 1.0f / m : 0.0f;
                for (int i = 0; i < count; ++i)
                {
                    impulses[i] = -massScale * invM * rhs[i] - impulseScale * oldImpulses[axisIndices[i]];
                }
            }
            else
            {
                B2FixedArray2<float> solution = new B2FixedArray2<float>(0.0f, 0.0f);
                if (count == 1)
                {
                    float k = m + iA * sA[0] * sA[0] + iB * sB[0] * sB[0];
                    solution[0] = k > 0.0f ? rhs[0] / k : 0.0f;
                }
                else
                {
                    float k11 = m + iA * sA[0] * sA[0] + iB * sB[0] * sB[0];
                    float k12 = iA * sA[0] * sA[1] + iB * sB[0] * sB[1];
                    float k22 = m + iA * sA[1] * sA[1] + iB * sB[1] * sB[1];
                    B2Mat22 k = new B2Mat22(new Vector2(k11, k12), new Vector2(k12, k22));
                    Vector2 s = b2Solve22(k, new Vector2(rhs[0], rhs[1]));
                    solution[0] = s.X;
                    solution[1] = s.Y;
                }

                for (int i = 0; i < count; ++i)
                {
                    impulses[i] = -massScale * solution[i] - impulseScale * oldImpulses[axisIndices[i]];
                }
            }

            Vector2 P = Vector2Zero;
            float LA = 0.0f;
            float LB = 0.0f;
            for (int i = 0; i < count; ++i)
            {
                oldImpulses[axisIndices[i]] += impulses[i];
                P = b2MulAdd(P, impulses[i], axes[i]);
                LA += impulses[i] * sA[i];
                LB += impulses[i] * sB[i];
            }

            joint.linearImpulse.X = oldImpulses[0];
            joint.linearImpulse.Y = oldImpulses[1];

            vA = b2MulSub(vA, mA, P);
            wA -= iA * LA;
            vB = b2MulAdd(vB, mB, P);
            wB += iB * LB;

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

        internal static void b2DrawMotionLockJoint(B2DebugDraw draw, B2JointSim @base, in B2WorldTransform transformA, in B2WorldTransform transformB, float drawScale)
        {
            B2_ASSERT(@base.type == B2JointType.b2_motionLockJoint);

            ref readonly B2MotionLockJoint joint = ref @base.uj.motionLockJoint;

            B2WorldTransform frameA = b2MulWorldTransforms(transformA, @base.localFrameA);
            B2WorldTransform frameB = b2MulWorldTransforms(transformB, @base.localFrameB);

            float length = 0.25f * drawScale;
            B2FixedArray2<Vector2> axes = new B2FixedArray2<Vector2>(b2Rot_GetXAxis(frameA.q), b2Rot_GetYAxis(frameA.q));
            B2FixedArray2<B2HexColor> colors = new B2FixedArray2<B2HexColor>(B2HexColor.b2_colorRed, B2HexColor.b2_colorGreen);

            for (int i = 0; i < 2; ++i)
            {
                if (joint.lockLinear[i] == false)
                {
                    continue;
                }

                Vector2 offset = b2MulSV(length, axes[i]);
                draw.DrawLineFcn(b2OffsetPos(frameA.p, b2Neg(offset)), b2OffsetPos(frameA.p, offset), colors[i], draw.context);
            }

            draw.DrawLineFcn(frameA.p, frameB.p, B2HexColor.b2_colorDarkSeaGreen, draw.context);
        }
    }
}
