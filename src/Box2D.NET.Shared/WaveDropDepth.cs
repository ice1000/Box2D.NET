// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

using System.Numerics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.Shared.WaveDrops;

namespace Box2D.NET.Shared
{
    public class WaveDropDepth
    {
        /// <summary>
        /// Wave height at each chain vertex
        /// </summary>
        public float[] heights = new float[WAVE_DROP_MAX_WAVE_COUNT + 1];

        public int waveCount;
        public float cellWidth;

        /// <summary>
        /// Collision points in the body frame, rounded by the radius
        /// </summary>
        public Vector2[] probes = new Vector2[B2_MAX_POLYGON_VERTICES];

        public int probeCount;
        public float radius;
        public float boundingRadius;
        public float amplitude;

        /// <summary>
        /// Deepest sink of each body so far, positive when inside the wave
        /// </summary>
        public float[] bodyDepths = new float[WAVE_DROP_BODY_COUNT];

        public float maxDepth;
        public int maxIndex;
        public int maxStep;
        public int stepCount;
    }
}
