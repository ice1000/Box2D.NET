// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// This struct holds callbacks you can implement to draw a Box2D world.
    /// Callbacks receive world coordinates. In large world mode the translation is double precision so
    /// it stays accurate far from the origin. Shift into your own camera frame inside the callbacks.
    /// Initialize with b2DefaultDebugDraw.
    /// @ingroup world
    /// </summary>
    public struct B2DebugDraw
    {
        public DrawPolygonFcn DrawPolygonFcn;
        public DrawSolidPolygonFcn DrawSolidPolygonFcn;
        public DrawCircleFcn DrawCircleFcn;
        public DrawSolidCircleFcn DrawSolidCircleFcn;
        public DrawSolidCapsuleFcn DrawSolidCapsuleFcn;
        public DrawLineFcn DrawLineFcn;
        public DrawTransformFcn DrawTransformFcn;
        public DrawPointFcn DrawPointFcn;
        public DrawStringFcn DrawStringFcn;
        public DrawBoundsFcn DrawBoundsFcn;
        
        /// <summary>
        /// World bounds to use for debug draw
        /// </summary>
        public B2AABB drawingBounds;
        
        /// <summary>
        /// Scale to use when drawing forces
        /// </summary>
        public float forceScale;

        /// <summary>
        /// Global scaling for joint drawing
        /// </summary>
        public float jointScale;
        
        /// <summary>
        /// Option to draw contact points
        /// </summary>
        public bool drawContacts;

        /// <summary>
        /// Draw anchor A for contact points (instead of anchorB)
        /// </summary>
        public bool drawAnchorA;

        /// <summary>
        /// Option to draw shapes
        /// </summary>
        public bool drawShapes;

        /// <summary>
        /// Option to draw chain shape normals
        /// </summary>
        public bool drawChainNormals;

        /// <summary>
        /// Option to draw joints
        /// </summary>
        public bool drawJoints;

        /// <summary>
        /// Option to draw additional information for joints
        /// </summary>
        public bool drawJointExtras;

        /// <summary>
        /// Option to draw the bounding boxes for shapes
        /// </summary>
        public bool drawBounds;

        /// <summary>
        /// Option to draw the mass and center of mass of dynamic bodies
        /// </summary>
        public bool drawMass;

        /// <summary>
        /// Option to draw body names
        /// </summary>
        public bool drawBodyNames;

        /// <summary>
        /// Option to visualize the graph coloring used for contacts and joints
        /// </summary>
        public bool drawGraphColors;

        /// <summary>
        /// Option to draw contact feature ids
        /// </summary>
        public bool drawContactFeatures;

        /// <summary>
        /// Option to draw contact normals
        /// </summary>
        public bool drawContactNormals;

        /// <summary>
        /// Option to draw contact normal forces
        /// </summary>
        public bool drawContactForces;

        /// <summary>
        /// Option to draw contact friction forces
        /// </summary>
        public bool drawFrictionForces;

        /// <summary>
        /// Option to draw islands as bounding boxes
        /// </summary>
        public bool drawIslands;

        /// <summary>
        /// User context that is passed as an argument to drawing callback functions
        /// </summary>
        public object context;
    }
    
}
