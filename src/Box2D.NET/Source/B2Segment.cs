// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A line segment with two-sided collision.
    /// </summary>
    public struct B2Segment(Vector2 point1, Vector2 point2)
    {
        /// <summary>
        /// The first point
        /// </summary>
        public Vector2 point1 = point1;

        /// <summary>
        /// The second point
        /// </summary>
        public Vector2 point2 = point2;
    }
}
