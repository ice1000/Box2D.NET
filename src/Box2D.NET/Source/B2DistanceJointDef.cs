// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Distance joint definition
    /// Connects a point on body A with a point on body B by a segment.
    /// Useful for ropes and springs.
    /// @ingroup distance_joint
    /// </summary>
    public struct B2DistanceJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// The rest length of this joint. Clamped to a stable minimum value.
        /// </summary>
        public float length;

        /// <summary>
        /// Enable the distance constraint to behave like a spring. If false
        /// then the distance joint will be rigid, overriding the limit and motor.
        /// </summary>
        public bool enableSpring;
        
        /// <summary>
        /// The lower spring force controls how much tension it can sustain
        /// </summary>
        public float lowerSpringForce;

        /// <summary>
        /// The upper spring force controls how much compression it an sustain
        /// </summary>
        public float upperSpringForce;

        /// <summary>
        /// The spring linear stiffness Hertz, cycles per second
        /// </summary>
        public float hertz;

        /// <summary>
        /// The spring linear damping ratio, non-dimensional
        /// </summary>
        public float dampingRatio;

        /// <summary>
        /// Enable/disable the joint limit
        /// </summary>
        public bool enableLimit;

        /// <summary>
        /// Minimum length for limit. Clamped to a stable minimum value.
        /// </summary>
        public float minLength;

        /// <summary>
        /// Maximum length for limit. Must be greater than or equal to the minimum length.
        /// </summary>
        public float maxLength;

        /// <summary>
        /// Enable/disable the joint motor
        /// </summary>
        public bool enableMotor;

        /// <summary>
        /// The maximum motor force, usually in newtons
        /// </summary>
        public float maxMotorForce;

        /// <summary>
        /// The desired motor speed, usually in meters per second
        /// </summary>
        public float motorSpeed;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
