// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.B2Ids;

namespace Box2D.NET.Shared
{
    // Wave Drop scene, shared by the Continuous / Wave Drop sample and the headless wave soak. A 2D port
    // of Box3D's Mesh Drop. A generation drops a grid of small bodies with random velocities on a wave shaped
    // chain loop and steps until they sleep. A body that falls out of the world tunneled. The depth tracker
    // measures how far each body sinks into the wave, which flags contact trouble far more often than tunnels
    // do. The grid is stacked in height, so the top rows land faster than the bottom rows. The rough wave has
    // bumps smaller than the bodies, so a body touches many chain segments with different normals.
    public static class WaveDrops
    {
        // Bodies per axis. A debug build drops a smaller patch at the same spacing so the samples app stays
        // responsive. The soak tool runs in release and keeps the full grid.
        public const int WAVE_DROP_GRID_COUNT = 32;
        public const int WAVE_DROP_BODY_COUNT = WAVE_DROP_GRID_COUNT * WAVE_DROP_GRID_COUNT;
        public const int WAVE_DROP_DEPTH_LEVEL_COUNT = 4;

        // Most wave cells, reached by the rough wave
        public const int WAVE_DROP_MAX_WAVE_COUNT = 640;

        private const int WAVE_COUNT = 40;
        private const float WAVE_CELL_WIDTH = 1.0f;
        private const float WAVE_ROW_FREQUENCY = 0.1f;
        private const float WAVE_COLUMN_FREQUENCY = 0.2f;
        private const float WAVE_AMPLITUDE = 0.5f;

        // Bumps about 20 cm across, close to the curvature of the bodies, on cells a few times smaller. The cell
        // width is a power of two so the chain vertices land where the depth tracker computes them.
        private const int ROUGH_WAVE_COUNT = 640;
        private const float ROUGH_CELL_WIDTH = 0.03125f;
        private const float ROUGH_ROW_FREQUENCY = 4.7f;
        private const float ROUGH_COLUMN_FREQUENCY = 5.9f;
        private const float ROUGH_AMPLITUDE = 0.015f;

        private const float DROP_SPACING = 0.5f;
        private const float DROP_HEIGHT = 5.0f;
        private const float BODY_RADIUS = 0.05f;
        private const float CAPSULE_HALF_LENGTH = 0.2f;

        // The bottom of the chain loop. A body that crosses the wave passes out through the one sided bottom.
        private const float GROUND_DEPTH = 1.0f;

        public static readonly string[] g_waveDropShapeNames = { "box", "capsule", "rounded", "circle" };

        // Sink depth thresholds in linear slop
        public static readonly float[] g_waveDropDepthLevels = { 4.0f, 8.0f, 16.0f, 32.0f };

        // Same XorShift32 and mapping as the samples, but with local state so worlds can run on many threads
        private static float RandomFloatRange(ref uint state, float lo, float hi)
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;

            int i = (int)(x % 32768);
            float r = (float)(i & 32767) / 32767.0f;
            return (hi - lo) * r + lo;
        }

        private static uint HashSeed(uint x)
        {
            unchecked
            {
                x ^= x >> 16;
                x *= 0x7feb352du;
                x ^= x >> 15;
                x *= 0x846ca68bu;
                x ^= x >> 16;
            }

            return x != 0 ? x : 1;
        }

        private struct WaveGrid(int count, float cellWidth, float rowFrequency, float columnFrequency)
        {
            public int count = count;
            public float cellWidth = cellWidth;
            public float rowFrequency = rowFrequency;
            public float columnFrequency = columnFrequency;
        }

        private static WaveGrid GetWaveGrid(bool rough)
        {
            if (rough)
            {
                return new WaveGrid(ROUGH_WAVE_COUNT, ROUGH_CELL_WIDTH, ROUGH_ROW_FREQUENCY, ROUGH_COLUMN_FREQUENCY);
            }

            return new WaveGrid(WAVE_COUNT, WAVE_CELL_WIDTH, WAVE_ROW_FREQUENCY, WAVE_COLUMN_FREQUENCY);
        }

