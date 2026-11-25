// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    [Flags]
    public enum B2BodyFlags
    {
        /// <summary>
        /// This body has fixed rotation
        /// </summary>
        b2_fixedRotation = 0x00000001,

        /// <summary>
        /// This flag is used for debug draw and contact recycling. Only on b2BodySim.
        /// </summary>
        b2_isFast = 0x00000008,

        /// <summary>
        /// This dynamic body does a final CCD pass against all body types, but not other bullets
        /// </summary>
        b2_isBullet = 0x00000010,

        /// <summary>
        /// This body was speed capped in the current time step
        /// </summary>
        b2_isSpeedCapped = 0x00000020,
	
        /// <summary>
        /// This body had a time of impact event in the current time step (body sim only).
        /// </summary>
        b2_hadTimeOfImpact = 0x00000040,

        /// <summary>
        /// This body has no limit on angular velocity
        /// </summary>
        b2_allowFastRotation = 0x00000080,

        /// <summary>
        /// This body need's to have its AABB increased
        /// </summary>
        b2_enlargeBulletBounds = 0x00000100,
        
        /// <summary>
        /// This body is dynamic so the solver should write to it.
        /// This prevents writing to kinematic bodies that causes a multithreaded sharing
        /// cache coherence problem even when the values are not changing.
        /// Used for b2BodyState flags.
        /// </summary>
        b2_dynamicFlag = 0x00000200,
        
        /// <summary>
        /// Flag to indicate the user has used the updateBodyMass option to defer mass
        /// computation but b2Body_UpdateMassFromShapes was not called before the world step.
        /// </summary>
        b2_dirtyMass = 0x00000400,

        b2_enableSleep = 0x00000800,

        b2_bodyEnableContactRecycling = 0x00001000,

        /// <summary>
        /// These flags are transient per time step. These may be different across b2Body, b2BodySim, and b2BodyState.
        /// </summary>
        b2_bodyTransientFlags = b2_isSpeedCapped | b2_hadTimeOfImpact,
    }
}
