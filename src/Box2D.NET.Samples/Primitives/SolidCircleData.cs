// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct SolidCircleData(in B2Transform transform, float radius, RGBA8 rgba)
{
    public B2Transform transform = transform;
    public float radius = radius;
    public RGBA8 rgba = rgba;
}

