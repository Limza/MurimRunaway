# Phase 2 작업 가이드 — 자원 2종 (HP / 내공) + VContainer 도입

>
> **목표 한 줄**: Player에 내공 자원을 추가하고(HP는 Phase 1에 이미 있음), 모든 자원 변경을 단일 진입점(`IResourceMutator`)으로 통과시킨다. 매 Snapshot이 자원 현재값을 담는다. 추가로 Phase 1까지 `BattleSceneController.Start()`에서 수동 조립하던 의존성(Tick·Rng·Engine)을 VContainer LifetimeScope로 위임한다.
>
> **참조 SSOT**: [BATTLE_DESIGN.md §3 Phase 2](../BATTLE_DESIGN.md) + [MILESTONES.md M2](../MILESTONES.md) — 본 가이드는 SSOT가 아니라 작업 절차 안내. 사양이 다르면 SSOT 우선.
>
> **함께 보기**: [Phase_2_Learned.md](Phase_2_Learned.md) — 본 Phase에서 등장한 개념 정리. [Phase_2_AssetQueue.md](Phase_2_AssetQueue.md) — Phase 2 코드 작업과 병렬로 진행할 에셋 큐.
>
> **v0.4.2 스코프 컷**: SSOT가 자원 4종 → 2종으로 줄었다. **기세(momentum)** 는 Phase 3 SkillData 도입과 동반으로 이동(스킬 쌓기·소비 메커닉과 짝이 있어야 의미 있음). **오성(wisdom)** 은 메타 패시브 슬롯(Phase 11+ PlayerData)으로 이관 — per-battle 자원으로 두면 키우기 결에서 곧 max라 의미 잃음.
>

---

## 0. 사전 점검

| 항목 | 확인 |
|------|------|
| Phase 1 Acceptance 4개 | 통과 + 커밋됨 (`afe00ac`) |
| `PlayerActor`/`EnemyActor` | Domain에 존재, Engine이 mutable 권한 보유 |
| `BattleSnapshot` | `TickIndex`/`TimeSec`/`Actors`/`Phase` 4필드 |
| `BattleSceneController.Start()` | 현재 RngService·Engine을 직접 `new` 중 (수동 조립) |
| 작업 브랜치 | `feature/phase-2` 권장 |

>
> Phase 2는 자원 **컨테이너만** 만든다. 자원을 소비/회복하는 룰(SpendInwoo를 부르는 코드)은 Phase 3 이후. 본 Phase에선 mutator를 노출만 해두고 호출처가 없다.
>

---

## 1. Domain 확장

### 1.1 `PlayerActor` — 내공 필드 추가

**역할**: Player 전용 자원 컨테이너. Enemy에는 추가하지 않는다 (자원은 Player 한정 개념).

- HP는 이미 base `Actor`에 있음 — 본 Phase에선 그대로 둔다. Phase 2는 **내공** 한 종만 새로 더한다.
- 모든 필드 `int` — 부동소수점 누적 오차 회피. SSOT [BATTLE_DESIGN §3 Phase 2](../BATTLE_DESIGN.md) 표 따름.

### `Scripts/Battle/Domain/PlayerActor.cs` — 확장

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>플레이어 액터. 이동 + 자원 (HP는 base Actor) 보유.</summary>
    public sealed class PlayerActor : Actor
    {
        public float MoveSpeed;

        // 내공 — Phase 2 추가. 변경은 IResourceMutator만 통과 ([§2.1](#21-iresourcemutator)).
        public int Inwoo;
        public int MaxInwoo;
    }
}
```

> 필드를 `public`으로 두는 이유: Engine 내부(`IResourceMutator` 구현)가 직접 쓴다. View는 `ActorView` 사본으로 격리되므로 캡슐화는 mutator 규약으로 강제 ([§5 흔한 함정](#7-흔한-함정) 참조).

### 1.2 `PlayerStartData` — 시작값 추가

**역할**: 한 전투 진입 시 Player의 자원 시작값.

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투에 들어갈 때의 Player 시작값. 영구 PlayerData(Phase 11+)와 별개.</summary>
    public sealed class PlayerStartData
    {
        public int MaxHp;
        public float MoveSpeed = 5f;
        public float AttackRange = 20f;

        // 내공 시작값 — SSOT 표의 디폴트 따름
        public int MaxInwoo = 100;
        public int StartingInwoo = 50;
    }
}
```

