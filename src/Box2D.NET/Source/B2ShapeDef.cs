// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Used to create a shape.
    /// This is a temporary object used to bundle shape creation parameters. You may use
    /// the same shape definition to create multiple shapes.
    /// Must be initialized using b2DefaultShapeDef().
    /// @ingroup shape
    /// </summary>
    public struct B2ShapeDef
    {
        /// <summary>
        /// Use this to store application specific shape data.
        /// </summary>
        public B2UserData userData;

        /// <summary>
        /// The surface material for this shape.
        /// </summary>
        public B2SurfaceMaterial material;

        /// <summary>
        /// The density, usually in kg/m^2.
        /// This is not part of the surface material because this is for the interior, which may have
        /// other considerations, such as being hollow. For example a wood barrel may be hollow or full of water.
        /// </summary>
        public float density;

        /// <summary>
        /// Collision filtering data.
        /// </summary>
        public B2Filter filter;
        
        /// <summary>
        /// Enable custom filtering. Only one of the two shapes needs to enable custom filtering. See b2WorldDef.
        /// </summary>
        public bool enableCustomFiltering;

        /// <summary>
        /// A sensor shape generates overlap events but never generates a collision response.
        /// Sensors do not have continuous collision. Instead, use a ray or shape cast for those scenarios.
        /// Sensors still contribute to the body mass if they have non-zero density.
        /// @note Sensor events are disabled by default.
        /// @see enableSensorEvents
        /// </summary>
        public bool isSensor;

        /// <summary>
        /// Enable sensor events for this shape. This applies to sensors and non-sensors. Both shapes involved must have this flag set to true.
        /// False by default, even for sensors.
        /// </summary>
        public bool enableSensorEvents;

        /// <summary>
        /// Enable contact events for this shape. Only applies to kinematic and dynamic bodies. Only one shape involved needs this flag set to true.
        /// Ignored for sensors. False by default.
        /// </summary>
        public bool enableContactEvents;

        /// <summary>
        /// Enable hit events for this shape. Only applies to kinematic and dynamic bodies. Only one shape involved needs this flag set to true.
        /// Ignored for sensors. False by default.
        /// </summary>
        public bool enableHitEvents;

        /// <summary>
        /// Enable pre-solve contact events for this shape. Only applies to dynamic bodies. These are expensive
        /// and must be carefully handled due to multithreading. Ignored for sensors.
        /// </summary>
        public bool enablePreSolveEvents;

        /// <summary>
        /// When shapes are created they will scan the environment for collision the next time step. This can significantly slow down
        /// static body creation when there are many static shapes.
        /// This is flag is ignored for dynamic and kinematic shapes which always invoke contact creation.
        /// </summary>
        public bool invokeContactCreation;

        /// <summary>
        /// Should the body update the mass properties when this shape is created. Default is true.
        /// Warning: if this is true, you MUST call b2Body_UpdateMassFromShapes before simulating the world.
        /// </summary>
        public bool updateBodyMass;

        /// <summary>
        /// Used internally to detect a valid definition. DO NOT SET.
        /// </summary>
        public int internalValue;
    }
}
