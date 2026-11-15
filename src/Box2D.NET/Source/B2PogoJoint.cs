// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// The pogo joint is a hybrid between a contact and a joint
    /// </summary>
    public struct B2PogoJoint
    {
        /// <summary>
        /// Settings
        /// </summary>
        public Vector2 normal;
        public float restLength;
        public float hertz;
        public float dampingRatio;
        public float maxTensionForce;
        public float maxCompressionForce;

        /// <summary>
        /// Runtime
        /// </summary>
        public float impulse;
        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public Vector2 deltaCenter;
        public float linearMass;
        public float velocity;
    }
}
