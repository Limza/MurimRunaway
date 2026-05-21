using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class BattleEngineDeterminismTests
    {
        [Test]
        public void 같은_시드와_입력이면_스냅샷_시퀀스가_완전히_동일하다()
        {
            var snapshotsA = RunBattle(seed: 42);
            var snapshotsB = RunBattle(seed: 42);

            Assert.AreEqual(snapshotsA.Count, snapshotsB.Count);
            for (var index = 0; index < snapshotsA.Count; index++)
            {
                Assert.AreEqual(snapshotsA[index].TickIndex, snapshotsB[index].TickIndex);
                Assert.AreEqual(snapshotsA[index].Phase, snapshotsB[index].Phase);
                Assert.AreEqual(snapshotsA[index].Actors[0].Position,
                                snapshotsB[index].Actors[0].Position, 0f); // Player.position
            }
        }

        private static List<BattleSnapshot> RunBattle(int seed)
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                seed: seed,
                enemies: new[] { BattleTestFactory.CreateEnemy("e", 100f) });
            var captured = new List<BattleSnapshot>();
            engine.SnapshotPublished += s => captured.Add(s);

            engine.Start();
            tick.PumpTicks(400);
            return captured;
        }
    }
}
