using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class BattleResultTests
    {
        [Test]
        public void 같은_입력이면_DamagePublished와_ActorDeathPublished_시퀀스가_동일하다()
        {
            var runA = RunAndCaptureCombatEvents(seed: 11);
            var runB = RunAndCaptureCombatEvents(seed: 11);

            CollectionAssert.AreEqual(runA, runB);
        }

        [Test]
        public void 사망_이벤트는_액터마다_한_번만_발생한다()
        {
            var tick = new MockTickService();
            var engine = CreateEnemyDeathEngine(tick);
            var actorDeathIds = new List<int>();
            engine.ActorDeathPublished += actorDeath => actorDeathIds.Add(actorDeath.ActorId);

            engine.Start();
            tick.PumpTicks(10);

            Assert.AreEqual(1, actorDeathIds.Count);
            Assert.AreEqual(1, actorDeathIds[0]);
        }

        [Test]
        public void 적이_죽으면_승리_결과_이벤트는_한_번만_발생한다()
        {
            var tick = new MockTickService();
            var engine = CreateEnemyDeathEngine(tick);
            var results = new List<BattleResult>();
            engine.BattleResultPublished += result => results.Add(result);

            engine.Start();
            tick.PumpTicks(10);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(BattleResult.Victory, results[0]);
        }

        [Test]
        public void 플레이어가_죽으면_패배_결과_이벤트는_한_번만_발생한다()
        {
            var tick = new MockTickService();
            var enemy = BattleTestFactory.CreateEnemy(
                spawnPosition: 20f,
                normalAttackDamage: 3,
                normalAttackPeriod: 2.5f,
                engageDistance: 20f);
            var engine = BattleTestFactory.CreateEngine(
                tick,
                moveSpeed: 0f,
                engageDistance: 20f,
                maxHp: 3,
                enemies: new[] { enemy });
            var results = new List<BattleResult>();
            engine.BattleResultPublished += result => results.Add(result);

            engine.Start();
            tick.PumpTicks(10);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(BattleResult.Defeat, results[0]);
        }

        private static List<string> RunAndCaptureCombatEvents(int seed)
        {
            var tick = new MockTickService();
            var engine = CreateEnemyDeathEngine(tick, seed);
            var events = new List<string>();
            engine.DamagePublished += damage =>
                events.Add($"{damage.Kind}:{damage.SourceId}>{damage.TargetId}:{damage.Amount}:{damage.SkillId}");
            engine.ActorDeathPublished += actorDeath =>
                events.Add($"Death:{actorDeath.ActorId}");

            engine.Start();
            tick.PumpTicks(10);
            return events;
        }

        private static BattleEngine CreateEnemyDeathEngine(MockTickService tick, int seed = 1)
        {
            var enemy = BattleTestFactory.CreateEnemy(spawnPosition: 20f, maxHp: 30);
            var skill = BattleTestFactory.CreateSkill(
                "tae_in_jang",
                SkillRange.Single,
                manaCost: 0,
                damageAmount: 30);
            return BattleTestFactory.CreateEngine(
                tick,
                seed: seed,
                moveSpeed: 0f,
                engageDistance: 20f,
                enemies: new[] { enemy },
                skills: new[] { skill });
        }
    }
}
