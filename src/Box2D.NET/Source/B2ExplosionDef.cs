// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// The explosion definition is used to configure options for explosions. Explosions
    /// consider shape geometry when computing the impulse.
    /// @ingroup world
    /// </summary>
    public struct B2ExplosionDef
    {
        /// <summary>
        /// Mask bits to filter shapes
        /// </summary>
        public ulong maskBits;

        /// <summary>
        /// The center of the explosion in world space
        /// </summary>
        public B2Pos position;

        /// <summary>
        /// The radius of the explosion
        /// </summary>
        public float radius;

        /// <summary>
        /// The falloff distance beyond the radius. Impulse is reduced to zero at this distance.
        /// </summary>
        public float falloff;

        /// <summary>
        /// Impulse per unit length. This applies an impulse according to the shape perimeter that
        /// is facing the explosion. Explosions only apply to circles, capsules, and polygons. This
        /// may be negative for implosions.
        /// </summary>
        public float impulsePerLength;
    }
}
