// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A contact manifold describes the contact points between colliding shapes.
    /// @note Box2D uses speculative collision so some contact points may be separated.
    /// </summary>
    public struct B2Manifold
    {
        /// <summary>
        /// The unit normal vector in world space, points from shape A to bodyB
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// Angular impulse applied for rolling resistance. N * m * s = kg * m^2 / s
        /// </summary>
        public float rollingImpulse;

        /// <summary>
        /// The manifold points, up to two are possible in 2D
        /// </summary>
        public B2FixedArray2<B2ManifoldPoint> points;

        /// <summary>
        /// The number of contacts points, will be 0, 1, or 2
        /// </summary>
        public int pointCount;
    }
}
