// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Soft contact constraints with sub-stepping support
    /// Uses fixed anchors for Jacobians for better behavior on rolling shapes (circles &amp; capsules)
    /// http://mmacklin.com/smallsteps.pdf
    /// https://box2d.org/files/ErinCatto_SoftConstraints_GDC2011.pdf
    /// </summary>
    public struct B2ContactConstraintWide
    {
        /// <summary>
        /// Read common
        /// </summary>
        public B2FixedArray4<int> indexA; // = new int[B2_SIMD_WIDTH];
        public B2FixedArray4<int> indexB; // = new int[B2_SIMD_WIDTH];

        public Vector4 invMassA, invMassB;
        public Vector4 invIA, invIB;
        public B2Vec2W normal;
        public B2Vec2W anchorA1, anchorB1;
        public B2Vec2W anchorA2, anchorB2;
        public Vector4 normalMass1, normalMass2;
        public Vector4 baseSeparation1;
        public Vector4 baseSeparation2;

        /// <summary>
        /// Write in push and solve
        /// </summary>
        public Vector4 normalImpulse1, normalImpulse2;
        public Vector4 totalNormalImpulse1, totalNormalImpulse2;

        /// <summary>
        /// Write in solve
        /// </summary>
        public Vector4 tangentImpulse1, tangentImpulse2;
        public Vector4 rollingImpulse;

        /// <summary>
        /// Read in solve
        /// </summary>
        public Vector4 friction;
        public Vector4 tangentSpeed;
        public Vector4 rollingResistance;
        public Vector4 tangentMass1, tangentMass2;
        public Vector4 restitution;
        public Vector4 relativeVelocity1, relativeVelocity2;
        public Vector4 restitutionImpulse1, restitutionImpulse2;
    }
}
