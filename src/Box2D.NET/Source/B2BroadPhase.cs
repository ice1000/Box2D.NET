// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// <summary>
    /// The broad-phase is used for computing pairs and performing volume queries and ray casts.
    /// This broad-phase does not persist pairs. Instead, this reports potentially new pairs.
    /// It is up to the client to consume the new pairs and to track subsequent overlap.
    /// </summary>
    public class B2BroadPhase
    {
        /// <summary>
        /// One tree for each body type.
        /// </summary>
        public B2DynamicTree[] trees;

        /// <summary>
        /// The moved siblings gathered from the dynamic body tree.
        /// </summary>
        public ArraySegment<int> movedSiblings;

        /// <summary>
        /// Tracks shape pairs that have a b2Contact
        /// </summary>
        public B2HashSet pairSet;

        /// <summary>
        /// Seeds for colliding the dynamic tree against the static and kinematic trees.
        /// Upstream keeps this on the stack. It is kept here because the parallel tasks need it.
        /// </summary>
        public B2NodePair[] crossSeeds;

        public void Clear()
        {
            trees = null;
            movedSiblings = null;
            pairSet = new B2HashSet();
            crossSeeds = null;
        }
    }
}
