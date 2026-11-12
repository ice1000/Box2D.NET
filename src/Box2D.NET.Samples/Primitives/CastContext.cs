// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET.Samples.Primitives;

// Context for ray cast callbacks. Do what you want with this.
public class CastContext
{
    public B2FixedArray3<Vector2> points;
    public B2FixedArray3<Vector2> normals;
    public B2FixedArray3<float> fractions;
    public int count;
};
