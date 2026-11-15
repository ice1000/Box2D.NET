// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Chain data created by merging polygons.
    /// </summary>
    public struct B2ChainData
    {
        public Vector2[] points;
        public int count;
    }
}
