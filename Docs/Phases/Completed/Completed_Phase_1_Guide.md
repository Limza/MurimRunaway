# Phase 1 작업 가이드 — Actor & 거리축 + 플레이어 진군

> [!abstract]- 가이드 개요 (한 번 읽고 접기)
> **목표 한 줄**: Player 1명·Enemy 1~4명이 1D 거리축에서 시간에 따라 **플레이어가 적 웨이브 쪽으로 진군**하고, 가장 가까운 적의 교전 거리에 닿으면 Resolve(victory)가 송신된다.
>
> **참조 SSOT**: [[BATTLE_DESIGN]] §3 Phase 1 — 본 가이드는 SSOT가 아니라 작업 절차 안내. 사양이 다르면 SSOT 우선.
>
> **함께 보기**: [[Completed_Phase_1_Learned]] — 본 Phase에서 등장한 개념 정리. [[Completed_Phase_1_AssetQueue]] — Phase 1 코드 작업과 병렬로 진행할 에셋 큐.
>

---

## 0. 사전 점검

| 항목 | 확인 |
|------|------|
| Phase 0 Acceptance 4개 | 통과 + 커밋됨 |
| `BattleEngine` | 빈 골격 상태, `SnapshotPublished` 이벤트 노출 |
| `BattleSnapshot` | `TickIndex` 한 필드. 본 Phase에서 `BattleSnapshot`으로 확장 |
| 작업 브랜치 | `feature/phase-1` 권장 |

> [!note]
> Phase 1은 데미지·스킬·자원이 없다. "시간이 흐르고 플레이어가 적 웨이브에 닿으면 끝"이 전부. Non-goals를 의식하며 YAGNI 유지.
>

---

## 1. Domain 타입 확장

### 1.1 무엇을 만드는가 — `ActorState`

**역할**: Actor FSM 상태를 표현하는 enum.

- Phase 1에서 실제 쓰이는 상태는 `Idle` / `Running` / `Dead`. 나머지는 후속 Phase에서 활성화될 자리만 잡아둔다.
- `Running`은 **플레이어 전용** 상태(적 쪽으로 진군). Enemy는 Phase 1 내내 `Idle` 고정.
- 후속 상태(`Casting`/`HeavyCharging`/`Stunned`)도 미리 enum에 선언해두는 이유는 [[BATTLE_DESIGN]] §3 Phase 1에서 enum 정의 자체가 Phase 1 산출물이기 때문. **로직 없이 이름만**.

> [!note]
> **진영 표현**: 진영은 enum 대신 `PlayerActor`/`EnemyActor` 타입 분리로 표현한다(§1.2). View가 그릴 때 진영 정보가 필요하면 `ActorView.IsPlayer`(bool) 사용. 진영이 3개 이상으로 늘어나는 시점에 enum 도입 고려.

