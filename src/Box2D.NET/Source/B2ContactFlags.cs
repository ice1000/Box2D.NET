// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// <summary>
    /// A contact edge is used to connect bodies and contacts together
    /// in a contact graph where each body is a node and each contact
    /// is an edge. A contact edge belongs to a doubly linked list
    /// maintained in each attached body. Each contact has two contact
    /// edges, one for each attached body.
    /// </summary>
    [Flags]
    public enum B2ContactFlags
    {
        /// <summary>
        /// Set when the solid shapes are touching.
        /// </summary>
        b2_contactTouchingFlag = 0x00000001,

        /// <summary>
        /// Contact has a hit event
        /// </summary>
        b2_contactHitEventFlag = 0x00000002,

        /// <summary>
        /// This contact wants contact events
        /// </summary>
        b2_contactEnableContactEvents = 0x00000004,

        b2_contactRecycleFlag = 0x00000008,

        /// <summary>
        /// Set when the shapes are touching
        /// </summary>
        b2_simTouchingFlag = 0x00010000,

        /// <summary>
        /// This contact no longer has overlapping AABBs
        /// </summary>
        b2_simDisjoint = 0x00020000,

        /// <summary>
        /// This contact started touching
        /// </summary>
        b2_simStartedTouching = 0x00040000,

        /// <summary>
        /// This contact stopped touching
        /// </summary>
        b2_simStoppedTouching = 0x00080000,

        /// <summary>
        /// This contact has a hit event
        /// </summary>
        b2_simEnableHitEvent = 0x00100000,

        /// <summary>
        /// This contact wants pre-solve events
        /// </summary>
        b2_simEnablePreSolveEvents = 0x00200000,

        /// <summary>
        /// This contact has a cached relative transform
        /// </summary>
        b2_simRelativeTransformValid = 0x00400000,
    }
}
