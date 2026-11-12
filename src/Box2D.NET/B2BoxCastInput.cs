// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// Input for casting an AABB through a dynamic tree
    public struct B2BoxCastInput
    {
        /// The AABB to cast, in the tree's frame.
        public B2AABB box;

        /// The sweep translation.
        public Vector2 translation;

        /// The maximum fraction of the translation to consider, typically 1.
        public float maxFraction;
    }
}
