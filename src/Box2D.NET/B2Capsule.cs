// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// A solid capsule can be viewed as two semicircles connected
    /// by a rectangle.
    public struct B2Capsule
    {
        /// Local center of the first semicircle
        public Vector2 center1;

        /// Local center of the second semicircle
        public Vector2 center2;

        /// The radius of the semicircles
        public float radius;

        public B2Capsule(Vector2 center1, Vector2 center2, float radius)
        {
            this.center1 = center1;
            this.center2 = center2;
            this.radius = radius;
        }
    }
}