### `Scripts/Battle/Domain/Enums/ActorState.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>Actor FSM. Phase 1에서 활성: Idle, Running, Dead. 나머지는 후속 Phase 자리.</summary>
    public enum ActorState
    {
        Idle,
        Running,      // Player 진군 (Phase 1)
        Casting,        // Phase 3
        HeavyCharging,  // Phase 5
        Stunned,        // Phase 5
        Dead
    }
}
```

### 1.2 무엇을 만드는가 — `Actor` / `PlayerActor` / `EnemyActor`

**역할**: 런타임 액터. `Actor`는 공통 base(abstract), `PlayerActor`/`EnemyActor`가 각자 전용 필드를 가진다. Engine 내부에서 mutable, View에는 `ActorView` 사본으로 노출.

- `Actor`는 Engine asmdef로 두는 게 맞아 보이지만, 데이터 모델이므로 Domain에 둔다. Engine은 필드를 직접 변경할 권리를 가진 유일한 계층.
- 진영은 타입 자체가 표현 — `Side` 같은 필드/프로퍼티 없음. View가 진영 정보를 필요로 하면 `ActorView.IsPlayer`로 받는다.
- `Id`는 한 전투 내에서만 unique. 전투 간 누적 식별자가 필요한 시점은 Phase 11(런 구조) 이후.
- `SourceId`는 적의 경우 `EnemyData.Id`를 그대로 보관 — telemetry/디버그용. Player는 `"player"`.
- **공용 필드 vs 전용 필드 구분**: `AttackRange`는 양쪽 모두에 의미가 있어(Phase 4+ 적도 능동 공격 시작 거리로 사용) `Actor` 공통에 둔다. 반면 `MoveSpeed`는 진군하는 Player만 의미가 있어 `PlayerActor`에 둔다. "한쪽에만 의미 있는 필드"가 발생할 때만 전용 클래스로 분리.

### `Scripts/Battle/Domain/Actor.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>런타임 액터 인스턴스의 공통 base. Engine만 mutable 권한을 가진다.</summary>
    public abstract class Actor
    {
        public int Id;
        public int Hp;
        public int MaxHp;
        public float Position;
        public ActorState State;
        public string SourceId;
        /// <summary>공격 사거리. Phase 1엔 Player만 사용(진군 정지 거리). Phase 4+에 Enemy도 능동 공격 시작 거리로 활용.</summary>
        public float AttackRange;

        public ActorView ToView() => new(this);
    }
}
```

### `Scripts/Battle/Domain/PlayerActor.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>플레이어 액터. 이동 속도를 가지며 적 쪽으로 진군한다.</summary>
    public sealed class PlayerActor : Actor
    {
        public float MoveSpeed;
    }
}
```

### `Scripts/Battle/Domain/EnemyActor.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>적 액터. spawnPosition에 고정 배치된다. Phase 1엔 전용 필드 없음.</summary>
    public sealed class EnemyActor : Actor
    {
    }
}
```

> [!note]
> Phase 2에서 `PlayerActor`가 자원 2종(HP/내공) 컨테이너를 추가로 갖게 된다. 지금은 `MoveSpeed`만. `EnemyActor`도 Phase 3+에서 무공 슬롯·쿨다운 등 전용 필드가 채워진다.
>

### 1.3 무엇을 만드는가 — `EnemyData` (ScriptableObject)

**역할**: 적 한 종(種)의 정적 데이터. Phase 1에서 정의하는 필드만 둔다.

- Inspector에서 디자이너(=본인)가 코드 빌드 없이 수정 가능한 자산이 되도록 SO로 둔다.
- `[CreateAssetMenu]`로 우클릭 메뉴에 생성 항목이 뜬다.

### `Scripts/Battle/Domain/EnemyData.cs`

```csharp
using UnityEngine;

