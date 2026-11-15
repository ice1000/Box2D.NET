// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Per thread task storage
    /// </summary>
    public class B2TaskContext
    {
        /// <summary>
        /// Collect per thread sensor continuous hit events.
        /// </summary>
        public B2Array<B2SensorHit> sensorHits;

        /// <summary>
        /// Broad-phase pairs.
        /// </summary>
        public B2Array<ulong> pairKeys;
        
        /// <summary>
        /// These bits align with the contact id capacity and signal a change in contact status
        /// </summary>
        public B2BitSet contactStateBitSet;

        /// <summary>
        /// These bits align with the contact id capacity and signal a hit event.
        /// </summary>
        public B2BitSet hitEventBitSet;

        /// <summary>
        /// Fast-path flag: true when this worker set at least one bit in hitEventBitSet this step.
        /// </summary>
        public bool hasHitEvents;

        /// <summary>
        /// These bits align with the joint id capacity and signal a change in contact status
        /// </summary>
        public B2BitSet jointStateBitSet;

        /// <summary>
        /// Used to track bodies with shapes that have enlarged AABBs. This avoids having a bit array
        /// that is very large when there are many static shapes.
        /// </summary>
        public B2BitSet enlargedSimBitSet;

        /// <summary>
        /// Used to put islands to sleep
        /// </summary>
        public B2BitSet awakeIslandBitSet;

        /// <summary>
        /// Per worker split island candidate
        /// </summary>
        public float splitSleepTime;
        public int splitIslandId;

        /// <summary>
        /// Number of contacts recycled this step (collide pass).
        /// </summary>
        public int recycledContactCount;
    }
}
