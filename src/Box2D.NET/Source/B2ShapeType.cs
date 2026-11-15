// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Shape type
    /// @ingroup shape
    /// </summary>
    public enum B2ShapeType
    {
        /// <summary>
        /// A circle with an offset
        /// </summary>
        b2_circleShape,

        /// <summary>
        /// A capsule is an extruded circle
        /// </summary>
        b2_capsuleShape,

        /// <summary>
        /// A line segment
        /// </summary>
        b2_segmentShape,

        /// <summary>
        /// A convex polygon
        /// </summary>
        b2_polygonShape,

        /// <summary>
        /// A line segment owned by a chain shape
        /// </summary>
        b2_chainSegmentShape,

        /// <summary>
        /// The number of shape types
        /// </summary>
        b2_shapeTypeCount
    }
}
