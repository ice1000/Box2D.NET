// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// Output for b2ShapeDistance
    public struct B2DistanceOutput
    {
        public Vector2 pointA; // Closest point on shapeA, in shape A's frame
        public Vector2 pointB; // Closest point on shapeB, in shape A's frame
        public Vector2 normal; // A to B normal in shape A's frame. Invalid if distance is zero.
        public float distance; // The final distance, zero if overlapped
        public int iterations; // Number of GJK iterations used
        public int simplexCount; // The number of simplexes stored in the simplex array
    }
}