// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// The class manages contact between two shapes. A contact exists for each overlapping
    /// AABB in the broad-phase (except if filtered). Therefore a contact object may exist
    /// that has no contact points.
    /// </summary>
    public class B2ContactSim
    {
        public int contactId;

        /// <summary>
        /// Cache for contact recycling.
        /// </summary>
        public B2Rot cachedRotationA;
        public B2Rot cachedRotationB;
        public B2Transform cachedRelativePose;

#if DEBUG
        public int bodyIdA;
        public int bodyIdB;
#endif

        /// <summary>
        /// Transient body indices
        /// </summary>
        public int encodedBodySimA;
        public int encodedBodySimB;

        public int shapeIdA;
        public int shapeIdB;

        public float invMassA;
        public float invIA;

        public float invMassB;
        public float invIB;

        public B2Manifold manifold;

        /// <summary>
        /// Mixed friction and restitution
        /// </summary>
        public float friction;
        public float restitution;
        public float rollingResistance;
        public float tangentSpeed;

        /// <summary>
        /// b2ContactFlags
        /// </summary>
        public uint simFlags;

        public B2SimplexCache cache;

        public void CopyFrom(B2ContactSim other)
        {
            contactId = other.contactId;
            cachedRotationA = other.cachedRotationA;
            cachedRotationB = other.cachedRotationB;
            cachedRelativePose = other.cachedRelativePose;

#if DEBUG
            bodyIdA = other.bodyIdA;
            bodyIdB = other.bodyIdB;
#endif

            encodedBodySimA = other.encodedBodySimA;
            encodedBodySimB = other.encodedBodySimB;

            shapeIdA = other.shapeIdA;
            shapeIdB = other.shapeIdB;

            invMassA = other.invMassA;
            invIA = other.invIA;

            invMassB = other.invMassB;
            invIB = other.invIB;

            manifold = other.manifold;

            friction = other.friction;
            restitution = other.restitution;
            rollingResistance = other.rollingResistance;
            tangentSpeed = other.tangentSpeed;

            simFlags = other.simFlags;

            cache = other.cache;
        }
    }
}