namespace MurimRunaway.Battle.Domain
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "MurimRunaway/Battle/EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("적 정의 — 전장에 고정 배치되는 적 한 종류의 SO")]

        [Tooltip("snake_case, 카테고리 내 unique")]
        public string Id;

        [Tooltip("L10n 키 (\"enemy.<id>\"). Unity Localization String Table 조회.")]
        public string NameKey;

        [Tooltip("최대 체력")]
        public int MaxHp = 30;

        [Tooltip("적은 이 위치에 고정 배치됨")]
        public float SpawnPosition = 100f;
    }
}
```

> [!note]
> Domain asmdef가 `UnityEngine`을 참조해도 되는 이유: ScriptableObject는 Unity 의존이지만 **데이터 자산**이며 런타임 로직이 없다. Domain의 "룰을 모른다"는 약속을 깨지 않음. 단, Domain의 POCO(`Actor` 등)는 절대 `using UnityEngine` 금지.
>

### 1.4 무엇을 만드는가 — `BattleStartData` / `PlayerStartData` (입력)

**역할**: 한 전투를 시작시키는 입력 묶음. View가 만들고 Engine에 넘긴다.

- `PlayerStartData`는 **per-battle 메모리 데이터** — 한 전투에 들어가는 Player 시작값. 미래엔 영구 `PlayerData`(Phase 11+ 런 구조, 로컬 저장, 레벨/소유 아이템/소유 무공) + 상점 buff + 던전 진행 상태 등을 합쳐 매 전투 직전 빌드된다. 이름이 겹치면 안 되므로 `PlayerStartData`로 분리.

### `Scripts/Battle/Domain/BattleStartData.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투를 시작시키는 입력. View가 만들고 Engine.Setup에 넘긴다.</summary>
    public sealed class BattleStartData
    {
        public int Seed;
        public PlayerStartData Player;
        public EnemyData[] Enemies;
    }
}
```

### `Scripts/Battle/Domain/PlayerStartData.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투에 들어갈 때의 Player 시작값. 영구 PlayerData(Phase 11+)와 별개.</summary>
    public sealed class PlayerStartData
    {
        public int MaxHp;
        public float MoveSpeed = 5f; // dist/s. Phase 9 경공이 일시 부스트.
        public float AttackRange = 20f; // 가장 가까운 적과의 거리 ≤ 이 값이면 진군 정지. Phase 8/12에서 재능·카드로 가변.
    }
}
```

### 1.5 무엇을 만드는가 — `BattlePhase` enum

**역할**: 전투 전체 진행 단계. Phase 1에서는 Setup / Approach / Resolve만 사용.

### `Scripts/Battle/Domain/Enums/BattlePhase.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    public enum BattlePhase { Setup, Approach, Engage, Resolve }
}
```

> [!note]
> `Engage`는 Phase 3에서 활성화된다. 이름만 미리.
>

### 1.6 무엇을 만드는가 — `ActorView` (읽기 전용 사본)

**역할**: View가 그릴 때 필요한 액터 정보의 **읽기 전용 스냅샷**. Engine 내부 `Actor`와 분리.

- `readonly struct` — View가 받아서 그리는 도중 Engine이 다음 틱을 진행해도 충돌 없음.

### `Scripts/Battle/Domain/ActorView.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 틱 View로 전달되는 액터 읽기 전용 사본.</summary>
    public readonly struct ActorView
    {
        public readonly int Id;
        public readonly bool IsPlayer;
        public readonly int Hp;
        public readonly int MaxHp;
        public readonly float Position;
        public readonly ActorState State;

        public ActorView(Actor actor)
        {
            Id = actor.Id;
            IsPlayer = actor is PlayerActor;
            Hp = actor.Hp;
            MaxHp = actor.MaxHp;
            Position = actor.Position;
            State = actor.State;
        }
    }
}
```

### 1.7 무엇을 만드는가 — `BattleSnapshot` (Phase 0의 `BattleSnapshot` 확장)

**역할**: 매 틱 View로 보내는 전체 스냅샷. Phase 0의 `BattleSnapshot`를 대체.

- `BattleSnapshot`는 더 이상 쓰지 않는다 — 본 Phase에서 **삭제**하고 `BattleSnapshot`으로 교체.
- `actors`는 id 오름차순 정렬 보장 (Edge 케이스 — 동일 position 적 N명의 결정성).

### `Scripts/Battle/Domain/BattleSnapshot.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 틱 Engine이 View에 보내는 전체 스냅샷. id 오름차순 정렬 보장.</summary>
    public readonly struct BattleSnapshot
    {
        public readonly long TickIndex;
        public readonly float TimeSec;
        public readonly ActorView[] Actors;
        public readonly BattlePhase Phase;

        public BattleSnapshot(long tickIndex, float timeSec, ActorView[] actors, BattlePhase phase)
        {
            TickIndex = tickIndex;
            TimeSec = timeSec;
            Actors = actors;
            Phase = phase;
        }
    }
}
```

---

## 2. Engine 확장

### 2.1 무엇을 만드는가 — `BattleEngine` (재작성)

**역할**: Phase 0의 카운터 증가 로직을 버리고, Setup/Approach/Resolve의 흐름을 구현.

- `Setup(data)`: RngService.Reseed(data.Seed) + Actor 생성. Player.position=0, state=Running. Enemy는 spawnPosition에 state=Idle 고정.
- 매 틱: Player가 Running이면 `player.position += playerRunSpeed * dt`. 가장 가까운 살아있는 적과의 거리가 `player.attackRange` 이하가 되면 Player → Idle (position 클램프).
- Player가 Idle이 되면 `BattlePhase.Resolve` 송신 후 Tick 구독 해제.
- 매 틱 끝에 `BattleSnapshot` 발행.

### `Scripts/Battle/Engine/BattleEngine.cs` — 재작성

```csharp
using System;
using System.Linq;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. Phase 1: 플레이어 진군과 Resolve(victory) 흐름.</summary>
    public sealed class BattleEngine
    {
        private readonly ITickService _tick;
        private readonly IRngService _rng;

        private PlayerActor _player;
        private EnemyActor[] _enemies;
        private long _tickIndex;
        private float _timeSec;
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

            _enemies = data.Enemies
                .Select((enemyData, index) => new EnemyActor
                {
                    Id = index + 1,
                    Hp = enemyData.MaxHp,
                    MaxHp = enemyData.MaxHp,
                    Position = enemyData.SpawnPosition,
                    State = ActorState.Idle,
                    SourceId = enemyData.Id,
                })
                .ToArray();
        }

        public void Start()
        {
            _phase = BattlePhase.Approach;
            PublishSnapshot();
        }

        // HandleTick은 오케스트레이션만. mutation 로직은 Tick~~ 서브루틴이 담당.
        // 새 동작 추가 = 새 Tick~~ 함수 + 호출 한 줄. HandleTick 내부 분기 늘리지 말 것.
        private void HandleTick(float deltaTime)
        {
            if (_phase == BattlePhase.Resolve)
            {
                return;
            }
            _tickIndex++;
            _timeSec += deltaTime;

            TickMovement(deltaTime);
            TickEngagementCheck();

            PublishSnapshot();
        }

        private void TickMovement(float deltaTime)
        {
            if (_player.State != ActorState.Running)
            {
                return;
            }
            _player.Position += _player.MoveSpeed * deltaTime;
        }

        private void TickEngagementCheck()
        {
            if (_player.State != ActorState.Running)
            {
                return;
            }

            var nearestIndex = FindNearestAliveEnemyIndex();
            if (nearestIndex < 0)
            {
                return;
            }

            var nearest = _enemies[nearestIndex];
            var distance = nearest.Position - _player.Position;
            if (distance > _player.AttackRange)
            {
                return;
            }

            // 클램프 — 큰 deltaTime이 사거리 안쪽으로 밀어넣는 것 방지
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
            // id 오름차순 — Player 먼저(id=0), 적은 id 순 (생성 시 이미 정렬)
            var actors = new ActorView[1 + _enemies.Length];
            actors[0] = _player.ToView();
            for (var index = 0; index < _enemies.Length; index++)
                actors[index + 1] = _enemies[index].ToView();

            SnapshotPublished?.Invoke(new BattleSnapshot(
                _tickIndex, _timeSec, actors, _phase));
        }
    }
}
```

> [!note]
> **Player/Enemy 도메인 타입 분리**: 양쪽에 공통으로 의미 있는 필드(`AttackRange` 등)는 `Actor` 베이스에 둔다. 한쪽만 의미 있는 필드(`MoveSpeed`는 Player 전용)는 전용 클래스에 둔다. 공용 `Actor`에 한쪽 전용 필드를 섞으면 반대편에서 의미 없는 필드가 된다. 진영 표현도 enum 필드가 아니라 타입 자체가 담당(`is PlayerActor`).

---

## 3. View 측

진행 순서: **Unity 자산 준비 → 코드 작성 → Inspector 연결 → Play 확인**. 코드는 자산을 참조하므로 자산이 먼저 있어야 슬롯에 끌어다 놓을 수 있다.

**역할**: 거리 게이지로 액터 위치를 그려본다. Placeholder 수준 — 가로 막대 + 색 사각형.

- TMP 카운터는 그대로 두고 거리 표시는 추가.
- 본 Phase의 시각화는 **빨간 사각형(적) + 파란 사각형(플레이어)**로 충분. 실에셋은 [[Completed_Phase_1_AssetQueue]]에서 미래 Phase용으로 큐잉.

### 3.1 EnemyData 자산 생성

1. Project 창에서 `Assets/_Project/Data/Enemies/` 폴더 생성(없으면).
2. 그 폴더에 우클릭 → Create → MurimRunaway → Battle → EnemyData.
3. Inspector에서 다음을 입력.

   | 필드 | 값 | 비고 |
   |------|----|----|
   | `Id` | `test_enemy` | snake_case, 카테고리 내 unique |
   | `NameKey` | `enemy.test_enemy` | L10n 키. Phase 1엔 실제 번역 없음 |
   | `MaxHp` | `30` | Phase 1엔 데미지가 없어 의미는 후속 Phase에서 |
   | `SpawnPosition` | `100` | **거리축 좌표. Player(0)에서 100 거리에 고정 배치. 게이지 끝 위치 매핑의 SSOT** ([§3.4](#34-battlescenecontroller-재작성)의 `_worldMax`가 이 값을 읽음) |

### 3.2 거리 게이지 UI 만들기

`Battle.unity` 열고 Hierarchy의 `Canvas` 아래에 다음을 추가.

- 빈 UI 오브젝트 `DistanceGauge`
  - RectTransform: Width=600, Height=20 정도
  - 배경 Image 컴포넌트는 선택 (회색)
- `DistanceGauge` 자식 → UI → Image, 이름 `PlayerMarker`
  - 색 파랑, Width/Height=20
  - **Anchor를 Left-Center로 설정**. `anchoredPosition.x`가 게이지 왼쪽 기준 픽셀이 되어 §3.4 코드의 매핑 공식이 그대로 맞는다.
- 같은 부모 아래 Image 하나 더 → 이름 `EnemyMarker`
  - 색 빨강, Anchor·크기는 PlayerMarker와 동일.

### 3.3 EnemyMarker를 Prefab으로

1. `Assets/_Project/Prefabs/Battle/` 폴더 생성.
2. Hierarchy의 `EnemyMarker`를 그 폴더로 드래그 → Prefab 생성.
3. Hierarchy에 남은 `EnemyMarker` 인스턴스는 **삭제**. 런타임에 적 수만큼 `Instantiate`로 생성된다.

> Player는 항상 1명이라 씬에 인스턴스로 두고 직접 참조한다(`_playerMarker`). Enemy는 1~4명 가변이라 Prefab을 두고 `Start()`에서 복제한다(`_enemyMarkerPrefab` → `Instantiate`). "마커" 이름 비대칭의 이유.

### 3.4 `BattleSceneController` (재작성)

**핵심 변경 사항**

- `BattleEngine` 생성자 시그니처 변경: `(ITickService, IRngService)` 받음. `BattleSceneController`도 RngService 인스턴스를 만들어 주입.
- `SnapshotPublished` 핸들러는 `BattleSnapshot` 받음. `state.TickIndex`, `state.Phase`, `state.Actors[i].Position`을 UI에 반영.
- 거리 게이지 매핑: `anchoredPosition.x = position / worldMax * gaugeWidth`. `worldMax`는 `_enemyDatas` 중 가장 먼 `SpawnPosition`(가장 멀리 있는 적이 게이지 끝에 오도록).

```csharp
using System.Linq;
using UnityEngine;
using TMPro;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>씬에서 Engine·View·TickService를 조립·연결하는 접착제.</summary>
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private UnityTickService _tickService;
        [SerializeField] private TMP_Text _counterText;
        [SerializeField] private RectTransform _gauge;
        [SerializeField] private RectTransform _playerMarker;
        [SerializeField] private RectTransform _enemyMarkerPrefab;

        [SerializeField] private int _seed = 1;
        [SerializeField] private int _playerMaxHp = 50;
        [SerializeField] private float _playerMoveSpeed = 5f;
        [SerializeField] private EnemyData[] _enemyDatas;

        private BattleEngine _engine;
        private RectTransform[] _enemyMarkers;
        private float _worldMax;

        private void Start()
        {
            var rng = new RngService();
            _engine = new BattleEngine(_tickService, rng);
            _engine.SnapshotPublished += HandleSnapshot;

            _worldMax = _enemyDatas.Max(enemyData => enemyData.SpawnPosition);

            _enemyMarkers = new RectTransform[_enemyDatas.Length];
            for (var index = 0; index < _enemyDatas.Length; index++)
                _enemyMarkers[index] = Instantiate(_enemyMarkerPrefab, _gauge);

            _engine.Setup(new BattleStartData
            {
                Seed = _seed,
                Player = new PlayerStartData { MaxHp = _playerMaxHp, MoveSpeed = _playerMoveSpeed },
                Enemies = _enemyDatas,
            });
            _engine.Start();
        }

        private void OnDestroy()
        {
            _engine?.Dispose();
        }

        private void HandleSnapshot(BattleSnapshot snapshot)
        {
            _counterText.text = $"Tick: {snapshot.TickIndex}  Phase: {snapshot.Phase}";

            var gaugeWidth = _gauge.rect.width;
            foreach (var actor in snapshot.Actors)
            {
                var marker = actor.IsPlayer ? _playerMarker : _enemyMarkers[actor.Id - 1];
                marker.anchoredPosition = new Vector2(actor.Position / _worldMax * gaugeWidth, 0f);
            }
        }
    }
}
```

> [!note]
> 마커 매핑은 `actor.Id`에 의존(player=0, enemy=1부터). Engine [§2.1](#21-battleenginecs-재작성)이 이 규약을 지킨다.
>
> `_worldMax`는 SO나 const가 아니라 데이터에서 파생. "가장 먼 적 = 게이지 끝"이 SSOT가 `EnemyData.SpawnPosition`이라는 의미. 전장 길이와 적 위치가 분리되어야 할 때(예: 여유 공간) 별도 필드로 승격.

### 3.5 Inspector 연결

`BattleSceneController` 컴포넌트가 붙은 오브젝트를 선택하고 슬롯을 채운다.

| 슬롯 | 연결할 대상 |
|------|------------|
| `_tickService` | 씬의 `UnityTickService` 오브젝트 |
| `_counterText` | TMP_Text 오브젝트 |
| `_gauge` | `DistanceGauge` |
| `_playerMarker` | `PlayerMarker` |
| `_enemyMarkerPrefab` | §3.3에서 만든 Prefab (Project 창에서 드래그) |
| `_enemyDatas` | Size=1, 슬롯에 §3.1의 EnemyData 자산 드래그 |

### 3.6 Play 확인

Play 누르면 파란 마커가 게이지 왼쪽(position=0)에서 오른쪽으로 이동하다 빨간 마커(position=100) 근처에서 멈추고, 카운터의 Phase가 `Resolve`로 바뀌면 OK. 안 멈추거나 Phase가 바뀌지 않으면 [§7 흔한 함정](#7-흔한-함정) 확인.

---

## 4. EditMode 테스트

### 4.1 `BattleEngineApproachTests.cs`

**검증**: Player 1 (runSpeed=5, attackRange=20), Enemy 1 (spawn=100) → 16.0s ± 0.05s 안에 Player Idle 전이 + Resolve.

```csharp
using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class BattleEngineApproachTests
    {
        [Test]
        public void 플레이어가_적_사거리에_도달하면_Idle_Resolve_전이된다()
        {
            var tick = new MockTickService();
            var rng = new RngService();
            var engine = new BattleEngine(tick, rng);

            BattleSnapshot last = default;
            engine.SnapshotPublished += s => last = s;

            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Id = "test";
            enemy.MaxHp = 30;
            enemy.SpawnPosition = 100f;

            engine.Setup(new BattleStartData
            {
                Seed = 1,
                Player = new PlayerStartData { MaxHp = 50, MoveSpeed = 5f, AttackRange = 20f },
                Enemies = new[] { enemy }
            });

            // 플레이어가 0 → 80까지 진군 = 80 거리 / 5 속도 = 16초. 0.05초 틱 = 320틱.
            tick.PumpTicks(320);

            Assert.AreEqual(BattlePhase.Resolve, last.Phase);
            Assert.AreEqual(ActorState.Idle, last.Actors[0].State);    // Player
            Assert.AreEqual(80f, last.Actors[0].Position, 1e-4f);      // Player position
            Assert.AreEqual(100f, last.Actors[1].Position, 1e-4f);     // Enemy 고정
        }
    }
}
```

> [!note]
> `ScriptableObject.CreateInstance`는 EditMode에서 호출 가능. Tests asmdef가 `UnityEngine`을 참조하는지 확인 (자동 참조됨).

### 4.2 `BattleEngineNearestEnemyTests.cs`

**검증**: 적 3명을 다른 위치(spawn=60/80/100)에 두고, Player가 **가장 가까운 적(spawn=60)** 에 닿는 순간 Resolve + position=40으로 클램프.

```csharp
using NUnit.Framework;
using MurimRunaway.Battle.Domain;
using MurimRunaway.Battle.Engine;
using UnityEngine;

