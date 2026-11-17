// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;

namespace Box2D.NET
{
    /// <summary>
    /// A batch of tasks for the pull model.
    /// @ingroup world
    /// </summary>
    public struct B2TaskBatch
    {
        /// The regular tasks that must be executed in this stage.
        public ArraySegment<B2Task> tasks;

        /// The task count.
        public int count;

        /// Earlier tasks that must now be joined before the next stage.
        public ArraySegment<B2Task> joinTasks;

        /// The join task count.
        public int joinCount;
    }
}
