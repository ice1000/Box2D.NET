// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Prismatic joint definition
    /// Body B may slide along the x-axis in local frame A. Body B cannot rotate relative to body A.
    /// The joint translation is zero when the local frame origins coincide in world space.
    /// @ingroup prismatic_joint
    /// </summary>
    public struct B2PrismaticJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// Enable a linear spring along the prismatic joint axis
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
        /// The target translation for the joint in meters. The spring-damper will drive
        /// to this translation.
        /// </summary>
        public float targetTranslation;

        /// <summary>
        /// Enable/disable the joint limit
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
        /// Enable/disable the joint motor
        /// </summary>
        public bool enableMotor;

        /// <summary>
        /// The maximum motor force, typically in newtons
        /// </summary>
        public float maxMotorForce;

        /// <summary>
        /// The desired motor speed, typically in meters per second
        /// </summary>
        public float motorSpeed;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
