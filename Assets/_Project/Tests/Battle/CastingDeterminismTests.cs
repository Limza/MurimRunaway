using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class CastingDeterminismTests
    {
        [Test]
        public void 같은_입력이면_SkillCastPublished_시퀀스가_완전히_동일하다()
        {
            var runA = RunAndCaptureCasts(seed: 7);
            var runB = RunAndCaptureCasts(seed: 7);

            CollectionAssert.AreEqual(runA, runB); // 순서·내용 완전 일치
        }

        private static List<string> RunAndCaptureCasts(int seed)
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                seed: seed,
                engageDistance: 50f,
                enemies: new[] { BattleTestFactory.CreateEnemy("e", 100f) },
                skills: new[]
                {
                    BattleTestFactory.CreateSkill("cheonha", SkillRange.NearbyPair, manaCost: 5),
                });
            var casts = new List<string>();
            engine.SkillCastPublished += skillCast =>
                casts.Add($"{skillCast.SlotIndex}:{skillCast.SkillId}");

            engine.Start();
            tick.PumpTicks(400);
            return casts;
        }
    }
}
