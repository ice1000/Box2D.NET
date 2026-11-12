// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    public struct B2ChainSegmentParams
    {
        public Vector2 edge1;
        public Vector2 normal0;
        public Vector2 normal2;
        public bool convex1;
        public bool convex2;
    }
}
