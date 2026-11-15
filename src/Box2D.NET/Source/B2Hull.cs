// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A convex hull. Used to create convex polygons.
    /// @warning Do not modify these values directly, instead use b2ComputeHull()
    /// </summary>
    public struct B2Hull
    {
        /// <summary>
        /// The final points of the hull
        /// </summary>
        public B2FixedArray8<Vector2> points;

        /// <summary>
        /// The number of points
        /// </summary>
        public int count;
    }
}
