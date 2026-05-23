namespace MurimRunaway.Battle.Domain
{
    /// <summary>HP 데미지가 적용됐을 때 View와 테스트에 알리는 사건.</summary>
    public readonly struct DamageEvent
    {
        public readonly int SourceId;
        public readonly int TargetId;
        public readonly int Amount;
        public readonly DamageKind Kind;
        public readonly string SkillId;

        public DamageEvent(int sourceId, int targetId, int amount, DamageKind kind, string skillId)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Amount = amount;
            Kind = kind;
            SkillId = skillId;
        }
    }
}