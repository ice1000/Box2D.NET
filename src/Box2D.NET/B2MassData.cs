// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// This holds the mass data computed for a shape.
    public struct B2MassData(float mass, Vector2 center, float rotationalInertia)
    {
        /// The mass of the shape, usually in kilograms.
        public float mass = mass;

        /// The position of the shape's centroid relative to the shape's origin.
        public Vector2 center = center;

        /// The rotational inertia of the shape about the shape center.
        public float rotationalInertia = rotationalInertia;
    }
}
