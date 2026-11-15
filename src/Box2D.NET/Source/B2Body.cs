// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Body organizational details that are not used in the solver.
    /// </summary>
    public class B2Body
    {
        public B2UserData userData;

        /// <summary>
        /// index of solver set stored in b2World
        /// may be B2_NULL_INDEX
        /// </summary>
        public int setIndex;

        /// <summary>
        /// body sim and state index within set
        /// may be B2_NULL_INDEX
        /// </summary>
        public int localIndex;

        /// <summary>
        /// [31 : contactId | 1 : edgeIndex]
        /// </summary>
        public int headContactKey;
        public int contactCount;

        /// <summary>
        /// todo maybe move this to the body sim
        /// </summary>
        public int headShapeId;
        public int shapeCount;

        public int headChainId;

        /// <summary>
        /// [31 : jointId | 1 : edgeIndex]
        /// </summary>
        public int headJointKey;
        public int jointCount;

        /// <summary>
        /// All enabled dynamic and kinematic bodies are in an island.
        /// </summary>
        public int islandId;

        /// <summary>
        /// Need this island index for faster union-find
        /// </summary>
        public int islandIndex;

        public float mass;

        /// <summary>
        /// Rotational inertia about the center of mass.
        /// </summary>
        public float inertia;

        public float sleepThreshold;
        public float sleepTime;
        public float safetyFactor;

        /// <summary>
        /// this is used to adjust the fellAsleep flag in the body move array
        /// </summary>
        public int bodyMoveIndex;

        public int id;

        /// <summary>
        /// b2BodyFlags
        /// </summary>
        public uint flags;

        public B2BodyType type;

        /// <summary>
        /// This is monotonically advanced when a body is allocated in this slot
        /// Used to check for invalid b2BodyId
        /// </summary>
        public ushort generation;

        public string name;
    }
}
