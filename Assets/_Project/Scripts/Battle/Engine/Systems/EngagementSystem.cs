using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>Player가 가장 가까운 적의 교전 거리에 닿으면 Idle 정지 + 전투 페이즈 Engage 전이.</summary>
    public sealed class EngagementSystem : IBattleSystem
    {
        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;
            if (player.State != ActorState.Running)
                return;

            var nearest = context.GetNearestAliveEnemy();
            if (nearest == null)
                return;

            var distance = nearest.Position - player.Position;
            if (distance > player.AttackRange)
                return;

            // 큰 deltaTime이 사거리 안쪽으로 밀어넣는 것 방지
            player.Position = nearest.Position - player.AttackRange;
            player.State = ActorState.Idle;
            context.Phase = BattlePhase.Engage;
        }
    }
}