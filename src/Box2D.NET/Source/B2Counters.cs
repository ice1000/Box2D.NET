// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Counters that give details of the simulation size.
    /// </summary>
    public struct B2Counters
    {
        public int bodyCount;
        public int shapeCount;
        public int contactCount;
        public int jointCount;
        public int islandCount;
        public int stackUsed;
        public int staticTreeHeight;
        public int treeHeight;
        public int byteCount;
        public int taskCount;
        public int enqueueCount;
        public B2FixedArray24<int> colorCounts;

        /// <summary>
        /// Number of contacts touched by the collide pass (graph contacts + awake-set non-touching).
        /// </summary>
        public int awakeContactCount;

        /// <summary>
        /// Number of contacts recycled in the most recent step.
        /// </summary>
        public int recycledContactCount;
    }
}
