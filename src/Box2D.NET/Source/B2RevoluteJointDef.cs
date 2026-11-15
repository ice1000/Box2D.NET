// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Revolute joint definition
    /// A point on body B is fixed to a point on body A. Allows relative rotation.
    /// @ingroup revolute_joint
    /// </summary>
    public struct B2RevoluteJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;
        
        /// <summary>
        /// The target angle for the joint in radians. The spring-damper will drive
        /// to this angle.
        /// </summary>
        public float targetAngle;

        /// <summary>
        /// Enable a rotational spring on the revolute hinge axis
        /// </summary>
        public bool enableSpring;

        /// <summary>
        /// The spring stiffness Hertz, cycles per second
        /// </summary>
        public float hertz;

        /// <summary>
        /// The spring damping ratio, non-dimensional
        /// </summary>
        public float dampingRatio;

        /// <summary>
        /// A flag to enable joint limits
        /// </summary>
        public bool enableLimit;

        /// <summary>
        /// The lower angle for the joint limit in radians. Minimum of -0.99*pi radians.
        /// </summary>
        public float lowerAngle;

        /// <summary>
        /// The upper angle for the joint limit in radians. Maximum of 0.99*pi radians.
        /// </summary>
        public float upperAngle;

        /// <summary>
        /// A flag to enable the joint motor
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