        // The Box3D mesh wave is a product of two sines across the grid. This is that surface along its diagonal.
        private static float GetWaveHeight(WaveGrid grid, float amplitude, int index)
        {
            float omegaA = 2.0f * B2_PI * grid.rowFrequency * grid.cellWidth;
            float omegaB = 2.0f * B2_PI * grid.columnFrequency * grid.cellWidth;
            return amplitude * b2ComputeCosSin(omegaA * index).sine * b2ComputeCosSin(omegaB * index).sine;
        }

        private static B2Polygon MakeDropBox()
        {
            // The soft contact solver cannot always resolve a thinner box at 4 sub-steps, and its size gets
            // close to the time of impact tolerances
            return b2MakeBox(0.02f, 0.2f);
        }

        private static B2Polygon MakeDropRounded()
        {
            return b2MakeRoundedBox(0.03f, 0.17f, 0.02f);
        }

        public static WaveDropSettings GetDefaultWaveDropSettings()
        {
            WaveDropSettings settings = new WaveDropSettings();
            settings.shape = WaveDropShape.e_waveDropBox;
            settings.amplitude = WAVE_AMPLITUDE;
            settings.rough = false;
            settings.collide = false;
            settings.soloIndex = -1;
            return settings;
        }

        public static float GetWaveDropDefaultAmplitude(bool rough)
        {
            return rough ? ROUGH_AMPLITUDE : WAVE_AMPLITUDE;
        }

        // Hashes the base before adding the generation, so nearby base seeds share no generations
        public static uint GetWaveDropSeed(uint baseSeed, int generation)
        {
            return HashSeed(unchecked(HashSeed(baseSeed) + (uint)generation));
        }

        // Steps a generation may take to fall asleep
        public static int GetWaveDropStepLimit(WaveDropShape shape)
        {
            return shape == WaveDropShape.e_waveDropCapsule ? 3000 : 1000;
        }

        // A body below this height tunneled
        public static float GetWaveDropTunnelHeight()
        {
            return -2.0f;
        }

        // Creates a static body with the wave chain and two walls
        public static B2BodyId CreateWaveDropGround(B2WorldId worldId, WaveDropSettings settings)
        {
            B2BodyDef bodyDef = b2DefaultBodyDef();
            B2BodyId groundId = b2CreateBody(worldId, bodyDef);

            WaveGrid grid = GetWaveGrid(settings.rough);
            float extent = 0.5f * grid.count * grid.cellWidth;

            // Counter clockwise so the one sided segments face out. The wave runs right to left.
            int pointCount = grid.count + 3;
            Vector2[] points = new Vector2[pointCount];
            for (int i = 0; i <= grid.count; ++i)
            {
                int index = grid.count - i;
                points[i] = new Vector2(-extent + index * grid.cellWidth, GetWaveHeight(grid, settings.amplitude, index));
            }

            points[grid.count + 1] = new Vector2(-extent, -GROUND_DEPTH);
            points[grid.count + 2] = new Vector2(extent, -GROUND_DEPTH);

            B2SurfaceMaterial material = b2DefaultSurfaceMaterial();

            B2ChainDef chainDef = b2DefaultChainDef();
            chainDef.points = points;
            chainDef.pointCount = pointCount;
            chainDef.materials = new[] { material };
            chainDef.materialCount = 1;
            chainDef.filter.categoryBits = 1;
            chainDef.isLoop = true;
            b2CreateChain(groundId, chainDef);

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            shapeDef.filter.categoryBits = 1;

            float halfHeight = 1.0f;
            for (int i = 0; i < 2; ++i)
            {
                Vector2 center = new Vector2(i == 0 ? -extent : extent, halfHeight);
                B2Polygon wall = b2MakeOffsetBox(0.1f, halfHeight, center, b2Rot_identity);
                b2CreatePolygonShape(groundId, shapeDef, wall);
            }

            return groundId;
        }

