// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct SolidCircleRender()
{
    public List<SolidCircleData> circles = new();

    public uint[] vaoId = new uint[1];
    public uint[] vboIds = new uint[2];
    public uint programId;
    public int projectionUniform;
    public int pixelScaleUniform;
}
