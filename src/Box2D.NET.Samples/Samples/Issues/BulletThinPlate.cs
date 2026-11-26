// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-License-Identifier: MIT

using System.Numerics;
using ImGuiNET;
using static Box2D.NET.B2Ids;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.Samples.Graphics.Draws;

namespace Box2D.NET.Samples.Samples.Issues;

// From Box3D issue #219: a small bullet circle fired into a thin static plate at high speed must stay on
// the approach side. A soft contact only removes part of the approach speed in the first sub-step, which
// may carry the circle through the plate. The left plate is a polygon, the right plate a chain loop. A
// circle drawn red has crossed its plate.
public class BulletThinPlate : Sample
{
    private static readonly int SampleBulletThinPlate = SampleFactory.Shared.RegisterSample("Issues", "Bullet Thin Plate", Create);

    private const float m_plateY = 1.0f;
    private const float m_plateHalfThickness = 0.0125f;
    private const float m_plateHalfLength = 5.0f;
    private const float m_dropHeight = 2.0f;
    private const int m_fireInterval = 60;

    private B2BodyId[] m_bulletIds = new B2BodyId[2];
    private float[] m_plateX = new float[2];
    private int[] m_shotCount = new int[2];
    private int[] m_crossCount = new int[2];
    private bool[] m_crossed = new bool[2];
    private int m_stepsSinceFire = 0;
    private float m_firedRadius = 0.01f;
    private float m_radius = 0.01f;
    private float m_speed = 300.0f;
    private float m_lateralSpeed = 30.0f;
    private bool m_autoFire = true;

    private static Sample Create(SampleContext context)
    {
        return new BulletThinPlate(context);
    }

    public BulletThinPlate(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new Vector2(0.0f, m_plateY);
            m_camera.zoom = 8.0f;
        }

        m_plateX[0] = -6.0f;
        m_plateX[1] = 6.0f;

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.name = "polygon plate";
        bodyDef.position = new B2Pos(m_plateX[0], m_plateY);
        B2BodyId polygonPlateId = b2CreateBody(m_worldId, bodyDef);

        // Top face at the body origin
        B2Polygon box = b2MakeOffsetBox(m_plateHalfLength, m_plateHalfThickness, new Vector2(0.0f, -m_plateHalfThickness), b2Rot_identity);
        b2CreatePolygonShape(polygonPlateId, shapeDef, box);

        bodyDef.name = "chain plate";
        bodyDef.position = new B2Pos(m_plateX[1], m_plateY);
        B2BodyId chainPlateId = b2CreateBody(m_worldId, bodyDef);

        float h = m_plateHalfLength;
        float t = 2.0f * m_plateHalfThickness;
        Vector2[] points = { new Vector2(-h, -t), new Vector2(h, -t), new Vector2(h, 0.0f), new Vector2(-h, 0.0f) };

        B2SurfaceMaterial material = b2DefaultSurfaceMaterial();
        material.friction = 0.0f;

        B2ChainDef chainDef = b2DefaultChainDef();
        chainDef.points = points;
        chainDef.pointCount = 4;
        chainDef.materials = new[] { material };
        chainDef.materialCount = 1;
        chainDef.isLoop = true;
        b2CreateChain(chainPlateId, chainDef);

        for (int i = 0; i < 2; ++i)
        {
            m_bulletIds[i] = b2_nullBodyId;
            m_shotCount[i] = 0;
            m_crossCount[i] = 0;
            m_crossed[i] = false;
        }

        Fire();
    }

    private void Fire()
    {
        B2ShapeDef shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.0f;

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.isBullet = true;
        bodyDef.enableSleep = false;
        bodyDef.linearVelocity = new Vector2(m_lateralSpeed, -m_speed);

        B2Circle circle = new B2Circle(Vector2Zero, m_radius);

        for (int i = 0; i < 2; ++i)
        {
            if (B2_IS_NON_NULL(m_bulletIds[i]))
            {
                b2DestroyBody(m_bulletIds[i]);
            }

            bodyDef.position = new B2Pos(m_plateX[i] - m_plateHalfLength + 1.0f, m_plateY + m_dropHeight);
            m_bulletIds[i] = b2CreateBody(m_worldId, bodyDef);
            b2CreateCircleShape(m_bulletIds[i], shapeDef, circle);

            m_shotCount[i] += 1;
            m_crossed[i] = false;
        }

        m_firedRadius = m_radius;
        m_stepsSinceFire = 0;
    }

    public override bool DrawControls()
    {
        ImGui.PushItemWidth(6.0f * ImGui.GetFontSize());

        ImGui.SliderFloat("Radius", ref m_radius, 0.005f, 0.1f, "%.3f m");
        ImGui.SliderFloat("Speed", ref m_speed, 10.0f, 1000.0f, "%.0f m/s");
        ImGui.SliderFloat("Lateral", ref m_lateralSpeed, 0.0f, 100.0f, "%.0f m/s");
        ImGui.Checkbox("Auto Fire", ref m_autoFire);

        if (ImGui.Button("Fire"))
        {
            Fire();
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear Counts"))
        {
            for (int i = 0; i < 2; ++i)
            {
                m_shotCount[i] = 0;
                m_crossCount[i] = 0;
            }
        }

        ImGui.PopItemWidth();
        return true;
    }

    public override void Step()
    {
        base.Step();

        if (m_didStep)
        {
            float plateBottom = m_plateY - 2.0f * m_plateHalfThickness;
            for (int i = 0; i < 2; ++i)
            {
                B2Pos p = b2Body_GetPosition(m_bulletIds[i]);

                // A circle that slid off the far end falls past the plate legitimately
                float x = p.X - m_plateX[i];
                float y = p.Y;
                if (m_crossed[i] == false && x < m_plateHalfLength && y + m_firedRadius < plateBottom)
                {
                    m_crossed[i] = true;
                    m_crossCount[i] += 1;
                }
            }

            m_stepsSinceFire += 1;
            if (m_autoFire && m_stepsSinceFire >= m_fireInterval)
            {
                Fire();
            }
        }

        string[] names = { "polygon", "chain" };
        for (int i = 0; i < 2; ++i)
        {
            B2Pos p = b2Body_GetPosition(m_bulletIds[i]);
            DrawPoint(m_draw, new Vector2(p.X, p.Y), 10.0f, m_crossed[i] ? B2HexColor.b2_colorRed : B2HexColor.b2_colorLime);

            DrawScreenTextLine($"{names[i]} plate: {(m_crossed[i] ? "CROSSED" : "ok")}, crossed {m_crossCount[i]} of {m_shotCount[i]} shots");
        }

        float substepTime = 1.0f / (m_context.hertz * m_context.subStepCount);
        DrawScreenTextLine(System.FormattableString.Invariant(
            $"plate thickness {2.0f * m_plateHalfThickness:F3} m, travel per sub-step {m_speed * substepTime:F3} m"));
    }
}
