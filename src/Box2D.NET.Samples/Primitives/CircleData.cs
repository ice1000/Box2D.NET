// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET.Samples.Primitives;

public struct CircleData(Vector2 position, float radius, RGBA8 rgba)
{
    public Vector2 position = position;
    public float radius = radius;
    public RGBA8 rgba = rgba;
}

