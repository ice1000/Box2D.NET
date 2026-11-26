// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Numerics;
using Box2D.NET.Shared;
using ImGuiNET;
using Serilog;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Timers;
using static Box2D.NET.B2Worlds;
using static Box2D.NET.Shared.WaveDrops;

namespace Box2D.NET.Samples.Samples.Continuous;

// A 2D port of Box3D's Mesh Drop. Drops a grid of small bodies on a wave shaped chain until they sleep. Auto
// Generate repeats with new seeds and stops on a tunnel or a pile that stays awake. The wave_soak tool
// runs the same scene headless. The rough wave has bumps smaller than the bodies. Registered a second
// time as Rough Wave Drop with that wave on.
public class WaveDrop : Sample
{
    private static readonly ILogger Logger = Log.ForContext<WaveDrop>();

    private static readonly int SampleWaveDrop = SampleFactory.Shared.RegisterSample("Continuous", "Wave Drop", Create);
    private static readonly int SampleRoughWaveDrop = SampleFactory.Shared.RegisterSample("Continuous", "Rough Wave Drop", CreateRough);

    private WaveDropSettings m_settings;
    private WaveDropDepth m_depth = new WaveDropDepth();
    private B2BodyId m_groundId;
    private B2BodyId[] m_bodies = new B2BodyId[WAVE_DROP_BODY_COUNT];
    private uint m_seed;
    private int m_runCount;
    private bool m_failure;
    private bool m_autoGenerate;
    private uint m_replaySeed = 0;
    private string m_replaySeedText = "0";
    private int[] m_solo = { -1, -1 };

    private static Sample Create(SampleContext context)
    {
        return new WaveDrop(context, false);
    }

    private static Sample CreateRough(SampleContext context)
    {
        return new WaveDrop(context, true);
    }

