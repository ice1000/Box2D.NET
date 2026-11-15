// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Numerics;

namespace Box2D.NET
{
    /// <summary>
    /// Prototype for user allocation function
    /// @param size the allocation size in bytes
    /// @param alignment the required alignment, guaranteed to be a power of 2
    /// </summary>
    public delegate byte[] b2AllocCallbackFcn(uint size, int alignment);

    /// <summary>
    /// Prototype for user free function
    /// @param mem the memory previously allocated through `b2AllocFcn`
    /// @param size the allocation size in bytes
    /// </summary>
    public delegate void b2FreeCallbackFcn(byte[] mem, uint size);

    /// <summary>
    /// Prototype for the user assert callback. Return 0 to skip the debugger break.
    /// </summary>
    public delegate int b2AssertCallbackFcn(string condition, string fileName, int lineNumber);

    /// <summary>
    /// Prototype for user log callback. Used to log warnings.
    /// </summary>
    public delegate void b2LogCallbackFcn(in string message);
    
    /// <summary>
    /// Task interface
    /// This is the prototype for a Box2D task. Your task system is expected to run this callback on a worker thread,
    /// exactly once per enqueue, passing back the same taskContext pointer supplied to b2EnqueueTaskCallback.
    /// @ingroup world
    /// </summary>
    public delegate void b2TaskCallbackFcn(object taskContext);

    /// <summary>
    /// These functions can be provided to Box2D to invoke a task system.
    /// Returns a pointer to the user's task object. May be nullptr. A nullptr indicates to Box2D that the work was executed
    /// serially within the callback and there is no need to call b2FinishTaskCallback.
    /// @ingroup world
    /// </summary>
    public delegate object b2EnqueueTaskCallbackFcn(b2TaskCallbackFcn task, object taskContext, object userContext);

    /// <summary>
    /// Finishes a user task object that wraps a Box2D task.
    /// @ingroup world
    /// </summary>
    public delegate void b2FinishTaskCallbackFcn(object userTask, object userContext);

    /// <summary>
    /// Optional friction mixing callback. This intentionally provides no context objects because this is called
    /// from a worker thread.
    /// @warning This function should not attempt to modify Box2D state or user application state.
    /// @ingroup world
    /// </summary>
    public delegate float b2FrictionCallbackFcn(float frictionA, ulong userMaterialIdA, float frictionB, ulong userMaterialIdB);

    /// <summary>
    /// Optional restitution mixing callback. This intentionally provides no context objects because this is called
    /// from a worker thread.
    /// @warning This function should not attempt to modify Box2D state or user application state.
    /// @ingroup world
    /// </summary>
    public delegate float b2RestitutionCallbackFcn(float restitutionA, ulong userMaterialIdA, float restitutionB, ulong userMaterialIdB);

