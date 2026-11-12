// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2ExplosionContext(B2World world, B2Pos position, float radius, float falloff, float impulsePerLength)
    {
        public B2World world = world;
        public B2Pos position = position;
        public float radius = radius;
        public float falloff = falloff;
        public float impulsePerLength = impulsePerLength;
    }
}
