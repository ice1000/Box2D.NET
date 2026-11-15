// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Sensor events are buffered in the world and are available
    /// as begin/end overlap event arrays after the time step is complete.
    /// Note: these may become invalid if bodies and/or shapes are destroyed
    /// </summary>
    public struct B2SensorEvents
    {
        /// <summary>
        /// Array of sensor begin touch events
        /// </summary>
        public B2SensorBeginTouchEvent[] beginEvents;

        /// <summary>
        /// Array of sensor end touch events
        /// </summary>
        public B2SensorEndTouchEvent[] endEvents;

        /// <summary>
        /// The number of begin touch events
        /// </summary>
        public int beginCount;

        /// <summary>
        /// The number of end touch events
        /// </summary>
        public int endCount;
    }
}
