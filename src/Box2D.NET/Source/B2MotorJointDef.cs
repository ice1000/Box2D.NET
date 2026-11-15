// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A motor joint is used to control the relative velocity and or transform between two bodies.
    /// With a velocity of zero this acts like top-down friction.
    /// @ingroup motor_joint
    /// </summary>
    public struct B2MotorJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// The desired linear velocity
        /// </summary>
        public Vector2 linearVelocity;

        /// <summary>
        /// The maximum motor force in newtons
        /// </summary>
        public float maxVelocityForce;

        /// <summary>
        /// The desired angular velocity
        /// </summary>
        public float angularVelocity;

        /// <summary>
        /// The maximum motor torque in newton-meters
        /// </summary>
        public float maxVelocityTorque;

        /// <summary>
        /// Linear spring hertz for position control
        /// </summary>
        public float linearHertz;

        /// <summary>
        /// Linear spring damping ratio
        /// </summary>
        public float linearDampingRatio;

        /// <summary>
        /// Maximum spring force in newtons
        /// </summary>
        public float maxSpringForce;

        /// <summary>
        /// Angular spring hertz for position control
        /// </summary>
        public float angularHertz;

        /// <summary>
        /// Angular spring damping ratio
        /// </summary>
        public float angularDampingRatio;

        /// <summary>
        /// Maximum spring torque in newton-meters
        /// </summary>
        public float maxSpringTorque;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
