// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Runtime.CompilerServices;
using static Box2D.NET.B2Ids;

namespace Box2D.NET
{
    /// Body id references a body instance. This should be treated as an opaque handle.
    public readonly struct B2BodyId(int index1, ushort world0, ushort generation) : IEquatable<B2BodyId>
    {
        public readonly int index1 = index1;
        public readonly ushort world0 = world0;
        public readonly ushort generation = generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua < ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua > ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(B2BodyId a, B2BodyId b)
        {
            ulong ua = b2StoreBodyId(a);
            ulong ub = b2StoreBodyId(b);
            return ua == ub;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(B2BodyId a, B2BodyId b)
        {
            return !(a == b);
        }

        public bool Equals(B2BodyId other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj is B2BodyId other)
            {
                return Equals(other);
            }

            return false;
        }

        // GetHashCode 메서드 구현
        public override int GetHashCode()
        {
            ulong ua = b2StoreBodyId(this);
            return ua.GetHashCode();
        }
    }
}