namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공의 종류.</summary>
    public enum SkillKind
    {
        /// <summary>아직 종류가 정해지지 않음.</summary>
        None = 0,
        /// <summary>초식 — 공격이나 방어에 직접 쓰는 기술.</summary>
        Technique,
        /// <summary>심법 — 몸 상태를 강화하는 무공.</summary>
        Focus,
        /// <summary>경공 — 이동/회피 무공.</summary>
        Step,
        /// <summary>오의 — 핫키 발동, 기세 소비.</summary>
        Ultimate,
    }
}