> SSOT는 `inwoo` 디폴트=50, `maxInwoo`=100. 시작값과 최댓값을 같은 자료에 두지 않으면 Phase 11+에서 메타 강화(maxInwoo +10)와 현재값을 별개 축으로 조작하기 어려워진다.

### 1.3 `ActorView` — Player 한정 내공 필드 추가

**역할**: View가 내공 게이지를 그릴 수 있도록 스냅샷에 현재/최대값을 노출.

- Enemy ActorView에는 내공 필드가 없다 — SSOT가 Player 한정으로 명시. struct 한 종류에 다 담되, Enemy의 경우 내공 필드는 0으로 떨어진다 (`IsPlayer` 게이팅으로 View가 판별).

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

        // Player 한정 — Enemy는 0 (View가 IsPlayer로 게이팅).
        public readonly int Inwoo;
        public readonly int MaxInwoo;

        public ActorView(Actor actor)
        {
            Id = actor.Id;
            IsPlayer = actor is PlayerActor;
            Hp = actor.Hp;
            MaxHp = actor.MaxHp;
            Position = actor.Position;
            State = actor.State;

            if (actor is PlayerActor player)
            {
                Inwoo = player.Inwoo;
                MaxInwoo = player.MaxInwoo;
            }
            else
            {
                Inwoo = 0;
                MaxInwoo = 0;
            }
        }
    }
}
```

> Enemy를 위한 별도 struct(`EnemyView`)를 만들 수도 있지만, `ActorView[]` 하나로 매 틱 직렬화하는 게 Phase 11(다수 적)에서 단순하다. 자원 필드 2개의 메모리 낭비는 무시할 수준. Phase 3에서 기세 필드가 추가되면 다시 ~16B 늘어나는데, Pool이 도입되면(Phase 11+) 재평가.

---

## 2. Engine 확장

### 2.1 `IResourceMutator`

**역할**: 자원 변경의 단일 진입점. 다른 코드는 PlayerActor 자원 필드를 직접 쓰지 않는다.

- Engine **내부** 인터페이스 — Domain이 아니라 Engine asmdef에 둔다. View가 호출할 일은 없음 (Phase 3에서 Skill 시전 시 호출).
- 인터페이스로 두는 이유: Phase 3+에서 Skill 효과가 mutator를 호출할 때 `BattleEngine` 본체에 의존하지 않도록 분리. 두 번째 구현이 아직 없지만, **호출처 ≠ 구현처**가 명확해지는 시점이므로 인터페이스화가 자연스럽다.

### `Scripts/Battle/Engine/IResourceMutator.cs`

```csharp
namespace MurimRunaway.Battle.Engine
{
    /// <summary>자원 변경의 단일 진입점. 모든 자원 변경은 이 인터페이스만 통과한다.</summary>
    public interface IResourceMutator
    {
        /// <summary>amount만큼 내공 소비. amount > 현재 inwoo면 false 반환, 값 불변(atomic).</summary>
        bool SpendInwoo(int amount);

        /// <summary>maxInwoo로 클램프. silent.</summary>
        void GainInwoo(int amount);
    }
}
```

> Phase 3에서 `SpendMomentum`/`GainMomentum`이 같은 인터페이스에 추가됨. Phase 2엔 내공 두 개만 노출.

### 2.2 `BattleEngine` — `IResourceMutator` 구현 + Setup 자원 초기화

**핵심 변경**:
1. `BattleEngine : IResourceMutator` 구현.
2. `Setup` 시 `PlayerStartData`의 내공 시작값을 `PlayerActor`에 복사.
3. `SpendInwoo`는 atomic — 부족 시 값 불변 + false 반환.
4. `GainInwoo`는 maxInwoo로 클램프.

### `Scripts/Battle/Engine/BattleEngine.cs` — 추가분만

```csharp
public sealed class BattleEngine : IResourceMutator
{
    // ... 기존 필드 / 생성자 / HandleTick / TickMovement / TickEngagementCheck / FindNearestAliveEnemyIndex / PublishSnapshot 유지 ...

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

            // Phase 2 추가
            Inwoo = data.Player.StartingInwoo,
            MaxInwoo = data.Player.MaxInwoo,
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

    // ── IResourceMutator ──────────────────────────────────────────────

    public bool SpendInwoo(int amount)
    {
        if (amount > _player.Inwoo)
            return false;
        _player.Inwoo -= amount;
        return true;
    }

