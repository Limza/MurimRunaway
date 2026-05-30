using System;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    public sealed class EnemyAttackSystem : IBattleSystem
    {
        private readonly IDamageApplier _damageApplier;

        public EnemyAttackSystem(IDamageApplier damageApplier)
        {
            _damageApplier = damageApplier;
        }

        public void Tick(BattleContext context, float deltaTime)
        {
            if (context.Phase != BattlePhase.Engage)
                return;

            var player = context.Player;
            if (player.State == ActorState.Dead || player.Hp <= 0)
                return;

            foreach (var enemy in context.Enemies)
            {
                if (enemy.State == ActorState.Dead || enemy.Hp <= 0)
                    continue;
                if (enemy.State != ActorState.Idle)
                    continue;

                if (enemy.NormalAttackCooldown > 0f)
                    enemy.NormalAttackCooldown = Math.Max(0f, enemy.NormalAttackCooldown - deltaTime);
                if (enemy.NormalAttackCooldown > 0f)
                    continue;

                var distance = Math.Abs(enemy.Position - player.Position);
                if (distance > enemy.EngageDistance)
                    continue;

                _damageApplier.ApplyDamage(enemy, player, enemy.NormalAttackDamage, DamageKind.NormalAttack, null);
                enemy.NormalAttackCooldown = enemy.NormalAttackPeriod;
            }
        }
    }
}