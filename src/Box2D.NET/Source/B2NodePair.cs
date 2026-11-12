// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2NodePair(B2TreeNode a, B2TreeNode b)
    {
        public B2TreeNode a = a;
        public B2TreeNode b = b;
    }
}