    public void GainInwoo(int amount)
    {
        var next = _player.Inwoo + amount;
        _player.Inwoo = next > _player.MaxInwoo ? _player.MaxInwoo : next;
    }
}
```

> **왜 Mathf.Clamp가 아니라 직접 비교?** Engine asmdef는 가능한 한 UnityEngine 의존을 끊는다 (`Mathf`는 UnityEngine). 두 줄 더 쓰는 비용으로 결정론 빌드 분리를 유지.

---

## 3. View — 자원 2 표시

진행 순서: **Unity UI 추가 → 코드 수정 → Inspector 연결 → Play 확인**.

### 3.1 자원 UI 만들기

`Battle.unity` 열고 Canvas 아래에 다음을 추가.

- 빈 UI 오브젝트 `ResourcePanel`
  - 세로 배치 (Vertical Layout Group 권장. 없으면 손수 정렬)
  - 자식 2개:
    - `HpBar` (TMP_Text + Image 게이지 — 빨강)
    - `InwooBar` (파랑)

각 Bar는 다음 구조:
```
HpBar (RectTransform)
 ├ Label (TMP_Text)        "HP"
 ├ Background (Image)      회색
 │  └ Fill (Image)         Anchor=Left, anchoredPosition.x=0, width를 코드로 조절
 └ ValueText (TMP_Text)    "50 / 50"
```

> **HpBar를 Prefab으로 만들고 1번 복제**해 라벨/색만 바꿔 InwooBar로 쓴다. Phase 2 placeholder 수준이라 정교한 UI는 Phase 14에서 다시 함. Phase 3에 기세 게이지가 추가되면 같은 Prefab을 한 번 더 복제.

### 3.2 `ResourceBar` MonoBehaviour (작은 헬퍼)

**역할**: `(current, max)`를 받아 Fill width + ValueText를 갱신. View 쪽 유틸.

### `Scripts/Battle/View/ResourceBar.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MurimRunaway.Battle.View
{
    /// <summary>current/max를 받아 Fill width + 텍스트를 갱신하는 자원 게이지 헬퍼.</summary>
    public sealed class ResourceBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private float _maxWidth = 200f;

        public void SetValue(int current, int max)
        {
            var ratio = max <= 0 ? 0f : (float)current / max;
            var size = _fill.sizeDelta;
            size.x = _maxWidth * ratio;
            _fill.sizeDelta = size;

            if (_valueText != null)
                _valueText.text = $"{current} / {max}";
        }
    }
}
```

### 3.3 `BattleSceneController` — 자원 표시 연결

추가 슬롯과 `HandleSnapshot` 분기.

```csharp
public sealed class BattleSceneController : MonoBehaviour
{
    // ... 기존 [SerializeField] 슬롯 유지 ...

    [SerializeField] private ResourceBar _hpBar;
    [SerializeField] private ResourceBar _inwooBar;

    // ... 기존 BattleEngine·_enemyMarkers·_worldMax 유지 ...

    private void HandleSnapshot(BattleSnapshot snapshot)
    {
        _counterText.text = $"Tick: {snapshot.TickIndex}  Phase: {snapshot.Phase}";

        var gaugeWidth = _gauge.rect.width;
        foreach (var actor in snapshot.Actors)
        {
            var marker = actor.IsPlayer ? _playerMarker : _enemyMarkers[actor.Id - 1];
            marker.anchoredPosition = new Vector2(actor.Position / _worldMax * gaugeWidth, 0f);

            if (actor.IsPlayer)
            {
                _hpBar.SetValue(actor.Hp, actor.MaxHp);
                _inwooBar.SetValue(actor.Inwoo, actor.MaxInwoo);
            }
        }
    }
}
```

### 3.4 Inspector 연결

| 슬롯 | 연결할 대상 |
|------|------------|
| `_hpBar` | `ResourcePanel/HpBar`에 붙은 `ResourceBar` |
| `_inwooBar` | `ResourcePanel/InwooBar` |

`PlayerStartData` 시작값은 일단 디폴트(`Inwoo=50/100`) 그대로. SerializeField로 노출할지는 Phase 3에서 Skill 비용 튜닝 시작할 때 결정.

### 3.5 Play 확인

Play 누르면:
- HP 막대가 50/50으로 가득 차 보임.
- 내공 막대가 50/100으로 절반.
- Phase 1 흐름(진군→Resolve)은 그대로 작동.

자원이 변하지 않는 게 정상 — Phase 2엔 mutator를 부르는 코드가 없음.

---

## 4. VContainer DI 도입

### 4.1 왜 지금 도입하는가

Phase 1 종료 시점의 [BattleSceneController.Start()](../../Assets/_Project/Scripts/Battle/View/BattleSceneController.cs)는 `RngService`/`BattleEngine`을 직접 `new`로 조립한다. Phase 2에서 의존성이 늘진 않지만, Phase 3에서 `IBattleSystem` 분리(SSOT [Phase 3 리팩토링 트리거](../BATTLE_DESIGN.md))가 들어오면 한 번에 5~6개 인스턴스를 손수 엮어야 한다. **그 시점에 도입하면 Phase 3 변경 + DI 도입이 한 커밋에 섞여 리뷰가 어려워진다** — 의존성이 4~5개 근방인 본 Phase가 도입 적기.

[memory/project_di_container.md] 메모리에도 "Phase 2 시작 시 도입" 명시.

### 4.2 VContainer 설치

1. Unity Editor: **Window → Package Manager → +(좌상단) → Add package from git URL**
2. 입력: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.9` (최신 안정 태그 확인 후 갱신)
3. asmdef 갱신: `MurimRunaway.Battle.View` asmdef의 `references`에 `VContainer` 추가.

