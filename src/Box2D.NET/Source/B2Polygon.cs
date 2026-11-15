// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// A solid convex polygon. It is assumed that the interior of the polygon is to
    /// the left of each edge.
    /// Polygons have a maximum number of vertices equal to B2_MAX_POLYGON_VERTICES.
    /// In most cases you should not need many vertices for a convex polygon.
    /// @warning DO NOT fill this out manually, instead use a helper function like
    /// b2MakePolygon or b2MakeBox.
    /// </summary>
    public struct B2Polygon
    {
        /// <summary>
        /// The polygon vertices
        /// </summary>
        public B2FixedArray8<Vector2> vertices; // = new B2Vec2[B2Constants.B2_MAX_POLYGON_VERTICES];

        /// <summary>
        /// The outward normal vectors of the polygon sides
        /// </summary>
        public B2FixedArray8<Vector2> normals; // = new B2Vec2[B2Constants.B2_MAX_POLYGON_VERTICES];

        /// <summary>
        /// The centroid of the polygon
        /// </summary>
        public Vector2 centroid;

        /// <summary>
        /// The external radius for rounded polygons
        /// </summary>
        public float radius;

        /// <summary>
        /// The number of polygon vertices
        /// </summary>
        public int count;
    }
}