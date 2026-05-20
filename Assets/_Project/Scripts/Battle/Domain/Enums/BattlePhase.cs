namespace MurimRunaway.Battle.Domain
{
    /// <summary>전투 전체의 진행 단계. Setup → Approach → Engage → Resolve.</summary>
    public enum BattlePhase
    {
        /// <summary>아직 단계가 정해지지 않음.</summary>
        None = 0,
        /// <summary>초기화: Actor 배치 + RNG seed.</summary>
        Setup,
        /// <summary>접근: Player가 적 쪽으로 진군 중 (교전 거리 밖).</summary>
        Approach,
        /// <summary>교전: Player가 교전 거리 안.</summary>
        Engage,
        /// <summary>정산: 승패 결정 후 마무리 처리.</summary>
        Resolve,
    }
}
