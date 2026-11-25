// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// A motion lock joint is designed to restrict motion to a line or a point. All restrictions are
    /// relative to the joint frame in body A.
    /// @ingroup motion_lock_joint
    /// </summary>
    public struct B2MotionLockJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// Lock movement along the x axis.
        /// </summary>
        public bool lockLinearX;

        /// <summary>
        /// Lock movement along the y axis.
        /// </summary>
        public bool lockLinearY;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
