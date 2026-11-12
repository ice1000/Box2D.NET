// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    public struct B2WorldOverlapContext(B2World world, b2OverlapResultCallbackFcn callbackFcn, in B2QueryFilter filter, in B2ShapeProxy proxy, in B2Pos origin, object userContext)
    {
        public B2World world = world;
        public b2OverlapResultCallbackFcn callbackFcn = callbackFcn;
        public B2QueryFilter filter = filter;
        public B2ShapeProxy proxy = proxy;
        public B2Pos origin = origin;
        public object userContext = userContext;
    }
}
