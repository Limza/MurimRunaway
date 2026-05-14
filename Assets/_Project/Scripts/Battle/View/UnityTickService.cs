using System;
using UnityEngine;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>Time.deltaTime 누적으로 0.05초 틱을 발생시키는 ITickService의 Unity 구현.</summary>
    public sealed class UnityTickService : MonoBehaviour, ITickService
    {
        private const float TickIntervalSeconds = 0.05f;
        private float _elapsedSinceLastTick;

        public event Action<float> Ticked;

        private void Update()
        {
            _elapsedSinceLastTick += Time.deltaTime;
            // while: 프레임 드랍 시 밀린 틱을 따라잡아 시뮬레이션 시간 ≈ 실시간 보장
            while (_elapsedSinceLastTick >= TickIntervalSeconds)
            {
                _elapsedSinceLastTick -= TickIntervalSeconds;
                Ticked?.Invoke(TickIntervalSeconds);
            }
        }
    }
}