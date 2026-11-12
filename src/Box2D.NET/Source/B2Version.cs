// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// Version numbering scheme.
    /// See https://semver.org/
    public struct B2Version(int major, int minor, int revision)
    {
        /// Significant changes
        public int major = major;

        /// Incremental changes
        public int minor = minor;

        /// Bug fixes
        public int revision = revision;
    }
}
