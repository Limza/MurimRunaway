namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공이 선호하는 적과의 거리대.</summary>
    public enum SkillRange
    {
        /// <summary>아직 거리대가 정해지지 않음.</summary>
        None = 0,
        /// <summary>근거리.</summary>
        Close,
        /// <summary>중거리.</summary>
        Mid,
        /// <summary>원거리.</summary>
        Long,
    }
}
