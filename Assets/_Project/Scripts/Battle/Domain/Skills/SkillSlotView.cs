namespace MurimRunaway.Battle.Domain
{
    /// <summary>슬롯 한 칸의 읽기 전용 사본.</summary>
    public readonly struct SkillSlotView
    {
        public readonly bool Filled;
        public readonly string SkillId;
        public readonly float CooldownRemaining;
        public readonly float CooldownSec;

        public SkillSlotView(SkillSlot slot)
        {
            Filled = slot != null && slot.Data != null;
            SkillId = Filled ? slot.Data.Id : null;
            CooldownRemaining = Filled ? slot.CooldownRemaining : 0f;
            CooldownSec = Filled ? slot.Data.CooldownSec : 0f;
        }
    }
}