using System;
using System.Linq;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. 틱마다 상태를 진행시키고 SnapshotPublished으로 통보.</summary>
    public sealed class BattleEngine : IResourceMutator, ISkillExecutor, IDamageApplier
    {
        public event Action<BattleSnapshot> SnapshotPublished;
        public event Action<SkillCastEvent> SkillCastPublished;
        public event Action<DamageEvent> DamagePublished;
        public event Action<ActorDeathEvent> ActorDeathPublished;
        public event Action<BattleResult> BattleResultPublished;

        private readonly ITickService _tick;
        private readonly IRngService _rng;
        private BattleContext _context;
        private IBattleSystem[] _systems;
        private BattleResult _result = BattleResult.None;

        public BattleEngine(ITickService tick, IRngService rng)
        {
            _tick = tick;
            _rng = rng;
            _tick.Ticked += HandleTick;
        }

        public void Dispose()
        {
            _tick.Ticked -= HandleTick;
        }

        public void Setup(BattleStartData data)
        {
            _rng.Reseed(data.Seed);
            
            var player = new PlayerActor
            {
                Id = 0,
                Hp = data.Player.MaxHp,
                MaxHp = data.Player.MaxHp,
                Mana = data.Player.StartingMana,
                MaxMana = data.Player.MaxMana,
                Momentum = 0,
                MaxMomentum = data.Player.MaxMomentum,
                Position = 0f,
                MoveSpeed = data.Player.MoveSpeed,
                EngageDistance = data.Player.EngageDistance,
                State = ActorState.Running,
                SourceId = "player",
                Skills = (data.Player.StartingSkills ?? Array.Empty<SkillData>())
                    .Select(skill => new SkillSlot { Data = skill })
                    .ToArray(),
            };

            var enemies = data.Enemies.Select((enemyData, index) => new EnemyActor
            {
                Id = index + 1,
                Hp = enemyData.MaxHp,
                MaxHp = enemyData.MaxHp,
                Position = enemyData.SpawnPosition,
                State = ActorState.Idle,
                SourceId = enemyData.Id,
                NormalAttackDamage = enemyData.NormalAttackDamage,
                NormalAttackPeriod = enemyData.NormalAttackPeriod,
                NormalAttackCooldown = 0f,
                EngageDistance = enemyData.EngageDistance,
            }).ToArray();

            _context = new BattleContext
            {
                Player = player,
                Enemies = enemies,
                TickIndex = 0,
                TimeSec = 0f,
                Phase = BattlePhase.Setup,
                Rng = _rng,
            };

            // 등록 순서 = 실행 순서. Movement → Engagement → Casting → EnemyAttack.
            _systems = new IBattleSystem[]
            {
                new MovementSystem(),
                new EngagementSystem(),
                new CastingSystem(this),
                new EnemyAttackSystem(this),
            };
        }

        public void Start()
        {
            _context.Phase = BattlePhase.Approach;
            PublishSnapshot();
        }

        public bool SpendMana(int amount)
        {
            if (amount > _context.Player.Mana)
                return false;

            _context.Player.Mana -= amount;
            return true;
        }

        public void GainMana(int amount)
        {
            var nextMana = _context.Player.Mana + amount;
            _context.Player.Mana = Math.Min(nextMana, _context.Player.MaxMana);
        }

        public bool SpendMomentum(int amount)
        {
            if (amount > _context.Player.Momentum)
                return false;

            _context.Player.Momentum -= amount;
            return true;
        }

        public void GainMomentum(int amount)
        {
            var nextMomentum = _context.Player.Momentum + amount;
            _context.Player.Momentum = Math.Min(nextMomentum, _context.Player.MaxMomentum);
        }

        public bool TryCast(Actor caster, int slotIndex)
        {
            var slot = _context.Player.Skills[slotIndex];
            var skill = slot.Data;
            var targets = _context.GetAliveEnemiesByRange(skill.PreferredRange);

            if (targets.Length == 0)
                return false;

            // 내공 소비
            if (!SpendMana(skill.ManaCost))
                return false;

            // 쿨다운 시작
            slot.CooldownRemaining = skill.CooldownSec;

            // 기세 획득
            GainMomentum(skill.MomentumGainOnCast);

            // 스킬 시전 이벤트 발행
            var skillCastEvent = new SkillCastEvent(caster.Id, slotIndex, skill.Id);
            SkillCastPublished?.Invoke(skillCastEvent);

            if (skill.DamageAmount > 0)
            {
                foreach (var skillTarget in targets.Span)
                    ApplyDamage(caster, skillTarget, skill.DamageAmount, DamageKind.Skill, skill.Id);
            }

            return true;
        }

        public void ApplyDamage(Actor source, Actor target, int amount, DamageKind damageKind, string skillId)
        {
            if (source.State == ActorState.Dead || target.State == ActorState.Dead)
                return;

            var finalDamage = Math.Max(0, amount);
            target.Hp = Math.Max(0, target.Hp - finalDamage);
            DamagePublished?.Invoke(new DamageEvent(source.Id, target.Id, finalDamage, damageKind, skillId));
        }

        private void HandleTick(float deltaTime)
        {
            if (_context.Phase == BattlePhase.Resolve)
                return;

            _context.TickIndex++;
            _context.TimeSec += deltaTime;

            foreach (var system in _systems)
                system.Tick(_context, deltaTime);

            // 사망한 액터를 Dead로 확정하고 이벤트를 한 번만 발행한다.
            {
                if (_context.Player.Hp <= 0 && _context.Player.State != ActorState.Dead)
                {
                    _context.Player.State = ActorState.Dead;
                    ActorDeathPublished?.Invoke(new ActorDeathEvent(_context.Player.Id));
                }

                foreach (var enemy in _context.Enemies)
                {
                    if (enemy.Hp > 0 || enemy.State == ActorState.Dead)
                        continue;

                    enemy.State = ActorState.Dead;
                    ActorDeathPublished?.Invoke(new ActorDeathEvent(enemy.Id));
                }
            }

            // 최종 전투 결과를 한 번만 발행한다.
            {
                if (_result == BattleResult.None && _context.Player.State == ActorState.Dead)
                {
                    PublishBattleResult(BattleResult.Defeat);
                }
                else if (_result == BattleResult.None
                    && _context.Enemies.All(enemy => enemy.State == ActorState.Dead))
                {
                    PublishBattleResult(BattleResult.Victory);
                }
            }

            // 사망 판정 전까지 시스템 실행 뒤 바로 스냅샷을 보낸다.
            PublishSnapshot();
        }

        private void PublishSnapshot()
        {
            var enemies = _context.Enemies;
            var enemyViews = new ActorView[enemies.Length];

            for (var index = 0; index < enemies.Length; index++)
                enemyViews[index] = enemies[index].ToView();

            var battleSnapshot = new BattleSnapshot(
                _context.TickIndex,
                _context.TimeSec,
                _context.Player.ToView(),
                enemyViews,
                _context.Phase);
            SnapshotPublished?.Invoke(battleSnapshot);
        }

        private void PublishBattleResult(BattleResult result)
        {
            _result = result;
            _context.Phase = BattlePhase.Resolve;
            BattleResultPublished?.Invoke(result);
        }
    }
}
