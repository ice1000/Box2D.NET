// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Shared
{
    public struct WaveDropSettings
    {
        public WaveDropShape shape;
        public float amplitude;

        /// <summary>
        /// Fine bumps instead of the broad wave. The amplitude should be a few centimeters.
        /// </summary>
        public bool rough;

        /// <summary>
        /// Let the bodies collide with each other. Off isolates chain collision.
        /// </summary>
        public bool collide;

        /// <summary>
        /// Create only this grid index, or -1 for all. Without body collision a body moves the same alone.
        /// </summary>
        public int soloIndex;
    }
}
