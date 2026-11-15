// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A line segment with one-sided collision. Only collides on the right side.
    /// Several of these are generated for a chain shape.
    /// ghost1 -&gt; point1 -&gt; point2 -&gt; ghost2
    /// </summary>
    public struct B2ChainSegment
    {
        /// <summary>
        /// The tail ghost vertex
        /// </summary>
        public Vector2 ghost1;

        /// <summary>
        /// The line segment
        /// </summary>
        public B2Segment segment;

        /// <summary>
        /// The head ghost vertex
        /// </summary>
        public Vector2 ghost2;

        /// <summary>
        /// The owning chain shape index (internal usage only)
        /// </summary>
        public int chainId;
    }
}