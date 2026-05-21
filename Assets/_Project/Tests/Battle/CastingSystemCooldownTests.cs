using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class CastingSystemCooldownTests
    {
        [Test]
        public void 쿨다운_중인_무공은_다시_시전되지_않는다()
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                moveSpeed: 0f,
                enemies: new[] { BattleTestFactory.CreateEnemy(spawnPosition: 20f) },
                skills: new[] { BattleTestFactory.CreateSkill("test_skill", SkillRange.Single, cooldownSec: 1f) });
            var castSkillIds = new List<string>();
            var castSlotIndexes = new List<int>();
            engine.SkillCastPublished += skillCast => castSkillIds.Add(skillCast.SkillId);
            engine.SkillCastPublished += skillCast => castSlotIndexes.Add(skillCast.SlotIndex);

            engine.Start();

            tick.PumpTicks(1);
            Assert.AreEqual(1, castSkillIds.Count);

            tick.PumpTicks(19);
            Assert.AreEqual(1, castSkillIds.Count);

            tick.PumpTicks(1);
            Assert.AreEqual(2, castSkillIds.Count);
        }

        [Test]
        public void 서로_다른_범위_무공도_쿨다운이_끝나면_시전_후보가_된다()
        {
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                moveSpeed: 0f,
                enemies: new[] { BattleTestFactory.CreateEnemy(spawnPosition: 20f) },
                skills: new[]
                {
                    BattleTestFactory.CreateSkill("single_skill", SkillRange.Single, cooldownSec: 1f),
                    BattleTestFactory.CreateSkill("nearby_pair_skill", SkillRange.NearbyPair, cooldownSec: 1f),
                });
            var castSkillIds = new List<string>();
            var castSlotIndexes = new List<int>();
            engine.SkillCastPublished += skillCast => castSkillIds.Add(skillCast.SkillId);
            engine.SkillCastPublished += skillCast => castSlotIndexes.Add(skillCast.SlotIndex);

            engine.Start();

            tick.PumpTicks(1);
            Assert.AreEqual("single_skill", castSkillIds[0]);
            Assert.AreEqual(0, castSlotIndexes[0]);

            tick.PumpTicks(1);
            Assert.AreEqual("nearby_pair_skill", castSkillIds[1]);
            Assert.AreEqual(1, castSlotIndexes[1]);
        }
    }
}