    public WaveDrop(SampleContext context, bool rough) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new Vector2(0.0f, 5.0f);
            m_camera.zoom = rough ? 12.0f : 22.0f;
        }

        m_settings = GetDefaultWaveDropSettings();
        m_settings.rough = rough;
        m_settings.amplitude = GetWaveDropDefaultAmplitude(rough);
        m_groundId = b2_nullBodyId;
        CreateGround();

        for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
        {
            m_bodies[i] = b2_nullBodyId;
        }

        m_seed = 0;
        m_runCount = 0;
        m_failure = false;
        m_autoGenerate = false;

        Generate();
    }

    private void CreateGround()
    {
        if (B2_IS_NON_NULL(m_groundId))
        {
            b2DestroyBody(m_groundId);
            m_groundId = b2_nullBodyId;
        }

        m_groundId = CreateWaveDropGround(m_worldId, m_settings);
    }

    private void Generate()
    {
        for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
        {
            if (B2_IS_NULL(m_bodies[i]))
            {
                continue;
            }

            b2DestroyBody(m_bodies[i]);
            m_bodies[i] = b2_nullBodyId;
        }

        // A non-zero seed replays a generation reported by the wave_soak tool
        m_seed = m_replaySeed != 0 ? m_replaySeed : (uint)b2GetTicks();

        m_runCount += 1;
        m_stepCount = 0;
        m_failure = false;
        Logger.Information("run {Run} seed {Seed}", m_runCount, m_seed);

        WaveDropSettings settings = m_settings;
        if (m_solo[0] >= 0 && m_solo[1] >= 0)
        {
            settings.soloIndex = WAVE_DROP_GRID_COUNT * m_solo[0] + m_solo[1];
        }

        CreateWaveDropBodies(m_worldId, settings, m_seed, m_bodies);
        ResetWaveDropDepth(m_depth, settings);
    }

    public override bool DrawControls()
    {
        ImGui.PushItemWidth(8.0f * ImGui.GetFontSize());

        int shapeType = (int)m_settings.shape;
        if (ImGui.Combo("Type", ref shapeType, g_waveDropShapeNames, (int)WaveDropShape.e_waveDropShapeCount))
        {
            m_settings.shape = (WaveDropShape)shapeType;
            Generate();
        }

        if (ImGui.Checkbox("Rough", ref m_settings.rough))
        {
            m_settings.amplitude = GetWaveDropDefaultAmplitude(m_settings.rough);
            CreateGround();
            Generate();
        }

        float maxAmplitude = m_settings.rough ? 0.05f : 1.0f;
        if (ImGui.SliderFloat("Amplitude", ref m_settings.amplitude, 0.0f, maxAmplitude, "%.3f"))
        {
            CreateGround();
            Generate();
        }

        if (ImGui.Checkbox("Collide", ref m_settings.collide))
        {
            Generate();
        }

        if (ImGui.InputText("Seed", ref m_replaySeedText, 16, ImGuiInputTextFlags.CharsDecimal))
        {
            m_replaySeed = uint.TryParse(m_replaySeedText, NumberStyles.None, CultureInfo.InvariantCulture, out uint seed) ? seed : 0;
        }

        // Drop only the body at these indices, -1 for all
        if (ImGui.InputInt2("Solo", ref m_solo[0]))
        {
            m_solo[0] = b2ClampInt(m_solo[0], -1, WAVE_DROP_GRID_COUNT - 1);
            m_solo[1] = b2ClampInt(m_solo[1], -1, WAVE_DROP_GRID_COUNT - 1);
        }

        ImGui.PopItemWidth();

        if (ImGui.Button("Generate"))
        {
            Generate();
        }

        if (ImGui.Button(m_autoGenerate ? "Stop Auto Generate" : "Auto Generate"))
        {
            m_autoGenerate = !m_autoGenerate;
            m_stepCount = 0;
        }

        return true;
    }

    public override void Step()
    {
        int stepCount = m_stepCount;
        base.Step();

        if (m_stepCount != stepCount)
        {
            UpdateWaveDropDepth(m_worldId, m_depth);
        }

        // Grid indices of the body under the mouse, for the Solo field
        {
            float bestDistance = 0.25f;
            int bestIndex = -1;
            for (int i = 0; i < WAVE_DROP_BODY_COUNT; ++i)
            {
                if (B2_IS_NULL(m_bodies[i]))
                {
                    continue;
                }

                B2Pos p = b2Body_GetPosition(m_bodies[i]);
                float distance = b2Length(new Vector2(p.X - m_mousePoint.X, p.Y - m_mousePoint.Y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                DrawScreenTextLine($"indices: ({bestIndex / WAVE_DROP_GRID_COUNT}, {bestIndex % WAVE_DROP_GRID_COUNT})");
            }
        }

        DrawScreenTextLine($"run {m_runCount} seed {m_seed}");

        if (m_depth.maxIndex >= 0)
        {
            int index = m_depth.maxIndex;
            DrawScreenTextLine(System.FormattableString.Invariant(
                $"deepest: ({index / WAVE_DROP_GRID_COUNT}, {index % WAVE_DROP_GRID_COUNT}) {m_depth.maxDepth / B2_LINEAR_SLOP:F1} slop at step {m_depth.maxStep}"));
        }

        if (m_failure)
        {
            DrawScreenTextLine("FAILED, see the console");
        }

        float tunnelHeight = GetWaveDropTunnelHeight();
        for (int i = 0; i < WAVE_DROP_BODY_COUNT && m_failure == false; ++i)
        {
            B2BodyId bodyId = m_bodies[i];
            if (B2_IS_NULL(bodyId))
            {
                continue;
            }

            B2Pos center = b2Body_GetWorldCenter(bodyId);
            if (center.Y < tunnelHeight)
            {
                Logger.Information("tunnel: seed {Seed} body ({I}, {J}) at ({X:F2}, {Y:F2})", m_seed, i / WAVE_DROP_GRID_COUNT,
                    i % WAVE_DROP_GRID_COUNT, center.X, center.Y);
                m_context.pause = true;
                m_failure = true;
                m_autoGenerate = false;
            }
        }

        if (m_autoGenerate)
        {
            B2BodyEvents bodyEvents = b2World_GetBodyEvents(m_worldId);
            if (bodyEvents.moveCount == 0)
            {
                Generate();
            }

            float timeStep = 0.0f;
            if (m_context.pause == false || m_context.singleStep)
            {
                timeStep = m_context.hertz > 0.0f ? 1.0f / m_context.hertz : 0.0f;
                m_context.singleStep = false;
            }

            // Many steps
            if (timeStep > 0.0f)
            {
                int n = 20;
                for (int i = 0; i < n; ++i)
                {
                    b2World_Step(m_worldId, timeStep, m_context.subStepCount);
                    UpdateWaveDropDepth(m_worldId, m_depth);
                }

                m_stepCount += n;
            }

            if (m_stepCount > GetWaveDropStepLimit(m_settings.shape))
            {
                Logger.Information("awake: seed {Seed}", m_seed);
                m_context.pause = true;
                m_failure = true;
                m_autoGenerate = false;
            }
        }
    }
}
