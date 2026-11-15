// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Contact id references a contact instance. This should be treated as an opaque handled.
    /// </summary>
    public readonly struct B2ContactId(int index1, ushort world0, short padding, uint generation)
    {
        public readonly int index1 = index1;
        public readonly ushort world0 = world0;
        public readonly short padding = padding;
        public readonly uint generation = generation;
    }
}
