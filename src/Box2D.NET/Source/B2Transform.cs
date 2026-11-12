// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;

namespace Box2D.NET
{
    /// A 2D rigid transform
    public struct B2Transform(Vector2 p, B2Rot q)
    {
        public Vector2 p = p;
        public B2Rot q = q;

        public bool TryWriteBytes(Span<byte> bytes)
        {
            if (!BitConverter.TryWriteBytes(bytes.Slice(0, 4), p.X))
                return false;

            if (!BitConverter.TryWriteBytes(bytes.Slice(4, 4), p.Y))
                return false;
            
            if (!BitConverter.TryWriteBytes(bytes.Slice(8, 4), q.c))
                return false;
            
            if (!BitConverter.TryWriteBytes(bytes.Slice(12, 4), q.s))
                return false;

            return true;
        }
    }
}
