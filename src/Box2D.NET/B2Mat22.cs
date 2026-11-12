// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// A 2-by-2 Matrix
    public struct B2Mat22
    {
        /// columns
        public Vector2 cx, cy;

        public B2Mat22(Vector2 cx, Vector2 cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }
}
