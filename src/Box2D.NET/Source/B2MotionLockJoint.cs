// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    public struct B2MotionLockJoint
    {
        public Vector2 linearImpulse;

        public int indexA;
        public int indexB;
        public B2Transform frameA;
        public B2Transform frameB;
        public Vector2 separation;

        public B2FixedArray2<bool> lockLinear;

        /// <summary>
        /// The linear block solve has no off diagonal elements
        /// </summary>
        public bool diagonal;
    }
}
