// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Motion locks to restrict the body movement
    public struct B2MotionLocks(bool linearX, bool linearY, bool angularZ)
    {
        /// Prevent translation along the x-axis
        public bool linearX = linearX;

        /// Prevent translation along the y-axis
        public bool linearY = linearY;

        /// Prevent rotation around the z-axis
        public bool angularZ = angularZ;
    }
}
