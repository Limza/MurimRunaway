using System.Collections.Generic;
using NUnit.Framework;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Tests
{
    public class CastingSystemPriorityTests
    {
        [Test]
        public void 서로_다른_범위_무공도_조건_충족이면_슬롯0이_먼저_시전된다()
        {
            var castSkillIds = new List<string>();
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                engageDistance: 50f,
                enemies: new[] { BattleTestFactory.CreateEnemy("e", 100f) },
                skills: new[]
                {
                    BattleTestFactory.CreateSkill(
                        "tae_in_jang",
                        SkillRange.Single,
                        manaCost: 5,
                        momentumGainOnCast: 1),
                    BattleTestFactory.CreateSkill(
                        "cheonha",
                        SkillRange.NearbyPair,
                        manaCost: 5,
                        momentumGainOnCast: 1),
                });
            engine.SkillCastPublished += skillCast => castSkillIds.Add(skillCast.SkillId);

            engine.Start();
            tick.PumpTicks(220);

            Assert.AreEqual("tae_in_jang", castSkillIds[0]);
        }

        // 슬롯 0 우선순위: 둘 다 조건을 충족하면 슬롯 0이 먼저, 한 Tick 한 시전.
        [Test]
        public void 두_무공이_모두_조건_충족이면_슬롯0이_먼저_시전된다()
        {
            var castSkillIds = new List<string>();
            var tick = new MockTickService();
            var engine = BattleTestFactory.CreateEngine(
                tick,
                engageDistance: 50f,
                enemies: new[] { BattleTestFactory.CreateEnemy("e", 100f) },
                skills: new[]
                {
                    BattleTestFactory.CreateSkill(
                        "first",
                        SkillRange.NearbyPair,
                        manaCost: 5,
                        momentumGainOnCast: 1),
                    BattleTestFactory.CreateSkill(
                        "second",
                        SkillRange.NearbyPair,
                        manaCost: 5,
                        momentumGainOnCast: 1),
                });
            engine.SkillCastPublished += skillCast => castSkillIds.Add(skillCast.SkillId);

            engine.Start();
            tick.PumpTicks(220);

            Assert.AreEqual("first", castSkillIds[0]);
        }
    }
}
