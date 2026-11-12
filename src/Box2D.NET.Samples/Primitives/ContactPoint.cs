// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET.Samples.Primitives;

public struct ContactPoint
{
    public B2ShapeId shapeIdA;
    public B2ShapeId shapeIdB;
    public Vector2 normal;
    public Vector2 position;
    public bool persisted;
    public float normalImpulse;
    public float tangentImpulse;
    public float separation;
    public int constraintIndex;
    public int color;
};

