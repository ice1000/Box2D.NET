// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct SolidPolygonRender()
{
    public List<PolygonData> polygons = new(10 * SolidPolygons.e_batchSize);

    public uint[] vaoId = new uint[1];
    public uint[] vboIds = new uint[2];
    public uint programId;
    public int projectionUniform;
    public int pixelScaleUniform;
}
