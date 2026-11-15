// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Input for casting an AABB through a dynamic tree
    /// </summary>
    public struct B2BoxCastInput
    {
        /// <summary>
        /// The AABB to cast, in the tree's frame.
        /// </summary>
        public B2AABB box;

        /// <summary>
        /// The sweep translation.
        /// </summary>
        public Vector2 translation;

        /// <summary>
        /// The maximum fraction of the translation to consider, typically 1.
        /// </summary>
        public float maxFraction;
    }
}
