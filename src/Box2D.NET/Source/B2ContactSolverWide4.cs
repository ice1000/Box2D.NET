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
using static Box2D.NET.B2ArenaAllocators;
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
using static Box2D.NET.B2ContactSolvers;

namespace Box2D.NET
{
    /// <summary>
    /// 4-wide contact solver: SSE2 / NEON, or scalar Vector4 fallback.
    /// </summary>
    internal sealed class B2ContactSolverWide4 : B2ContactSolverWide
    {
        private const int WIDTH = 4;
        private const int SHIFT = 2;

        private ArraySegment<B2ContactConstraintWide> wideConstraints;

        public override int Width => WIDTH;
        public override int Shift => SHIFT;

        public override void Allocate(B2StackAllocator stack, int wideContactCount)
        {
            B2_ASSERT(B2FixedArray4<int>.Size == WIDTH);
            wideConstraints = b2StackAlloc<B2ContactConstraintWide>(stack, wideContactCount, "contact constraint");
        }

        public override void Free(B2StackAllocator stack)
        {
            b2StackFree(stack, wideConstraints);
            wideConstraints = default;
        }

        public override void ClearConstraint(int wideIndex)
        {
            wideConstraints[wideIndex] = new B2ContactConstraintWide();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<B2ContactConstraintWide> b2GetColorConstraints(B2StepContext context, int colorIndex)
        {
            ref B2GraphColor color = ref context.graph.colors[colorIndex];
            return wideConstraints.AsSpan(color.wideConstraintStart, color.wideConstraintCount);
        }

        // Gather/scatter for 4 lanes. The C reference for each SIMD type is in box2d/src/contact_solver.c.

        // 4x4 transpose building blocks, named after the SSE2 intrinsics used by the C version.
        // Each one only moves lanes, so results are bit-identical on every path.

        // (a0, b0, a1, b1)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2UnpackLoW(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported) return Sse.UnpackLow(a, b);
            if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.ZipLow(a, b);
            return Vector128.Create(a.GetElement(0), b.GetElement(0), a.GetElement(1), b.GetElement(1));
        }

        // (a2, b2, a3, b3)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2UnpackHiW(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported) return Sse.UnpackHigh(a, b);
            if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.ZipHigh(a, b);
            return Vector128.Create(a.GetElement(2), b.GetElement(2), a.GetElement(3), b.GetElement(3));
        }

