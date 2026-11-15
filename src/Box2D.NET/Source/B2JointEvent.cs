// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Joint events report joints that are awake and have a force and/or torque exceeding the threshold
    /// The observed forces and torques are not returned for efficiency reasons.
    /// </summary>
    public struct B2JointEvent
    {
        /// <summary>
        /// The joint id
        /// </summary>
        public B2JointId jointId;

        /// <summary>
        /// The user data from the joint for convenience
        /// </summary>
        public B2UserData userData;
    }
}
