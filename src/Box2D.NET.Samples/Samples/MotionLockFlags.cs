// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET.Samples.Samples;

/// <summary>
/// Locks in world axes, built from a motion lock joint and body fixed rotation
/// </summary>
public struct MotionLockFlags(bool linearX, bool linearY, bool fixedRotation)
{
    public bool linearX = linearX;
    public bool linearY = linearY;
    public bool fixedRotation = fixedRotation;
}
