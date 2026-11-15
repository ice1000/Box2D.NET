// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A distance proxy is used by the GJK algorithm. It encapsulates any shape.
    /// You can provide between 1 and B2_MAX_POLYGON_VERTICES and a radius.
    /// </summary>
    public struct B2ShapeProxy
    {
        /// <summary>
        /// The point cloud
        /// </summary>
        public B2FixedArray8<Vector2> points;

        /// <summary>
        /// The number of points. Must be greater than 0.
        /// </summary>
        public int count;

        /// <summary>
        /// The external radius of the point cloud. May be zero.
        /// </summary>
        public float radius;
    }
}
