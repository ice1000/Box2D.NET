// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// A task in the pull model.
    /// @ingroup world
    /// </summary>
    public struct B2Task
    {
        /// The task function.
        public b2TaskCallbackFcn fcn;

        /// The context that must be passed to the task function.
        public object context;

        /// The task type. Informational.
        public B2TaskType type;

        /// Is this task deferred? This indicates that this task must be joined
        /// at a later stage instead of immediately before the next stage.
        /// @see B2TaskBatch.joinTasks
        public bool deferred;
    }
}
