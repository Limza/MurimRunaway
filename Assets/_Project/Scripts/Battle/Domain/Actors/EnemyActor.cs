namespace MurimRunaway.Battle.Domain
{
    /// <summary>적 액터. spawnPosition에 고정 배치된다.</summary>
    public sealed class EnemyActor : Actor
    {
        public int NormalAttackDamage;
        public float NormalAttackPeriod;
        public float NormalAttackCooldown;
    }
}
