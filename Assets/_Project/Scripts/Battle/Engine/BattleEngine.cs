using System;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. 틱마다 상태를 진행시키고 OnSnapshot으로 통보.</summary>
    public sealed class BattleEngine
    {
        private readonly ITickService _tick;
        private long _tickIndex;

        public event Action<BattleState> OnSnapshot;

        public BattleEngine(ITickService tick)
        {
            _tick = tick;
            _tick.OnTick += HandleTick;
        }

        public void Dispose()
        {
            _tick.OnTick -= HandleTick;
        } 

        public void Start() { _tickIndex = 0; }

        private void HandleTick(float dt)
        {
            _tickIndex++;
            OnSnapshot?.Invoke(new BattleState(_tickIndex));
        }
    }
}