namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투를 시작시키는 입력. View가 만들고 Engine.Setup에 넘긴다.</summary>
    public sealed class BattleStartData
    {
        public int Seed;
        public PlayerStartData Player;
        public EnemyData[] Enemies;
    }
}
