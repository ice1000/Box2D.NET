// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;
using Silk.NET.GLFW;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.Samples.SampleText;

namespace Box2D.NET.Samples.Samples.Joints;

public class MotionLockJoint : Sample
{
    private static readonly int SampleMotionLockJoint = SampleFactory.Shared.RegisterSample("Joints", "Motion Lock Joint", Create);

    public const int e_count = 3;

    private B2BodyId[] m_bodyIds = new B2BodyId[e_count];
    private B2BodyId m_circleId;

    private struct Preset(string name, bool x, bool y)
    {
        public string name = name;
        public bool x = x;
        public bool y = y;
    }

    private static Sample Create(SampleContext context)
    {
        return new MotionLockJoint(context);
    }

    public MotionLockJoint(SampleContext context) : base(context)
    {
        if (m_context.restart == false)
        {
            m_camera.center = new Vector2(0.0f, 5.0f);
            m_camera.zoom = 25.0f * 0.5f;
        }

        B2BodyId groundId;
        {
            B2BodyDef groundDef = b2DefaultBodyDef();
            groundId = b2CreateBody(m_worldId, groundDef);

            B2ShapeDef groundShapeDef = b2DefaultShapeDef();
            B2Segment segment = new B2Segment(new Vector2(-20.0f, 0.0f), new Vector2(20.0f, 0.0f));
            b2CreateSegmentShape(groundId, groundShapeDef, segment);
        }

        B2ShapeDef shapeDef = b2DefaultShapeDef();
        B2Polygon box = b2MakeBox(1.0f, 0.5f);

        B2BodyDef bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.enableSleep = false;

        Preset[] presets =
        [
            new Preset("point", true, true),
            new Preset("line x", false, true),
            new Preset("line y", true, false),
        ];

        for (int i = 0; i < e_count; ++i)
        {
            B2Pos position = new B2Pos(-6.0f + 6.0f * i, 4.0f);
            bodyDef.name = presets[i].name;
            bodyDef.position = position;
            m_bodyIds[i] = b2CreateBody(m_worldId, bodyDef);
            b2CreatePolygonShape(m_bodyIds[i], shapeDef, box);

            B2MotionLockJointDef jointDef = b2DefaultMotionLockJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = m_bodyIds[i];
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, position);
            jointDef.@base.collideConnected = true;
            jointDef.lockLinearX = presets[i].x;
            jointDef.lockLinearY = presets[i].y;
            b2CreateMotionLockJoint(m_worldId, jointDef);
        }

        // A heavy circle on a light platform held by a point lock and fixed rotation
        {
            B2Pos position = new B2Pos(0.0f, 8.0f);
            bodyDef.name = "platform";
            bodyDef.position = position;
            bodyDef.fixedRotation = true;
            B2BodyId platformId = b2CreateBody(m_worldId, bodyDef);
            B2Polygon platform = b2MakeBox(2.0f, 0.05f);
            b2CreatePolygonShape(platformId, shapeDef, platform);

            B2MassData massData = b2Body_GetMassData(platformId);
            massData.rotationalInertia = massData.rotationalInertia / massData.mass;
            massData.mass = 1.0f;
            b2Body_SetMassData(platformId, massData);

            B2MotionLockJointDef jointDef = b2DefaultMotionLockJointDef();
            jointDef.@base.bodyIdA = groundId;
            jointDef.@base.bodyIdB = platformId;
            jointDef.@base.localFrameA.p = b2Body_GetLocalPoint(groundId, position);
            jointDef.lockLinearX = true;
            jointDef.lockLinearY = true;
            b2CreateMotionLockJoint(m_worldId, jointDef);

            bodyDef.name = null;
            bodyDef.position = new B2Pos(0.0f, 9.0f);
            bodyDef.fixedRotation = false;
            m_circleId = b2CreateBody(m_worldId, bodyDef);
            B2Circle circle = new B2Circle(Vector2Zero, 0.5f);
            b2CreateCircleShape(m_circleId, shapeDef, circle);

            massData = b2Body_GetMassData(m_circleId);
            massData.rotationalInertia = 10.0f * massData.rotationalInertia / massData.mass;
            massData.mass = 10.0f;
            b2Body_SetMassData(m_circleId, massData);
        }
    }

    public override void Step()
    {
        if (GetKey(Keys.L) == InputAction.Press)
        {
            for (int i = 0; i < e_count; ++i)
            {
                b2Body_ApplyLinearImpulseToCenter(m_bodyIds[i], new Vector2(5.0f, 5.0f), true);
                b2Body_ApplyAngularImpulse(m_bodyIds[i], 2.0f, true);
            }
        }

        base.Step();

        DrawScreenTextLine("Point, line x, line y. Press L to push.");
        DrawScreenTextLine($"10 kg circle on a 1 kg locked platform: gap = {FormatFloat(b2Body_GetPosition(m_circleId).Y - 8.55f, 3)} m");
    }
}
