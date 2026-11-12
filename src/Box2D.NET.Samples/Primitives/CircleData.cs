// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct CircleData
{
    public Vector2 position;
    public float radius;
    public RGBA8 rgba;

    public CircleData(Vector2 position, float radius, RGBA8 rgba)
    {
        this.position = position;
        this.radius = radius;
        this.rgba = rgba;
    }
}

