// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /**@}*/
    /**
     * @defgroup tree Dynamic Tree
     * The dynamic tree is a binary AABB tree to organize and query large numbers of geometric objects
     *
     * Box2D uses the dynamic tree internally to sort collision shapes into a binary bounding volume hierarchy.
     * This data structure may have uses in games for organizing other geometry data and may be used independently
     * of Box2D rigid body simulation.
     *
     * A dynamic AABB tree broad-phase, inspired by Nathanael Presson's btDbvt.
     * A dynamic tree arranges data in a binary tree to accelerate
     * queries such as AABB queries and ray casts. Leaf nodes are proxies
     * with an AABB. These are used to hold a user collision object.
     * Nodes are pooled and relocatable, so I use node indices rather than pointers.
     * The dynamic tree is made available for advanced users that would like to use it to organize
     * spatial game data besides rigid bodies.
     * @{
     */
    /// <summary>
    /// The dynamic tree structure. This should be considered private data.
    /// It is placed here for performance reasons.
    /// </summary>
    public class B2DynamicTree
    {
        /// <summary>
        /// Array of nodes. The root is at index zero and index 1 is empty.
        /// Otherwise siblings are paired at even indices. Has holes for free node pairs.
        /// </summary>
        public B2TreeNode[] nodes;

        /// <summary>
        /// Parent index per node. The free list is interweaved.
        /// </summary>
        public int[] parents;

        /// <summary>
        /// Proxy data split from node array as cold data.
        /// </summary>
        public B2TreeProxy[] proxies;

        /// <summary>
        /// Every allocated node has a lower index than this.
        /// </summary>
        public int nodeEnd;

        /// <summary>
        /// The allocated node space
        /// </summary>
        public int nodeCapacity;

        /// <summary>
        /// Free pairs below nodeEnd
        /// </summary>
        public int pairFreeList;

        /// <summary>
        /// Number of proxies created
        /// </summary>
        public int proxyCount;

        /// <summary>
        /// The allocated proxy space
        /// </summary>
        public int proxyCapacity;

        /// <summary>
        /// Proxy free list
        /// </summary>
        public int proxyFreeList;

        /// <summary>
        /// Array of nodes for rebuild.
        /// </summary>
        public B2TreeNode[] swapNodes;

        /// <summary>
        /// Leaf indices for rebuild
        /// </summary>
        public int[] leafIndices;

        /// <summary>
        /// Leaves for the rebuild. May represent a proxy or a retained subtree.
        /// </summary>
        public B2TreeNode[] leafNodes;

        /// <summary>
        /// Leaf bounding boxes for rebuild
        /// </summary>
        public B2AABB[] leafBoxes;

        /// <summary>
        /// Leaf bounding box centers for rebuild
        /// </summary>
        public Vector2[] leafCenters;

        /// <summary>
        /// Bins for sorting during rebuild
        /// </summary>
        public int[] binIndices;

        /// <summary>
        /// Allocated space for rebuilding
        /// </summary>
        public int rebuildCapacity;

        /// <summary>
        /// Rebuild orders the nodes so the children follow parents. Cache friendly for queries
        /// and refitting. The order can be disrupted by proxy creation.
        /// </summary>
        public bool dfsOrdered;

        public void Clear()
        {
            nodes = null;
            parents = null;
            proxies = null;
            nodeEnd = 0;
            nodeCapacity = 0;
            pairFreeList = 0;
            proxyCount = 0;
            proxyCapacity = 0;
            proxyFreeList = 0;
            swapNodes = null;
            leafIndices = null;
            leafNodes = null;
            leafBoxes = null;
            leafCenters = null;
            binIndices = null;
            rebuildCapacity = 0;
            dfsOrdered = false;
        }
    }
}
