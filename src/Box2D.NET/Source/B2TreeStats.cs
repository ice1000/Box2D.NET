// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// These are performance results returned by dynamic tree queries.
    /// </summary>
    public struct B2TreeStats
    {
        /// <summary>
        /// Number of internal nodes visited during the query
        /// </summary>
        public int nodeVisits;

        /// <summary>
        /// Number of leaf nodes visited during the query
        /// </summary>
        public int leafVisits;
    }
}
