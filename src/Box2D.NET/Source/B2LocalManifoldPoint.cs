// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Contact manifold point in local coordinates (frame A).
    /// </summary>
    public struct B2LocalManifoldPoint
    {
        /// <summary>
        /// Contact point in frame A
        /// </summary>
        public Vector2 point;

        /// <summary>
        /// The separation of the contact point, negative if penetrating. May be positive or negative.
        /// </summary>
        public float separation;

        /// <summary>
        /// Uniquely identifies a contact point between two shapes
        /// </summary>
        public ushort id;
    }
}
