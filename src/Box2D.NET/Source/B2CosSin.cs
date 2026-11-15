// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Cosine and sine pair
    /// This uses a custom implementation designed for cross-platform determinism
    /// </summary>
    public struct B2CosSin
    {
        /// <summary>
        /// cosine and sine
        /// </summary>
        public float cosine;

        public float sine;
    }
}
