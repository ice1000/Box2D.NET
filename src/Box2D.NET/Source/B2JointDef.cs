// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Base joint definition used by all joint types.
    /// The local frames are measured from the body's origin rather than the center of mass because:
    /// 1. you might not know where the center of mass will be
    /// 2. if you add/remove shapes from a body and recompute the mass, the joints will be broken
    /// </summary>
    public struct B2JointDef
    {
        /// <summary>
        /// User data pointer
        /// </summary>
        public B2UserData userData;

        /// <summary>
        /// The first attached body
        /// </summary>
        public B2BodyId bodyIdA;

        /// <summary>
        /// The second attached body
        /// </summary>
        public B2BodyId bodyIdB;

        /// <summary>
        /// The first local joint frame
        /// </summary>
        public B2Transform localFrameA;

        /// <summary>
        /// The second local joint frame
        /// </summary>
        public B2Transform localFrameB;

        /// <summary>
        /// Force threshold for joint events
        /// </summary>
        public float forceThreshold;

        /// <summary>
        /// Torque threshold for joint events
        /// </summary>
        public float torqueThreshold;

        /// <summary>
        /// Constraint hertz (advanced feature)
        /// </summary>
        public float constraintHertz;

        /// <summary>
        /// Constraint damping ratio (advanced feature)
        /// </summary>
        public float constraintDampingRatio;

        /// <summary>
        /// Debug draw scale
        /// </summary>
        public float drawScale;

        /// <summary>
        /// Set this flag to true if the attached bodies should collide
        /// </summary>
        public bool collideConnected; 
    }
}
