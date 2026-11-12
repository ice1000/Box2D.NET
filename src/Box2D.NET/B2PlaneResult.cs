// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// These are the collision planes returned from b2World_CollideMover
    public struct B2PlaneResult(B2Plane plane, Vector2 point, bool hit)
    {
        /// The collision plane between the mover and a convex shape
        public B2Plane plane = plane;
        
        // The collision point on the shape.
        public Vector2 point = point;
        
        /// Did the collision register a hit? If not this plane should be ignored.
        public bool hit = hit;
    }
}
