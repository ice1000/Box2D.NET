// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Primitives;

public struct HitEvent
{
    public Vector2 point;
    public float speed;
    public int stepIndex;

    public void Clear()
    {
        point = new Vector2();
        speed = 0.0f;
        stepIndex = 0;
    }
}
