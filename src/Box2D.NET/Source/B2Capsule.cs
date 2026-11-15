// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A solid capsule can be viewed as two semicircles connected
    /// by a rectangle.
    /// </summary>
    public struct B2Capsule(Vector2 center1, Vector2 center2, float radius)
    {
        /// <summary>
        /// Local center of the first semicircle
        /// </summary>
        public Vector2 center1 = center1;

        /// <summary>
        /// Local center of the second semicircle
        /// </summary>
        public Vector2 center2 = center2;

        /// <summary>
        /// The radius of the semicircles
        /// </summary>
        public float radius = radius;
    }
}