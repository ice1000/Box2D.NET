// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldQueryContext(B2World world, b2OverlapResultCallbackFcn callbackFcn, in B2QueryFilter filter, object userContext)
    {
        public B2World world = world;
        public b2OverlapResultCallbackFcn callbackFcn = callbackFcn;
        public B2QueryFilter filter = filter;
        public object userContext = userContext;
    }
}
