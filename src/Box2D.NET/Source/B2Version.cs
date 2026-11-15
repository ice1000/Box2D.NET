// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Version numbering scheme.
    /// See https://semver.org/
    /// </summary>
    public struct B2Version(int major, int minor, int revision)
    {
        /// <summary>
        /// Significant changes
        /// </summary>
        public int major = major;

        /// <summary>
        /// Incremental changes
        /// </summary>
        public int minor = minor;

        /// <summary>
        /// Bug fixes
        /// </summary>
        public int revision = revision;
    }
}
