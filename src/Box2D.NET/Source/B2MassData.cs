// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// This holds the mass data computed for a shape.
    /// </summary>
    public struct B2MassData(float mass, Vector2 center, float rotationalInertia)
    {
        /// <summary>
        /// The mass of the shape, usually in kilograms.
        /// </summary>
        public float mass = mass;

        /// <summary>
        /// The position of the shape's centroid relative to the shape's origin.
        /// </summary>
        public Vector2 center = center;

        /// <summary>
        /// The rotational inertia of the shape about the shape center.
        /// </summary>
        public float rotationalInertia = rotationalInertia;
    }
}