    /// <summary>
    /// Prototype for a contact filter callback.
    /// This is called when a contact pair is considered for collision. This allows you to
    /// perform custom logic to prevent collision between shapes. This is only called if
    /// one of the two shapes has custom filtering enabled.
    /// Notes:
    /// - this function must be thread-safe
    /// - this is only called if one of the two shapes has enabled custom filtering
    /// - this may be called for awake dynamic bodies and sensors
    /// Return false if you want to disable the collision
    /// @see b2ShapeDef
    /// @warning Do not attempt to modify the world inside this callback
    /// @ingroup world
    /// </summary>
    public delegate bool b2CustomFilterCallbackFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, object context);

    /// <summary>
    /// Prototype for a pre-solve callback.
    /// This is called after a contact manifold is updated. This allows you to inspect a
    /// manifold before it goes to the solver. If you are careful, you can modify the
    /// manifold (e.g. modify the normal). You can set the manifold point count to zero disable
    /// collision.
    /// Notes:
    /// - this function must be thread-safe
    /// - this is only called if the shape has enabled pre-solve events
    /// - this is called only for awake dynamic bodies
    /// - this is not called for sensors
    /// - the supplied manifold impulses are not reliable
    /// @warning Do not attempt to modify the world inside this callback
    /// @ingroup world
    /// </summary>
    public delegate void b2PreSolveCallbackFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, ref B2Manifold manifold, object context);

    /// <summary>
    /// Prototype for a pre-continuous callback.
    /// This is called when a time of impact event is computed in continuous collision. This function
    /// can be used to skip the event. Unlike the pre-solve callback, this only supplies the point and normal.
    /// At this stage of the solver the manifold may not exist or may be stale.
    /// Notes:
    /// - this function must be thread-safe
    /// - this is only called if the shape has enabled pre-solve events
    /// - this is called only for awake dynamic bodies
    /// - this is not called for sensors
    /// Return false if you want to disable the time of impact event.
    /// @warning Do not attempt to modify the world inside this callback
    /// @ingroup world
    /// </summary>
    public delegate bool b2PreContinuousCallbackFcn(B2ShapeId shapeIdA, B2ShapeId shapeIdB, B2Pos point, Vector2 normal, object context);

    /// <summary>
    /// Prototype callback for overlap queries.
    /// Called for each shape found in the query.
    /// @see b2World_OverlapABB
    /// @return false to terminate the query.
    /// @ingroup world
    /// </summary>
    public delegate bool b2OverlapResultCallbackFcn(B2ShapeId shapeId, object context);

    /// <summary>
    /// Prototype callback for ray and shape casts.
    /// Called for each shape found in the query. You control how the ray cast
    /// proceeds by returning a float:
    /// return -1: ignore this shape and continue
    /// return 0: terminate the ray cast
    /// return fraction: clip the ray to this point
    /// return 1: don't clip the ray and continue
    /// A cast with initial overlap will return a zero fraction and a zero normal.
    /// @param shapeId the shape hit by the ray
    /// @param point the point of initial intersection
    /// @param normal the normal vector at the point of intersection, zero for a shape cast with initial overlap
    /// @param fraction the fraction along the ray at the point of intersection, zero for a shape cast with initial overlap
    /// @param context the user context
    /// @return -1 to filter, 0 to terminate, fraction to clip the ray for closest hit, 1 to continue
    /// @see b2World_CastRay
    /// @ingroup world
    /// </summary>
    public delegate float b2CastResultCallbackFcn<T>(B2ShapeId shapeId, B2Pos point, Vector2 normal, float fraction, ref T context);

    /// <summary>
    /// Used to collect collision planes for character movers.
    /// Return true to continue gathering planes.
    /// </summary>
    public delegate bool b2PlaneResultCallbackFcn(B2ShapeId shapeId, ref B2PlaneResult plane, object context);

    /// <summary>
    /// Manifold functions should compute important results in local space to improve precision. However, this
    /// interface function takes two world transforms instead of a relative transform for these reasons:
    ///
    /// First:
    /// The anchors need to be computed relative to the shape origin in world space. This is necessary so the
    /// solver does not need to access static body transforms. Not even in constraint preparation. This approach
    /// has world space vectors yet retains precision.
    ///
    /// Second:
    /// b2ManifoldPoint::point is very useful for debugging and it is in world space.
    ///
    /// Third:
    /// The user may call the manifold functions directly and they should be easy to use and have easy to use
    /// results.
    /// </summary>
    public delegate B2LocalManifold b2ManifoldFcn(B2Shape shapeA, B2Shape shapeB, in B2Transform xf, ref B2SimplexCache cache);

    /// <summary>
    /// This function receives proxies found in the AABB query.
    /// @return true if the query should continue
    /// </summary>
    public delegate bool b2TreeQueryCallbackFcn<T>(int proxyId, ulong userData, ref T context);

    /// <summary>
    /// This function receives clipped ray cast input for a proxy. The function
    /// returns the new ray fraction.
    /// - return a value of 0 to terminate the ray cast
    /// - return a value less than input-&gt;maxFraction to clip the ray
    /// - return a value of input-&gt;maxFraction to continue the ray cast without clipping
    /// </summary>
    public delegate float b2TreeRayCastCallbackFcn<T>(in B2RayCastInput input, int proxyId, ulong userData, ref T context);

    /// <summary>
    /// This function receives clipped AABB cast input for a proxy. The function
    /// returns the new cast fraction.
    /// - return a value of 0 to terminate the cast
    /// - return a value less than input-&gt;maxFraction to clip the cast
    /// - return a value of input-&gt;maxFraction to continue the cast without clipping
    /// </summary>
    public delegate float b2TreeBoxCastCallbackFcn<T>(in B2BoxCastInput input, int proxyId, ulong userData, ref T context);

    // -----------------------------------------------------------------------------------------------------------------
    // Draw
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Draw a closed polygon provided in CCW order.
    /// </summary>
        public delegate void DrawPolygonFcn(in B2WorldTransform transform, ReadOnlySpan<Vector2> vertices, int vertexCount, B2HexColor color, object context);

    /// <summary>
    /// Draw a solid closed polygon provided in CCW order.
    /// </summary>
    public delegate void DrawSolidPolygonFcn(in B2WorldTransform transform, ReadOnlySpan<Vector2> vertices, int vertexCount, float radius, B2HexColor color, object context);

    /// <summary>
    /// Draw a circle.
    /// </summary>
    public delegate void DrawCircleFcn(in B2Pos center, float radius, B2HexColor color, object context);

    /// <summary>
    /// Draw a solid circle.
    /// </summary>
    public delegate void DrawSolidCircleFcn(in B2WorldTransform transform, Vector2 center, float radius, B2HexColor color, object context);

    /// <summary>
    /// Draw a solid capsule.
    /// </summary>
    public delegate void DrawSolidCapsuleFcn(in B2Pos p1, in B2Pos p2, float radius, B2HexColor color, object context);

    /// <summary>
    /// Draw a line segment.
    /// </summary>
    public delegate void DrawLineFcn(in B2Pos p1, in B2Pos p2, B2HexColor color, object context);

    /// <summary>
    /// Draw a transform. Choose your own length scale.
    /// </summary>
    public delegate void DrawTransformFcn(in B2WorldTransform transform, object context);

    /// <summary>
    /// Draw a point.
    /// </summary>
    public delegate void DrawPointFcn(in B2Pos p, float size, B2HexColor color, object context);

    /// <summary>
    /// Draw a string in world space
    /// </summary>
    public delegate void DrawStringFcn(in B2Pos p, string s, B2HexColor color, object context);

    /// <summary>
    /// Draw a bounding box. With double precision enabled, the single precision bounding box
    /// gets increasing padding when moving far from the origin.
    /// </summary>
    public delegate void DrawBoundsFcn(in B2AABB aabb, B2HexColor color, object context);
}
