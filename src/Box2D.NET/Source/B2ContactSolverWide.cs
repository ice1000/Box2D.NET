// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static Box2D.NET.B2Cores;

namespace Box2D.NET
{
    /// <summary>
    /// Graph color contact solver, dispatched at run time on the SIMD width.
    /// The C version picks the width at compile time (SSE2/NEON = 4, AVX2 = 8). Here each width is a
    /// separate subclass with its own float vector type and constraint layout written out by hand,
    /// so the JIT sees concrete vector types in every hot loop.
    ///
    /// One instance per world. It owns the per-step wide constraint buffer, and each graph color
    /// stores only its offset into that buffer.
    /// </summary>
    internal abstract class B2ContactSolverWide
    {
        /// <summary>
        /// Number of contacts per wide constraint (4 or 8)
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        /// log2(Width)
        /// </summary>
        public abstract int Shift { get; }

        /// <summary>
        /// Allocate the wide constraint buffer for one step
        /// </summary>
        public abstract void Allocate(B2StackAllocator stack, int wideContactCount);

        public abstract void Free(B2StackAllocator stack);

        /// <summary>
        /// Zero one wide constraint so the unused lanes of a partially filled slot read as null bodies.
        /// </summary>
        public abstract void ClearConstraint(int wideIndex);

        public abstract void Prepare(B2SolverBlock block, B2StepContext context);
        public abstract void WarmStart(B2SolverBlock block, B2StepContext context);
        public abstract void Push(B2SolverBlock block, B2StepContext context);
        public abstract void Solve(B2SolverBlock block, B2StepContext context);
        public abstract void ApplyRestitution(B2SolverBlock block, B2StepContext context);
        public abstract void StoreImpulses(B2SolverBlock block, B2StepContext context, int workerIndex);

        /// <summary>
        /// True when the 8-wide solver can run on this machine.
        /// </summary>
        public static bool b2IsWide8Supported()
        {
            // AVX2 for the 256-bit integer shift in the scatter, like the C version
            return Avx2.IsSupported && Vector256.IsHardwareAccelerated;
        }

        /// <summary>
        /// Create the solver for a new world. See <see cref="B2Cores.b2_contactSolverSimdWidth"/>.
        /// </summary>
        public static B2ContactSolverWide b2CreateContactSolverWide()
        {
            int width = b2_contactSolverSimdWidth;
            if (width == 0)
            {
                width = b2IsWide8Supported() ? 8 : 4;
            }

            if (width == 8)
            {
                if (b2IsWide8Supported() == false)
                {
                    throw new PlatformNotSupportedException("The 8-wide contact solver needs AVX2.");
                }

                return new B2ContactSolverWide8();
            }

            if (width == 4)
            {
                return new B2ContactSolverWide4();
            }

            throw new ArgumentOutOfRangeException(nameof(b2_contactSolverSimdWidth), width, "Contact solver SIMD width must be 0, 4 or 8.");
        }
    }
}
