// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A solid circle
    /// </summary>
    public struct B2Circle(Vector2 center, float radius)
    {
        /// <summary>
        /// The local center
        /// </summary>
        public Vector2 center = center;

        /// <summary>
        /// The radius
        /// </summary>
        public float radius = radius;
    }
}
