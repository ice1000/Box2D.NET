// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Separate storage for tree leaves.
    /// </summary>
    public struct B2TreeProxy
    {
        /// <summary>
        /// User data is an index instead of void* because it is used internally as a shape index.
        /// </summary>
        public ulong userData;

        /// <summary>
        /// Category bits for collision filtering.
        /// </summary>
        public ulong categoryBits;

        /// <summary>
        /// The leaf node. B2_NULL_INDEX for a free proxy.
        /// </summary>
        public int node;

        /// <summary>
        /// Next free proxy.
        /// </summary>
        public int next;
    }
}