        // Creates the bodies of one generation in grid order. Bodies left out by the solo index are null. The
        // body user data encodes the grid index, see GetWaveDropBodyIndex.
        public static void CreateWaveDropBodies(B2WorldId worldId, WaveDropSettings settings, uint seed, B2BodyId[] bodies)
        {
            B2Polygon box = MakeDropBox();
            B2Polygon rounded = MakeDropRounded();
            B2Capsule capsule = new B2Capsule(new Vector2(0.0f, -CAPSULE_HALF_LENGTH), new Vector2(0.0f, CAPSULE_HALF_LENGTH), BODY_RADIUS);
            B2Circle circle = new B2Circle(Vector2Zero, BODY_RADIUS);

            B2BodyDef bodyDef = b2DefaultBodyDef();
            bodyDef.type = B2BodyType.b2_dynamicBody;

            B2ShapeDef shapeDef = b2DefaultShapeDef();
            bool round = settings.shape == WaveDropShape.e_waveDropCapsule || settings.shape == WaveDropShape.e_waveDropCircle;
            shapeDef.material.rollingResistance = round ? 0.4f : 0.1f;

            if (settings.collide == false)
            {
                shapeDef.filter.categoryBits = 2;
                shapeDef.filter.maskBits = 1;
            }

            uint state = seed;
            int index = 0;
            for (int i = 0; i < WAVE_DROP_GRID_COUNT; ++i)
            {
                for (int j = 0; j < WAVE_DROP_GRID_COUNT; ++j)
                {
                    // Draw for every body so a solo body gets the same velocity it has in the full drop
                    Vector2 linearVelocity;
                    linearVelocity.X = RandomFloatRange(ref state, -1.0f, 1.0f);
                    linearVelocity.Y = RandomFloatRange(ref state, -1.0f, 1.0f);
                    float angularVelocity = RandomFloatRange(ref state, -5.0f, 5.0f);

                    if (settings.soloIndex >= 0 && settings.soloIndex != index)
                    {
                        bodies[index] = b2_nullBodyId;
                        index += 1;
                        continue;
                    }

                    bodyDef.position = new B2Pos(DROP_SPACING * (i - 0.5f * WAVE_DROP_GRID_COUNT), DROP_HEIGHT + DROP_SPACING * j);
                    bodyDef.linearVelocity = linearVelocity;
                    bodyDef.angularVelocity = angularVelocity;
                    bodyDef.userData = B2UserData.Signed(index + 1);
                    B2BodyId bodyId = b2CreateBody(worldId, bodyDef);

                    switch (settings.shape)
                    {
                        case WaveDropShape.e_waveDropBox:
                            b2CreatePolygonShape(bodyId, shapeDef, box);
                            break;
                        case WaveDropShape.e_waveDropCapsule:
                            b2CreateCapsuleShape(bodyId, shapeDef, capsule);
                            break;
                        case WaveDropShape.e_waveDropRounded:
                            b2CreatePolygonShape(bodyId, shapeDef, rounded);
                            break;
                        default:
                            b2CreateCircleShape(bodyId, shapeDef, circle);
                            break;
                    }

                    bodies[index] = bodyId;
                    index += 1;
                }
            }
        }

        // Grid index from body user data, or -1 if the body is not part of the drop
        public static int GetWaveDropBodyIndex(B2UserData userData)
        {
            long value = userData.GetSigned(0);
            return 1 <= value && value <= WAVE_DROP_BODY_COUNT ? (int)value - 1 : -1;
        }