namespace MurimRunaway.Battle.Tests
{
    public class BattleEngineNearestEnemyTests
    {
        [Test]
        public void 적이_여러_위치에_있을_때_가장_가까운_적_앞에서_멈춘다()
        {
            var tick = new MockTickService();
            var engine = new BattleEngine(tick, new RngService());

            BattleSnapshot last = default;
            engine.SnapshotPublished += s => last = s;

            engine.Setup(new BattleStartData
            {
                Seed = 1,
                Player = new PlayerStartData { MaxHp = 50, MoveSpeed = 5f, AttackRange = 20f },
                Enemies = new[]
                {
                    MakeEnemy("e_near", 60f),
                    MakeEnemy("e_mid", 80f),
                    MakeEnemy("e_far", 100f),
                }
            });

            // 0 → 40 거리 / 5 속도 = 8초. 0.05초 틱 = 160틱.
            tick.PumpTicks(160);

            Assert.AreEqual(BattlePhase.Resolve, last.Phase);
            Assert.AreEqual(ActorState.Idle, last.Actors[0].State);
            Assert.AreEqual(40f, last.Actors[0].Position, 1e-4f);   // Player: 가장 가까운 적-AttackRange
            Assert.AreEqual(60f, last.Actors[1].Position, 1e-4f);   // 적은 모두 spawn 위치 유지
            Assert.AreEqual(80f, last.Actors[2].Position, 1e-4f);
            Assert.AreEqual(100f, last.Actors[3].Position, 1e-4f);
        }

