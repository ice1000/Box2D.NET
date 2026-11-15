// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Used to create a chain of line segments. This is designed to eliminate ghost collisions with some limitations.
    /// - chains are one-sided
    /// - chains have no mass and should be used on static bodies
    /// - chains have a counter-clockwise winding order (normal points right of segment direction)
    /// - chains are either a loop or open
    /// - the distance between any two points must be greater than B2_LINEAR_SLOP
    /// - a chain shape should not self intersect (this is not validated)
    /// - an open chain shape needs a leading and trailing ghost point.
    /// - you may overlap two open chains using the ghost points to get smooth collision.
    /// - a chain shape creates multiple line segment shapes on the body
    /// https://en.wikipedia.org/wiki/Polygonal_chain
    /// Must be initialized using b2DefaultChainDef().
    /// @warning Do not use chain shapes unless you understand the limitations. This is an advanced feature.
    /// @ingroup shape
    /// </summary>
    public struct B2ChainDef
    {
        /// <summary>
        /// Use this to store application specific shape data.
        /// </summary>
        public B2UserData userData;

        /// <summary>
        /// Array of chain points. Each consecutive pair forms a segment. For a loop
        /// the last and first point form a segment. Cloned.
        /// </summary>
        public Vector2[] points;

        /// <summary>
        /// The point count. At least 2 for an open chain and at least 3 for a loop.
        /// segmentCount = isLoop ? pointCount : pointCount - 1
        /// </summary>
        public int pointCount;

        /// <summary>
        /// Leading ghost point for an open chain. Distance from first point must be greater
        /// than B2_LINEAR_SLOP. Ignored for loops.
        /// </summary>
        public Vector2 ghost1;

        /// <summary>
        /// Ending ghost point for an open chain. Distance from last point must be greater
        /// than B2_LINEAR_SLOP. Ignored for loops.
        /// </summary>
        public Vector2 ghost2;

        /// <summary>
        /// One material for the whole chain or one for each segment. Cloned.
        /// </summary>
        public B2SurfaceMaterial[] materials;

        /// <summary>
        /// The material count. Must be 1 or the segment count.
        /// </summary>
        public int materialCount;

        /// <summary>
        /// Contact filtering data.
        /// </summary>
        public B2Filter filter;

        /// <summary>
        /// Indicates a closed chain formed by connecting the first and last point.
        /// </summary>
        public bool isLoop;
        
        /// <summary>
        /// Enable sensors to detect this chain. False by default.
        /// </summary>
        public bool enableSensorEvents;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
