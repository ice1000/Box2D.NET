// SPDX-FileCopyrightText: 2026 Erin Catto
// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    internal class B2StepTask
    {
        public b2TaskCallbackFcn fcn;
        public object context;
        public B2StepContext stepContext;
        // index into B2StepContext.stepTasks/tasks (pointer arithmetic in C)
        public int index;
    }
}
