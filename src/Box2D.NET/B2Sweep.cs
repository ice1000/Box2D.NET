// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Box2D.NET
{
    /// This describes the motion of a body/shape for TOI computation. Shapes are defined with respect to the body origin,
    /// which may not coincide with the center of mass. However, to support dynamics we must interpolate the center of mass
    /// position.
    public struct B2Sweep(Vector2 localCenter, Vector2 c1, Vector2 c2, B2Rot q1, B2Rot q2)
    {
        public Vector2 localCenter = localCenter; // Local center of mass position
        public Vector2 c1 = c1; // Starting center of mass world position
        public Vector2 c2 = c2; // Ending center of mass world position
        public B2Rot q1 = q1; // Starting world rotation
        public B2Rot q2 = q2; // Ending world rotation
    }
}