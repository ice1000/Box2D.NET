// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Input for b2ShapeDistance
    /// </summary>
    public struct B2DistanceInput
    {
        /// <summary>
        /// The proxy for shape A
        /// </summary>
        public B2ShapeProxy proxyA;

        /// <summary>
        /// The proxy for shape B
        /// </summary>
        public B2ShapeProxy proxyB;

        /// <summary>
        /// Transform of shape B in shape A's frame, the relative pose B in A
        /// (b2InvMulTransforms( worldA, worldB )). The query is origin independent and runs in frame A.
        /// </summary>
        public B2Transform transform;

        /// <summary>
        /// Should the proxy radius be considered?
        /// </summary>
        public bool useRadii;
    }
}
