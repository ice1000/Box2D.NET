// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// <summary>
    /// A world position. Upstream Box2D stores this in double precision in large world mode
    /// (BOX2D_DOUBLE_PRECISION). Box2D.NET only supports single precision, so this holds floats,
    /// but it is kept as a distinct type so the API matches upstream.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B2Pos(float x, float y) : IEquatable<B2Pos>
    {
        /// <summary>
        /// coordinates
        /// </summary>
        public float X = x, Y = y;

        /// <summary>
        /// Offset a world position by a vector
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Pos operator +(B2Pos a, Vector2 b)
        {
            return new B2Pos(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Offset a world position by a vector
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static B2Pos operator -(B2Pos a, Vector2 b)
        {
            return new B2Pos(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// Delta between two world positions
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(B2Pos a, B2Pos b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(B2Pos a, B2Pos b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(B2Pos a, B2Pos b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Bitwise compare, see B2Vec2.Equals
        /// </summary>
        public bool Equals(B2Pos other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is B2Pos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X, Y).GetHashCode();
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
