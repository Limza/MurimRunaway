using System;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>0.05초 고정 간격 틱 신호의 추상화. Engine을 Unity 시간에서 분리.</summary>
    public interface ITickService
    {
        event Action<float> OnTick;
        void Pause();
        void Resume();
        bool IsRunning { get; }
    }
}