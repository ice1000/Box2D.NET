// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// Result returned by b2SolvePlanes.
    public struct B2PlaneSolverResult(Vector2 delta, int iterationCount)
    {
        /// The final relative translation.
        public Vector2 delta = delta;
        
        /// The number of iterations used by the plane solver. For diagnostics.
        public int iterationCount = iterationCount;
    }
}
