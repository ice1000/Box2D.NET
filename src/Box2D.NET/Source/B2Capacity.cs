// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Optional world capacities that can be used to avoid run-time allocations.
    /// @see b2World_GetMaxCapacity
    /// @ingroup world
    /// </summary>
    public struct B2Capacity
    {
        /// <summary>
        /// Number of expected static shapes.
        /// </summary>
        public int staticShapeCount;

        /// <summary>
        /// Number of expected dynamic and kinematic shapes.
        /// </summary>
        public int dynamicShapeCount;

        /// <summary>
        /// Number of expected static bodies.
        /// </summary>
        public int staticBodyCount;

        /// <summary>
        /// Number of expected dynamic and kinematic bodies.
        /// </summary>
        public int dynamicBodyCount;

        /// <summary>
        /// Number of expected contacts.
        /// </summary>
        public int contactCount;

        /// <summary>
        /// Reserve arena allocator space, used for temporary allocations.
        /// </summary>
        public int arenaByteCount;
    }
}
