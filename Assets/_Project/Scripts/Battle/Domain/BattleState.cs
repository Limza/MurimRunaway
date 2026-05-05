namespace MurimRunaway.Battle.Domain
{
    /// <summary> 매 틱 Engine 이 View에 던지는 읽기 전용 상태 스냅샷 </summary>
    public readonly struct BattleState
    {
        public readonly long TickIndex;
        public BattleState(long tickIndex) { TickIndex = tickIndex; }
    }
}
