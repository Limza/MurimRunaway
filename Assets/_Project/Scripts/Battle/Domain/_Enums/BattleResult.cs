namespace MurimRunaway.Battle.Domain
{
    /// <summary>전투 종료 결과.</summary>
    public enum BattleResult
    {
        /// <summary>아직 결과가 정해지지 않음.</summary>
        None = 0,
        /// <summary>모든 적을 처치함.</summary>
        Victory,
        /// <summary>플레이어가 사망함.</summary>
        Defeat,
    }
}