        private static EnemyData MakeEnemy(string id, float spawn)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Id = id;
            enemy.MaxHp = 30;
            enemy.SpawnPosition = spawn;
            return enemy;
        }
    }
}
```

> [!note]
> 이 케이스가 `FindNearestAliveEnemyIndex`의 진짜 검증. 적 1명 케이스(§4.1)에선 nearest 선택 분기가 안 돌아간다.

> [!note]
> Phase 1엔 RNG 실제 호출이 없어 결정성 테스트는 별도로 두지 않는다. Phase 3(무공·스킬)에서 `IRngService` 실제 사용이 들어가는 시점부터 추가.

---

## 5. 최종 검증 (Acceptance Checklist)

[[BATTLE_DESIGN]] §3 Phase 1 Acceptance:

- [ ] Player 1 (runSpeed=5, attackRange=20), Enemy 1 (spawn=100) → 16.0s ± 0.05s 안에 Player Idle 전이 + Resolve(victory) 송신.
- [ ] Player(attackRange=20) vs Enemy 3명 spawn={60, 80, 100} → 가장 가까운 적(spawn=60)이 사거리 내가 되는 순간 Resolve. Player.position=40으로 클램프, 적 3명은 Idle 유지.
- [ ] §4 EditMode 테스트 2개(`Approach`, `NearestEnemy`) 통과.
- [ ] PlayMode 시각 확인: 플레이어 마커가 거리 게이지에서 적 쪽으로 실시간 이동.

---

## 6. 커밋

```bash
git add Assets/_Project Docs/Phases/Phase_1_Guide.md Docs/Phases/Phase_1_Learned.md Docs/Phases/Phase_1_AssetQueue.md
git commit -m "Phase 1: actor & distance axis (1D approach, Resolve, deterministic snapshot)"
```

이후 [[BATTLE_DESIGN]] §5 변경 이력에 Phase 1 완료 한 줄 추가.

---

## 7. 흔한 함정

- **`Actor`에 한쪽 전용 필드 섞기** — `MoveSpeed`처럼 한쪽만 의미 있는 필드를 공용 `Actor`에 두면 반대편에서 의미 없는 0이 됨. 반대로 양쪽에 의미 있는 `AttackRange`는 공용 base에 둬야 자연스러움. 기준: "반대편에서도 같은 의미로 쓸 일이 있는가".
- **병렬 배열 + "같은 인덱스" 주석** — `_enemies[i]`와 `_extras[i]`처럼 인덱스 동기화를 주석으로 강제하는 패턴은 한쪽만 정렬·필터되는 순간 침묵 버그. 한 객체에 묶어 컴파일러가 짝을 보장하게 한다.
- **거리 계산을 절댓값으로** — `Mathf.Abs(enemy.Position - player.Position)`은 의미 없음. 적은 항상 플레이어보다 앞(큰 position)이므로 `enemy.Position - player.Position`가 거리. 음수가 나오면 클램프 실패 → 버그 신호.
- **`Time.deltaTime` 직접 호출** — Engine은 절대 금지. 진군 속도 계산은 반드시 `HandleTick(dt)`의 dt 파라미터로.
- **`actors` 배열을 매 틱 새로 할당** — Phase 1에선 GC가 문제 아님. Phase 11+에서 적 수가 많아지면 Pool 검토.
- **EnemyData를 직접 mutate** — SO는 자산이라 런타임 변경이 디스크에 반영될 수 있음(에디터). 반드시 Actor 인스턴스로 복사 후 변경.

---

## 8. Phase 1 → Phase 2 진입 조건

§5 체크리스트 4개 + 커밋 완료. Phase 2는 [[BATTLE_DESIGN]] §3 Phase 2에서 자원 2종(HP/내공) 컨테이너와 `IResourceMutator`, VContainer를 도입한다.
