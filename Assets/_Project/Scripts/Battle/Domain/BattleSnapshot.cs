namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 틱 Engine이 View에 보내는 전체 스냅샷. id 오름차순 정렬 보장.</summary>
    public readonly struct BattleSnapshot
    {
        /// <summary>시뮬레이션 스텝 ID. 정수·결정론. 리플레이/골든 테스트의 동일성 비교 키, 로그 식별자.</summary>
        public readonly long TickIndex;
        /// <summary>전투 시작 후 누적 경과 시간(초). float 누적이라 드리프트 가능. UI 표시·게임 로직 타이밍용.</summary>
        public readonly float TimeSec;
        public readonly ActorView[] Actors;
        public readonly BattlePhase Phase;

        public BattleSnapshot(long tickIndex, float timeSec, ActorView[] actors, BattlePhase phase)
        {
            TickIndex = tickIndex; 
            TimeSec = timeSec; 
            Actors = actors; 
            Phase = phase;
        }
    }
}