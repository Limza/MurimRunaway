namespace MurimRunaway.Battle.Domain
{
    /// <summary>플레이어 액터. 이동 속도를 가지며 적 쪽으로 진군한다.</summary>
    public sealed class PlayerActor : Actor
    {
        public float MoveSpeed;

        // 내공
        public int Mana;
        public int MaxMana;

        // 기세
        public int Momentum;
        public int MaxMomentum;

        // 학습한 무공
        public SkillSlot[] Skills;
    }
}
