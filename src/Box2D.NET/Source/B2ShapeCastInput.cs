// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Low level shape cast input in generic form. This allows casting an arbitrary point
    /// cloud wrap with a radius. For example, a circle is a single point with a non-zero radius.
    /// A capsule is two points with a non-zero radius. A box is four points with a zero radius.
    /// </summary>
    public struct B2ShapeCastInput
    {
        /// <summary>
        /// A generic shape
        /// </summary>
        public B2ShapeProxy proxy;

        /// <summary>
        /// The translation of the shape cast
        /// </summary>
        public Vector2 translation;

        /// <summary>
        /// The maximum fraction of the translation to consider, typically 1
        /// </summary>
        public float maxFraction;
        
        /// <summary>
        /// Allow shape cast to encroach when initially touching. This only works if the radius is greater than zero.
        /// </summary>
        public bool canEncroach;
    }
}
