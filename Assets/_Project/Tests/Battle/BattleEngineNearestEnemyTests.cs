using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class BattleEngineNearestEnemyTests
    {
        [Test]
        public void 적이_여러_위치에_있을_때_가장_가까운_적_앞에서_멈춘다()
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                enemies: new[]
                {
                    BattleTestFactory.CreateEnemy("e_near", 60f),
                    BattleTestFactory.CreateEnemy("e_mid", 80f),
                    BattleTestFactory.CreateEnemy("e_far", 100f),
                });

            BattleSnapshot last = default;
            engine.SnapshotPublished += s => last = s;

            engine.Start();

            // 0 → 40 거리 / 5 속도 = 8초. 0.05초 틱 = 160틱.
            tick.PumpTicks(160);

            Assert.AreEqual(BattlePhase.Engage, last.Phase);
            Assert.AreEqual(ActorState.Idle, last.Player.State);
            Assert.AreEqual(40f, last.Player.Position, 1e-4f);
            Assert.AreEqual(60f, last.Enemies[0].Position, 1e-4f);
            Assert.AreEqual(80f, last.Enemies[1].Position, 1e-4f);
            Assert.AreEqual(100f, last.Enemies[2].Position, 1e-4f);
        }
    }
}
