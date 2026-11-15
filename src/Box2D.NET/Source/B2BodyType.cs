// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// The body simulation type.
    /// Each body is one of these three types. The type determines how the body behaves in the simulation.
    /// @ingroup body
    /// </summary>
    public enum B2BodyType
    {
        /// <summary>
        /// zero mass, zero velocity, may be manually moved
        /// </summary>
        b2_staticBody = 0,

        /// <summary>
        /// zero mass, velocity set by user, moved by solver
        /// </summary>
        b2_kinematicBody = 1,

        /// <summary>
        /// positive mass, velocity determined by forces, moved by solver
        /// </summary>
        b2_dynamicBody = 2,

        /// <summary>
        /// number of body types
        /// </summary>
        b2_bodyTypeCount,
    }
}
