// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// World-space cast output. The hit point is a world position. The normal stays a float direction.
    /// </summary>
    public struct B2WorldCastOutput
    {
        /// <summary>
        /// The surface normal at the hit point
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The surface hit point in world space
        /// </summary>
        public B2Pos point;

        /// <summary>
        /// The fraction of the input translation at collision
        /// </summary>
        public float fraction;

        /// <summary>
        /// The number of iterations used
        /// </summary>
        public int iterations;

        /// <summary>
        /// Did the cast hit?
        /// </summary>
        public bool hit;
    }
}
