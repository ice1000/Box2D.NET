// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Box2D.NET.B2ArenaAllocators;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Profiling;
using static Box2D.NET.B2Atomics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2ConstraintGraphs;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2BitSets;

namespace Box2D.NET
{
    /// <summary>
    /// 8-wide contact solver on AVX2 (Vector256). Mirrors the C B2_SIMD_AVX2 build.
    /// The solver loops are the same as B2ContactSolverWide4 with the float vector type swapped.
    /// Within a graph color no two constraints share a dynamic body, so grouping contacts
    /// 8 at a time instead of 4 does not change any result.
    /// Only created when Avx2.IsSupported, so the gather/scatter uses AVX/AVX2 intrinsics directly.
    /// </summary>
    internal sealed class B2ContactSolverWide8 : B2ContactSolverWide
    {
        private const int WIDTH = 8;
        private const int SHIFT = 3;

        // The wide types are private to this width on purpose: the 4-wide solver has its own.

        private struct Vec2W
        {
            public Vector256<float> X;
            public Vector256<float> Y;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vec2W(in Vector256<float> x, in Vector256<float> y)
            {
                X = x;
                Y = y;
            }
        }

        private struct RotW
        {
            public Vector256<float> C;
            public Vector256<float> S;
        }

        private struct BodyStateW
        {
            public Vec2W v;
            public Vector256<float> w;
            public Vector256<float> flags;
            public Vec2W dp;
            public RotW dq;
        }

        /// <summary>
        /// Same fields as B2ContactConstraintWide, 8 lanes each
        /// </summary>
        private struct ConstraintW
        {
            public B2FixedArray8<int> indexA;
            public B2FixedArray8<int> indexB;

            public Vector256<float> invMassA, invMassB;
            public Vector256<float> invIA, invIB;
            public Vec2W normal;
            public Vec2W anchorA1, anchorB1;
            public Vec2W anchorA2, anchorB2;
            public Vector256<float> normalMass1, normalMass2;
            public Vector256<float> baseSeparation1;
            public Vector256<float> baseSeparation2;

            public Vector256<float> normalImpulse1, normalImpulse2;
            public Vector256<float> totalNormalImpulse1, totalNormalImpulse2;

            public Vector256<float> tangentImpulse1, tangentImpulse2;
            public Vector256<float> rollingImpulse;

            public Vector256<float> friction;
            public Vector256<float> tangentSpeed;
            public Vector256<float> rollingResistance;
            public Vector256<float> tangentMass1, tangentMass2;
            public Vector256<float> restitution;
            public Vector256<float> relativeVelocity1, relativeVelocity2;
            public Vector256<float> restitutionImpulse1, restitutionImpulse2;
        }

        private ArraySegment<ConstraintW> wideConstraints;

        public override int Width => WIDTH;
        public override int Shift => SHIFT;

        public override void Allocate(B2StackAllocator stack, int wideContactCount)
        {
            B2_ASSERT(B2FixedArray8<int>.Size == WIDTH);
            wideConstraints = b2StackAlloc<ConstraintW>(stack, wideContactCount, "contact constraint");
        }

        public override void Free(B2StackAllocator stack)
        {
            b2StackFree(stack, wideConstraints);
            wideConstraints = default;
        }