        // _mm_movelh_ps: (a0, a1, b0, b1)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2MoveLHW(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported) return Sse.MoveLowToHigh(a, b);
            if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.ZipLow(a.AsUInt64(), b.AsUInt64()).AsSingle();
            return Vector128.Create(a.GetElement(0), a.GetElement(1), b.GetElement(0), b.GetElement(1));
        }

        // _mm_movehl_ps: (b2, b3, a2, a3)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2MoveHLW(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported) return Sse.MoveHighToLow(a, b);
            if (AdvSimd.Arm64.IsSupported) return AdvSimd.Arm64.ZipHigh(b.AsUInt64(), a.AsUInt64()).AsSingle();
            return Vector128.Create(b.GetElement(2), b.GetElement(3), a.GetElement(2), a.GetElement(3));
        }

        // First half of a body state: (v.x, v.y, w, flags)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2LoadBodyLo(ref readonly B2BodyState state)
        {
            return Unsafe.ReadUnaligned<Vector128<float>>(ref Unsafe.As<B2BodyState, byte>(ref Unsafe.AsRef(in state)));
        }

        // Second half of a body state: (dp.x, dp.y, dq.c, dq.s)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<float> b2LoadBodyHi(ref readonly B2BodyState state)
        {
            return Unsafe.ReadUnaligned<Vector128<float>>(ref Unsafe.Add(ref Unsafe.As<B2BodyState, byte>(ref Unsafe.AsRef(in state)), 16));
        }

        /// <summary>
        /// This is a load and transpose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static B2BodyStateW b2GatherBodies(ReadOnlySpan<B2BodyState> states, ReadOnlySpan<int> indices)
        {
            B2_ASSERT(Unsafe.SizeOf<B2BodyState>() == 32);
            B2_VALIDATE(indices[0] >= 0 && indices[1] >= 0 && indices[2] >= 0 && indices[3] >= 0);

            // zero means null
            int i1 = indices[0] - 1;
            int i2 = indices[1] - 1;
            int i3 = indices[2] - 1;
            int i4 = indices[3] - 1;

            ref readonly B2BodyState s1 = ref i1 == B2_NULL_INDEX ? ref b2_identityBodyState : ref states[i1];
            ref readonly B2BodyState s2 = ref i2 == B2_NULL_INDEX ? ref b2_identityBodyState : ref states[i2];
            ref readonly B2BodyState s3 = ref i3 == B2_NULL_INDEX ? ref b2_identityBodyState : ref states[i3];
            ref readonly B2BodyState s4 = ref i4 == B2_NULL_INDEX ? ref b2_identityBodyState : ref states[i4];

            B2BodyStateW simdBody;

            {
                Vector128<float> r1 = b2LoadBodyLo(in s1);
                Vector128<float> r2 = b2LoadBodyLo(in s2);
                Vector128<float> r3 = b2LoadBodyLo(in s3);
                Vector128<float> r4 = b2LoadBodyLo(in s4);

                Vector128<float> t1 = b2UnpackLoW(r1, r2);
                Vector128<float> t2 = b2UnpackHiW(r1, r2);
                Vector128<float> t3 = b2UnpackLoW(r3, r4);
                Vector128<float> t4 = b2UnpackHiW(r3, r4);

                simdBody.v.X = b2MoveLHW(t1, t3).AsVector4();
                simdBody.v.Y = b2MoveHLW(t3, t1).AsVector4();
                simdBody.w = b2MoveLHW(t2, t4).AsVector4();
                // raw flag bits, never used as numbers
                simdBody.flags = b2MoveHLW(t4, t2).AsVector4();
            }

            {
                Vector128<float> r1 = b2LoadBodyHi(in s1);
                Vector128<float> r2 = b2LoadBodyHi(in s2);
                Vector128<float> r3 = b2LoadBodyHi(in s3);
                Vector128<float> r4 = b2LoadBodyHi(in s4);

                Vector128<float> t1 = b2UnpackLoW(r1, r2);
                Vector128<float> t2 = b2UnpackHiW(r1, r2);
                Vector128<float> t3 = b2UnpackLoW(r3, r4);
                Vector128<float> t4 = b2UnpackHiW(r3, r4);

                simdBody.dp.X = b2MoveLHW(t1, t3).AsVector4();
                simdBody.dp.Y = b2MoveHLW(t3, t1).AsVector4();
                simdBody.dq.C = b2MoveLHW(t2, t4).AsVector4();
                simdBody.dq.S = b2MoveHLW(t4, t2).AsVector4();
            }

            return simdBody;
        }

        /// <summary>
        /// This writes only the velocities back to the solver bodies (plus the unchanged flags in the same 16 bytes)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2ScatterBodies(Span<B2BodyState> states, ReadOnlySpan<int> indices, ref B2BodyStateW simdBody)
        {
            B2_VALIDATE(indices[0] >= 0 && indices[1] >= 0 && indices[2] >= 0 && indices[3] >= 0);

            // transpose
            Vector128<float> t1 = b2UnpackLoW(simdBody.v.X.AsVector128(), simdBody.v.Y.AsVector128());
            Vector128<float> t2 = b2UnpackHiW(simdBody.v.X.AsVector128(), simdBody.v.Y.AsVector128());
            Vector128<float> t3 = b2UnpackLoW(simdBody.w.AsVector128(), simdBody.flags.AsVector128());
            Vector128<float> t4 = b2UnpackHiW(simdBody.w.AsVector128(), simdBody.flags.AsVector128());

            // zero means null
            int i1 = indices[0] - 1;
            int i2 = indices[1] - 1;
            int i3 = indices[2] - 1;
            int i4 = indices[3] - 1;

            if (i1 != B2_NULL_INDEX && (states[i1].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<B2BodyState, byte>(ref states[i1]), b2MoveLHW(t1, t3));
            }

            if (i2 != B2_NULL_INDEX && (states[i2].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<B2BodyState, byte>(ref states[i2]), b2MoveHLW(t3, t1));
            }

            if (i3 != B2_NULL_INDEX && (states[i3].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<B2BodyState, byte>(ref states[i3]), b2MoveLHW(t2, t4));
            }

            if (i4 != B2_NULL_INDEX && (states[i4].flags & (uint)B2BodyFlags.b2_dynamicFlag) != 0)
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<B2BodyState, byte>(ref states[i4]), b2MoveHLW(t4, t2));
            }
        }

        private static readonly B2ContactSim b2_zeroContactSim = new B2ContactSim();

        /// <summary>
        /// Prepare wide contact constraints.
        /// Potential optimization: for SSE2 load 4 manifold points and transpose, like the body gather/scatter.
        /// I would need to reorder the manifold point so that constraint data comes first.
        /// </summary>
        public override void Prepare(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_contact, "Prepare Contact", B2HexColor.b2_colorYellow, true);
            B2World world = context.world;
            B2BodyState[] states = context.states;
            B2ContactPrepareSpan[] spans = context.contactPrepareSpans;
            Span<B2ContactConstraintWide> wideBase = wideConstraints;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            Vector4 zeroW = b2ZeroW();
            Vector4 oneW = b2SplatW(1.0f);
            Vector4 warmStartScale = world.enableWarmStarting ? oneW : zeroW;
            bool anyRestitution = false;

            int wideIndex = block.startIndex;
            int endWideIndex = block.startIndex + block.count;

            // Find color for start index. Linear search but fast.
            int colorIndex = 0;
            while (spans[colorIndex + 1].start <= wideIndex)
            {
                colorIndex += 1;
            }

            // Loop over block
            while (wideIndex < endWideIndex)
            {
                int colorWideStart = spans[colorIndex].start;
                int colorWideEndIndex = b2MinInt(spans[colorIndex + 1].start, endWideIndex);
                int colorContactCount = spans[colorIndex].count;
                B2ContactSim[] contactSims = spans[colorIndex].contacts;

#if DEBUG
                int expectedWide = colorContactCount > 0 ? ((colorContactCount - 1) >> SHIFT) + 1 : 0;
                B2_ASSERT(spans[colorIndex + 1].start - spans[colorIndex].start == expectedWide);
#endif

                // Loop over color
                for (; wideIndex < colorWideEndIndex; ++wideIndex)
                {
                    ref B2ContactConstraintWide cw = ref wideBase[wideIndex];
                    int localWideIndex = wideIndex - colorWideStart;
                    uint hitEvents = 0;

                    for (int laneIndex = 0; laneIndex < WIDTH; ++laneIndex)
                    {
                        int contactIndex = WIDTH * localWideIndex + laneIndex;
                        if (contactIndex < colorContactCount)
                        {
                            B2ContactSim c = contactSims[contactIndex];

                            // index base-1
                            cw.indexA[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimA) + 1;
                            cw.indexB[laneIndex] = b2DecodeAwakeIndex(c.encodedBodySimB) + 1;
                            hitEvents |= (c.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent);

#if DEBUG
                            B2Body bodyA = bodies[c.bodyIdA];
                            B2Body bodyB = bodies[c.bodyIdB];
                            B2_ASSERT(c.encodedBodySimA == b2EncodeBodySimIndex(bodyA));
                            B2_ASSERT(c.encodedBodySimB == b2EncodeBodySimIndex(bodyB));
#endif
                        }
                    }

                    int laneBase = WIDTH * localWideIndex;
                    B2ContactSim l0 = laneBase + 0 < colorContactCount ? contactSims[laneBase + 0] : b2_zeroContactSim;
                    B2ContactSim l1 = laneBase + 1 < colorContactCount ? contactSims[laneBase + 1] : b2_zeroContactSim;
                    B2ContactSim l2 = laneBase + 2 < colorContactCount ? contactSims[laneBase + 2] : b2_zeroContactSim;
                    B2ContactSim l3 = laneBase + 3 < colorContactCount ? contactSims[laneBase + 3] : b2_zeroContactSim;

                    cw.invMassA = b2SetW(l0.invMassA, l1.invMassA, l2.invMassA, l3.invMassA);
                    cw.invMassB = b2SetW(l0.invMassB, l1.invMassB, l2.invMassB, l3.invMassB);
                    cw.invIA = b2SetW(l0.invIA, l1.invIA, l2.invIA, l3.invIA);
                    cw.invIB = b2SetW(l0.invIB, l1.invIB, l2.invIB, l3.invIB);
                    cw.normal.X = b2SetW(l0.manifold.normal.X, l1.manifold.normal.X, l2.manifold.normal.X, l3.manifold.normal.X);
                    cw.normal.Y = b2SetW(l0.manifold.normal.Y, l1.manifold.normal.Y, l2.manifold.normal.Y, l3.manifold.normal.Y);
                    cw.friction = b2SetW(l0.friction, l1.friction, l2.friction, l3.friction);
                    cw.tangentSpeed = b2SetW(l0.tangentSpeed, l1.tangentSpeed, l2.tangentSpeed, l3.tangentSpeed);
                    cw.rollingResistance = b2SetW(l0.rollingResistance, l1.rollingResistance, l2.rollingResistance, l3.rollingResistance);
                    cw.rollingImpulse = b2SetW(l0.manifold.rollingImpulse, l1.manifold.rollingImpulse, l2.manifold.rollingImpulse, l3.manifold.rollingImpulse);
                    cw.rollingImpulse = b2MulW(warmStartScale, cw.rollingImpulse);

                    cw.restitution = b2SetW(l0.restitution, l1.restitution, l2.restitution, l3.restitution);

                    B2Vec2W tangent = b2RightPerpW(cw.normal);

                    Vector4 pointCounts = b2SetW(l0.manifold.pointCount, l1.manifold.pointCount, l2.manifold.pointCount, l3.manifold.pointCount);
                    Vector4 twoPointMask = b2GreaterThanW(pointCounts, oneW);

                    {
                        // Transpose gather of the first manifold point. Matches the upstream layout:
                        // anchorA, anchorB, separation, normalImpulse, tangentImpulse
                        ref B2ManifoldPoint m1 = ref l0.manifold.points[0];
                        ref B2ManifoldPoint m2 = ref l1.manifold.points[0];
                        ref B2ManifoldPoint m3 = ref l2.manifold.points[0];
                        ref B2ManifoldPoint m4 = ref l3.manifold.points[0];

                        cw.anchorA1.X = b2SetW(m1.anchorA.X, m2.anchorA.X, m3.anchorA.X, m4.anchorA.X);
                        cw.anchorA1.Y = b2SetW(m1.anchorA.Y, m2.anchorA.Y, m3.anchorA.Y, m4.anchorA.Y);
                        cw.anchorB1.X = b2SetW(m1.anchorB.X, m2.anchorB.X, m3.anchorB.X, m4.anchorB.X);
                        cw.anchorB1.Y = b2SetW(m1.anchorB.Y, m2.anchorB.Y, m3.anchorB.Y, m4.anchorB.Y);
                        cw.baseSeparation1 = b2SetW(m1.separation, m2.separation, m3.separation, m4.separation);
                        cw.normalImpulse1 = b2SetW(m1.normalImpulse, m2.normalImpulse, m3.normalImpulse, m4.normalImpulse);
                        cw.tangentImpulse1 = b2SetW(m1.tangentImpulse, m2.tangentImpulse, m3.tangentImpulse, m4.tangentImpulse);

                        // separation1 = cw.baseSeparation1;

                        Vector4 offset = b2DotW(b2SubVW(cw.anchorB1, cw.anchorA1), cw.normal);
                        cw.baseSeparation1 = b2SubW(cw.baseSeparation1, offset);
                        cw.normalImpulse1 = b2MulW(warmStartScale, cw.normalImpulse1);
                        cw.tangentImpulse1 = b2MulW(warmStartScale, cw.tangentImpulse1);
                        cw.totalNormalImpulse1 = zeroW;
                        cw.relativeVelocity1 = zeroW;
                        cw.restitutionImpulse1 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector4 rnA = b2CrossW(cw.anchorA1, cw.normal);
                            Vector4 rnB = b2CrossW(cw.anchorB1, cw.normal);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector4 rtA = b2CrossW(cw.anchorA1, tangent);
                            Vector4 rtB = b2CrossW(cw.anchorB1, tangent);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }
                    }

                    {
                        ref B2ManifoldPoint m1 = ref l0.manifold.points[1];
                        ref B2ManifoldPoint m2 = ref l1.manifold.points[1];
                        ref B2ManifoldPoint m3 = ref l2.manifold.points[1];
                        ref B2ManifoldPoint m4 = ref l3.manifold.points[1];

                        cw.anchorA2.X = b2SetW(m1.anchorA.X, m2.anchorA.X, m3.anchorA.X, m4.anchorA.X);
                        cw.anchorA2.Y = b2SetW(m1.anchorA.Y, m2.anchorA.Y, m3.anchorA.Y, m4.anchorA.Y);
                        cw.anchorB2.X = b2SetW(m1.anchorB.X, m2.anchorB.X, m3.anchorB.X, m4.anchorB.X);
                        cw.anchorB2.Y = b2SetW(m1.anchorB.Y, m2.anchorB.Y, m3.anchorB.Y, m4.anchorB.Y);
                        cw.baseSeparation2 = b2SetW(m1.separation, m2.separation, m3.separation, m4.separation);
                        cw.normalImpulse2 = b2SetW(m1.normalImpulse, m2.normalImpulse, m3.normalImpulse, m4.normalImpulse);
                        cw.tangentImpulse2 = b2SetW(m1.tangentImpulse, m2.tangentImpulse, m3.tangentImpulse, m4.tangentImpulse);

                        // separation2 = cw.baseSeparation2;

                        Vector4 offset = b2DotW(b2SubVW(cw.anchorB2, cw.anchorA2), cw.normal);
                        cw.baseSeparation2 = b2SubW(cw.baseSeparation2, offset);
                        cw.normalImpulse2 = b2MulW(warmStartScale, cw.normalImpulse2);
                        cw.tangentImpulse2 = b2MulW(warmStartScale, cw.tangentImpulse2);
                        cw.totalNormalImpulse2 = zeroW;
                        cw.relativeVelocity2 = zeroW;
                        cw.restitutionImpulse2 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector4 rnA = b2CrossW(cw.anchorA2, cw.normal);
                            Vector4 rnB = b2CrossW(cw.anchorB2, cw.normal);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector4 rtA = b2CrossW(cw.anchorA2, tangent);
                            Vector4 rtB = b2CrossW(cw.anchorB2, tangent);
                            Vector4 k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector4 test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Zero effective mass on lanes that only have one point.
                        cw.normalMass2 = b2BlendW(zeroW, cw.normalMass2, twoPointMask);
                        cw.tangentMass2 = b2BlendW(zeroW, cw.tangentMass2, twoPointMask);
                    }

                    // Only sample contact point normal velocity if needed.
                    Vector4 restitutionMask = b2GreaterThanW(cw.restitution, zeroW);
                    bool haveRestitution = b2AnyTrueW(restitutionMask);
                    anyRestitution = anyRestitution || haveRestitution;

                    if ((hitEvents & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0 || haveRestitution)
                    {
                        B2BodyStateW bA = b2GatherBodies(states, cw.indexA.AsSpan());
                        B2BodyStateW bB = b2GatherBodies(states, cw.indexB.AsSpan());

                        {
                            B2Vec2W rA = cw.anchorA1;
                            B2Vec2W rB = cw.anchorB1;

                            // Relative velocity at contact
                            Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            Vector4 vn = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity1 = vn;

                            // Check the point count to avoid a race writing to b2_zeroContactSim.
                            if (l0.manifold.pointCount > 0) l0.manifold.points[0].normalVelocity = vn.X;
                            if (l1.manifold.pointCount > 0) l1.manifold.points[0].normalVelocity = vn.Y;
                            if (l2.manifold.pointCount > 0) l2.manifold.points[0].normalVelocity = vn.Z;
                            if (l3.manifold.pointCount > 0) l3.manifold.points[0].normalVelocity = vn.W;
                        }

                        {
                            B2Vec2W rA = cw.anchorA2;
                            B2Vec2W rB = cw.anchorB2;

                            // Relative velocity at contact
                            Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            Vector4 vn = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity2 = vn;

                            if (l0.manifold.pointCount > 1) l0.manifold.points[1].normalVelocity = vn.X;
                            if (l1.manifold.pointCount > 1) l1.manifold.points[1].normalVelocity = vn.Y;
                            if (l2.manifold.pointCount > 1) l2.manifold.points[1].normalVelocity = vn.Z;
                            if (l3.manifold.pointCount > 1) l3.manifold.points[1].normalVelocity = vn.W;
                        }
                    }
                }

                // Advance to next color
                colorIndex += 1;
            }

            if (anyRestitution)
            {
                b2AtomicStoreInt(ref context.anyRestitution, 1);
            }

            b2TracyCZoneEnd(B2TracyCZone.prepare_contact);
        }

        public override void WarmStart(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.warm_start_contact, "Warm Start", B2HexColor.b2_colorGreen, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = b2GetColorConstraints(context, colorIndex);

            for (int i = block.startIndex; i < block.startIndex + block.count; ++i)
            {
                ref B2ContactConstraintWide c = ref constraints[i];
                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector4 tangentX = c.normal.Y;
                Vector4 tangentY = b2SubW(b2ZeroW(), c.normal.X);

                {
                    // fixed anchors
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    B2Vec2W P;
                    P.X = b2AddW(b2MulW(c.normalImpulse1, c.normal.X), b2MulW(c.tangentImpulse1, tangentX));
                    P.Y = b2AddW(b2MulW(c.normalImpulse1, c.normal.Y), b2MulW(c.tangentImpulse1, tangentY));
                    bA.w = b2MulSubW(bA.w, c.invIA, b2CrossW(rA, P));
                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, P.X);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, P.Y);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2CrossW(rB, P));
                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, P.X);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, P.Y);

                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, c.normalImpulse1);
                }

                {
                    // fixed anchors
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    B2Vec2W P;
                    P.X = b2AddW(b2MulW(c.normalImpulse2, c.normal.X), b2MulW(c.tangentImpulse2, tangentX));
                    P.Y = b2AddW(b2MulW(c.normalImpulse2, c.normal.Y), b2MulW(c.tangentImpulse2, tangentY));
                    bA.w = b2MulSubW(bA.w, c.invIA, b2CrossW(rA, P));
                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, P.X);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, P.Y);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2CrossW(rB, P));
                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, P.X);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, P.Y);

                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, c.normalImpulse2);
                }

                bA.w = b2MulSubW(bA.w, c.invIA, c.rollingImpulse);
                bB.w = b2MulAddW(bB.w, c.invIB, c.rollingImpulse);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.warm_start_contact);
        }

        public override void Push(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = b2GetColorConstraints(context, colorIndex);
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 contactSpeed = b2SplatW(-context.world.contactSpeed);
            Vector4 oneW = b2SplatW(1.0f);
            Vector4 zeroW = b2ZeroW();

            Vector4 dynamicBiasRate = b2SplatW(context.contactSoftness.massScale * context.contactSoftness.biasRate);
            Vector4 dynamicMassScale = b2SplatW(context.contactSoftness.massScale);
            Vector4 dynamicImpulseScale = b2SplatW(context.contactSoftness.impulseScale);
            Vector4 staticBiasRate = b2SplatW(context.staticSoftness.massScale * context.staticSoftness.biasRate);
            Vector4 staticMassScale = b2SplatW(context.staticSoftness.massScale);
            Vector4 staticImpulseScale = b2SplatW(context.staticSoftness.impulseScale);

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector4 softMask = b2SoftMaskW(c.indexA.AsSpan(), c.indexB.AsSpan());
                Vector4 biasRate = b2BlendW(dynamicBiasRate, staticBiasRate, softMask);
                Vector4 massScale = b2BlendW(dynamicMassScale, staticMassScale, softMask);
                Vector4 impulseScale = b2BlendW(dynamicImpulseScale, staticImpulseScale, softMask);

                B2Vec2W normal = c.normal;
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector4 velocityBias1, massScale1, impulseScale1;
                {
                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Apply speculative bias if separation is greater than zero, otherwise apply soft constraint bias
                    // The contactSpeed is meant to limit stiffness, not increase it.
                    Vector4 separated = b2GreaterThanW(s, zeroW);

                    // Speculative bias - positive
                    Vector4 specBias = b2MulW(s, inv_h);

                    // Overlap bias - negative when overlapped
                    Vector4 overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);

                    // velocity bias = s > 0 ? spec : overlap
                    velocityBias1 = b2BlendW(overlapBias, specBias, separated);

                    massScale1 = b2BlendW(massScale, oneW, separated);
                    impulseScale1 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector4 velocityBias2, massScale2, impulseScale2;
                {
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    Vector4 separated = b2GreaterThanW(s, zeroW);
                    Vector4 specBias = b2MulW(s, inv_h);
                    Vector4 overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);
                    velocityBias2 = b2BlendW(overlapBias, specBias, separated);

                    massScale2 = b2BlendW(massScale, oneW, separated);
                    impulseScale2 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector4 rnA1 = b2CrossW(c.anchorA1, normal);
                Vector4 rnB1 = b2CrossW(c.anchorB1, normal);
                Vector4 rnA2 = b2CrossW(c.anchorA2, normal);
                Vector4 rnB2 = b2CrossW(c.anchorB2, normal);

                // Coupling of point 2 normal velocity to point 1 normal impulse
                Vector4 k21 = b2AddW(b2AddW(c.invMassA, c.invMassB),
                    b2AddW(b2MulW(b2MulW(c.invIA, rnA1), rnA2), b2MulW(b2MulW(c.invIB, rnB1), rnB2)));

                Vector4 dn = b2DotW(b2SubVW(bB.v, bA.v), normal);
                Vector4 vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector4 vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));

                Vector4 negImpulse1 = b2AddW(b2MulW(c.normalMass1, b2AddW(b2MulW(massScale1, vn1), velocityBias1)),
                    b2MulW(impulseScale1, c.normalImpulse1));
                Vector4 impulse1 = b2MaxW(b2NegW(negImpulse1), b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse1);

                vn2 = b2MulAddW(vn2, k21, impulse1);

                Vector4 negImpulse2 = b2AddW(b2MulW(c.normalMass2, b2AddW(b2MulW(massScale2, vn2), velocityBias2)),
                    b2MulW(impulseScale2, c.normalImpulse2));
                Vector4 impulse2 = b2MaxW(b2NegW(negImpulse2), b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse2);

                Vector4 impulse = b2AddW(impulse1, impulse2);
                Vector4 Px = b2MulW(impulse, normal.X);
                Vector4 Py = b2MulW(impulse, normal.Y);
                Vector4 angularA = b2AddW(b2MulW(impulse1, rnA1), b2MulW(impulse2, rnA2));
                Vector4 angularB = b2AddW(b2MulW(impulse1, rnB1), b2MulW(impulse2, rnB2));

                bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                bA.w = b2MulSubW(bA.w, c.invIA, angularA);

                bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                bB.w = b2MulAddW(bB.w, c.invIB, angularB);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        /// <summary>
        /// Solve the normal constraint, friction, and rolling resistance.
        /// </summary>
        public override void Solve(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.solve_contact, "Solve Contact", B2HexColor.b2_colorAliceBlue, true);

            B2BodyState[] states = context.states;
            int colorIndex = block.colorIndex;
            Span<B2ContactConstraintWide> constraints = b2GetColorConstraints(context, colorIndex);
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 zeroW = b2ZeroW();

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                B2Vec2W normal = c.normal;
                B2Vec2W tangent = b2RightPerpW(normal);
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector4 velocityBias1, velocityBias2;
                {
                    // Moving anchors for current separation
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Speculative bias, positive if separated and zero if overlapped
                    velocityBias1 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                {
                    B2Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    velocityBias2 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                Vector4 rnA1 = b2CrossW(c.anchorA1, normal);
                Vector4 rnB1 = b2CrossW(c.anchorB1, normal);
                Vector4 rnA2 = b2CrossW(c.anchorA2, normal);
                Vector4 rnB2 = b2CrossW(c.anchorB2, normal);
                Vector4 rtA1 = b2CrossW(c.anchorA1, tangent);
                Vector4 rtB1 = b2CrossW(c.anchorB1, tangent);
                Vector4 rtA2 = b2CrossW(c.anchorA2, tangent);
                Vector4 rtB2 = b2CrossW(c.anchorB2, tangent);

                Vector4 wnA1 = b2MulW(c.invIA, rnA1);
                Vector4 wnB1 = b2MulW(c.invIB, rnB1);
                Vector4 wnA2 = b2MulW(c.invIA, rnA2);
                Vector4 wnB2 = b2MulW(c.invIB, rnB2);
                Vector4 wtA1 = b2MulW(c.invIA, rtA1);
                Vector4 wtB1 = b2MulW(c.invIB, rtB1);

                // Couplings between the sequential impulses: rows are (n1, n2, t1, t2)
                Vector4 invMass = b2AddW(c.invMassA, c.invMassB);
                Vector4 k21 = b2AddW(invMass, b2AddW(b2MulW(wnA1, rnA2), b2MulW(wnB1, rnB2)));
                Vector4 k31 = b2AddW(b2MulW(wnA1, rtA1), b2MulW(wnB1, rtB1));
                Vector4 k32 = b2AddW(b2MulW(wnA2, rtA1), b2MulW(wnB2, rtB1));
                Vector4 k41 = b2AddW(b2MulW(wnA1, rtA2), b2MulW(wnB1, rtB2));
                Vector4 k42 = b2AddW(b2MulW(wnA2, rtA2), b2MulW(wnB2, rtB2));
                Vector4 k43 = b2AddW(invMass, b2AddW(b2MulW(wtA1, rtA2), b2MulW(wtB1, rtB2)));

                B2Vec2W dv = b2SubVW(bB.v, bA.v);
                Vector4 dn = b2DotW(dv, normal);
                Vector4 dt = b2SubW(b2DotW(dv, tangent), c.tangentSpeed);

                Vector4 vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector4 vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));
                Vector4 vt1 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB1), b2MulW(bA.w, rtA1)));
                Vector4 vt2 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB2), b2MulW(bA.w, rtA2)));

                Vector4 normalImpulse1 = b2MaxW(b2NegW(b2MulW(c.normalMass1, b2AddW(vn1, velocityBias1))),
                    b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, normalImpulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, normalImpulse1);

                vn2 = b2MulAddW(vn2, k21, normalImpulse1);

                Vector4 normalImpulse2 = b2MaxW(b2NegW(b2MulW(c.normalMass2, b2AddW(vn2, velocityBias2))),
                    b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, normalImpulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, normalImpulse2);

                vt1 = b2MulAddW(b2MulAddW(vt1, k31, normalImpulse1), k32, normalImpulse2);
                vt2 = b2MulAddW(b2MulAddW(vt2, k41, normalImpulse1), k42, normalImpulse2);

                Vector4 angularA = b2AddW(b2MulW(normalImpulse1, rnA1), b2MulW(normalImpulse2, rnA2));
                Vector4 angularB = b2AddW(b2MulW(normalImpulse1, rnB1), b2MulW(normalImpulse2, rnB2));

                // Rolling resistance
                if (b2AllZeroW(c.rollingResistance) == false)
                {
                    Vector4 relativeW = b2SubW(b2SubW(bA.w, bB.w), b2AddW(b2MulW(b2AddW(wnA1, wnB1), normalImpulse1),
                        b2MulW(b2AddW(wnA2, wnB2), normalImpulse2)));
                    Vector4 k = b2AddW(c.invIA, c.invIB);
                    Vector4 mask = b2GreaterThanW(k, zeroW);
                    Vector4 deltaLambda = b2BlendW(zeroW, b2DivW(relativeW, k), mask);
                    Vector4 lambda = c.rollingImpulse;
                    Vector4 maxLambda = b2MulW(c.rollingResistance, b2AddW(c.normalImpulse1, c.normalImpulse2));
                    c.rollingImpulse = b2SymClampW(b2AddW(lambda, deltaLambda), maxLambda);
                    deltaLambda = b2SubW(c.rollingImpulse, lambda);

                    Vector4 wtA2 = b2MulW(c.invIA, rtA2);
                    Vector4 wtB2 = b2MulW(c.invIB, rtB2);
                    vt1 = b2MulAddW(vt1, b2AddW(wtA1, wtB1), deltaLambda);
                    vt2 = b2MulAddW(vt2, b2AddW(wtA2, wtB2), deltaLambda);

                    angularA = b2AddW(angularA, deltaLambda);
                    angularB = b2AddW(angularB, deltaLambda);
                }

                Vector4 maxFriction1 = b2MulW(c.friction, c.normalImpulse1);
                Vector4 tangentImpulse1 = b2NegW(b2MulW(c.tangentMass1, vt1));
                tangentImpulse1 = b2MaxW(b2SubW(b2NegW(maxFriction1), c.tangentImpulse1),
                    b2MinW(tangentImpulse1, b2SubW(maxFriction1, c.tangentImpulse1)));
                c.tangentImpulse1 = b2AddW(c.tangentImpulse1, tangentImpulse1);

                vt2 = b2MulAddW(vt2, k43, tangentImpulse1);

                Vector4 maxFriction2 = b2MulW(c.friction, c.normalImpulse2);
                Vector4 tangentImpulse2 = b2NegW(b2MulW(c.tangentMass2, vt2));
                tangentImpulse2 = b2MaxW(b2SubW(b2NegW(maxFriction2), c.tangentImpulse2),
                    b2MinW(tangentImpulse2, b2SubW(maxFriction2, c.tangentImpulse2)));
                c.tangentImpulse2 = b2AddW(c.tangentImpulse2, tangentImpulse2);

                angularA = b2AddW(angularA, b2AddW(b2MulW(tangentImpulse1, rtA1), b2MulW(tangentImpulse2, rtA2)));
                angularB = b2AddW(angularB, b2AddW(b2MulW(tangentImpulse1, rtB1), b2MulW(tangentImpulse2, rtB2)));

                Vector4 normalImpulse = b2AddW(normalImpulse1, normalImpulse2);
                Vector4 tangentImpulse = b2AddW(tangentImpulse1, tangentImpulse2);
                Vector4 Px = b2AddW(b2MulW(normalImpulse, normal.X), b2MulW(tangentImpulse, tangent.X));
                Vector4 Py = b2AddW(b2MulW(normalImpulse, normal.Y), b2MulW(tangentImpulse, tangent.Y));

                bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                bA.w = b2MulSubW(bA.w, c.invIA, angularA);

                bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                bB.w = b2MulAddW(bB.w, c.invIB, angularB);

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.solve_contact);
        }

        public override void ApplyRestitution(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.restitution, "Restitution", B2HexColor.b2_colorDodgerBlue, true);

            B2BodyState[] states = context.states;
            Span<B2ContactConstraintWide> constraints = b2GetColorConstraints(context, block.colorIndex);
            Vector4 inv_h = b2SplatW(context.inv_h);
            Vector4 negRestitutionThreshold = b2SplatW(-context.world.restitutionThreshold);
            Vector4 zeroW = b2ZeroW();
            bool propagate = context.world.enableRestitutionPropagation;

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref B2ContactConstraintWide c = ref constraints[wideIndex];
                if (propagate == false && b2AllZeroW(c.restitution))
                {
                    continue;
                }

                B2BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                B2BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector4 restitutionMask = b2GreaterThanW(c.restitution, zeroW);
                Vector4 normalMass1 = propagate ? c.normalMass1 : b2BlendW(zeroW, c.normalMass1, restitutionMask);
                Vector4 normalMass2 = propagate ? c.normalMass2 : b2BlendW(zeroW, c.normalMass2, restitutionMask);
                B2Vec2W dp = new B2Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                {
                    B2Vec2W rA = c.anchorA1;
                    B2Vec2W rB = c.anchorB1;

                    Vector4 compressionImpulse = b2SubW(c.totalNormalImpulse1, c.restitutionImpulse1);
                    Vector4 armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity1, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation1);

                    Vector4 specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector4 velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity1), armed);

                    Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector4 vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector4 negImpulse = b2MulW(normalMass1, b2AddW(vn, velocityBias));

                    Vector4 newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), zeroW);
                    Vector4 impulse = b2SubW(newImpulse, c.normalImpulse1);

                    Vector4 approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass1, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector4 allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse1);
                    Vector4 maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse);
                    c.restitutionImpulse1 = b2AddW(c.restitutionImpulse1, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse);

                    Vector4 Px = b2MulW(impulse, c.normal.X);
                    Vector4 Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                {
                    B2Vec2W rA = c.anchorA2;
                    B2Vec2W rB = c.anchorB2;

                    Vector4 compressionImpulse = b2SubW(c.totalNormalImpulse2, c.restitutionImpulse2);
                    Vector4 armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity2, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    B2Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    B2Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    B2Vec2W ds = new B2Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector4 s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation2);

                    Vector4 specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector4 velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity2), armed);

                    Vector4 dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector4 dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector4 vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector4 negImpulse = b2MulW(normalMass2, b2AddW(vn, velocityBias));

                    Vector4 newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), zeroW);
                    Vector4 impulse = b2SubW(newImpulse, c.normalImpulse2);

                    Vector4 approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass2, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector4 allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse2);
                    Vector4 maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse);
                    c.restitutionImpulse2 = b2AddW(c.restitutionImpulse2, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse);

                    Vector4 Px = b2MulW(impulse, c.normal.X);
                    Vector4 Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                b2ScatterBodies(states, c.indexA.AsSpan(), ref bA);
                b2ScatterBodies(states, c.indexB.AsSpan(), ref bB);
            }

            b2TracyCZoneEnd(B2TracyCZone.restitution);
        }

        /// <summary>
        /// I tried adding this to the last relax iterations but it was slower.
        ///
        /// Runs as a flat parallel-for over the whole wide constraint range. Per-color
        /// contact sims are looked up through the prepareSpans cursor rather than the
        /// block's colorIndex, matching the layout of b2PrepareContacts_Wide.
        ///
        /// I could store the manifold pointer in the b2ContactConstraintWide to simplify
        /// this. But it is important to keep the constraint small.
        /// </summary>
        public override void StoreImpulses(B2SolverBlock block, B2StepContext context, int workerIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            B2World world = context.world;
            ReadOnlySpan<B2ContactPrepareSpan> spans = context.contactPrepareSpans;
            ReadOnlySpan<B2ContactConstraintWide> wideBase = wideConstraints;
            B2TaskContext taskContext = world.taskContexts.data[workerIndex];
            ref B2BitSet hitEventBitSet = ref taskContext.hitEventBitSet;
            bool hasHitEvents = taskContext.hasHitEvents;
            float negHitThreshold = -world.hitEventThreshold;

            int wideIndex = block.startIndex;
            int endWideIndex = block.startIndex + block.count;

            // Find color for start index
            int colorIndex = 0;
            while (spans[colorIndex + 1].start <= wideIndex)
            {
                colorIndex += 1;
            }

            while (wideIndex < endWideIndex)
            {
                int colorWideEndIndex = b2MinInt(spans[colorIndex + 1].start, endWideIndex);
                int colorWideStart = spans[colorIndex].start;
                int colorContactCount = spans[colorIndex].count;
                Span<B2ContactSim> contactSims = spans[colorIndex].contacts;

                for (; wideIndex < colorWideEndIndex; ++wideIndex)
                {
                    ref readonly B2ContactConstraintWide c = ref wideBase[wideIndex];
                    ref readonly Vector4 rollingImpulse = ref c.rollingImpulse;
                    ref readonly Vector4 normalImpulse1 = ref c.normalImpulse1;
                    ref readonly Vector4 normalImpulse2 = ref c.normalImpulse2;
                    ref readonly Vector4 tangentImpulse1 = ref c.tangentImpulse1;
                    ref readonly Vector4 tangentImpulse2 = ref c.tangentImpulse2;
                    ref readonly Vector4 totalNormalImpulse1 = ref c.totalNormalImpulse1;
                    ref readonly Vector4 totalNormalImpulse2 = ref c.totalNormalImpulse2;

                    int localWideIndex = wideIndex - colorWideStart;
                    int baseIndex = WIDTH * localWideIndex;

                    for (int laneIndex = 0; laneIndex < WIDTH; ++laneIndex)
                    {
                        int contactIndex = baseIndex + laneIndex;
                        if (contactIndex >= colorContactCount)
                        {
                            break;
                        }

                        B2ContactSim contactSim = contactSims[contactIndex];
                        ref B2Manifold m = ref contactSim.manifold;
                        m.rollingImpulse = b2GetLaneW(in rollingImpulse, laneIndex);

                        m.points[0].normalImpulse = b2GetLaneW(in normalImpulse1, laneIndex);
                        m.points[0].tangentImpulse = b2GetLaneW(in tangentImpulse1, laneIndex);
                        m.points[0].totalNormalImpulse = b2GetLaneW(in totalNormalImpulse1, laneIndex);

                        m.points[1].normalImpulse = b2GetLaneW(in normalImpulse2, laneIndex);
                        m.points[1].tangentImpulse = b2GetLaneW(in tangentImpulse2, laneIndex);
                        m.points[1].totalNormalImpulse = b2GetLaneW(in totalNormalImpulse2, laneIndex);

                        // Check for hit events to speed up serial processing later in the step
                        if ((contactSim.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0)
                        {
                            for (int k = 0; k < contactSim.manifold.pointCount; ++k)
                            {
                                ref B2ManifoldPoint mp = ref m.points[k];

                                // Need to check total impulse because the point may be speculative and not colliding
                                if (mp.normalVelocity < negHitThreshold && mp.totalNormalImpulse > 0.0f)
                                {
                                    b2SetBit(ref hitEventBitSet, contactSim.contactId);
                                    hasHitEvents = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Advance to next color
                colorIndex += 1;
            }

            taskContext.hasHitEvents = hasHitEvents;

            b2TracyCZoneEnd(B2TracyCZone.store_impulses);
        }
    }
}