> VContainer를 고른 이유: Zenject 대비 IL2CPP·AOT 호환 + 코드 생성 없는 reflection 모드 + 학습용 친화적 문서. 모바일 빌드(Android 우선)에 안전.

### 4.3 LifetimeScope 작성

### `Scripts/Battle/View/BattleLifetimeScope.cs`

```csharp
using VContainer;
using VContainer.Unity;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>전투 씬의 의존성 그래프. BattleSceneController를 Entry Point로 사용.</summary>
    public sealed class BattleLifetimeScope : LifetimeScope
    {
        [UnityEngine.SerializeField] private UnityTickService _tickService;

        protected override void Configure(IContainerBuilder builder)
        {
            // 씬에 이미 있는 MonoBehaviour는 인스턴스로 등록
            builder.RegisterComponent<ITickService>(_tickService);

            // Engine 측 POCO는 컨테이너가 new
            builder.Register<IRngService, RngService>(Lifetime.Singleton);
            builder.Register<BattleEngine>(Lifetime.Singleton).AsSelf().As<IResourceMutator>();

            // BattleSceneController를 EntryPoint로
            builder.RegisterComponentInHierarchy<BattleSceneController>();
        }
    }
}
```

### 4.4 `BattleSceneController` — 생성자/필드 주입

`Start()`의 수동 조립을 제거하고 컨테이너 주입을 받는다.

```csharp
public sealed class BattleSceneController : MonoBehaviour
{
    // [SerializeField] _tickService 슬롯 제거 (LifetimeScope가 가지고 있음)

    [SerializeField] private TMP_Text _counterText;
    [SerializeField] private RectTransform _gauge;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _enemyMarkerPrefab;
    [SerializeField] private ResourceBar _hpBar;
    [SerializeField] private ResourceBar _inwooBar;
    [SerializeField] private ResourceBar _momentumBar;
    [SerializeField] private TMP_Text _wisdomText;

    [SerializeField] private int _seed = 1;
    [SerializeField] private int _playerMaxHp = 50;
    [SerializeField] private float _playerMoveSpeed = 5f;
    [SerializeField] private EnemyData[] _enemyDatas;

    private BattleEngine _engine;
    private RectTransform[] _enemyMarkers;
    private float _worldMax;

    [VContainer.Inject]
    public void Construct(BattleEngine engine)
    {
        _engine = engine;
    }

    private void Start()
    {
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

    // HandleSnapshot은 §3.3과 동일
}
```

### 4.5 씬 배치

1. Hierarchy 비어있는 GameObject 만들고 이름 `BattleLifetimeScope`.
2. `BattleLifetimeScope` 컴포넌트 attach.
3. Inspector의 `_tickService` 슬롯에 `UnityTickService` 오브젝트 드래그.
4. `BattleSceneController` 컴포넌트의 `_tickService` 슬롯 제거(필드 자체 삭제됨).

> `BattleSceneController`는 `BattleLifetimeScope`와 같은 씬에 있으면 `RegisterComponentInHierarchy` 가 알아서 찾아낸다. EntryPoint 표시는 안 해도 됨 — MonoBehaviour는 자체 Awake에서 `[Inject]` 메서드를 호출받는다.

