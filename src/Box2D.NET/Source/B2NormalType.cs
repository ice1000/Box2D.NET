// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2NormalType
    {
        /// <summary>
        /// This means the normal points in a direction that is non-smooth relative to a convex vertex and should be skipped
        /// </summary>
        b2_normalSkip,

        /// <summary>
        /// This means the normal points in a direction that is smooth relative to a convex vertex and should be used for collision
        /// </summary>
        b2_normalAdmit,

        /// <summary>
        /// This means the normal is in a region of a concave vertex and should be snapped to the segment normal
        /// </summary>
        b2_normalSnap
    }
}
