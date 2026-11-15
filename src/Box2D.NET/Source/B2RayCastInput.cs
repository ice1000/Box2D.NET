// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /**
     * @defgroup geometry Geometry
     * @brief Geometry types and algorithms
     *
     * Definitions of circles, capsules, segments, and polygons. Various algorithms to compute hulls, mass properties, and so on.
     * Functions should take the shape as the first argument to assist editor auto-complete.
     * @{
     */
    /// <summary>
    /// Low level ray cast input data
    /// </summary>
    public struct B2RayCastInput(Vector2 origin, Vector2 translation, float maxFraction)
    {
        /// <summary>
        /// Start point of the ray cast
        /// </summary>
        public Vector2 origin = origin;

        /// <summary>
        /// Translation of the ray cast
        /// </summary>
        public Vector2 translation = translation;

        /// <summary>
        /// The maximum fraction of the translation to consider, typically 1
        /// </summary>
        public float maxFraction = maxFraction;
    }
}
