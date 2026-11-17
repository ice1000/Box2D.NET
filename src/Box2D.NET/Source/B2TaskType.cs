// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// The task type for the pull model.
    /// @ingroup world
    /// </summary>
    public enum B2TaskType
    {
        b2_taskPairs, // Finding pairs of potentially colliding shapes. The broad-phase.
        b2_taskCollide, // Computing contact manifolds. The narrow-phase.
        b2_taskTreeRebuild, // Rebuilding the bounding volume hierarchy. A long running side task.
        b2_taskIslandSplit, // Splitting an island in order to let an island sleep.
        b2_taskSolver, // Solving constraints.
        b2_taskFinalize, // Updating body transforms and shape bounding boxes.
        b2_taskContinuous, // Continuous collision detection.
        b2_taskSensors, // Sensor overlaps and events.
        b2_taskTypeCount, // The type count.
    }
}
