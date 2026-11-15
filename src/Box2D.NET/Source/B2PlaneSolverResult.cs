// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Result returned by b2SolvePlanes.
    /// </summary>
    public struct B2PlaneSolverResult(Vector2 delta, int iterationCount)
    {
        /// <summary>
        /// The final relative translation.
        /// </summary>
        public Vector2 delta = delta;
        
        /// <summary>
        /// The number of iterations used by the plane solver. For diagnostics.
        /// </summary>
        public int iterationCount = iterationCount;
    }
}
