using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;
using UnityEngine;

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
            var engine = new BattleEngine(tick, new RngService());
            var captured = new List<BattleSnapshot>();
            engine.SnapshotPublished += s => captured.Add(s);

            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Id = "e";
            enemy.MaxHp = 30;
            enemy.SpawnPosition = 100f;

            engine.Setup(new BattleStartData
            {
                Seed = seed,
                Player = new PlayerStartData { MaxHp = 50, MoveSpeed = 5f, AttackRange = 20f },
                Enemies = new[] { enemy }
            });
            tick.PumpTicks(400);
            return captured;
        }
    }
}