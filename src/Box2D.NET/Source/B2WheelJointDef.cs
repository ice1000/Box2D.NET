// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Wheel joint definition
    /// Body B is a wheel that may rotate freely and slide along the local x-axis in frame A.
    /// The joint translation is zero when the local frame origins coincide in world space.
    /// @ingroup wheel_joint
    /// </summary>
    public struct B2WheelJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// Enable a linear spring along the local axis
        /// </summary>
        public bool enableSpring;

        /// <summary>
        /// Spring stiffness in Hertz
        /// </summary>
        public float hertz;

        /// <summary>
        /// Spring damping ratio, non-dimensional
        /// </summary>
        public float dampingRatio;

        /// <summary>
        /// Enable/disable the joint linear limit
        /// </summary>
        public bool enableLimit;

        /// <summary>
        /// The lower translation limit
        /// </summary>
        public float lowerTranslation;

        /// <summary>
        /// The upper translation limit
        /// </summary>
        public float upperTranslation;

        /// <summary>
        /// Enable/disable the joint rotational motor
        /// </summary>
        public bool enableMotor;

        /// <summary>
        /// The maximum motor torque, typically in newton-meters
        /// </summary>
        public float maxMotorTorque;

        /// <summary>
        /// The desired motor speed in radians per second
        /// </summary>
        public float motorSpeed;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
