namespace MurimRunaway.Battle.Domain
{
    /// <summary>데미지가 어떤 방식으로 들어왔는지 구분한다.</summary>
    public enum DamageKind
    {
        /// <summary>아직 데미지 방식이 정해지지 않음.</summary>
        None = 0,
        /// <summary>일반 공격.</summary>
        NormalAttack,
        /// <summary>무공.</summary>
        Skill,
    }
}