// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using static Box2D.NET.B2Cores;

namespace Box2D.NET
{
    public static class B2Constants
    {
        /// <summary>
        /// Used to detect bad values. Positions greater than about 16km will have precision
        /// problems, so 100km as a limit should be fine in all cases.
        /// </summary>
        public static float B2_HUGE => (100000.0f * b2_lengthUnitsPerMeter);

        /// <summary>
        /// Maximum parallel workers. Used for some fixed size arrays.
        /// </summary>
        public const int B2_MAX_WORKERS = 32;

        /// <summary>
        /// Maximum number of tasks queued per world step. b2EnqueueTaskCallback will never be called
        /// more than this per world step. This is related to B2_MAX_WORKERS. With 32 workers,
        /// the maximum observed task count is 130. This allows an external task system to use a fixed
        /// size array for Box2D task, which may help with creating stable user task pointers.
        /// </summary>
        public const int B2_MAX_TASKS = 256;

        /// <summary>
        /// Maximum number of colors in the constraint graph. Constraints that cannot
        /// find a color are added to the overflow set which are solved single-threaded.
        /// The compound barrel benchmark has minor overflow with 24 colors
        /// </summary>
        public const int B2_GRAPH_COLOR_COUNT = 24;

        /// <summary>
        /// A small length used as a collision and constraint tolerance. Usually it is
        /// chosen to be numerically significant, but visually insignificant. In meters.
        /// Normally this is 0.5cm.
        /// @warning modifying this can have a significant impact on stability
        /// </summary>
        public static float B2_LINEAR_SLOP => (0.005f * b2_lengthUnitsPerMeter);

        /// <summary>
        /// Maximum number of simultaneous worlds that can be allocated
        /// </summary>
        public const int B2_MAX_WORLDS = 128;

        /// <summary>
        /// Maximum length of the body name. Can be 0 if you don't need names.
        /// </summary>
        public const int B2_NAME_LENGTH = 10;

        /// <summary>
        /// The maximum rotation of a body per time step. This limit is very large and is used
        /// to prevent numerical problems. You shouldn't need to adjust this.
        /// @warning increasing this to 0.5f * b2_pi or greater will break continuous collision.
        /// </summary>
        public static readonly float B2_MAX_ROTATION = (0.25f * B2MathFunction.B2_PI);

        /// <summary>
        /// Box2D uses limited speculative collision. This reduces jitter.
        /// Normally this is 2cm.
        /// @warning modifying this can have a significant impact on performance and stability
        /// </summary>
        public static float B2_SPECULATIVE_DISTANCE => (4.0f * B2_LINEAR_SLOP);

        /// <summary>
        /// The default contact recycling distance.
        /// </summary>
        public static float B2_CONTACT_RECYCLE_DISTANCE => (10.0f * B2_LINEAR_SLOP);

        /// <summary>
        /// The default contact recycling world angle threshold. 0.98 ~= 11.5 degrees
        /// </summary>
        public const float B2_CONTACT_RECYCLE_COS_ANGLE = 0.98f;

        /// <summary>
        /// This is used to fatten AABBs in the dynamic tree. This allows proxies
        /// to move by a small amount without triggering a tree adjustment. This is in meters.
        /// Normally this is 5cm.
        /// @warning modifying this can have a significant impact on performance
        /// </summary>
        public static float B2_MAX_AABB_MARGIN => (0.05f * b2_lengthUnitsPerMeter);

        /// <summary>
        /// For small objects the margin is limited to this fraction times the maximum extent
        /// </summary>
        public static readonly float B2_AABB_MARGIN_FRACTION = 0.125f;

        /// <summary>
        /// The time that a body must be still before it will go to sleep. In seconds.
        /// </summary>
        public const float B2_TIME_TO_SLEEP = 0.5f;

        /// <summary>
        /// The maximum number of restitution iterations. Needed to avoid a solver overflow.
        /// </summary>
        public const int B2_MAX_RESTITUTION_ITERATIONS = 63;

        /// <summary>
        /// solver
        /// </summary>
        public const int B2_MAX_CONTINUOUS_SENSOR_HITS = 8;

        /// <summary>
        /// collision
        /// -----------------------------------------------------------------------------------------------------------------
        /// The maximum number of vertices on a convex polygon. Changing this affects performance even if you
        /// don't use more vertices.
        /// </summary>
        public const int B2_MAX_POLYGON_VERTICES = 8;


        /// <summary>
        /// types
        /// </summary>
        public const ulong B2_DEFAULT_CATEGORY_BITS = 1;
        public const ulong B2_DEFAULT_MASK_BITS = ulong.MaxValue;

        /// <summary>
        /// core
        /// Used to indicate an unset or invalid index value.
        /// </summary>
        public const int B2_NULL_INDEX = -1;

        /// <summary>
        /// Use 32 byte alignment for everything. Works with 256bit SIMD.
        /// </summary>
        public const int B2_ALIGNMENT = 32;

        /// <summary>
        /// Use to validate definitions. Do not take my cookie.
        /// </summary>
        public const int B2_SECRET_COOKIE = 1152023;

        /// <summary>
        /// @base
        /// Simple djb2 hash function for determinism testing
        /// </summary>
        public const int B2_HASH_INIT = 5381;
    }
}
