using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class SkillDamageTests
    {
        [Test]
        public void 데미지_무공은_적_HP를_깎는다()
        {
            var tick = new MockTickService();
            var enemy = BattleTestFactory.CreateEnemy(spawnPosition: 20f, maxHp: 30);
            var skill = BattleTestFactory.CreateSkill(
                "tae_in_jang",
                SkillRange.Single,
                manaCost: 0,
                damageAmount: 5);
            var engine = BattleTestFactory.CreateEngine(
                tick,
                moveSpeed: 0f,
                enemies: new[] { enemy },
                skills: new[] { skill });

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += next => snapshot = next;

            engine.Start();
            tick.PumpTicks(1);

            Assert.AreEqual(25, snapshot.Enemies[0].Hp);
        }
    }
}
