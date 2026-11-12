// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct VertexData
{
    public Vector2 position;
    public RGBA8 rgba;

    public VertexData(Vector2 position, RGBA8 rgba)
    {
        this.position = position;
        this.rgba = rgba;
    }
}

