using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;
using UnityEngine;

namespace MurimRunaway.Battle.Tests
{
    internal static class BattleTestFactory
    {
        public static BattleEngine CreateEngine(
            MockTickService tick = null,
            int seed = 1,
            float moveSpeed = 5f,
            float engageDistance = 20f,
            int maxHp = 50,
            int maxMana = 100,
            int startingMana = 50,
            int maxMomentum = 10,
            EnemyData[] enemies = null,
            SkillData[] skills = null)
        {
            if (tick == null)
                tick = new MockTickService();

            var engine = new BattleEngine(tick, new RngService());
            engine.Setup(CreateStartData(
                seed: seed,
                moveSpeed: moveSpeed,
                engageDistance: engageDistance,
                maxHp: maxHp,
                maxMana: maxMana,
                startingMana: startingMana,
                maxMomentum: maxMomentum,
                enemies: enemies,
                skills: skills));
            return engine;
        }

        public static BattleStartData CreateStartData(
            int seed = 1,
            float moveSpeed = 5f,
            float engageDistance = 20f,
            int maxHp = 50,
            int maxMana = 100,
            int startingMana = 50,
            int maxMomentum = 10,
            EnemyData[] enemies = null,
            SkillData[] skills = null)
        {
            return new BattleStartData
            {
                Seed = seed,
                Player = new PlayerStartData
                {
                    MaxHp = maxHp,
                    MaxMana = maxMana,
                    StartingMana = startingMana,
                    MaxMomentum = maxMomentum,
                    MoveSpeed = moveSpeed,
                    EngageDistance = engageDistance,
                    StartingSkills = skills,
                },
                Enemies = enemies ?? new EnemyData[0],
            };
        }

        public static EnemyData CreateEnemy(
            string id = "test_enemy",
            float spawnPosition = 20f,
            int maxHp = 30,
            int normalAttackDamage = 3,
            float normalAttackPeriod = 2.5f,
            float engageDistance = 20f)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Id = id;
            enemy.MaxHp = maxHp;
            enemy.SpawnPosition = spawnPosition;
            enemy.NormalAttackDamage = normalAttackDamage;
            enemy.NormalAttackPeriod = normalAttackPeriod;
            enemy.EngageDistance = engageDistance;
            return enemy;
        }

        public static SkillData CreateSkill(
            string id,
            SkillRange range,
            int manaCost = 0,
            float cooldownSec = 1.5f,
            int momentumGainOnCast = 0,
            int damageAmount = 0)
        {
            var skill = ScriptableObject.CreateInstance<SkillData>();
            skill.Id = id;
            skill.Kind = SkillKind.Technique;
            skill.ManaCost = manaCost;
            skill.CooldownSec = cooldownSec;
            skill.PreferredRange = range;
            skill.MomentumGainOnCast = momentumGainOnCast;
            skill.DamageAmount = damageAmount;
            return skill;
        }
    }
}
