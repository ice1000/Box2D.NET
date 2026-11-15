// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A hit touch event is generated when two shapes collide with a speed faster than the hit speed threshold.
    /// This may be reported for speculative contacts that have a confirmed impulse.
    /// </summary>
    public struct B2ContactHitEvent
    {
        /// <summary>
        /// Id of the first shape
        /// </summary>
        public B2ShapeId shapeIdA;

        /// <summary>
        /// Id of the second shape
        /// </summary>
        public B2ShapeId shapeIdB;

        /// <summary>
        /// Id of the contact.
        /// @warning this contact may have been destroyed
        /// @see b2Contact_IsValid
        /// </summary>
        public B2ContactId contactId;
        
        /// <summary>
        /// Point where the shapes hit. This is a mid-point between the two surfaces. It could be
        /// a speculative point where the shapes were not touching at the beginning of the time
        /// step. The point is reconstructed after the step, so when both bodies are moving it may
        /// trail the impact by up to one step of motion.
        /// </summary>
        public B2Pos point;

        /// <summary>
        /// Normal vector pointing from shape A to shape B
        /// </summary>
        public Vector2 normal;

        /// <summary>
        /// The speed the shapes are approaching. Always positive. Typically in meters per second.
        /// </summary>
        public float approachSpeed;
    }
}
