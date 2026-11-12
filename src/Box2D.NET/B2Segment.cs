// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// A line segment with two-sided collision.
    public struct B2Segment
    {
        /// The first point
        public Vector2 point1;

        /// The second point
        public Vector2 point2;

        public B2Segment(Vector2 point1, Vector2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }
    }
}
