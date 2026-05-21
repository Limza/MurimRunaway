using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class ResourceMutatorTests
    {
        [Test]
        public void 기세가_부족하면_SpendMomentum은_false를_반환하고_값은_그대로다()
        {
            var engine = BattleTestFactory.CreateEngine(); // Momentum 시작 0
            IResourceMutator mutator = engine;

            var ok = mutator.SpendMomentum(1); // 시작 0, 1 시도
            Assert.IsFalse(ok);

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(0, snapshot.Actors[0].Momentum);
        }

        [Test]
        public void GainMomentum은_maxMomentum으로_클램프된다()
        {
            var engine = BattleTestFactory.CreateEngine();
            IResourceMutator mutator = engine;

            mutator.GainMomentum(80); // 0 + 80 = 80 → maxMomentum 10으로 클램프

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(10, snapshot.Actors[0].Momentum);
        }
    }
}