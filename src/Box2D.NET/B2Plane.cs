// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// separation = dot(normal, point) - offset
    public struct B2Plane(Vector2 normal, float offset)
    {
        public Vector2 normal = normal;
        public float offset = offset;
    }
}
