// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public enum B2StepPhase
    {
        b2_stepIdle,
        b2_stepBegin,
        b2_stepCrossPairs,
        b2_stepSelfPairs,
        b2_stepCollide,
        b2_stepSolve,
        b2_stepFinalize,
        b2_stepBullets,
        b2_stepSensors,
    }
}
