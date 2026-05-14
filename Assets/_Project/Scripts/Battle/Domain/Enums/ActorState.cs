namespace MurimRunaway.Battle.Domain
{
    /// <summary>Actor의 현재 행동 상태 (FSM). 매 Tick Engine이 갱신.</summary>
    public enum ActorState
    {
        /// <summary>대기: 행동 없이 정지 상태.</summary>
        Idle,
        /// <summary>이동 중: Player가 진군하는 상태.</summary>
        Running,
        /// <summary>스킬 시전 중 (Phase 3+에서 사용).</summary>
        Casting,
        /// <summary>강공격 차징 중 (Phase 3+에서 사용).</summary>
        HeavyCharging,
        /// <summary>기절: 행동 불능 (Phase 3+에서 사용).</summary>
        Stunned,
        /// <summary>사망: 전투에서 제거됨.</summary>
        Dead,
    }
}