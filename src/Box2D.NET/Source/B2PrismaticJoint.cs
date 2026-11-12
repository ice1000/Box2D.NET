// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    public struct B2PrismaticJoint
    {
        public Vector2 impulse;
        public float springImpulse;
        public float motorImpulse;
        public float lowerImpulse;
        public float upperImpulse;
        public float hertz;
        public float dampingRatio;
        public float targetTranslation;
        public float maxMotorForce;
        public float motorSpeed;
        public float lowerTranslation;
        public float upperTranslation;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public Vector2 deltaCenter;
        public B2Softness springSoftness;

        public bool enableSpring;
        public bool enableLimit;
        public bool enableMotor;
    }
}