---

## 5. EditMode 테스트

### 5.1 `ResourceMutatorTests.cs`

**검증**:
- SpendInwoo 부족 시 false + 값 불변 (I-2.2).
- GainInwoo가 maxInwoo로 클램프 (Edge).

```csharp
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
                    MaxInwoo = 100, StartingInwoo = 50,
                },
                Enemies = new EnemyData[0],
            });
            return engine;
        }

        [Test]
        public void 내공이_부족하면_SpendInwoo는_false를_반환하고_값은_그대로다()
        {
            IResourceMutator mutator = CreateEngine();

            var ok = mutator.SpendInwoo(60); // 시작 50, 60 시도

            Assert.IsFalse(ok);
            // 스냅샷으로 검증
            ((BattleEngine)mutator).Start();
            BattleSnapshot snapshot = default;
            ((BattleEngine)mutator).SnapshotPublished += s => snapshot = s;
            Assert.AreEqual(50, snapshot.Actors[0].Inwoo);
        }

        [Test]
        public void GainInwoo는_maxInwoo로_클램프된다()
        {
            var engine = CreateEngine();
            IResourceMutator mutator = engine;

            mutator.GainInwoo(80); // 50 + 80 = 130 → 100으로 클램프

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(100, snapshot.Actors[0].Inwoo);
        }
    }
}
```

> 테스트 메서드명은 한글 시나리오 묘사 — `feedback_test_naming` 메모리 규칙. 클래스명·필드명은 영문.

---

## 6. 최종 검증 (Acceptance Checklist)

[BATTLE_DESIGN §3 Phase 2 Acceptance](../BATTLE_DESIGN.md) + M2 추가:

- [ ] EditMode: SpendInwoo로 0 미만이 되는 시도가 false 반환 + 값 불변. (§5.1)
- [ ] EditMode: GainInwoo가 maxInwoo 초과 시 maxInwoo로 클램프. (§5.1)
- [ ] PlayMode: 화면에 HP·내공 게이지 표시.
- [ ] PlayMode: VContainer LifetimeScope에서 의존성 주입 동작 확인 — Play 시 NullReferenceException 없이 Phase 1 흐름 그대로 재현.

---

## 7. 흔한 함정

- **PlayerActor.Inwoo 직접 쓰기** — `engine._player.Inwoo -= 5` 같은 직접 쓰기는 mutator를 우회. 코드 리뷰 시 grep으로 검출 (`\.Inwoo\s*[-+*/]?=` 패턴이 BattleEngine.cs의 mutator 메서드 외에 나타나면 위반).
- **GainInwoo의 음수 인자** — `GainInwoo(-5)`는 silent로 inwoo를 5 깎는다. 의도된 동작이 아니라면 호출처 버그. SpendInwoo로 명시할 것. 본 Phase에선 가드 안 추가(Phase 4 데미지 계산이 들어올 때 통합 검토).
- **Enemy ActorView의 Inwoo 필드를 게이지에 묶기** — Enemy는 Inwoo 0. `IsPlayer` 게이팅 누락하면 Enemy 마커 옆에 빈 게이지가 그려짐.
- **LifetimeScope에 Engine을 트랜션트로 등록** — Singleton이어야 매 틱 같은 인스턴스. `Lifetime.Transient`로 두면 `Construct`가 받는 Engine과 `Setup`/`Start`를 호출한 Engine이 달라질 수 있음.
- **`[Inject]` 메서드를 `private`으로** — VContainer는 public/private 모두 reflect하지만 IL2CPP 빌드(모바일)에서 stripping될 위험. `public void Construct(...)`로 두면 안전.

---

## 8. Phase 2 → Phase 3 진입 조건

§6 체크리스트 4개 + 커밋 완료. Phase 3는 [BATTLE_DESIGN §3 Phase 3](../BATTLE_DESIGN.md) — `SkillData` SO + 자동 시전 결정 트리 + **기세(momentum) 자원 컨테이너 도입**(v0.4.2 스코프 컷으로 P2에서 옮겨옴). **첫 작업으로 `IBattleSystem` 패턴 도입**(SSOT 리팩토링 트리거)이 들어와 `BattleEngine.HandleTick`이 dispatcher로 줄어든다. 본 Phase의 `IResourceMutator`는 그 때 `GainMomentum`/`SpendMomentum`이 추가되며 Skill 효과의 호출처가 된다.
