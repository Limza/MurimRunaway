using System;

namespace MurimRunaway.Battle.Domain
{
    /// <summary>대상 HP를 줄이는 무공 효과.</summary>
    [Serializable]
    public sealed class SkillDamageEffect : SkillEffect
    {
        public int Amount;
    }
}