// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Box2D.NET.Samples.Primitives;

namespace Box2D.NET.Samples.Graphics;

public struct LineRender()
{
    public List<VertexData> m_points = new();

    public uint[] m_vaoId = new uint[1];
    public uint[] m_vboId = new uint[1];
    public uint m_programId;
    public int m_projectionUniform;
}
