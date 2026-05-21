namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공이 닿는 타겟 범위.</summary>
    public enum SkillRange
    {
        /// <summary>아직 타겟 범위가 정해지지 않음.</summary>
        None = 0,
        /// <summary>가장 앞의 살아있는 적 1명.</summary>
        Single,
        /// <summary>가장 앞의 살아있는 적과 그 다음 살아있는 적까지 최대 2명.</summary>
        NearbyPair,
        /// <summary>화면 안의 모든 살아있는 적.</summary>
        All,
    }
}
