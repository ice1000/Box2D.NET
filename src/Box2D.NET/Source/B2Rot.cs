// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace Box2D.NET
{
    /// <summary>
    /// 2D rotation
    /// This is similar to using a complex number for rotation
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct B2Rot(float c, float s)
    {
        /// <summary>
        /// cosine and sine
        /// </summary>
        public float c = c, s = s;
    }
}
