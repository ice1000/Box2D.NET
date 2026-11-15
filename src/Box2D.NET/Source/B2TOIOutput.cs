// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Time of impact output
    /// </summary>
    public struct B2TOIOutput
    {
        /// <summary>
        /// The type of result
        /// </summary>
        public B2TOIState state;

        /// <summary>
        /// The hit point
        /// </summary>
        public Vector2 point;

        /// <summary>
        /// The hit normal
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The sweep time of the collision
        /// </summary>
        public float fraction;
    }
}