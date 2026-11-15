// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Low level ray cast or shape-cast output data. Returns a zero fraction and normal in the case of initial overlap.
    /// </summary>
    public struct B2CastOutput
    {
        /// <summary>
        /// The surface normal at the hit point
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The surface hit point
        /// </summary>
        public Vector2 point;

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