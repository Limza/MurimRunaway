using System;
using System.Linq;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. 틱마다 상태를 진행시키고 SnapshotPublished으로 통보.</summary>
    public sealed class BattleEngine
    {
        private readonly ITickService _tick;
        private readonly IRngService _rng;

        private PlayerActor _player;
        private EnemyActor[] _enemies;
        private long _tickIndex = 0;
        private float _timeSec = 0f;
        private BattlePhase _phase;

        public event Action<BattleSnapshot> SnapshotPublished;

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
            _tickIndex = 0;
            _timeSec = 0f;
            _phase = BattlePhase.Setup;
            
            _player = new PlayerActor
            {
                Id = 0,
                Hp = data.Player.MaxHp,
                MaxHp = data.Player.MaxHp,
                Position = 0f,
                MoveSpeed = data.Player.MoveSpeed,
                AttackRange = data.Player.AttackRange,
                State = ActorState.Running,
                SourceId = "player",
            };

            _enemies = data.Enemies.Select((enemyData, index) => new EnemyActor
            {
                Id = index + 1,
                Hp = enemyData.MaxHp,
                MaxHp = enemyData.MaxHp,
                Position = enemyData.SpawnPosition,
                State = ActorState.Idle,
                SourceId = enemyData.Id,
            }).ToArray();
        } 

        public void Start() 
        { 
            _phase = BattlePhase.Approach;
            PublishSnapshot();
        }

        private void HandleTick(float deltaTime)
        {
            if (_phase == BattlePhase.Resolve)
                return;

            _tickIndex++;
            _timeSec += deltaTime;

            TickMovement(deltaTime);
            TickEngagementCheck();

            PublishSnapshot();
        }

        private void TickMovement(float deltaTime)
        {
            if (_player.State != ActorState.Running)
                return;

            _player.Position += _player.MoveSpeed * deltaTime;
        }

        private void TickEngagementCheck()
        {
            if (_player.State != ActorState.Running)
                return;

            var nearestIndex = FindNearestAliveEnemyIndex();
            if (nearestIndex < 0)
                return;

            var nearest = _enemies[nearestIndex];
            var distance = nearest.Position - _player.Position;
            if (distance > _player.AttackRange)
            {
                return;
            }

            _player.Position = nearest.Position - _player.AttackRange;
            _player.State = ActorState.Idle;
            _phase = BattlePhase.Resolve;
        }

        private int FindNearestAliveEnemyIndex()
        {
            var nearestIndex = -1;
            var nearestPosition = float.MaxValue;
            for (var index = 0; index < _enemies.Length; index++)
            {
                var enemy = _enemies[index];
                
                if (enemy.State == ActorState.Dead)
                    continue;

                if (enemy.Position < nearestPosition)
                {
                    nearestPosition = enemy.Position;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        private void PublishSnapshot()
        {
            var actors = new ActorView[1 + _enemies.Length];

            actors[0] = _player.ToView();
            for (var index = 0; index < _enemies.Length; index++)
                actors[index + 1] = _enemies[index].ToView();

            SnapshotPublished?.Invoke(new BattleSnapshot(_tickIndex, _timeSec, actors, _phase));
        }
    }
}