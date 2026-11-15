// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Axis-aligned bounding box
    /// </summary>
    public struct B2AABB(Vector2 lowerBound, Vector2 upperBound)
    {
        public Vector2 lowerBound = lowerBound;
        public Vector2 upperBound = upperBound;
    }
}
