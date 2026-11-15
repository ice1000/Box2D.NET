// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// A begin touch event is generated when two shapes begin touching.
    /// </summary>
    public struct B2ContactBeginTouchEvent(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2ContactId contactId)
    {
        /// <summary>
        /// Id of the first shape
        /// </summary>
        public B2ShapeId shapeIdA = shapeIdA;

        /// <summary>
        /// Id of the second shape
        /// </summary>
        public B2ShapeId shapeIdB = shapeIdB;

        /// <summary>
        /// The transient contact id. This contact maybe destroyed automatically when the world is modified or simulated.
        /// Used b2Contact_IsValid before using this id.
        /// </summary>
        public B2ContactId contactId = contactId;
    }
}
