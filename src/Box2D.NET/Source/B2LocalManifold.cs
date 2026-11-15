// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Contact manifold in local coordinates (frame A).
    /// </summary>
    public struct B2LocalManifold
    {
        /// <summary>
        /// The unit normal vector in frame A, points from shape A to shape B
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The manifold points, up to two are possible in 2D
        /// </summary>
        public B2FixedArray2<B2LocalManifoldPoint> points;

        /// <summary>
        /// The number of contacts points, will be 0, 1, or 2
        /// </summary>
        public int pointCount;
    }
}
