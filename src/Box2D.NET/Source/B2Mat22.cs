// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A 2-by-2 Matrix
    /// </summary>
    public struct B2Mat22(Vector2 cx, Vector2 cy)
    {
        /// <summary>
        /// columns
        /// </summary>
        public Vector2 cx = cx, cy = cy;
    }
}
