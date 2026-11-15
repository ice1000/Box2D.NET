// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A mover joint is used to move a dynamic character mover through velocity commands.
    /// The x and y directions are handled separately. Does not affect rotation.
    /// @ingroup mover_joint
    /// </summary>
    public struct B2MoverJointDef
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
        /// The maximum motor force in newtons.
        /// </summary>
        public Vector2 maxVelocityForce;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
