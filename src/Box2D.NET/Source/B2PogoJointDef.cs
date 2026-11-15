// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Pogo joint definition
    /// @ingroup pogo_joint
    /// </summary>
    public struct B2PogoJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;

        /// <summary>
        /// Normal vector at the contact
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The spring stiffness Hertz, cycles per second
        /// </summary>
        public float hertz;

        /// <summary>
        /// The spring damping ratio, non-dimensional
        /// </summary>
        public float dampingRatio;

        /// <summary>
        /// Spring length.
        /// </summary>
        public float restLength;

        /// <summary>
        /// Maximum tension force. Controls how sticky the mover is to ground.
        /// </summary>
        public float maxTensionForce;

        /// <summary>
        /// Maximum compression force. How hard the pogo pushes the mover up from the ground.
        /// </summary>
        public float maxCompressionForce;

        /// <summary>
        /// The initial pogo internal impulse. The pogo is typically recreated every frame. This
        /// gives a way to move the internal state forward for smoother spring behavior.
        /// </summary>
        public float impulse;

        /// <summary>
        /// The initial pogo internal velocity. The pogo is typically recreated every frame. This
        /// gives a way to move the internal state forward for smoother spring behavior.
        /// </summary>
        public float velocity;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
