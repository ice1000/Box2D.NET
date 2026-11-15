// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Body events are buffered in the Box2D world and are available
    /// as event arrays after the time step is complete.
    /// Note: this data becomes invalid if bodies are destroyed
    /// </summary>
    public struct B2BodyEvents(B2BodyMoveEvent[] moveEvents, int moveCount)
    {
        /// <summary>
        /// Array of move events
        /// </summary>
        public B2BodyMoveEvent[] moveEvents = moveEvents;

        /// <summary>
        /// Number of move events
        /// </summary>
        public int moveCount = moveCount;
    }
}
