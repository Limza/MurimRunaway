using System;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    /// <summary>테스트용 수동 펌프. PumpTicks(n)로 OnTick을 즉시 n번 호출.</summary>
    public sealed class MockTickService : ITickService
    {
        private const float TickIntervalSeconds = 0.05f;

        public event Action<float> OnTick;

        public void PumpTicks(int count)
        {
            for (var i = 0; i < count; i++)
                OnTick?.Invoke(TickIntervalSeconds);
        }
    }
}