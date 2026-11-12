// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    // Wide vec2
    public struct B2Vec2W(in Vector4 x, in Vector4 y)
    {
        public Vector4 X = x;
        public Vector4 Y = y;
    }
}
