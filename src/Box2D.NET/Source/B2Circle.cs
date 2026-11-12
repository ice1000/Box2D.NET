// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// A solid circle
    public struct B2Circle(Vector2 center, float radius)
    {
        /// The local center
        public Vector2 center = center;

        /// The radius
        public float radius = radius;
    }
}