        public override void ClearConstraint(int wideIndex)
        {
            wideConstraints[wideIndex] = new ConstraintW();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<ConstraintW> b2GetColorConstraints(B2StepContext context, int colorIndex)
        {
            ref B2GraphColor color = ref context.graph.colors[colorIndex];
            return wideConstraints.AsSpan(color.wideConstraintStart, color.wideConstraintCount);
        }

        // Float helpers. Same semantics as the Vector4 versions in B2ContactSolvers, so both widths
        // give the same bits: no FMA, and min/max/clamp use compare + select instead of IEEE min/max.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2ZeroW() => Vector256<float>.Zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2SplatW(float scalar) => Vector256.Create(scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2NegW(in Vector256<float> a) => Vector256.Negate(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2AddW(in Vector256<float> a, in Vector256<float> b) => a + b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2SubW(in Vector256<float> a, in Vector256<float> b) => a - b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2MulW(in Vector256<float> a, in Vector256<float> b) => a * b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2DivW(in Vector256<float> a, in Vector256<float> b) => a / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2MulAddW(in Vector256<float> a, in Vector256<float> b, in Vector256<float> c) => a + b * c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2MulSubW(in Vector256<float> a, in Vector256<float> b, in Vector256<float> c) => a - b * c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2MinW(in Vector256<float> a, in Vector256<float> b)
        {
            return Vector256.ConditionalSelect(Vector256.LessThanOrEqual(a, b), a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2MaxW(in Vector256<float> a, in Vector256<float> b)
        {
            return Vector256.ConditionalSelect(Vector256.GreaterThanOrEqual(a, b), a, b);
        }

        /// <summary>
        /// a = clamp(a, -b, b), same as b2ClampFloat
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2SymClampW(in Vector256<float> a, in Vector256<float> b)
        {
            Vector256<float> lower = Vector256.Xor(b, Vector256.Create(-0.0f));
            Vector256<float> r = Vector256.ConditionalSelect(Vector256.GreaterThan(a, b), b, a);
            return Vector256.ConditionalSelect(Vector256.LessThan(a, lower), lower, r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2AndW(in Vector256<float> a, in Vector256<float> b) => a & b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2GreaterThanW(in Vector256<float> a, in Vector256<float> b) => Vector256.GreaterThan(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2LessThanW(in Vector256<float> a, in Vector256<float> b) => Vector256.LessThan(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2AnyTrueW(in Vector256<float> mask) => Vector256.ExtractMostSignificantBits(mask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool b2AllZeroW(in Vector256<float> a) => Vector256.EqualsAll(a, Vector256<float>.Zero);

        /// <summary>
        /// component-wise returns mask ? b : a
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2BlendW(in Vector256<float> a, in Vector256<float> b, in Vector256<float> mask)
        {
            return Vector256.ConditionalSelect(mask, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2SoftMaskW(ReadOnlySpan<int> indexA, ReadOnlySpan<int> indexB)
        {
            Vector256<int> zero = Vector256<int>.Zero;
            Vector256<int> mask = Vector256.Equals(Vector256.Create(indexA), zero) | Vector256.Equals(Vector256.Create(indexB), zero);
            return mask.AsSingle();
        }

        /// <summary>
        /// One lane as a float in memory, for reading and writing lanes with a variable index
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref float b2LaneW(ref Vector256<float> v, int laneIndex)
        {
            B2_ASSERT(0 <= laneIndex && laneIndex < WIDTH);
            return ref Unsafe.Add(ref Unsafe.As<Vector256<float>, float>(ref v), laneIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float b2GetLaneW(in Vector256<float> v, int laneIndex)
        {
            return b2LaneW(ref Unsafe.AsRef(in v), laneIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vec2W b2SubVW(in Vec2W a, in Vec2W b) => new Vec2W(b2SubW(a.X, b.X), b2SubW(a.Y, b.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vec2W b2RightPerpW(in Vec2W a) => new Vec2W(a.Y, b2NegW(a.X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2DotW(in Vec2W a, in Vec2W b) => b2AddW(b2MulW(a.X, b.X), b2MulW(a.Y, b.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2CrossW(in Vec2W a, in Vec2W b) => b2SubW(b2MulW(a.X, b.Y), b2MulW(a.Y, b.X));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vec2W b2RotateVectorW(in RotW q, in Vec2W v)
        {
            return new Vec2W(b2SubW(b2MulW(q.C, v.X), b2MulW(q.S, v.Y)), b2AddW(b2MulW(q.S, v.X), b2MulW(q.C, v.Y)));
        }

        // _MM_SHUFFLE(1, 0, 1, 0) and _MM_SHUFFLE(3, 2, 3, 2)
        private const byte SHUFFLE_1010 = 0x44;
        private const byte SHUFFLE_3232 = 0xEE;

        /// <summary>
        /// A whole body state is 32 bytes: (v.x, v.y, w, flags, dp.x, dp.y, dq.c, dq.s)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<float> b2LoadBody(ReadOnlySpan<B2BodyState> states, int index)
        {
            // zero means null
            int i = index - 1;
            ref readonly B2BodyState s = ref i == B2_NULL_INDEX ? ref b2_identityBodyState : ref states[i];
            return Unsafe.ReadUnaligned<Vector256<float>>(ref Unsafe.As<B2BodyState, byte>(ref Unsafe.AsRef(in s)));
        }

        /// <summary>
        /// This is a load and 8x8 transpose
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BodyStateW b2GatherBodies(ReadOnlySpan<B2BodyState> states, ReadOnlySpan<int> indices)
        {
            B2_ASSERT(Unsafe.SizeOf<B2BodyState>() == 32);

            Vector256<float> b0 = b2LoadBody(states, indices[0]);
            Vector256<float> b1 = b2LoadBody(states, indices[1]);
            Vector256<float> b2 = b2LoadBody(states, indices[2]);
            Vector256<float> b3 = b2LoadBody(states, indices[3]);
            Vector256<float> b4 = b2LoadBody(states, indices[4]);
            Vector256<float> b5 = b2LoadBody(states, indices[5]);
            Vector256<float> b6 = b2LoadBody(states, indices[6]);
            Vector256<float> b7 = b2LoadBody(states, indices[7]);

            Vector256<float> t0 = Avx.UnpackLow(b0, b1);
            Vector256<float> t1 = Avx.UnpackHigh(b0, b1);
            Vector256<float> t2 = Avx.UnpackLow(b2, b3);
            Vector256<float> t3 = Avx.UnpackHigh(b2, b3);
            Vector256<float> t4 = Avx.UnpackLow(b4, b5);
            Vector256<float> t5 = Avx.UnpackHigh(b4, b5);
            Vector256<float> t6 = Avx.UnpackLow(b6, b7);
            Vector256<float> t7 = Avx.UnpackHigh(b6, b7);
            Vector256<float> tt0 = Avx.Shuffle(t0, t2, SHUFFLE_1010);
            Vector256<float> tt1 = Avx.Shuffle(t0, t2, SHUFFLE_3232);
            Vector256<float> tt2 = Avx.Shuffle(t1, t3, SHUFFLE_1010);
            Vector256<float> tt3 = Avx.Shuffle(t1, t3, SHUFFLE_3232);
            Vector256<float> tt4 = Avx.Shuffle(t4, t6, SHUFFLE_1010);
            Vector256<float> tt5 = Avx.Shuffle(t4, t6, SHUFFLE_3232);
            Vector256<float> tt6 = Avx.Shuffle(t5, t7, SHUFFLE_1010);
            Vector256<float> tt7 = Avx.Shuffle(t5, t7, SHUFFLE_3232);

            BodyStateW simdBody;
            simdBody.v.X = Avx.Permute2x128(tt0, tt4, 0x20);
            simdBody.v.Y = Avx.Permute2x128(tt1, tt5, 0x20);
            simdBody.w = Avx.Permute2x128(tt2, tt6, 0x20);
            // raw flag bits, never used as numbers
            simdBody.flags = Avx.Permute2x128(tt3, tt7, 0x20);
            simdBody.dp.X = Avx.Permute2x128(tt0, tt4, 0x31);
            simdBody.dp.Y = Avx.Permute2x128(tt1, tt5, 0x31);
            simdBody.dq.C = Avx.Permute2x128(tt2, tt6, 0x31);
            simdBody.dq.S = Avx.Permute2x128(tt3, tt7, 0x31);
            return simdBody;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2StoreBody(ref B2BodyState s0, int count, uint dynamicMask, int lane, ReadOnlySpan<int> indices, Vector128<float> value)
        {
            // Null lanes gathered the identity body, whose flags are zero, so they are never stored
            if ((dynamicMask & (1u << lane)) != 0)
            {
                // zero means null
                int i = indices[lane] - 1;
                B2_ASSERT(0 <= i && i < count);
                Unsafe.WriteUnaligned(ref Unsafe.As<B2BodyState, byte>(ref Unsafe.Add(ref s0, i)), value);
            }
        }

        /// <summary>
        /// This writes only the velocities back to the solver bodies (plus the unchanged flags in the same 16 bytes),
        /// like the 4-wide solver. The C AVX2 version transposes and stores all 32 bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void b2ScatterBodies(Span<B2BodyState> states, ReadOnlySpan<int> indices, ref BodyStateW simdBody)
        {
            Vector256<float> t0 = Avx.UnpackLow(simdBody.v.X, simdBody.v.Y);
            Vector256<float> t1 = Avx.UnpackHigh(simdBody.v.X, simdBody.v.Y);
            Vector256<float> t2 = Avx.UnpackLow(simdBody.w, simdBody.flags);
            Vector256<float> t3 = Avx.UnpackHigh(simdBody.w, simdBody.flags);

            // 128-bit lane 0 of ttk holds (v.x, v.y, w, flags) of body k, lane 1 holds body k + 4
            Vector256<float> tt0 = Avx.Shuffle(t0, t2, SHUFFLE_1010);
            Vector256<float> tt1 = Avx.Shuffle(t0, t2, SHUFFLE_3232);
            Vector256<float> tt2 = Avx.Shuffle(t1, t3, SHUFFLE_1010);
            Vector256<float> tt3 = Avx.Shuffle(t1, t3, SHUFFLE_3232);

            // I don't use any dummy body in the body array because this will lead to multithreaded sharing and the
            // associated cache flushing.
            // Dynamic flag of each lane from the gathered flags, so the flags are not reloaded from memory
            const int dynamicBit = 9;
            B2_ASSERT((uint)B2BodyFlags.b2_dynamicFlag == 1u << dynamicBit);
            uint dynamicMask = Vector256.ExtractMostSignificantBits(Vector256.ShiftLeft(simdBody.flags.AsInt32(), 31 - dynamicBit));

            ref B2BodyState s0 = ref MemoryMarshal.GetReference(states);
            int count = states.Length;
            b2StoreBody(ref s0, count, dynamicMask, 0, indices, tt0.GetLower());
            b2StoreBody(ref s0, count, dynamicMask, 1, indices, tt1.GetLower());
            b2StoreBody(ref s0, count, dynamicMask, 2, indices, tt2.GetLower());
            b2StoreBody(ref s0, count, dynamicMask, 3, indices, tt3.GetLower());
            b2StoreBody(ref s0, count, dynamicMask, 4, indices, tt0.GetUpper());
            b2StoreBody(ref s0, count, dynamicMask, 5, indices, tt1.GetUpper());
            b2StoreBody(ref s0, count, dynamicMask, 6, indices, tt2.GetUpper());
            b2StoreBody(ref s0, count, dynamicMask, 7, indices, tt3.GetUpper());
        }

        private static readonly B2ContactSim b2_zeroContactSim = new B2ContactSim();

        /// <summary>
        /// Prepare wide contact constraints. Lanes are filled one contact at a time, like the C version.
        /// </summary>
        public override void Prepare(B2SolverBlock block, B2StepContext context)
        {
            b2TracyCZoneNC(B2TracyCZone.prepare_contact, "Prepare Contact", B2HexColor.b2_colorYellow, true);
            B2World world = context.world;
            B2BodyState[] states = context.states;
            B2ContactPrepareSpan[] spans = context.contactPrepareSpans;
            Span<ConstraintW> wideBase = wideConstraints;

#if DEBUG
            B2Body[] bodies = world.bodies.data;
#endif

            Vector256<float> zeroW = b2ZeroW();
            Vector256<float> oneW = b2SplatW(1.0f);
            Vector256<float> warmStartScale = world.enableWarmStarting ? oneW : zeroW;
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
                    ref ConstraintW cw = ref wideBase[wideIndex];
                    int laneBase = WIDTH * (wideIndex - colorWideStart);
                    uint hitEvents = 0;
                    uint toiFlags = 0;
                    Vector256<float> pointCounts = zeroW;

                    // Separation tweak to make contacts more rigid after a TOI event.
                    Vector256<float> separationFloor = b2SplatW(-float.MaxValue);

                    for (int j = 0; j < WIDTH; ++j)
                    {
                        int contactIndex = laneBase + j;
                        B2ContactSim c;
                        if (contactIndex < colorContactCount)
                        {
                            c = contactSims[contactIndex];

                            // index base-1
                            cw.indexA[j] = b2DecodeAwakeIndex(c.encodedBodySimA) + 1;
                            cw.indexB[j] = b2DecodeAwakeIndex(c.encodedBodySimB) + 1;
                            hitEvents |= (c.simFlags & (uint)B2ContactFlags.b2_simEnableHitEvent);
                            toiFlags |= (c.simFlags & (uint)B2ContactFlags.b2_simHadTimeOfImpact);
                            if ((c.simFlags & (uint)B2ContactFlags.b2_simHadTimeOfImpact) != 0)
                            {
                                b2LaneW(ref separationFloor, j) = B2ContactSolvers.B2_TOI_RIGID_SEPARATION;
                            }

#if DEBUG
                            B2Body bodyA = bodies[c.bodyIdA];
                            B2Body bodyB = bodies[c.bodyIdB];
                            B2_ASSERT(c.encodedBodySimA == b2EncodeBodySimIndex(bodyA));
                            B2_ASSERT(c.encodedBodySimB == b2EncodeBodySimIndex(bodyB));
#endif
                        }
                        else
                        {
                            // Unused lanes read zeros, like the 4-wide solver. Their indices were cleared in setup.
                            c = b2_zeroContactSim;
                        }

                        b2LaneW(ref cw.invMassA, j) = c.invMassA;
                        b2LaneW(ref cw.invMassB, j) = c.invMassB;
                        b2LaneW(ref cw.invIA, j) = c.invIA;
                        b2LaneW(ref cw.invIB, j) = c.invIB;
                        b2LaneW(ref cw.normal.X, j) = c.manifold.normal.X;
                        b2LaneW(ref cw.normal.Y, j) = c.manifold.normal.Y;
                        b2LaneW(ref cw.friction, j) = c.friction;
                        b2LaneW(ref cw.tangentSpeed, j) = c.tangentSpeed;
                        b2LaneW(ref cw.rollingResistance, j) = c.rollingResistance;
                        b2LaneW(ref cw.rollingImpulse, j) = c.manifold.rollingImpulse;
                        b2LaneW(ref cw.restitution, j) = c.restitution;
                        b2LaneW(ref pointCounts, j) = c.manifold.pointCount;

                        // anchorA, anchorB, separation, normalImpulse, tangentImpulse
                        ref B2ManifoldPoint m1 = ref c.manifold.points[0];
                        b2LaneW(ref cw.anchorA1.X, j) = m1.anchorA.X;
                        b2LaneW(ref cw.anchorA1.Y, j) = m1.anchorA.Y;
                        b2LaneW(ref cw.anchorB1.X, j) = m1.anchorB.X;
                        b2LaneW(ref cw.anchorB1.Y, j) = m1.anchorB.Y;
                        b2LaneW(ref cw.baseSeparation1, j) = m1.separation;
                        b2LaneW(ref cw.normalImpulse1, j) = m1.normalImpulse;
                        b2LaneW(ref cw.tangentImpulse1, j) = m1.tangentImpulse;

                        ref B2ManifoldPoint m2 = ref c.manifold.points[1];
                        b2LaneW(ref cw.anchorA2.X, j) = m2.anchorA.X;
                        b2LaneW(ref cw.anchorA2.Y, j) = m2.anchorA.Y;
                        b2LaneW(ref cw.anchorB2.X, j) = m2.anchorB.X;
                        b2LaneW(ref cw.anchorB2.Y, j) = m2.anchorB.Y;
                        b2LaneW(ref cw.baseSeparation2, j) = m2.separation;
                        b2LaneW(ref cw.normalImpulse2, j) = m2.normalImpulse;
                        b2LaneW(ref cw.tangentImpulse2, j) = m2.tangentImpulse;
                    }

                    cw.rollingImpulse = b2MulW(warmStartScale, cw.rollingImpulse);

                    Vec2W tangent = b2RightPerpW(cw.normal);
                    Vector256<float> twoPointMask = b2GreaterThanW(pointCounts, oneW);

                    {
                        if (toiFlags != 0)
                        {
                            Vector256<float> separation = cw.baseSeparation1;
                            Vector256<float> liftMask = b2GreaterThanW(separation, b2SplatW(B2ContactSolvers.B2_TOI_RIGID_SEPARATION_LIMIT));
                            cw.baseSeparation1 = b2BlendW(separation, b2MaxW(separation, separationFloor), liftMask);
                        }

                        Vector256<float> offset = b2DotW(b2SubVW(cw.anchorB1, cw.anchorA1), cw.normal);
                        cw.baseSeparation1 = b2SubW(cw.baseSeparation1, offset);
                        cw.normalImpulse1 = b2MulW(warmStartScale, cw.normalImpulse1);
                        cw.tangentImpulse1 = b2MulW(warmStartScale, cw.tangentImpulse1);
                        cw.totalNormalImpulse1 = zeroW;
                        cw.relativeVelocity1 = zeroW;
                        cw.restitutionImpulse1 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector256<float> rnA = b2CrossW(cw.anchorA1, cw.normal);
                            Vector256<float> rnB = b2CrossW(cw.anchorB1, cw.normal);
                            Vector256<float> k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector256<float> test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector256<float> rtA = b2CrossW(cw.anchorA1, tangent);
                            Vector256<float> rtB = b2CrossW(cw.anchorB1, tangent);
                            Vector256<float> k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector256<float> test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass1 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }
                    }

                    {
                        if (toiFlags != 0)
                        {
                            Vector256<float> separation = cw.baseSeparation2;
                            Vector256<float> liftMask = b2GreaterThanW(separation, b2SplatW(B2ContactSolvers.B2_TOI_RIGID_SEPARATION_LIMIT));
                            cw.baseSeparation2 = b2BlendW(separation, b2MaxW(separation, separationFloor), liftMask);
                        }

                        Vector256<float> offset = b2DotW(b2SubVW(cw.anchorB2, cw.anchorA2), cw.normal);
                        cw.baseSeparation2 = b2SubW(cw.baseSeparation2, offset);
                        cw.normalImpulse2 = b2MulW(warmStartScale, cw.normalImpulse2);
                        cw.tangentImpulse2 = b2MulW(warmStartScale, cw.tangentImpulse2);
                        cw.totalNormalImpulse2 = zeroW;
                        cw.relativeVelocity2 = zeroW;
                        cw.restitutionImpulse2 = zeroW;

                        // Effective mass along normal.
                        {
                            Vector256<float> rnA = b2CrossW(cw.anchorA2, cw.normal);
                            Vector256<float> rnB = b2CrossW(cw.anchorB2, cw.normal);
                            Vector256<float> k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rnA, rnA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rnB, rnB));
                            Vector256<float> test = b2GreaterThanW(k, b2ZeroW());
                            cw.normalMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Effective mass along tangent.
                        {
                            Vector256<float> rtA = b2CrossW(cw.anchorA2, tangent);
                            Vector256<float> rtB = b2CrossW(cw.anchorB2, tangent);
                            Vector256<float> k = b2AddW(cw.invMassA, cw.invMassB);
                            k = b2MulAddW(k, cw.invIA, b2MulW(rtA, rtA));
                            k = b2MulAddW(k, cw.invIB, b2MulW(rtB, rtB));
                            Vector256<float> test = b2GreaterThanW(k, b2ZeroW());
                            cw.tangentMass2 = b2BlendW(b2ZeroW(), b2DivW(oneW, k), test);
                        }

                        // Zero effective mass on lanes that only have one point.
                        cw.normalMass2 = b2BlendW(zeroW, cw.normalMass2, twoPointMask);
                        cw.tangentMass2 = b2BlendW(zeroW, cw.tangentMass2, twoPointMask);
                    }

                    // Only sample contact point normal velocity if needed.
                    Vector256<float> restitutionMask = b2GreaterThanW(cw.restitution, zeroW);
                    bool haveRestitution = b2AnyTrueW(restitutionMask);
                    anyRestitution = anyRestitution || haveRestitution;

                    if ((hitEvents & (uint)B2ContactFlags.b2_simEnableHitEvent) != 0 || haveRestitution)
                    {
                        BodyStateW bA = b2GatherBodies(states, cw.indexA.AsSpan());
                        BodyStateW bB = b2GatherBodies(states, cw.indexB.AsSpan());

                        Vector256<float> vn1, vn2;
                        {
                            Vec2W rA = cw.anchorA1;
                            Vec2W rB = cw.anchorB1;

                            // Relative velocity at contact
                            Vector256<float> dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector256<float> dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            vn1 = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity1 = vn1;
                        }

                        {
                            Vec2W rA = cw.anchorA2;
                            Vec2W rB = cw.anchorB2;

                            // Relative velocity at contact
                            Vector256<float> dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                            Vector256<float> dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                            vn2 = b2AddW(b2MulW(dvx, cw.normal.X), b2MulW(dvy, cw.normal.Y));
                            cw.relativeVelocity2 = vn2;
                        }

                        // Only real contacts, so nothing is written to b2_zeroContactSim.
                        for (int j = 0; j < WIDTH; ++j)
                        {
                            int contactIndex = laneBase + j;
                            if (contactIndex >= colorContactCount)
                            {
                                break;
                            }

                            ref B2Manifold m = ref contactSims[contactIndex].manifold;
                            if (m.pointCount > 0) m.points[0].normalVelocity = b2GetLaneW(vn1, j);
                            if (m.pointCount > 1) m.points[1].normalVelocity = b2GetLaneW(vn2, j);
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
            Span<ConstraintW> constraints = b2GetColorConstraints(context, colorIndex);

            for (int i = block.startIndex; i < block.startIndex + block.count; ++i)
            {
                ref ConstraintW c = ref constraints[i];
                BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector256<float> tangentX = c.normal.Y;
                Vector256<float> tangentY = b2SubW(b2ZeroW(), c.normal.X);

                {
                    // fixed anchors
                    Vec2W rA = c.anchorA1;
                    Vec2W rB = c.anchorB1;

                    Vec2W P;
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
                    Vec2W rA = c.anchorA2;
                    Vec2W rB = c.anchorB2;

                    Vec2W P;
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
            Span<ConstraintW> constraints = b2GetColorConstraints(context, colorIndex);
            Vector256<float> inv_h = b2SplatW(context.inv_h);
            Vector256<float> contactSpeed = b2SplatW(-context.world.contactSpeed);
            Vector256<float> oneW = b2SplatW(1.0f);
            Vector256<float> zeroW = b2ZeroW();

            Vector256<float> dynamicBiasRate = b2SplatW(context.contactSoftness.massScale * context.contactSoftness.biasRate);
            Vector256<float> dynamicMassScale = b2SplatW(context.contactSoftness.massScale);
            Vector256<float> dynamicImpulseScale = b2SplatW(context.contactSoftness.impulseScale);
            Vector256<float> staticBiasRate = b2SplatW(context.staticSoftness.massScale * context.staticSoftness.biasRate);
            Vector256<float> staticMassScale = b2SplatW(context.staticSoftness.massScale);
            Vector256<float> staticImpulseScale = b2SplatW(context.staticSoftness.impulseScale);

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref ConstraintW c = ref constraints[wideIndex];

                BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector256<float> softMask = b2SoftMaskW(c.indexA.AsSpan(), c.indexB.AsSpan());
                Vector256<float> biasRate = b2BlendW(dynamicBiasRate, staticBiasRate, softMask);
                Vector256<float> massScale = b2BlendW(dynamicMassScale, staticMassScale, softMask);
                Vector256<float> impulseScale = b2BlendW(dynamicImpulseScale, staticImpulseScale, softMask);

                Vec2W normal = c.normal;
                Vec2W dp = new Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector256<float> velocityBias1, massScale1, impulseScale1;
                {
                    // Moving anchors for current separation
                    Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Apply speculative bias if separation is greater than zero, otherwise apply soft constraint bias
                    // The contactSpeed is meant to limit stiffness, not increase it.
                    Vector256<float> separated = b2GreaterThanW(s, zeroW);

                    // Speculative bias - positive
                    Vector256<float> specBias = b2MulW(s, inv_h);

                    // Overlap bias - negative when overlapped
                    Vector256<float> overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);

                    // velocity bias = s > 0 ? spec : overlap
                    velocityBias1 = b2BlendW(overlapBias, specBias, separated);

                    massScale1 = b2BlendW(massScale, oneW, separated);
                    impulseScale1 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector256<float> velocityBias2, massScale2, impulseScale2;
                {
                    Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    Vector256<float> separated = b2GreaterThanW(s, zeroW);
                    Vector256<float> specBias = b2MulW(s, inv_h);
                    Vector256<float> overlapBias = b2MaxW(b2MulW(biasRate, s), contactSpeed);
                    velocityBias2 = b2BlendW(overlapBias, specBias, separated);

                    massScale2 = b2BlendW(massScale, oneW, separated);
                    impulseScale2 = b2BlendW(impulseScale, zeroW, separated);
                }

                Vector256<float> rnA1 = b2CrossW(c.anchorA1, normal);
                Vector256<float> rnB1 = b2CrossW(c.anchorB1, normal);
                Vector256<float> rnA2 = b2CrossW(c.anchorA2, normal);
                Vector256<float> rnB2 = b2CrossW(c.anchorB2, normal);

                // Coupling of point 2 normal velocity to point 1 normal impulse
                Vector256<float> k21 = b2AddW(b2AddW(c.invMassA, c.invMassB),
                    b2AddW(b2MulW(b2MulW(c.invIA, rnA1), rnA2), b2MulW(b2MulW(c.invIB, rnB1), rnB2)));

                Vector256<float> dn = b2DotW(b2SubVW(bB.v, bA.v), normal);
                Vector256<float> vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector256<float> vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));

                Vector256<float> negImpulse1 = b2AddW(b2MulW(c.normalMass1, b2AddW(b2MulW(massScale1, vn1), velocityBias1)),
                    b2MulW(impulseScale1, c.normalImpulse1));
                Vector256<float> impulse1 = b2MaxW(b2NegW(negImpulse1), b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse1);

                vn2 = b2MulAddW(vn2, k21, impulse1);

                Vector256<float> negImpulse2 = b2AddW(b2MulW(c.normalMass2, b2AddW(b2MulW(massScale2, vn2), velocityBias2)),
                    b2MulW(impulseScale2, c.normalImpulse2));
                Vector256<float> impulse2 = b2MaxW(b2NegW(negImpulse2), b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse2);

                Vector256<float> impulse = b2AddW(impulse1, impulse2);
                Vector256<float> Px = b2MulW(impulse, normal.X);
                Vector256<float> Py = b2MulW(impulse, normal.Y);
                Vector256<float> angularA = b2AddW(b2MulW(impulse1, rnA1), b2MulW(impulse2, rnA2));
                Vector256<float> angularB = b2AddW(b2MulW(impulse1, rnB1), b2MulW(impulse2, rnB2));

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
            Span<ConstraintW> constraints = b2GetColorConstraints(context, colorIndex);
            Vector256<float> inv_h = b2SplatW(context.inv_h);
            Vector256<float> zeroW = b2ZeroW();

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref ConstraintW c = ref constraints[wideIndex];

                BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vec2W normal = c.normal;
                Vec2W tangent = b2RightPerpW(normal);
                Vec2W dp = new Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                Vector256<float> velocityBias1, velocityBias2;
                {
                    // Moving anchors for current separation
                    Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA1);
                    Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB1);

                    // compute current separation
                    // this is subject to round-off error if the anchor is far from the body center of mass
                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(normal, ds), c.baseSeparation1);

                    // Speculative bias, positive if separated and zero if overlapped
                    velocityBias1 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                {
                    Vec2W rsA = b2RotateVectorW(bA.dq, c.anchorA2);
                    Vec2W rsB = b2RotateVectorW(bB.dq, c.anchorB2);

                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(normal, ds), c.baseSeparation2);

                    velocityBias2 = b2MaxW(zeroW, b2MulW(s, inv_h));
                }

                Vector256<float> rnA1 = b2CrossW(c.anchorA1, normal);
                Vector256<float> rnB1 = b2CrossW(c.anchorB1, normal);
                Vector256<float> rnA2 = b2CrossW(c.anchorA2, normal);
                Vector256<float> rnB2 = b2CrossW(c.anchorB2, normal);
                Vector256<float> rtA1 = b2CrossW(c.anchorA1, tangent);
                Vector256<float> rtB1 = b2CrossW(c.anchorB1, tangent);
                Vector256<float> rtA2 = b2CrossW(c.anchorA2, tangent);
                Vector256<float> rtB2 = b2CrossW(c.anchorB2, tangent);

                Vector256<float> wnA1 = b2MulW(c.invIA, rnA1);
                Vector256<float> wnB1 = b2MulW(c.invIB, rnB1);
                Vector256<float> wnA2 = b2MulW(c.invIA, rnA2);
                Vector256<float> wnB2 = b2MulW(c.invIB, rnB2);
                Vector256<float> wtA1 = b2MulW(c.invIA, rtA1);
                Vector256<float> wtB1 = b2MulW(c.invIB, rtB1);

                // Couplings between the sequential impulses: rows are (n1, n2, t1, t2)
                Vector256<float> invMass = b2AddW(c.invMassA, c.invMassB);
                Vector256<float> k21 = b2AddW(invMass, b2AddW(b2MulW(wnA1, rnA2), b2MulW(wnB1, rnB2)));
                Vector256<float> k31 = b2AddW(b2MulW(wnA1, rtA1), b2MulW(wnB1, rtB1));
                Vector256<float> k32 = b2AddW(b2MulW(wnA2, rtA1), b2MulW(wnB2, rtB1));
                Vector256<float> k41 = b2AddW(b2MulW(wnA1, rtA2), b2MulW(wnB1, rtB2));
                Vector256<float> k42 = b2AddW(b2MulW(wnA2, rtA2), b2MulW(wnB2, rtB2));
                Vector256<float> k43 = b2AddW(invMass, b2AddW(b2MulW(wtA1, rtA2), b2MulW(wtB1, rtB2)));

                Vec2W dv = b2SubVW(bB.v, bA.v);
                Vector256<float> dn = b2DotW(dv, normal);
                Vector256<float> dt = b2SubW(b2DotW(dv, tangent), c.tangentSpeed);

                Vector256<float> vn1 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB1), b2MulW(bA.w, rnA1)));
                Vector256<float> vn2 = b2AddW(dn, b2SubW(b2MulW(bB.w, rnB2), b2MulW(bA.w, rnA2)));
                Vector256<float> vt1 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB1), b2MulW(bA.w, rtA1)));
                Vector256<float> vt2 = b2AddW(dt, b2SubW(b2MulW(bB.w, rtB2), b2MulW(bA.w, rtA2)));

                Vector256<float> normalImpulse1 = b2MaxW(b2NegW(b2MulW(c.normalMass1, b2AddW(vn1, velocityBias1))),
                    b2NegW(c.normalImpulse1));
                c.normalImpulse1 = b2AddW(c.normalImpulse1, normalImpulse1);
                c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, normalImpulse1);

                vn2 = b2MulAddW(vn2, k21, normalImpulse1);

                Vector256<float> normalImpulse2 = b2MaxW(b2NegW(b2MulW(c.normalMass2, b2AddW(vn2, velocityBias2))),
                    b2NegW(c.normalImpulse2));
                c.normalImpulse2 = b2AddW(c.normalImpulse2, normalImpulse2);
                c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, normalImpulse2);

                vt1 = b2MulAddW(b2MulAddW(vt1, k31, normalImpulse1), k32, normalImpulse2);
                vt2 = b2MulAddW(b2MulAddW(vt2, k41, normalImpulse1), k42, normalImpulse2);

                Vector256<float> angularA = b2AddW(b2MulW(normalImpulse1, rnA1), b2MulW(normalImpulse2, rnA2));
                Vector256<float> angularB = b2AddW(b2MulW(normalImpulse1, rnB1), b2MulW(normalImpulse2, rnB2));

                // Rolling resistance
                if (b2AllZeroW(c.rollingResistance) == false)
                {
                    Vector256<float> relativeW = b2SubW(b2SubW(bA.w, bB.w), b2AddW(b2MulW(b2AddW(wnA1, wnB1), normalImpulse1),
                        b2MulW(b2AddW(wnA2, wnB2), normalImpulse2)));
                    Vector256<float> k = b2AddW(c.invIA, c.invIB);
                    Vector256<float> mask = b2GreaterThanW(k, zeroW);
                    Vector256<float> deltaLambda = b2BlendW(zeroW, b2DivW(relativeW, k), mask);
                    Vector256<float> lambda = c.rollingImpulse;
                    Vector256<float> maxLambda = b2MulW(c.rollingResistance, b2AddW(c.normalImpulse1, c.normalImpulse2));
                    c.rollingImpulse = b2SymClampW(b2AddW(lambda, deltaLambda), maxLambda);
                    deltaLambda = b2SubW(c.rollingImpulse, lambda);

                    Vector256<float> wtA2 = b2MulW(c.invIA, rtA2);
                    Vector256<float> wtB2 = b2MulW(c.invIB, rtB2);
                    vt1 = b2MulAddW(vt1, b2AddW(wtA1, wtB1), deltaLambda);
                    vt2 = b2MulAddW(vt2, b2AddW(wtA2, wtB2), deltaLambda);

                    angularA = b2AddW(angularA, deltaLambda);
                    angularB = b2AddW(angularB, deltaLambda);
                }

                Vector256<float> maxFriction1 = b2MulW(c.friction, c.normalImpulse1);
                Vector256<float> tangentImpulse1 = b2NegW(b2MulW(c.tangentMass1, vt1));
                tangentImpulse1 = b2MaxW(b2SubW(b2NegW(maxFriction1), c.tangentImpulse1),
                    b2MinW(tangentImpulse1, b2SubW(maxFriction1, c.tangentImpulse1)));
                c.tangentImpulse1 = b2AddW(c.tangentImpulse1, tangentImpulse1);

                vt2 = b2MulAddW(vt2, k43, tangentImpulse1);

                Vector256<float> maxFriction2 = b2MulW(c.friction, c.normalImpulse2);
                Vector256<float> tangentImpulse2 = b2NegW(b2MulW(c.tangentMass2, vt2));
                tangentImpulse2 = b2MaxW(b2SubW(b2NegW(maxFriction2), c.tangentImpulse2),
                    b2MinW(tangentImpulse2, b2SubW(maxFriction2, c.tangentImpulse2)));
                c.tangentImpulse2 = b2AddW(c.tangentImpulse2, tangentImpulse2);

                angularA = b2AddW(angularA, b2AddW(b2MulW(tangentImpulse1, rtA1), b2MulW(tangentImpulse2, rtA2)));
                angularB = b2AddW(angularB, b2AddW(b2MulW(tangentImpulse1, rtB1), b2MulW(tangentImpulse2, rtB2)));

                Vector256<float> normalImpulse = b2AddW(normalImpulse1, normalImpulse2);
                Vector256<float> tangentImpulse = b2AddW(tangentImpulse1, tangentImpulse2);
                Vector256<float> Px = b2AddW(b2MulW(normalImpulse, normal.X), b2MulW(tangentImpulse, tangent.X));
                Vector256<float> Py = b2AddW(b2MulW(normalImpulse, normal.Y), b2MulW(tangentImpulse, tangent.Y));

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
            Span<ConstraintW> constraints = b2GetColorConstraints(context, block.colorIndex);
            Vector256<float> inv_h = b2SplatW(context.inv_h);
            Vector256<float> negRestitutionThreshold = b2SplatW(-context.world.restitutionThreshold);
            Vector256<float> zeroW = b2ZeroW();
            bool propagate = context.world.enableRestitutionPropagation;

            for (int wideIndex = block.startIndex; wideIndex < block.startIndex + block.count; ++wideIndex)
            {
                ref ConstraintW c = ref constraints[wideIndex];
                if (propagate == false && b2AllZeroW(c.restitution))
                {
                    continue;
                }

                BodyStateW bA = b2GatherBodies(states, c.indexA.AsSpan());
                BodyStateW bB = b2GatherBodies(states, c.indexB.AsSpan());

                Vector256<float> restitutionMask = b2GreaterThanW(c.restitution, zeroW);
                Vector256<float> normalMass1 = propagate ? c.normalMass1 : b2BlendW(zeroW, c.normalMass1, restitutionMask);
                Vector256<float> normalMass2 = propagate ? c.normalMass2 : b2BlendW(zeroW, c.normalMass2, restitutionMask);
                Vec2W dp = new Vec2W(b2SubW(bB.dp.X, bA.dp.X), b2SubW(bB.dp.Y, bA.dp.Y));

                {
                    Vec2W rA = c.anchorA1;
                    Vec2W rB = c.anchorB1;

                    Vector256<float> compressionImpulse = b2SubW(c.totalNormalImpulse1, c.restitutionImpulse1);
                    Vector256<float> armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity1, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation1);

                    Vector256<float> specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector256<float> velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity1), armed);

                    Vector256<float> dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector256<float> dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector256<float> vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector256<float> negImpulse = b2MulW(normalMass1, b2AddW(vn, velocityBias));

                    Vector256<float> newImpulse = b2MaxW(b2SubW(c.normalImpulse1, negImpulse), zeroW);
                    Vector256<float> impulse = b2SubW(newImpulse, c.normalImpulse1);

                    Vector256<float> approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass1, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector256<float> allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse1);
                    Vector256<float> maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse1 = b2AddW(c.normalImpulse1, impulse);
                    c.restitutionImpulse1 = b2AddW(c.restitutionImpulse1, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse1 = b2AddW(c.totalNormalImpulse1, impulse);

                    Vector256<float> Px = b2MulW(impulse, c.normal.X);
                    Vector256<float> Py = b2MulW(impulse, c.normal.Y);

                    bA.v.X = b2MulSubW(bA.v.X, c.invMassA, Px);
                    bA.v.Y = b2MulSubW(bA.v.Y, c.invMassA, Py);
                    bA.w = b2MulSubW(bA.w, c.invIA, b2SubW(b2MulW(rA.X, Py), b2MulW(rA.Y, Px)));

                    bB.v.X = b2MulAddW(bB.v.X, c.invMassB, Px);
                    bB.v.Y = b2MulAddW(bB.v.Y, c.invMassB, Py);
                    bB.w = b2MulAddW(bB.w, c.invIB, b2SubW(b2MulW(rB.X, Py), b2MulW(rB.Y, Px)));
                }

                {
                    Vec2W rA = c.anchorA2;
                    Vec2W rB = c.anchorB2;

                    Vector256<float> compressionImpulse = b2SubW(c.totalNormalImpulse2, c.restitutionImpulse2);
                    Vector256<float> armed = b2AndW(b2AndW(restitutionMask, b2LessThanW(c.relativeVelocity2, negRestitutionThreshold)),
                        b2GreaterThanW(compressionImpulse, zeroW));

                    Vec2W rsA = b2RotateVectorW(bA.dq, rA);
                    Vec2W rsB = b2RotateVectorW(bB.dq, rB);
                    Vec2W ds = new Vec2W(b2AddW(dp.X, b2SubW(rsB.X, rsA.X)), b2AddW(dp.Y, b2SubW(rsB.Y, rsA.Y)));
                    Vector256<float> s = b2AddW(b2DotW(c.normal, ds), c.baseSeparation2);

                    Vector256<float> specBias = b2MaxW(zeroW, b2MulW(s, inv_h));
                    Vector256<float> velocityBias = b2BlendW(specBias, b2MulW(c.restitution, c.relativeVelocity2), armed);

                    Vector256<float> dvx = b2SubW(b2SubW(bB.v.X, b2MulW(bB.w, rB.Y)), b2SubW(bA.v.X, b2MulW(bA.w, rA.Y)));
                    Vector256<float> dvy = b2SubW(b2AddW(bB.v.Y, b2MulW(bB.w, rB.X)), b2AddW(bA.v.Y, b2MulW(bA.w, rA.X)));
                    Vector256<float> vn = b2AddW(b2MulW(dvx, c.normal.X), b2MulW(dvy, c.normal.Y));

                    Vector256<float> negImpulse = b2MulW(normalMass2, b2AddW(vn, velocityBias));

                    Vector256<float> newImpulse = b2MaxW(b2SubW(c.normalImpulse2, negImpulse), zeroW);
                    Vector256<float> impulse = b2SubW(newImpulse, c.normalImpulse2);

                    Vector256<float> approachImpulse = b2MinW(b2MaxW(b2NegW(b2MulW(normalMass2, vn)), zeroW), b2MaxW(impulse, zeroW));
                    Vector256<float> allowance =
                        b2SubW(b2MulW(c.restitution, b2AddW(compressionImpulse, approachImpulse)), c.restitutionImpulse2);
                    Vector256<float> maxImpulse = b2AddW(approachImpulse, b2MaxW(allowance, zeroW));
                    impulse = b2BlendW(impulse, b2MinW(impulse, maxImpulse), armed);

                    c.normalImpulse2 = b2AddW(c.normalImpulse2, impulse);
                    c.restitutionImpulse2 = b2AddW(c.restitutionImpulse2, b2SubW(impulse, approachImpulse));
                    c.totalNormalImpulse2 = b2AddW(c.totalNormalImpulse2, impulse);

                    Vector256<float> Px = b2MulW(impulse, c.normal.X);
                    Vector256<float> Py = b2MulW(impulse, c.normal.Y);

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
        /// Runs as a flat parallel-for over the whole wide constraint range, like the 4-wide version.
        /// </summary>
        public override void StoreImpulses(B2SolverBlock block, B2StepContext context, int workerIndex)
        {
            b2TracyCZoneNC(B2TracyCZone.store_impulses, "Store", B2HexColor.b2_colorFireBrick, true);

            B2World world = context.world;
            ReadOnlySpan<B2ContactPrepareSpan> spans = context.contactPrepareSpans;
            ReadOnlySpan<ConstraintW> wideBase = wideConstraints;
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
                    ref readonly ConstraintW c = ref wideBase[wideIndex];
                    int baseIndex = WIDTH * (wideIndex - colorWideStart);

                    for (int laneIndex = 0; laneIndex < WIDTH; ++laneIndex)
                    {
                        int contactIndex = baseIndex + laneIndex;
                        if (contactIndex >= colorContactCount)
                        {
                            break;
                        }

                        B2ContactSim contactSim = contactSims[contactIndex];
                        ref B2Manifold m = ref contactSim.manifold;
                        m.rollingImpulse = b2GetLaneW(in c.rollingImpulse, laneIndex);

                        m.points[0].normalImpulse = b2GetLaneW(in c.normalImpulse1, laneIndex);
                        m.points[0].tangentImpulse = b2GetLaneW(in c.tangentImpulse1, laneIndex);
                        m.points[0].totalNormalImpulse = b2GetLaneW(in c.totalNormalImpulse1, laneIndex);

                        m.points[1].normalImpulse = b2GetLaneW(in c.normalImpulse2, laneIndex);
                        m.points[1].tangentImpulse = b2GetLaneW(in c.tangentImpulse2, laneIndex);
                        m.points[1].totalNormalImpulse = b2GetLaneW(in c.totalNormalImpulse2, laneIndex);

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
