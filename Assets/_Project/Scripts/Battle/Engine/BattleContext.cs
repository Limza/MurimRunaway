using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>한 전투의 가변 시뮬레이션 상태. 모든 IBattleSystem이 공유·변경한다.</summary>
    public sealed class BattleContext
    {
        public PlayerActor Player;
        public EnemyActor[] Enemies;
        public long TickIndex;
        public float TimeSec;
        public BattlePhase Phase;
        public IRngService Rng;

        /// <summary>살아있는 적 중 position 최소. 동률은 배열 순서(= id 오름차순). 없으면 null.</summary>
        public EnemyActor GetNearestAliveEnemy()
        {
            EnemyActor nearest = null;
            var nearestPosition = float.MaxValue;
            for (var index = 0; index < Enemies.Length; index++)
            {
                var enemy = Enemies[index];
                if (enemy.State == ActorState.Dead)
                    continue;
                if (enemy.Position < nearestPosition)
                {
                    nearestPosition = enemy.Position;
                    nearest = enemy;
                }
            }
            return nearest;
        }
    }
}