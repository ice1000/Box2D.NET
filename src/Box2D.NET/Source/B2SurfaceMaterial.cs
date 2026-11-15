// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Surface materials allow chain shapes to have per segment surface properties.
    /// @ingroup shape
    /// </summary>
    public struct B2SurfaceMaterial
    {
        /// <summary>
        /// The Coulomb (dry) friction coefficient, usually in the range [0,1].
        /// </summary>
        public float friction;

        /// <summary>
        /// The coefficient of restitution (bounce) usually in the range [0,1].
        /// https://en.wikipedia.org/wiki/Coefficient_of_restitution
        /// </summary>
        public float restitution;

        /// <summary>
        /// The rolling resistance usually in the range [0,1].
        /// </summary>
        public float rollingResistance;

        /// <summary>
        /// The tangent speed for conveyor belts
        /// </summary>
        public float tangentSpeed;

        /// <summary>
        /// User material identifier. This is passed with query results and to friction and restitution
        /// combining functions. It is not used internally.
        /// </summary>
        public ulong userMaterialId;

        /// <summary>
        /// Custom debug draw color.
        /// </summary>
        public uint customColor;
    }
}
