// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// These are the collision planes returned from b2World_CollideMover
    /// </summary>
    public struct B2PlaneResult(B2Plane plane, Vector2 point, bool hit)
    {
        /// <summary>
        /// The collision plane between the mover and a convex shape
        /// </summary>
        public B2Plane plane = plane;
        
        /// <summary>
        /// The collision point on the shape.
        /// </summary>
        public Vector2 point = point;
        
        /// <summary>
        /// Did the collision register a hit? If not this plane should be ignored.
        /// </summary>
        public bool hit = hit;
    }
}
