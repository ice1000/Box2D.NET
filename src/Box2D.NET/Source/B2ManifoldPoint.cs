// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
        /**@}*/
    /**
     * @defgroup collision Collision
     * @brief Functions for colliding pairs of shapes
     * @{
     */
    /// <summary>
    /// A manifold point is a contact point belonging to a contact manifold.
    /// It holds details related to the geometry and dynamics of the contact points.
    /// Box2D uses speculative collision so some contact points may be separated.
    /// You may use the totalNormalImpulse to determine if there was an interaction during
    /// the time step.
    /// </summary>
    public struct B2ManifoldPoint
    {
        /// <summary>
        /// Location of the contact point relative to bodyA's center of mass in world space.
        /// This can be converted to a world point using:
        /// b2Pos worldPointA = b2OffsetPos(b2Body_GetWorldCenter(myBodyIdA), anchorA);
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        /// </summary>
        public Vector2 anchorA;

        /// <summary>
        /// Location of the contact point relative to bodyB's center of mass in world space.
        /// This can be converted to a world point using:
        /// b2Pos worldPointB = b2OffsetPos(b2Body_GetWorldCenter(myBodyIdB), anchorB);
        /// @note When used internally to the Box2D solver, this is relative to the body center of mass.
        /// </summary>
        public Vector2 anchorB;

        /// <summary>
        /// The separation of the contact point, negative if penetrating
        /// </summary>
        public float separation;
        
        /// <summary>
        /// The impulse along the manifold normal vector.
        /// </summary>
        public float normalImpulse;

        /// <summary>
        /// The friction impulse
        /// </summary>
        public float tangentImpulse;

        /// <summary>
        /// The total normal impulse applied across sub-stepping and restitution. This is important
        /// to identify speculative contact points that had an interaction in the time step.
        /// This includes the warm starting impulse, the sub-step delta impulse, and the restitution
        /// impulse.
        /// </summary>
        public float totalNormalImpulse;

        /// <summary>
        /// Relative normal velocity pre-solve. Negative when approaching. This is only
        /// computed if hit events are enabled. It may be computed otherwise as a side effect
        /// of SIMD bundling, but that is not guaranteed.
        /// </summary>
        public float normalVelocity;

        /// <summary>
        /// Cached separation used for contact recycling
        /// </summary>
        public float baseSeparation;

        /// <summary>
        /// Uniquely identifies a contact point between two shapes
        /// </summary>
        public ushort id;

        /// <summary>
        /// Did this contact point exist the previous step?
        /// </summary>
        public bool persisted;
    }
}
