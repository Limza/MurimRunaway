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
            Assert.AreEqual(ActorState.Idle, last.Actors[0].State);
            Assert.AreEqual(40f, last.Actors[0].Position, 1e-4f);   // Player: 가장 가까운 적-EngageDistance
            Assert.AreEqual(60f, last.Actors[1].Position, 1e-4f);   // 적은 모두 spawn 위치 유지
            Assert.AreEqual(80f, last.Actors[2].Position, 1e-4f);
            Assert.AreEqual(100f, last.Actors[3].Position, 1e-4f);
        }
    }
}
