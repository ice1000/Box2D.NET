// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Motion locks to restrict the body movement
    /// </summary>
    public struct B2MotionLocks(bool linearX, bool linearY, bool angularZ)
    {
        /// <summary>
        /// Prevent translation along the x-axis
        /// </summary>
        public bool linearX = linearX;

        /// <summary>
        /// Prevent translation along the y-axis
        /// </summary>
        public bool linearY = linearY;

        /// <summary>
        /// Prevent rotation around the z-axis
        /// </summary>
        public bool angularZ = angularZ;
    }
}
