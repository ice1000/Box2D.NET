// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Contact events are buffered in the Box2D world and are available
    /// as event arrays after the time step is complete.
    /// Note: these may become invalid if bodies and/or shapes are destroyed
    /// </summary>
    public struct B2ContactEvents
    {
        /// <summary>
        /// Array of begin touch events
        /// </summary>
        public B2ContactBeginTouchEvent[] beginEvents;

        /// <summary>
        /// Array of end touch events
        /// </summary>
        public B2ContactEndTouchEvent[] endEvents;

        /// <summary>
        /// Array of hit events
        /// </summary>
        public B2ContactHitEvent[] hitEvents;

        /// <summary>
        /// Number of begin touch events
        /// </summary>
        public int beginCount;

        /// <summary>
        /// Number of end touch events
        /// </summary>
        public int endCount;

        /// <summary>
        /// Number of hit events
        /// </summary>
        public int hitCount;
    }
}
