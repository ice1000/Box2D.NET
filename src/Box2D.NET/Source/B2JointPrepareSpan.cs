// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Similar for joints
    /// </summary>
    public struct B2JointPrepareSpan
    {
        public int start;
        public int count;
        public B2JointSim[] joints;
    }
}
