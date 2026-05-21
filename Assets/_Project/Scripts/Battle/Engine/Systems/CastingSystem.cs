using System;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>자동 시전 결정 트리. 슬롯 순서 = 우선순위. 한 Tick 한 시전.</summary>
    public sealed class CastingSystem : IBattleSystem
    {
        private readonly ISkillExecutor _executor;

        public CastingSystem(ISkillExecutor executor)
        {
            _executor = executor;
        }

        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;

            // ① 쿨다운 감소 — 페이즈와 무관하게 항상 흐른다.
            foreach (var slot in player.Skills)
            {
                if (slot.Data == null)
                    continue;
                slot.CooldownRemaining = Math.Max(0f, slot.CooldownRemaining - deltaTime);
            }

            // 교전 페이즈에서만 시전
            if (context.Phase != BattlePhase.Engage)
                return;

            // ② 타겟 선택
            var target = context.GetNearestAliveEnemy();
            if (target == null)
                return;

            // ③ 슬롯 순회 — 통과한 첫 슬롯에서 break
            for (var slotIndex = 0; slotIndex < player.Skills.Length; slotIndex++)
            {
                var slot = player.Skills[slotIndex];
                var skill = slot.Data;

                if (skill == null)
                    continue;
                if (slot.CooldownRemaining > 0f)
                    continue;
                if (player.Mana < skill.ManaCost)
                    continue;

                // ④ 통과 — 시전하고 한 Tick 종료 (두 무공 동시 발동 금지)
                _executor.TryCast(player, slotIndex, target);
                break;
            }
        }
    }
}
