using System;
using System.Linq;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. 틱마다 상태를 진행시키고 SnapshotPublished으로 통보.</summary>
    public sealed class BattleEngine : IResourceMutator, ISkillExecutor
    {
        private readonly ITickService _tick;
        private readonly IRngService _rng;

        private BattleContext _context;
        private IBattleSystem[] _systems;

        public event Action<BattleSnapshot> SnapshotPublished;
        public event Action<SkillCastEvent> SkillCastPublished;

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

            // 등록 순서 = 실행 순서. Movement → Engagement → Casting.
            _systems = new IBattleSystem[]
            {
                new MovementSystem(),
                new EngagementSystem(),
                new CastingSystem(this),
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

        public bool TryCast(Actor caster, int slotIndex, Actor target)
        {
            var slot = _context.Player.Skills[slotIndex];
            var skill = slot.Data;

            if (!SpendMana(skill.ManaCost))
                return false;

            slot.CooldownRemaining = skill.CooldownSec;
            GainMomentum(skill.MomentumGainOnCast);

            var skillCastEvent = new SkillCastEvent(caster.Id, slotIndex, skill.Id, target.Id);
            SkillCastPublished?.Invoke(skillCastEvent);
            return true;
        }

        private void HandleTick(float deltaTime)
        {
            if (_context.Phase == BattlePhase.Resolve)
                return;

            _context.TickIndex++;
            _context.TimeSec += deltaTime;

            foreach (var system in _systems)
                system.Tick(_context, deltaTime);

            // 사망 판정 전까지 시스템 실행 뒤 바로 스냅샷을 보낸다.
            PublishSnapshot();
        }

        private void PublishSnapshot()
        {
            var enemies = _context.Enemies;
            var actors = new ActorView[1 + enemies.Length];

            actors[0] = _context.Player.ToView();
            for (var index = 0; index < enemies.Length; index++)
                actors[index + 1] = enemies[index].ToView();

            SnapshotPublished?.Invoke(
                new BattleSnapshot(_context.TickIndex, _context.TimeSec, actors, _context.Phase));
        }
    }
}
