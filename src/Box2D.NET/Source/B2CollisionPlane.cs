// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// These are collision planes that can be fed to b2SolvePlanes. Normally
    /// this is assembled by the user from plane results in b2PlaneResult
    /// </summary>
    public struct B2CollisionPlane(B2Plane plane, float pushLimit, float push, bool clipVelocity)
    {
        /// <summary>
        /// The collision plane between the mover and some shape
        /// </summary>
        public B2Plane plane = plane;
        
        /// <summary>
        /// Setting this to FLT_MAX makes the plane as rigid as possible. Lower values can
        /// make the plane collision soft. Usually in meters.
        /// </summary>
        public float pushLimit = pushLimit;
        
        /// <summary>
        /// The push on the mover determined by b2SolvePlanes. Usually in meters.
        /// </summary>
        public float push = push;
        
        /// <summary>
        /// Indicates if b2ClipVector should clip against this plane. Should be false for soft collision.
        /// </summary>
        public bool clipVelocity = clipVelocity;
    }
}
