namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투에 들어갈 때의 Player 시작값.</summary>
    public sealed class PlayerStartData
    {
        public int MaxHp;
        public int MaxMana;
        public int StartingMana;
        public int MaxMomentum;
        public float MoveSpeed;
        public float EngageDistance;

        public SkillData[] StartingSkills;
    }
}
