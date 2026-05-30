namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공 시전이 성공했을 때 View와 테스트에 알리는 이산 사건.</summary>
    public readonly struct SkillCastEvent
    {
        public readonly int CasterId;
        public readonly int SlotIndex;
        public readonly string SkillId;

        public SkillCastEvent(int casterId, int slotIndex, string skillId)
        {
            CasterId = casterId;
            SlotIndex = slotIndex;
            SkillId = skillId;
        }
    }
}
