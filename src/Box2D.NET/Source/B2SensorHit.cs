// SPDX-FileCopyrightText: 2025 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

namespace Box2D.NET
{
    /// <summary>
    /// Used to track shapes that hit sensors using time of impact
    /// </summary>
    public struct B2SensorHit
    {
        public int sensorId;
        public int visitorId;
    }
}
