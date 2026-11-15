// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Arrays;

namespace Box2D.NET
{
    /// <summary>
    /// This holds solver set data. The following sets are used:
    /// - static set for all static bodies and joints between static bodies
    /// - active set for all active bodies with body states (no contacts or joints)
    /// - disabled set for disabled bodies and their joints
    /// - all further sets are sleeping island sets along with their contacts and joints
    /// The purpose of solver sets is to achieve high memory locality.
    /// https://www.youtube.com/watch?v=nZNd5FjSquk
    /// </summary>
    public class B2SolverSet
    {
        /// <summary>
        /// Body array. Empty for unused set.
        /// </summary>
        public B2Array<B2BodySim> bodySims;

        /// <summary>
        /// Body state only exists for active set
        /// </summary>
        public B2Array<B2BodyState> bodyStates;

        /// <summary>
        /// This holds sleeping/disabled joints. Empty for static/active set.
        /// </summary>
        public B2Array<B2JointSim> jointSims;

        /// <summary>
        /// This holds all contacts for sleeping sets.
        /// This holds non-touching contacts for the awake set.
        /// </summary>
        public B2Array<B2ContactSim> contactSims;

        /// <summary>
        /// The awake set has an array of islands. Sleeping sets normally have a single islands. However, joints
        /// created between sleeping sets causes the sets to merge, leaving them with multiple islands. These sleeping
        /// islands will be naturally merged with the set is woken.
        /// The static and disabled sets have no islands.
        /// Islands live in the solver sets to limit the number of islands that need to be considered for sleeping.
        /// </summary>
        public B2Array<B2IslandSim> islandSims;

        /// <summary>
        /// Aligns with b2World::solverSetIdPool. Used to create a stable id for body/contact/joint/islands.
        /// </summary>
        public int setIndex;

        public void Clear()
        {
            b2Array_Clear(ref bodySims);
            b2Array_Clear(ref bodyStates);
            b2Array_Clear(ref jointSims);
            b2Array_Clear(ref contactSims);
            b2Array_Clear(ref islandSims);
            setIndex = 0;
        }
    }
}
