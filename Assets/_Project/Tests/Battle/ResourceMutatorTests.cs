using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class ResourceMutatorTests
    {
        private static BattleEngine CreateEngine()
        {
            var engine = new BattleEngine(new MockTickService(), new RngService());
            engine.Setup(new BattleStartData
            {
                Seed = 1,
                Player = new PlayerStartData
                {
                    MaxHp = 50, MoveSpeed = 5f, AttackRange = 20f,
                    MaxMana = 100, StartingMana = 50,
                },
                Enemies = new EnemyData[0],
            });
            return engine;
        }

        [Test]
        public void 내공이_부족하면_SpendMana는_false를_반환하고_값은_그대로다()
        {
            var engine = CreateEngine();
            IResourceMutator mutator = engine;

            var ok = mutator.SpendMana(60); // 시작 50, 60 시도
            Assert.IsFalse(ok);

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(50, snapshot.Actors[0].Mana);
        }

        [Test]
        public void GainMana는_maxMana로_클램프된다()
        {
            var engine = CreateEngine();
            IResourceMutator mutator = engine;

            mutator.GainMana(80); // 50 + 80 = 130 → 100으로 클램프

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(100, snapshot.Actors[0].Mana);
        }
    }
}