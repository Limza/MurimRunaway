namespace MurimRunaway.Battle.Domain
{
    /// <summary>학습한 무공 한 슬롯 = 무공 데이터 + 남은 쿨다운. 빈 슬롯은 Data == null.</summary>
    public sealed class SkillSlot
    {
        public SkillData Data;
        public float CooldownRemaining;
    }
}