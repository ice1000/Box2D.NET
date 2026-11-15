// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Cold contact data. Used as a persistent handle and for persistent island
    /// connectivity.
    /// </summary>
    public class B2Contact
    {
        public B2FixedArray2<B2ContactEdge> edges;
        
        /// <summary>
        /// A contact only belongs to an island if touching, otherwise B2_NULL_INDEX.
        /// </summary>
        public int islandId;
        
        /// <summary>
        /// Index into the island's contacts array for O(1) swap-removal.
        /// B2_NULL_INDEX when not in an island.
        /// </summary>
        public int islandIndex;
        
        /// <summary>
        /// index of simulation set stored in b2World
        /// B2_NULL_INDEX when slot is free
        /// </summary>
        public int setIndex;

        /// <summary>
        /// index into the constraint graph color array
        /// B2_NULL_INDEX for non-touching or sleeping contacts
        /// B2_NULL_INDEX when slot is free
        /// </summary>
        public int colorIndex;

        /// <summary>
        /// contact index within set or graph color
        /// B2_NULL_INDEX when slot is free
        /// </summary>
        public int localIndex;

        public int shapeIdA;
        public int shapeIdB;
        public int contactId;

        /// <summary>
        /// b2ContactFlags
        /// </summary>
        public uint flags;
        
        /// <summary>
        /// This is monotonically advanced when a contact is allocated in this slot
        /// Used to check for invalid b2ContactId
        /// </summary>
        public uint generation;
    }
}
