// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    public struct B2MoverJoint
    {
        public Vector2 linearVelocity;
        public Vector2 maxVelocityForce;

        public Vector2 linearVelocityImpulse;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public float linearMass;
    }
}
