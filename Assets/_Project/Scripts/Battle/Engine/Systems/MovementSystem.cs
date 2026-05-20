using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>Player가 Running이면 진군 속도만큼 position 전진.</summary>
    public sealed class MovementSystem : IBattleSystem
    {
        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;
            if (player.State != ActorState.Running)
                return;

            player.Position += player.MoveSpeed * deltaTime;
        }
    }
}