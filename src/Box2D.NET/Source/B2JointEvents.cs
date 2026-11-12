// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Joint events are buffered in the world and are available
    /// as event arrays after the time step is complete.
    /// Note: this data becomes invalid if joints are destroyed
    public readonly struct B2JointEvents(B2JointEvent[] jointEvents, int count)
    {
        /// Array of events
        public readonly B2JointEvent[] jointEvents = jointEvents;

        /// Number of events
        public readonly int count = count;
    }
}
