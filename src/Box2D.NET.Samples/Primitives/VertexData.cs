// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET.Samples.Primitives;

public struct VertexData(Vector2 position, RGBA8 rgba)
{
    public Vector2 position = position;
    public RGBA8 rgba = rgba;
}

