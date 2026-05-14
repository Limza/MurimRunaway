namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투에 들어갈 때의 Player 시작값. 영구 PlayerData(Phase 11+)와 별개.</summary>
    public sealed class PlayerStartData
    {
        public int MaxHp;
        public float MoveSpeed = 5f;
        public float AttackRange = 20f;
    }
}