        // Starts tracking a new generation
        public static void ResetWaveDropDepth(WaveDropDepth depth, WaveDropSettings settings)
        {
            WaveGrid grid = GetWaveGrid(settings.rough);
            for (int i = 0; i <= grid.count; ++i)
            {
                depth.heights[i] = GetWaveHeight(grid, settings.amplitude, i);
            }

            depth.waveCount = grid.count;
            depth.cellWidth = grid.cellWidth;
            depth.amplitude = MathF.Abs(settings.amplitude);
            depth.probeCount = 0;
            depth.radius = 0.0f;

            switch (settings.shape)
            {
                case WaveDropShape.e_waveDropBox:
                case WaveDropShape.e_waveDropRounded:
                {
                    B2Polygon polygon = settings.shape == WaveDropShape.e_waveDropBox ? MakeDropBox() : MakeDropRounded();
                    for (int i = 0; i < polygon.count; ++i)
                    {
                        depth.probes[i] = polygon.vertices[i];
                    }

                    depth.probeCount = polygon.count;
                    depth.radius = polygon.radius;
                }
                    break;

                case WaveDropShape.e_waveDropCapsule:
                    depth.probes[0] = new Vector2(0.0f, -CAPSULE_HALF_LENGTH);
                    depth.probes[1] = new Vector2(0.0f, CAPSULE_HALF_LENGTH);
                    depth.probeCount = 2;
                    depth.radius = BODY_RADIUS;
                    break;

                default:
                    depth.probes[0] = Vector2Zero;
                    depth.probeCount = 1;
                    depth.radius = BODY_RADIUS;
                    break;
            }

            float maxLength = 0.0f;
            for (int i = 0; i < depth.probeCount; ++i)
            {
                maxLength = MathF.Max(maxLength, b2Length(depth.probes[i]));
            }

            depth.boundingRadius = maxLength + depth.radius;

            for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
            {
                depth.bodyDepths[i] = -float.MaxValue;
            }

            depth.maxDepth = -float.MaxValue;
            depth.maxIndex = -1;
            depth.maxStep = 0;
            depth.stepCount = 0;
        }

        // Signed distance to the line of the wave segment under the point. Exact away from the joints, where it
        // reads a little deep on a crest and shallow in a valley.
        private static float GetWaveDistance(WaveDropDepth depth, Vector2 p)
        {
            float w = depth.cellWidth;
            float origin = -0.5f * depth.waveCount * w;
            int ix = b2ClampInt((int)MathF.Floor((p.X - origin) / w), 0, depth.waveCount - 1);

            Vector2 a = new Vector2(origin + ix * w, depth.heights[ix]);
            Vector2 b = new Vector2(origin + (ix + 1) * w, depth.heights[ix + 1]);
            Vector2 normal = b2Normalize(b2LeftPerp(b2Sub(b, a)));
            return b2Dot(normal, b2Sub(p, a));
        }

        // Call after each world step
        public static void UpdateWaveDropDepth(B2WorldId worldId, WaveDropDepth depth)
        {
            depth.stepCount += 1;

            B2BodyEvents events = b2World_GetBodyEvents(worldId);
            for (int i = 0; i < events.moveCount; ++i)
            {
                ref readonly B2BodyMoveEvent @event = ref events.moveEvents[i];
                int index = GetWaveDropBodyIndex(@event.userData);
                if (index < 0)
                {
                    continue;
                }

                B2WorldTransform transform = @event.transform;
                if ((float)transform.p.Y - depth.boundingRadius > depth.amplitude)
                {
                    continue;
                }

                float sink = -float.MaxValue;
                for (int j = 0; j < depth.probeCount; ++j)
                {
                    B2Pos point = b2TransformWorldPoint(transform, depth.probes[j]);
                    Vector2 p = new Vector2((float)point.X, (float)point.Y);
                    sink = MathF.Max(sink, depth.radius - GetWaveDistance(depth, p));
                }

                if (sink > depth.bodyDepths[index])
                {
                    depth.bodyDepths[index] = sink;
                }

                if (sink > depth.maxDepth)
                {
                    depth.maxDepth = sink;
                    depth.maxIndex = index;
                    depth.maxStep = depth.stepCount;
                }
            }
        }

        // Number of bodies that sank deeper than each depth level
        public static void GetWaveDropDepthCounts(WaveDropDepth depth, int[] counts)
        {
            for (int level = 0; level < WAVE_DROP_DEPTH_LEVEL_COUNT; ++level)
            {
                float threshold = g_waveDropDepthLevels[level] * B2_LINEAR_SLOP;
                int count = 0;
                for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
                {
                    count += depth.bodyDepths[i] > threshold ? 1 : 0;
                }

                counts[level] = count;
            }
        }
    }
}
