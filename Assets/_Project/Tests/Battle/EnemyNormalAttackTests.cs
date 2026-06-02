using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class EnemyNormalAttackTests
    {
        [Test]
        public void 적은_주기마다_플레이어_HP를_깎는다()
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
                maxHp: 100,
                enemies: new[] { enemy });

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += next => snapshot = next;

            engine.Start();
            tick.PumpTicks(500);

            Assert.AreEqual(70, snapshot.Player.Hp);
        }
    }
}
