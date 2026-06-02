namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 Tick 끝에 Engine이 View와 Test로 보내는 전투 상태 읽기 전용 사본.</summary>
    public readonly struct BattleSnapshot
    {
        /// <summary>시뮬레이션 스텝 ID. 결정성 비교와 로그 식별에 사용한다.</summary>
        public readonly long TickIndex;
        /// <summary>전투 시작 후 누적 경과 시간(초).</summary>
        public readonly float TimeSec;
        public readonly ActorView Player;
        public readonly ActorView[] Enemies;
        public readonly BattlePhase Phase;

        public BattleSnapshot(
            long tickIndex,
            float timeSec,
            ActorView player,
            ActorView[] enemies,
            BattlePhase phase)
        {
            TickIndex = tickIndex;
            TimeSec = timeSec;
            Player = player;
            Enemies = enemies;
            Phase = phase;
        }
    }
}
