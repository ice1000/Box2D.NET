// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /**@}*/
    /**
     * @defgroup distance Distance
     * Functions for computing the distance between shapes.
     *
     * These are advanced functions you can use to perform distance calculations. There
     * are functions for computing the closest points between shapes, doing linear shape casts,
     * and doing rotational shape casts. The latter is called time of impact (TOI).
     * @{
     */
    /// <summary>
    /// Result of computing the distance between two line segments
    /// </summary>
    public struct B2SegmentDistanceResult
    {
        /// <summary>
        /// The closest point on the first segment
        /// </summary>
        public Vector2 closest1;

        /// <summary>
        /// The closest point on the second segment
        /// </summary>
        public Vector2 closest2;

        /// <summary>
        /// The barycentric coordinate on the first segment
        /// </summary>
        public float fraction1;

        /// <summary>
        /// The barycentric coordinate on the second segment
        /// </summary>
        public float fraction2;

        /// <summary>
        /// The squared distance between the closest points
        /// </summary>
        public float distanceSquared;
    }
}
