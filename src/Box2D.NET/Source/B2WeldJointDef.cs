// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Weld joint definition
    /// Connects two bodies together rigidly. This constraint provides springs to mimic
    /// soft-body simulation.
    /// @note The approximate solver in Box2D cannot hold many bodies together rigidly
    /// @ingroup weld_joint
    /// </summary>
    public struct B2WeldJointDef
    {
        /// <summary>
        /// Base joint definition
        /// </summary>
        public B2JointDef @base;
            
        /// <summary>
        /// Linear stiffness expressed as Hertz (cycles per second). Use zero for maximum stiffness.
        /// </summary>
        public float linearHertz;

        /// <summary>
        /// Angular stiffness as Hertz (cycles per second). Use zero for maximum stiffness.
        /// </summary>
        public float angularHertz;

        /// <summary>
        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        /// </summary>
        public float linearDampingRatio;

        /// <summary>
        /// Linear damping ratio, non-dimensional. Use 1 for critical damping.
        /// </summary>
        public float angularDampingRatio;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
