# Phase 3 작업 가이드 — 무공 자동 시전 (IBattleSystem 리팩토링 + 결정 트리 + 기세)

> [!abstract]- 가이드 개요 — 목표·참조·무게 (한 번 읽고 접기)
> **목표 한 줄**: Phase 1의 `BattleEngine.HandleTick` 진군·교전 로직을 `IBattleSystem`들로 추출하고(첫 작업), `SkillData`(SO)와 자동 시전 결정 트리(`CastingSystem`)를 추가해 **거리·쿨·내공 조건이 맞으면 슬롯 우선순위대로 무공이 자동 발동**되게 한다.
> v0.4.2 스코프 컷으로 **기세(momentum)** 자원도 본 Phase에서 도입한다.
>
> **참조 SSOT**: [[BATTLE_DESIGN]] §3 Phase 3 + [[MILESTONES]] M3 — 본 가이드는 SSOT가 아니라 작업 절차 안내. 사양이 다르면 SSOT 우선.
> 단, 본 Phase는 SSOT 표기와 코드 컨벤션이 충돌하는 지점이 2곳 있다 ([§0.1 SSOT 정합 노트](#01-ssot-정합-노트)) — 가이드는 컨벤션을 따르고 그 자리에 노트를 둔다.
>
> **함께 보기**:
> - [[Completed_Phase_3_Learned]] — 본 Phase에서 등장한 개념 정리.
> - [[Completed_Phase_3_AssetQueue]] — 병렬 에셋 큐(Track A).
> - [[Completed_Phase_2_Learned]] §5 — ActorView 분리 트리거 판단 근거를 본 Phase에서 다시 참조한다.
>
> **본 Phase의 무게**: M3 = **오토배틀의 코어 그 자체**(MILESTONES 부담 M).
> Phase 1/2(부담 S)보다 크다.
> §1 리팩토링을 먼저 끝낸 뒤 §2~§3로 넘어간다.
>

---

## 0. 사전 점검

| 항목 | 확인 |
|------|------|
| Phase 2 Acceptance 4개 | 통과 + 커밋됨 (`3f018fa`) |
| Domain 현재 타입 | `Actor`/`PlayerActor`(`Mana`/`MaxMana`)/`EnemyActor`/`ActorView`(Player 한정 `Mana`/`MaxMana`)/`BattleSnapshot`/`ActorState`/`BattlePhase` |
| `BattleEngine` | `HandleTick` → `TickMovement` + `TickEngagementCheck` 서브루틴 구조, `IResourceMutator` 구현 |
| `BattlePhase.Engage` | enum에 이름만 있고 미사용 — 본 Phase에서 활성 |
| `ActorState.Casting` | enum에 이름만 있고 미사용 — 본 Phase에서 활성 |
| VContainer | `BattleLifetimeScope`에서 Tick·Rng·Engine 조립 (Phase 2 도입 완료) |
| 작업 브랜치 | `feature/phase-3` 권장 (단 Phase 0~2는 main 직커밋 — 프로젝트 관행 따름) |

> [!note]
> Phase 3는 **데미지가 없다**. 무공이 발동해도 적 HP는 안 깎인다(`SkillCastPublished` 이벤트만 발생).
> 데미지·사망·승패는 Phase 4. 강공/오의 분기는 Phase 5/6.
> Non-goals를 의식하며 YAGNI 유지.
>

### 0.1 SSOT 정합 노트

SSOT [[BATTLE_DESIGN]] §3 Phase 3의 산문 표기가 프로젝트 코드 컨벤션([Scripts/CLAUDE.md](../../Assets/_Project/Scripts/CLAUDE.md) + 메모리 규칙)과 충돌하는 지점 2곳.
가이드는 **컨벤션을 따르고** 아래처럼 정리한다.
SSOT 본문은 추후 같은 방향으로 정합 권장(루트 [CLAUDE.md](../../CLAUDE.md) §1: 설계 표기 변경은 SSOT 우선 갱신).

| # | SSOT 표기 | 충돌 규칙 | 본 가이드 처리 |
|---|-----------|-----------|----------------|
| C-1 | `skillSlots: SkillData[]` + `skillCooldowns: float[]` (병렬 배열, 같은 인덱스) | 병렬 배열 + 인덱스 동기화 금지 (`feedback_no_parallel_arrays`) | `SkillSlot { Data; CooldownRemaining; }` 한 컨테이너로 번들 ([§2.4](#24-skillslot--병렬-배열-번들)) |

---

## 1. 리팩토링 — `IBattleSystem` + `BattleContext` (본 Phase 첫 작업)

SSOT [[BATTLE_DESIGN]] §3 Phase 3 리팩토링 트리거: *"본 Phase 첫 작업으로 `IBattleSystem` 패턴 도입."*
§2 Domain보다 **먼저** 한다 — 결정 트리(`CastingSystem`)가 이 골격 위에 올라가기 때문.

### 1.1 왜 지금인가

Phase 1의 `BattleEngine.HandleTick`은 tick mutation을 `TickMovement`/`TickEngagementCheck` 2개 들고 있다.
Phase 3에서 자동 시전(`CastingSystem`)이 **3번째 tick mutation**으로 들어온다.

메모리 `project_tick_split`: *"Tick~~ 함수가 늘고 내부 상태를 보유하기 시작하면 시스템 클래스로 분리"* — 그 트리거가 지금(N=2→3) 충족된다.
미루면 Phase 5 강공·Phase 6 오의 매트릭스가 전부 `BattleEngine` 안에 쌓여 god class가 된다.

분리 결과:
- **`BattleContext`** — 시스템들이 공유하는 가변 시뮬레이션 상태(액터·틱·페이즈·rng). `BattleEngine`의 private 필드였던 것을 한 객체로.
- **`IBattleSystem`** — `Tick(context, dt)` 한 메서드. Movement / Engagement / Casting이 구현.
- **`BattleEngine`** — `IBattleSystem[]`을 순서대로 호출하는 **dispatcher** + `IResourceMutator`/`ISkillExecutor` 구현 + 이벤트 발행만 남는다.

폴더 구조 — 구현체는 `Engine/Systems/` 하위로 모은다(파일 11→13개 누적 시 인터페이스/엔진 코어와 시각적 분리). namespace는 부모 그대로 `MurimRunaway.Battle.Engine` 유지 (폴더는 조직용 — `feedback_enum_convention`과 동일 원칙):

```
Engine/
├── BattleEngine.cs          // dispatcher
├── BattleContext.cs
├── IBattleSystem.cs
├── IResourceMutator.cs
├── ISkillExecutor.cs
├── ITickService.cs / IRngService.cs / RngService.cs
└── Systems/
    ├── MovementSystem.cs
    ├── EngagementSystem.cs
    └── CastingSystem.cs
```

> [!note]
> **`Service` vs `System` 어휘 구분** ([Scripts/CLAUDE.md §2 명명 규칙](../../Assets/_Project/Scripts/CLAUDE.md))
>
> - `ITickService`/`IRngService` = **외부 게이트웨이**. Unity 전역(`Time`/`Random`)을 막은 어댑터. mock으로 갈아 끼워도 게임 규칙 불변.
> - `MovementSystem`/`EngagementSystem`/`CastingSystem` = **내부 시뮬 모듈**. `BattleContext`를 매 Tick 변경하는 규칙 자체. 갈아 끼우면 게임이 달라짐.
>
> 그래서 `IBattleSystem`은 이름에 "Tick"을 넣지 않는다 — `ITickService`의 "tick"(50Hz pulse)과 어휘 충돌이 생기기 때문.

> [!note]
> **시스템을 VContainer에 등록하지 않는 이유 (YAGNI)**
>
> 세 시스템은 `BattleEngine` 내부 구현이다.
> `CastingSystem`은 `ISkillExecutor`(= `BattleEngine`)를 필요로 하는데, `BattleEngine`이 다시 `IBattleSystem[]`을 생성자로 받으면 **DI 순환**이 된다.
>
> 시스템은 아직 독립 교체·독립 Mock 대상이 아니므로(엔진 통합 테스트로 검증됨) `BattleEngine`이 `Setup`에서 직접 `new` 한다.
> VContainer는 Phase 2 그대로 거친 그래프(Tick·Rng·Engine·Controller)만 조립한다.
>
> 인터페이스는 두 번째 구현/Mock이 필요한 시점에 추출 — Scripts/CLAUDE.md §1.

### `Scripts/Battle/Engine/BattleContext.cs`

```csharp
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>한 전투의 가변 시뮬레이션 상태. 모든 IBattleSystem이 공유·변경한다.</summary>
    public sealed class BattleContext
    {
        public PlayerActor Player;
        public EnemyActor[] Enemies;
        public long TickIndex;
        public float TimeSec;
        public BattlePhase Phase;
        public IRngService Rng;

        /// <summary>살아있는 적 중 position 최소. 동률은 배열 순서(= id 오름차순). 없으면 null.</summary>
        public EnemyActor GetNearestAliveEnemy()
        {
            EnemyActor nearest = null;
            var nearestPosition = float.MaxValue;
            for (var index = 0; index < Enemies.Length; index++)
            {
                var enemy = Enemies[index];
                if (enemy.State == ActorState.Dead)
                    continue;
                if (enemy.Position < nearestPosition)
                {
                    nearestPosition = enemy.Position;
                    nearest = enemy;
                }
            }
            return nearest;
        }
    }
}
```

> [!note]
> Phase 1의 `FindNearestAliveEnemyIndex`(인덱스 반환)는 `GetNearestAliveEnemy`(객체 반환)로 합친다.
> Engagement·Casting 두 시스템이 같은 "가장 가까운 적"을 쓰므로 `BattleContext`의 공용 헬퍼가 자연스러운 자리.
>
> 인덱스가 아니라 객체를 돌려도 되는 이유: 호출처가 더 이상 `_enemies[index]`로 되짚지 않는다.

### `Scripts/Battle/Engine/IBattleSystem.cs`

```csharp
namespace MurimRunaway.Battle.Engine
{
    /// <summary>매 Tick 한 단계를 진행시키는 시뮬레이션 시스템. 등록 순서 = 실행 순서.</summary>
    public interface IBattleSystem
    {
        void Tick(BattleContext context, float deltaTime);
    }
}
```

### `Scripts/Battle/Engine/Systems/MovementSystem.cs`

Phase 1 `TickMovement`을 그대로 추출.

```csharp
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>Player가 Running이면 진군 속도만큼 position 전진.</summary>
    public sealed class MovementSystem : IBattleSystem
    {
        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;
            if (player.State != ActorState.Running)
                return;

            player.Position += player.MoveSpeed * deltaTime;
        }
    }
}
```

### `Scripts/Battle/Engine/Systems/EngagementSystem.cs`

Phase 1 `TickEngagementCheck` 추출 + **전이 대상 변경**: 가장 가까운 적의 교전 거리에 닿으면 Phase 1은 `Resolve`(즉시 승리)를 보냈지만, Phase 3은 SSOT [[BATTLE_DESIGN]] §3 Phase 3 State machine에 따라 **`Engage`**(교전 시작)로 전이한다.
데미지·승패(Resolve)는 Phase 4로 이동.

```csharp
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>Player가 가장 가까운 적의 교전 거리에 닿으면 Idle 정지 + 전투 페이즈 Engage 전이.</summary>
    public sealed class EngagementSystem : IBattleSystem
    {
        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;
            if (player.State != ActorState.Running)
                return;

            var nearest = context.GetNearestAliveEnemy();
            if (nearest == null)
                return;

            var distance = nearest.Position - player.Position;
            if (distance > player.EngageDistance)
                return;

            // 큰 deltaTime이 교전 거리 안쪽으로 밀어넣는 것 방지
            player.Position = nearest.Position - player.EngageDistance;
            player.State = ActorState.Idle;
            context.Phase = BattlePhase.Engage;
        }
    }
}
```

> [!warning]
> **⚠ Phase 1 테스트 회귀**
>
> Phase 1 `BattleEngineApproachTests`/`BattleEngineNearestEnemyTests`는 적에 닿으면 `last.Phase == BattlePhase.Resolve`를 단언한다.
> 본 변경으로 그 시점 페이즈가 `Engage`가 되어 **두 테스트가 깨진다**.
>
> §5.0에서 두 테스트의 기대값을 `BattlePhase.Engage`로 갱신한다(가이드↔산출물 양방향 확인 — `feedback_guide_code_rigor`).
> `BattleEngineDeterminismTests`는 두 런의 Phase **동일성**만 보므로 영향 없음(둘 다 결정론적으로 Engage).

### `Scripts/Battle/Engine/ISkillExecutor.cs`

`IResourceMutator`(Phase 2)와 같은 결의 **엔진 레이어 인터페이스** — `BattleEngine`이 구현하고 `CastingSystem`(§3.3)이 호출한다.
정의를 §1에 둔 이유: `CastingSystem`이 §3.3에서 이 인터페이스를 먼저 참조하기 때문이다.

```csharp
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>무공 시전 실행의 단일 진입점. 비용·쿨·기세·이벤트를 한 번에 처리.</summary>
    public interface ISkillExecutor
    {
        /// <summary>slotIndex 무공을 시전. 내공 부족 시 false + 아무 변경 없음.</summary>
        bool TryCast(Actor caster, int slotIndex, Actor target);
    }
}
```

호출처·결정 트리와의 분업은 [§3.3](#33-castingsystem--자동-시전-결정-트리)에서.

### 1.2 §1 완료 조건

여기서는 `BattleEngine` 최종 재작성까지 가지 않는다.
아직 `PlayerActor.Momentum`, `PlayerActor.Skills`, `SkillData`, `CastingSystem`이 없기 때문이다.

§1의 완료 조건:
- `BattleContext`/`IBattleSystem` 생성.
- `MovementSystem`/`EngagementSystem` 생성.
- `ISkillExecutor` 생성.
- 기존 `BattleEngine`은 컴파일 가능한 상태로 유지.

`BattleEngine`의 최종 dispatcher 재작성은 §2 Domain 확장과 §3 `CastingSystem` 작성 뒤, [§3.4](#34-battleenginecs--dispatcher-최종-재작성)에서 한다.

---

## 2. Domain 확장 — `SkillData` + enum 2종 + `PlayerActor`

### 2.1 `SkillKind` enum

SSOT 표기와 코드 이름은 `SkillKind`로 맞춘다.
도메인 의미 = "무공의 종류" → `SkillKind`.

`Enums/` 하위 폴더, namespace는 부모(`Domain`)와 동일, 멤버마다 `///` (Scripts/CLAUDE.md 명명 규칙).

### `Scripts/Battle/Domain/Enums/SkillKind.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공의 종류.</summary>
    public enum SkillKind
    {
        /// <summary>아직 종류가 정해지지 않음.</summary>
        None = 0,
        /// <summary>초식 — 공격이나 방어에 직접 쓰는 기술.</summary>
        Technique,
        /// <summary>심법 — 몸 상태를 강화하는 무공.</summary>
        Focus,
        /// <summary>경공 — 이동/회피 무공.</summary>
        Step,
        /// <summary>오의 — 핫키 발동, 기세 소비.</summary>
        Ultimate,
    }
}
```

### 2.2 `SkillRange` enum

타겟 범위. "Range"는 도메인 어휘이므로 접미사 규칙에 안 걸린다.

### `Scripts/Battle/Domain/Enums/SkillRange.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공이 닿는 타겟 범위.</summary>
    public enum SkillRange
    {
        /// <summary>아직 타겟 범위가 정해지지 않음.</summary>
        None = 0,
        /// <summary>가장 앞의 살아있는 적 1명.</summary>
        Single,
        /// <summary>가장 앞의 살아있는 적과 그 다음 살아있는 적까지 최대 2명.</summary>
        NearbyPair,
        /// <summary>화면 안의 모든 살아있는 적.</summary>
        All,
    }
}
```

### 2.3 `SkillData` (ScriptableObject)

무공 한 종의 정적 데이터. `EnemyData`와 같은 SO 패턴([데이터 컨벤션](../../Assets/_Project/Scripts/CLAUDE.md): Id snake_case, 표시 문자열 금지 → `NameKey`/`DescKey`).

> [!note]
> **`effects` 필드는 Phase 3에 안 만든다 (YAGNI)**
>
> SSOT 표는 `effects: SkillEffect[]`를 적었지만 "Phase 4에서 정의"라 단서가 붙어 있다.
> Scripts/CLAUDE.md §1: *"인터페이스가 참조하므로 식으로 빈 스텁 타입을 미리 정의하지 않는다."*
>
> `SkillEffect` 빈 타입을 지금 만들면 데드 코드.
> Phase 4 데미지 도입 시 추가하고, 그때 `TryCast`의 효과 적용 한 줄도 같이 들어온다.

### `Scripts/Battle/Domain/SkillData.cs`

```csharp
using UnityEngine;

namespace MurimRunaway.Battle.Domain
{
    [CreateAssetMenu(fileName = "SkillData", menuName = "MurimRunaway/Battle/SkillData")]
    public sealed class SkillData : ScriptableObject
    {
        [Header("무공 정의 — 한 종류의 무공 SO")]

        [Tooltip("snake_case, 같은 종류 안에서 unique")]
        public string Id;

        [Tooltip("표시 이름 문구 키(\"skill.<id>.name\").")]
        public string NameKey;

        [Tooltip("설명 문구 키(\"skill.<id>.desc\").")]
        public string DescKey;

        [Tooltip("무공 종류")]
        public SkillKind Kind = SkillKind.None;

        [Tooltip("시전 비용 (내공)")]
        public int ManaCost = 5;

        [Tooltip("쿨타임 (초)")]
        public float CooldownSec = 1.5f;

        [Tooltip("사거리 등급 — 해당 거리 이하에서 자동 시전")]
        public SkillRange PreferredRange = SkillRange.None;

        [Tooltip("시전 성공 시 획득하는 기세")]
        public int MomentumGainOnCast = 1;
    }
}
```

### 2.4 `SkillSlot` — 병렬 배열 번들

SSOT 표기는 `skillSlots: SkillData[]` + `skillCooldowns: float[]`(같은 인덱스).
병렬 배열 + "같은 인덱스" 주석은 한쪽만 정렬·필터되는 순간 침묵 버그 ([§0.1 C-1](#01-ssot-정합-노트), `feedback_no_parallel_arrays`).

한 컨테이너로 묶어 컴파일러가 짝을 보장하게 한다.

### `Scripts/Battle/Domain/SkillSlot.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>학습한 무공 한 슬롯 = 무공 데이터 + 남은 쿨다운. 빈 슬롯은 Data == null.</summary>
    public sealed class SkillSlot
    {
        public SkillData Data;
        public float CooldownRemaining;
    }
}
```

### 2.5 `PlayerActor` — 슬롯 + 기세 추가

기세는 v0.4.2 스코프 컷으로 Phase 2가 아니라 본 Phase에서 도입(SSOT [[BATTLE_DESIGN]] §3 Phase 3). `Mana`/`MaxMana`(Phase 2)는 그대로.

### `Scripts/Battle/Domain/PlayerActor.cs` — 확장

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>플레이어 액터. 이동 + 자원(HP는 base Actor / 내공·기세) + 무공 슬롯 보유.</summary>
    public sealed class PlayerActor : Actor
    {
        public float MoveSpeed;

        // 내공
        public int Mana;
        public int MaxMana;

        // 기세. 변경은 IResourceMutator만 통과.
        public int Momentum;
        public int MaxMomentum;

        // 학습한 무공 (시작 3, 최대 6). 슬롯 순서 = 자동 시전 우선순위.
        public SkillSlot[] Skills;
    }
}
```

### 2.6 `PlayerStartData` — 시작 슬롯 + 기세 시작값

### `Scripts/Battle/Domain/PlayerStartData.cs` — 확장

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>한 전투에 들어갈 때의 Player 시작값.</summary>
    public sealed class PlayerStartData
    {
        public int MaxHp;
        public int MaxMana;
        public int StartingMana;
        public int MaxMomentum;
        public float MoveSpeed;
        public float EngageDistance;

        public SkillData[] StartingSkills;
    }
}
```

> [!note]
> `PlayerStartData`에는 밸런스 기본값을 넣지 않는다.
> 나중에 Config 파일이나 PlayerData 빌더가 만든 값을 그대로 담는 입력 타입이기 때문이다.
> 지금은 씬 SerializeField와 테스트 헬퍼가 값을 모두 채워서 넘긴다.
>
> 기세 시작값은 SSOT 디폴트 `momentum=0`이므로 `StartingMomentum` 필드를 두지 않고 `Setup`에서 `Momentum = 0` 고정([§3.4](#34-battleenginecs--dispatcher-최종-재작성)).
>
> 0이 아닌 시작 기세가 의미를 갖는 시점(메타 강화, Phase 11+)에 필드로 승격 — 안 일어날 시나리오에 필드 미리 안 박음(YAGNI).

### 2.7 `ActorView` — 기세 + 슬롯 쿨다운 (Player 한정) + **분리 트리거 판단**

View가 기세 게이지와 슬롯 쿨 게이지를 그리려면 스냅샷에 노출해야 한다. 슬롯 쿨다운은 작은 읽기 전용 사본 배열로.

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>슬롯 한 칸의 읽기 전용 사본. 빈 슬롯은 Filled == false.</summary>
    public readonly struct SkillSlotView
    {
        public readonly bool Filled;
        public readonly string SkillId;
        public readonly float CooldownRemaining;
        public readonly float CooldownSec;

        public SkillSlotView(SkillSlot slot)
        {
            Filled = slot != null && slot.Data != null;
            SkillId = Filled ? slot.Data.Id : null;
            CooldownRemaining = Filled ? slot.CooldownRemaining : 0f;
            CooldownSec = Filled ? slot.Data.CooldownSec : 0f;
        }
    }
}
```

### `Scripts/Battle/Domain/ActorView.cs` — 확장

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
        public readonly int Mana;
        public readonly int MaxMana;
        public readonly int Momentum;
        public readonly int MaxMomentum;
        public readonly SkillSlotView[] Skills;
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

            if (actor is PlayerActor player)
            {
                Mana = player.Mana;
                MaxMana = player.MaxMana;
                Momentum = player.Momentum;
                MaxMomentum = player.MaxMomentum;
                Skills = ToSlotViews(player.Skills);
            }
            else
            {
                Mana = 0;
                MaxMana = 0;
                Momentum = 0;
                MaxMomentum = 0;
                Skills = System.Array.Empty<SkillSlotView>();
            }
        }

        private static SkillSlotView[] ToSlotViews(SkillSlot[] slots)
        {
            var views = new SkillSlotView[slots.Length];
            for (var index = 0; index < slots.Length; index++)
                views[index] = new SkillSlotView(slots[index]);
            return views;
        }
    }
}
```

> [!warning]-
> **ActorView 분리 트리거 — 지금은 분리 안 한다 (명시적 보류)**
> 메모리 `project_actorview_split`: *"진영별 전용 필드 2개+ 또는 IsPlayer 분기 3곳+ 누적 시 IActorView로 묶고 PlayerView/EnemyView 분리."*
>
> Player 한정 필드가 Phase 2의 2개(`Mana`/`MaxMana`)에서 본 Phase에 6개(+`Momentum`/`MaxMomentum`/`Skills`)로 늘어 **필드 수 절반은 트리거 충족**이다.
> 그럼에도 분리를 보류하는 이유는 [[Completed_Phase_2_Learned]] §5의 판단을 그대로 잇는다:
> 1. 지금 분리하면 `EnemyView`는 base와 동일(Enemy 전용 필드 0개) — 분리 이득이 거의 0.
> 2. `IsPlayer` 분기는 여전히 2곳(`ActorView` 생성자, `BattleSceneController.HandleSnapshot`)으로 "3곳+" 미충족. CastingSystem은 `ActorView`가 아니라 `context.Player`를 직접 다뤄 분기를 안 늘린다.
> 3. **재평가 지점 = Phase 5**: 보스전 매트릭스에서 Enemy 전용 필드(`IsBoss`/`PatternType`)가 들어와 양쪽 모두 전용 필드를 가질 때 분리 이득이 처음 양수가 된다.
>    그 전 분리는 공통 필드 추가 시 재병합 비용만 키운다.
> 즉 "필드 2개+"는 숫자만 보면 충족이나 **분리 이득이 음수**라 보류가 합리적. Phase 5 진입 시 이 노트를 다시 연다.

### 2.8 `SkillCastEvent` — 시전 알림 데이터

### `Scripts/Battle/Domain/SkillCastEvent.cs`

시전 이벤트 데이터.
`Action<int, int, string, int>`처럼 순서로 의미를 외우게 하지 않고, 필드 이름으로 읽히게 둔다.
`BattleSnapshot`/`ActorView`처럼 Engine 밖으로 공개되는 읽기 전용 전투 결과 데이터이므로 `Domain`에 둔다.
`Data`는 `BattleStartData`/`SkillData`처럼 입력값·정적 정의에 붙이고, 시전 성공처럼 플레이 중 한 번 발생한 알림은 `Event`로 부른다.

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>무공 시전이 성공했을 때 View와 테스트에 알리는 정보.</summary>
    public readonly struct SkillCastEvent
    {
        public readonly int CasterId;
        public readonly int SlotIndex;
        public readonly string SkillId;
        public readonly int TargetId;

        public SkillCastEvent(int casterId, int slotIndex, string skillId, int targetId)
        {
            CasterId = casterId;
            SlotIndex = slotIndex;
            SkillId = skillId;
            TargetId = targetId;
        }
    }
}
```

---

## 3. Engine 확장 — `IResourceMutator`(기세) + `CastingSystem`

### 3.1 `IResourceMutator` — 기세 메서드 추가

Phase 2 `SpendMana`/`GainMana`와 같은 규칙을 따른다.
소비는 부족하면 거절하고 값을 그대로 둔다.
획득은 최대값을 넘지 않게 자른다.
구현은 [§3.4 `BattleEngine`](#34-battleenginecs--dispatcher-최종-재작성)에서 한다.

### `Scripts/Battle/Engine/IResourceMutator.cs` — 확장

```csharp
namespace MurimRunaway.Battle.Engine
{
    /// <summary>자원 변경의 단일 진입점. 모든 자원 변경은 이 인터페이스만 통과한다.</summary>
    public interface IResourceMutator
    {
        /// <summary>amount만큼 내공 소비. 부족하면 false를 반환하고 값을 그대로 둔다.</summary>
        bool SpendMana(int amount);

        /// <summary>maxMana를 넘지 않게 채운다.</summary>
        void GainMana(int amount);

        /// <summary>amount만큼 기세 소비. 부족하면 false를 반환하고 값을 그대로 둔다.</summary>
        bool SpendMomentum(int amount);

        /// <summary>maxMomentum을 넘지 않게 채운다.</summary>
        void GainMomentum(int amount);
    }
}
```

> [!note]
> `SpendMomentum`은 Phase 3엔 호출처가 없다(오의 발동 = Phase 6).
>
> 그럼에도 지금 노출하는 이유: SSOT가 본 Phase 산출물로 명시 + `GainMomentum`과 짝(자원 한 종의 소비/획득은 한 인터페이스에서 같이 보이는 게 단일 진입점 규칙).
> "부족하면 실패하고 값은 그대로"라는 규칙을 호출처 없이도 테스트로 고정한다([§5.3](#53-resourcemutatortests--기세-추가분)).

### 3.2 `ISkillExecutor` — 호출 분업

인터페이스 정의는 [§1의 `ISkillExecutor.cs`](#scriptsbattleengineiskillexecutorcs) (엔진 레이어 인터페이스라 §1로 앞당김).

SSOT [[BATTLE_DESIGN]] §3 Phase 3 Interfaces가 명시한 분업:

- `CastingSystem`(결정 트리, §3.3) — **"어느 슬롯을 쏠지" 결정만**.
- `ISkillExecutor`(= `BattleEngine`) — **실제 비용 차감·쿨 설정·기세 획득·이벤트 발행**.

Phase 2 `IResourceMutator`가 자원 변경 단일 진입점이듯, 시전 실행의 단일 진입점.

### 3.3 `CastingSystem` — 자동 시전 결정 트리

SSOT [[BATTLE_DESIGN]] §3 Phase 3 Behaviors 그대로. 매 Tick: ① 쿨다운 감소 → ② 타겟 선택 → ③ 슬롯 0..N-1 순회하며 skip 조건 검사 → ④ 통과한 **첫** 슬롯을 시전하고 break(한 Tick 한 시전).

Phase 3은 아직 데미지가 없으므로 `SkillRange`별 다중 타겟 적용은 하지 않는다.
`SkillRange`는 Phase 4 데미지 도입 때 타겟 선택 범위로 사용한다.

### `Scripts/Battle/Engine/Systems/CastingSystem.cs`

```csharp
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>자동 시전 결정 트리. 슬롯 순서 = 우선순위. 한 Tick 한 시전.</summary>
    public sealed class CastingSystem : IBattleSystem
    {
        private readonly ISkillExecutor _executor;

        public CastingSystem(ISkillExecutor executor)
        {
            _executor = executor;
        }

        public void Tick(BattleContext context, float deltaTime)
        {
            var player = context.Player;

            // ① 쿨다운 감소 — 페이즈와 무관하게 항상 흐른다.
            foreach (var slot in player.Skills)
            {
                if (slot.Data == null)
                    continue;
                slot.CooldownRemaining = Math.Max(0f, slot.CooldownRemaining - deltaTime);
            }

            // 교전 페이즈에서만 시전 (SSOT: Approach 동안 결정 트리 비활성)
            if (context.Phase != BattlePhase.Engage)
                return;

            // ② 타겟 선택
            var target = context.GetNearestAliveEnemy();
            if (target == null)
                return;

            // ③ 슬롯 순회 — 통과한 첫 슬롯에서 break
            for (var slotIndex = 0; slotIndex < player.Skills.Length; slotIndex++)
            {
                var slot = player.Skills[slotIndex];
                var skill = slot.Data;

                if (skill == null)
                    continue;
                if (slot.CooldownRemaining > 0f)
                    continue;
                if (player.Mana < skill.ManaCost)
                    continue;

                // ④ 통과 — 시전하고 한 Tick 종료 (두 무공 동시 발동 금지)
                _executor.TryCast(player, slotIndex, target);
                break;
            }
        }
    }
}
```

> [!note]
> **`System.Math.Max` (Mathf 아님)**
>
> Engine asmdef는 UnityEngine 의존을 끊는다 — `Mathf`는 금지, `System.Math.Max(float, float)`은 .NET BCL이라 안전(Phase 2 `GainMana`의 `Math.Min`과 동일 판단, `Completed_Phase_2_Guide.md` §2.2).
> 파일 상단 `using System;` 한 줄 필요(`Math` 한정자 생략 시).
>
> **심법 중복 방지 skip은 Phase 3에 안 넣는다**
>
> SSOT 결정 트리에 *"kind == Focus이고 효과가 이미 활성이면 skip"* 단계가 있으나, "효과 활성" 상태는 Phase 4 효과 시스템이 생겨야 판정 가능하다.
> Phase 3엔 효과가 없으므로(데미지·버프 = Phase 4) 그 조건은 판정 대상이 없다 → 지금 넣으면 항상 false인 죽은 분기.
>
> Phase 4에서 효과 상태와 함께 추가.
> 본 Phase는 Technique 위주로 검증하고 Focus는 "거리/쿨/내공만 통과하면 시전"까지만.
>
> **결정성(I-3.4)은 RNG 없이 구조적으로 성립**
>
> 결정 트리에 난수가 없다.
> 같은 (seed, 슬롯 구성, 적 배치) → 같은 `SkillCastPublished` 시퀀스가 틱 단위로 재현된다.
>
> `IRngService`는 `BattleContext.Rng`로 들고만 있고 본 Phase 트리에서 호출하지 않는다(회피 RNG = Phase 9).
> [§5.2](#52-castingsystemtests--결정-트리--결정성)가 이를 골든 비교로 고정.

### 3.4 `BattleEngine.cs` — dispatcher 최종 재작성

이 시점에는 `PlayerActor.Momentum`, `PlayerActor.Skills`, `SkillData`, `SkillSlot`, `CastingSystem`이 모두 있다.
이제 `BattleEngine`을 최종 형태로 바꾼다.

`_player`/`_enemies`/`_tickIndex`/… 개별 필드는 `BattleContext` 한 객체로 묶는다.
`HandleTick`은 시스템 순회 + 스냅샷 발행만 맡는다.
`IResourceMutator`(Phase 2)는 그대로 두고, `ISkillExecutor`를 추가한다.

```csharp
using System;
using System.Linq;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 진행 본체. 등록된 시스템을 순서대로 실행하고 결과를 알린다.</summary>
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
                Skills = data.Player.StartingSkills
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

            var skillCast = new SkillCastEvent(caster.Id, slotIndex, skill.Id, target.Id);
            SkillCastPublished?.Invoke(skillCast);
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
```

자원·쿨처럼 계속 변하는 상태는 스냅샷, 시전처럼 한 번 발생한 알림은 이벤트로 보낸다.
Phase 2 [[Completed_Phase_2_Learned]] §4의 "다음 스냅샷 vs 즉시 이벤트" 구분과 같은 결이다.

> [!note]
> **순환 의존 회피**
>
> `CastingSystem(this)`에서 `this`는 `ISkillExecutor`로 좁혀 넘긴다.
> Phase 2의 `BattleEngine : IResourceMutator`와 같은 방식이다.
>
> `BattleEngine`은 `IBattleSystem[]`을 생성자가 아니라 `Setup`에서 직접 만든다.
> 그래서 VContainer가 풀어야 할 순환이 없다.

### 3.5 VContainer 등록 갱신

`BattleLifetimeScope`에서 `BattleEngine`을 `ISkillExecutor`로도 노출. **한 줄 추가**가 전부 — 시스템은 Engine 내부라 등록 대상이 아니다([§1.1](#11-왜-지금인가)).

### `Scripts/Battle/View/BattleLifetimeScope.cs` — 한 줄 변경

```csharp
// 기존 등록에 ISkillExecutor 얼굴 추가
builder.Register<BattleEngine>(Lifetime.Singleton)
       .AsSelf()
       .As<IResourceMutator>()
       .As<ISkillExecutor>();
```

> [!note]
> `.As<>()`를 체인하면 암묵적 self 등록이 취소되므로 `.AsSelf()`가 필수로 남아야 한다([[Completed_Phase_2_Learned]] §6.1의 함정 노트 그대로).
>
> `ISkillExecutor`는 현재 `BattleEngine`이 `CastingSystem`에 `this`로 직접 주입하므로 컨테이너가 resolve할 필요는 없지만, 다른 시스템(Phase 6 오의)이 컨테이너 경유로 받을 때를 위해 얼굴을 노출해 둔다.

---

## 4. View — 슬롯 쿨 게이지 + 시전 이름 표시

진행 순서: **Unity UI 추가 → 코드 수정 → Inspector 연결 → Play 확인** (Phase 2와 동일 절차).

### 4.1 슬롯 UI 만들기

`Battle.unity`의 Canvas 아래, Phase 2 `ResourcePanel` 밑에 추가. 최종 계층:

```
ResourcePanel (기존 — HpBar / ManaBar)
 └ MomentumBar  (ManaBar Prefab 인스턴스 복제, 라벨 "MO" + Fill 노랑)
SkillPanel (RectTransform + Horizontal Layout Group)
 ├ SkillSlot0 (RectTransform)
 │   ├ Icon (Image)            placeholder 단색
 │   ├ CooldownOverlay (Image) 반투명 검정 — Filled/Radial 360 원형 쿨다운
 │   └ NameFlash (TMP_Text)    시전 시 1초 표시 후 숨김
 ├ SkillSlot1 ( "" 복제 )
 └ SkillSlot2 ( "" 복제 )
```

- **MomentumBar**: Phase 2 `ManaBar`를 복제(라벨 `MP→MO`, Fill 색 파랑→노랑). Phase 2 [[Completed_Phase_2_Guide]] §3의 ResourceBar/앵커 규칙을 그대로 재사용 — 새 개념 없음.
- **SkillSlot**: 시작 무공 3개라 슬롯 3칸. `CooldownOverlay`는 슬롯 전체를 덮는 Image로 두고, 코드는 `fillAmount`로 남은 쿨다운 비율을 표시한다.

#### 단계별 절차

1. `ResourcePanel` 아래 `ManaBar`를 Ctrl+D → 이름 `MomentumBar`, `Label` 텍스트 `MO`, `Fill` Color 노랑. (Prefab 인스턴스면 슬롯 자동 재해석 — Phase 2 §3.4 함정 노트 참조.)
2. `Canvas` 우클릭 → UI → Empty, 이름 `SkillPanel`. 앵커 bottom-center. `Add Component → Horizontal Layout Group` (Spacing 8).
3. `SkillPanel` 아래 UI → Empty `SkillSlot0`. Width/Height 64.
4. `SkillSlot0` 아래: Image `Icon`(단색 placeholder), Image `CooldownOverlay`(검정 alpha 120; Source Image에 임의의 흰색 Sprite 지정, 앵커 stretch-stretch, Type=Filled, Fill Method=Radial 360, Fill Origin=Top), Text-TMP `NameFlash`(빈 문자열).
5. `SkillSlot0`를 `Assets/_Project/Prefabs/Battle/`로 드래그 → Prefab. `SkillSlot1`/`SkillSlot2`는 Prefab 인스턴스로 2개 더 배치.

> [!note]
> Phase 2 placeholder 수준. 정교한 슬롯 UI(아이콘·테두리·등급)는 Phase 14. Phase 3은 "쿨이 도는 게 보이고, 시전되면 이름이 1초 뜬다"까지.
>
> Unity `Image`의 `Type=Filled` 항목은 `Source Image`가 `None`이면 보이지 않는다.
> `CooldownOverlay`에 우선 아무 흰색 Sprite나 넣고 Color를 검정 반투명으로 바꾼다.

### 4.2 `BattleSceneController` — 슬롯 쿨 게이지 + 시전 이름

추가 슬롯 + `HandleSnapshot` 분기(원형 쿨다운) + `SkillCastPublished` 구독(이름 1초 표시). 시전 이름은 코루틴으로 1초 후 숨김.

`HandleSnapshot`은 새 표시 목표만 저장한다.
마커 이동과 쿨타임 게이지는 `Update()`에서 마지막 스냅샷 기준으로 매 프레임 그린다.
Engine Tick은 20Hz로 유지하고, View만 자기 프레임에 보간 표시한다.

```csharp
public sealed class BattleSceneController : MonoBehaviour
{
    // ... 기존 [SerializeField] 슬롯 (counter/gauge/markers/hpBar/manaBar) 유지 ...

    [SerializeField] private ResourceBar _momentumBar;
    [SerializeField] private float _snapshotIntervalSeconds = 0.05f;
    [SerializeField] private RectTransform[] _skillCooldownOverlays; // 슬롯 i의 CooldownOverlay
    [SerializeField] private TMP_Text[] _skillNameFlashes;           // 슬롯 i의 NameFlash

    private Image[] _skillCooldownOverlayImages;
    private Coroutine[] _skillNameFlashRoutines;
    private SkillSlotView[] _playerSkillSnapshot;
    private float[] _markerStartPositions;
    private float[] _markerTargetPositions;
    private float _lastSnapshotTime;
    private bool _hasSnapshot;

    [VContainer.Inject]
    public void Construct(BattleEngine engine)
    {
        _engine = engine;
    }

    private void Start()
    {
        _engine.SnapshotPublished += HandleSnapshot;
        _engine.SkillCastPublished += HandleSkillCast;
        _skillNameFlashRoutines = new Coroutine[_skillNameFlashes.Length];
        // ... CooldownOverlay Image 캐싱 + Filled/Radial 설정 ...
        // ... marker 보간용 배열 초기화 ...
        // ... 기존 Start() 본문 유지 (worldMax/마커/Setup/Start) ...
    }

    private void OnDestroy()
    {
        if (_engine == null)
            return;
        _engine.SnapshotPublished -= HandleSnapshot;
        _engine.SkillCastPublished -= HandleSkillCast;
        _engine.Dispose();
    }

    private void HandleSnapshot(BattleSnapshot snapshot)
    {
        _counterText.text = $"Tick: {snapshot.TickIndex}  Phase: {snapshot.Phase}";

        var isFirstSnapshot = !_hasSnapshot;
        var gaugeWidth = _gauge.rect.width;
        foreach (var actor in snapshot.Actors)
        {
            var markerIndex = actor.Id;
            var marker = GetMarker(actor);
            var markerTargetPosition = actor.Position / _worldMax * gaugeWidth;
            _markerStartPositions[markerIndex] = isFirstSnapshot
                ? markerTargetPosition
                : marker.anchoredPosition.x;
            _markerTargetPositions[markerIndex] = markerTargetPosition;

            if (!actor.IsPlayer)
                continue;

            _hpBar.SetValue(actor.Hp, actor.MaxHp);
            _manaBar.SetValue(actor.Mana, actor.MaxMana);
            _momentumBar.SetValue(actor.Momentum, actor.MaxMomentum);
            _playerSkillSnapshot = actor.Skills;
        }

        _lastSnapshotTime = Time.time;
        _hasSnapshot = true;
        UpdateMarkerPositions();
        UpdateSkillCooldowns();
    }

    private void Update()
    {
        if (!_hasSnapshot)
            return;

        UpdateMarkerPositions();
        UpdateSkillCooldowns();
    }

    private RectTransform GetMarker(ActorView actor)
    {
        return actor.IsPlayer ? _playerMarker : _enemyMarkers[actor.Id - 1];
    }

    private void UpdateMarkerPositions()
    {
        var snapshotRatio = Mathf.Clamp01((Time.time - _lastSnapshotTime) / _snapshotIntervalSeconds);
        _playerMarker.anchoredPosition = GetMarkerPosition(0, snapshotRatio);

        for (var enemyIndex = 0; enemyIndex < _enemyMarkers.Length; enemyIndex++)
        {
            var markerIndex = enemyIndex + 1;
            _enemyMarkers[enemyIndex].anchoredPosition = GetMarkerPosition(markerIndex, snapshotRatio);
        }
    }

    private Vector2 GetMarkerPosition(int markerIndex, float snapshotRatio)
    {
        var markerPosition = Mathf.Lerp(
            _markerStartPositions[markerIndex],
            _markerTargetPositions[markerIndex],
            snapshotRatio);

        return new Vector2(markerPosition, 0f);
    }

    private void UpdateSkillCooldowns()
    {
        if (_playerSkillSnapshot == null)
            return;

        var elapsedSinceSnapshot = Time.time - _lastSnapshotTime;
        for (var slotIndex = 0; slotIndex < _playerSkillSnapshot.Length; slotIndex++)
        {
            var slotView = _playerSkillSnapshot[slotIndex];
            var displayedCooldownRemaining = Mathf.Max(
                0f,
                slotView.CooldownRemaining - elapsedSinceSnapshot);
            var cooldownRatio = slotView.CooldownSec <= 0f
                ? 0f
                : displayedCooldownRemaining / slotView.CooldownSec;
            var overlayImage = _skillCooldownOverlayImages[slotIndex];
            overlayImage.fillAmount = cooldownRatio;
            overlayImage.enabled = cooldownRatio > 0f;
        }
    }

    private void HandleSkillCast(SkillCastEvent skillCast)
    {
        var slotIndex = skillCast.SlotIndex;
        if (_skillNameFlashRoutines[slotIndex] != null)
            StopCoroutine(_skillNameFlashRoutines[slotIndex]);

        _skillNameFlashRoutines[slotIndex] = StartCoroutine(FlashSkillName(slotIndex, skillCast.SkillId));
    }

    private System.Collections.IEnumerator FlashSkillName(int slotIndex, string skillId)
    {
        _skillNameFlashes[slotIndex].text = skillId;
        yield return new WaitForSeconds(1f);
        _skillNameFlashes[slotIndex].text = string.Empty;
        _skillNameFlashRoutines[slotIndex] = null;
    }
}
```

> [!note]
> **쿨 오버레이 크기 계산은 자기 부모 높이 기준**으로 둬야 정확하다(Phase 2 `ResourceBar`가 `_track.rect.width`를 읽은 것과 같은 원리 — [[Completed_Phase_2_Guide]] §3.2).
> 위 코드는 가이드 단순화 표기다.
>
> 슬롯 3칸 각각 쿨이 따로 도므로 `ResourceBar` 한 종으로는 부족 → 오버레이 RectTransform을 직접 만진다.
> 정식 슬롯 컴포넌트화는 Phase 14.
>
> **이름 표시는 이벤트의 `SlotIndex`를 따른다**
>
> 시전 슬롯은 Engine이 이미 알고 있으므로 `SkillCastEvent`에 같이 싣는다.
> View가 `skillId`로 슬롯을 역추적하지 않는다.
> 코루틴도 전체 정지가 아니라 해당 슬롯의 플래시만 갱신한다.

### 4.3 Inspector 연결

| 슬롯 | 연결할 대상 |
|------|------------|
| `_momentumBar` | `ResourcePanel/MomentumBar`의 `ResourceBar` |
| `_skillCooldownOverlays` | Size=3, 각 `SkillSlotN/CooldownOverlay` (RectTransform, Image 포함) |
| `_skillNameFlashes` | Size=3, 각 `SkillSlotN/NameFlash` (TMP_Text) |

`PlayerStartData.StartingSkills`는 씬에서 주입한다 — `BattleSceneController`에 `[SerializeField] private SkillData[] _startingSkills;`를 더하고 `Setup`의 `Player = new PlayerStartData { ..., StartingSkills = _startingSkills }`로 넘긴다.

SkillData SO 3개(§4.4)를 Inspector 배열에 드래그.

### 4.4 SkillData 자산 생성 (PlayMode 확인용 최소 3개)

`Assets/_Project/Data/Skills/` 폴더에 우클릭 → Create → MurimRunaway → Battle → SkillData 3개:

| 파일 | Id | Kind | ManaCost | CooldownSec | PreferredRange | 의도 |
|------|----|----------|----------|-------------|----------------|------|
| `tae_in_jang` | `tae_in_jang` | Technique | 5 | 1.5 | Single | 선두 적 1명 |
| `cheonha_36_geom` | `cheonha_36_geom` | Technique | 12 | 3.0 | NearbyPair | 선두 적과 다음 적까지 |
| `simbeop_unki` | `simbeop_unki` | Focus | 8 | 5.0 | None | 심법(Phase 3에서는 시전 확인 대상 아님) |

> [!note]
> 심법은 적과의 거리대보다 캐릭터 버프에 가깝다.
> 방어도, 공격력, 내공 상승 같은 효과를 다룰 때 `PreferredRange = None` 처리와 자동 시전 조건을 다시 본다.

`NameKey`는 `skill.<id>.name` 규약으로 채우되 Phase 3엔 번역 문구가 아직 연결되지 않아 화면엔 `Id`가 뜬다(§4.2).

### 4.5 Play 확인

Play 누르면:
- 파란 마커가 진군 → 적 근처 정지, 카운터 Phase가 `Approach` → **`Engage`**(Phase 1/2의 `Resolve` 아님).
- Engage 진입 후: 내공이 차 있는 동안 슬롯 쿨 오버레이가 주기적으로 찼다 줄고, 시전 순간 무공 `Id`가 1초 표시.
- 내공이 바닥나면 시전이 멈췄다가 (회복원이 없으므로) 그대로 정지 — Phase 3엔 내공 회복 룰이 없다(GainMana 호출처 없음). **정상**.
- HP/내공/기세 게이지 표시. 데미지가 없어 HP는 안 변함 — 정상(Phase 4).

전투가 끝나지 않고 Engage에 머무는 것도 정상 — Resolve(승패)는 Phase 4.

---

## 5. EditMode 테스트

### 5.0 Phase 1 테스트 회귀 수정 (먼저)

§1 `EngagementSystem`이 적 도달 시 `Resolve` → `Engage`로 바꿨다. 두 Phase 1 테스트의 기대값을 갱신한다(나머지 단언은 그대로).

### `Scripts/.../Tests/Battle/BattleEngineApproachTests.cs` — 기대값 변경

```csharp
// 변경 전: Assert.AreEqual(BattlePhase.Resolve, last.Phase);
Assert.AreEqual(BattlePhase.Engage, last.Phase);
Assert.AreEqual(ActorState.Idle, last.Actors[0].State);   // Player (그대로)
Assert.AreEqual(80f, last.Actors[0].Position, 1e-4f);     // 그대로
```

`BattleEngineNearestEnemyTests`도 동일하게 `Resolve` 단언 한 줄을 `Engage`로. `BattleEngineDeterminismTests`는 두 런 **동일성**만 비교하므로 수정 불필요(둘 다 Engage로 동일하게 수렴).

> [!note]
> 이건 "테스트를 통과시키려 기대값을 무르게 바꾸는" 게 아니다.
> SSOT가 Phase 3에서 전이 의미 자체를 바꿨다(적 도달 = 교전 시작이지 승리가 아님).
>
> 기대값이 새 사양을 따라가는 것 — 회귀 추적의 정상 동작.

### 5.1 `CastingSystemPriorityTests.cs` — 슬롯 우선순위

SSOT Acceptance: *"슬롯[태인장(Single), 천하삼십육검(NearbyPair)]이 모두 조건을 통과하면 슬롯 순서대로 태인장이 먼저 시전."*

```csharp
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
```

> [!note]
> Phase 1/2 테스트는 `tick.PumpTicks(n)`을 직접 썼다.
> Phase 3 테스트도 같은 패턴을 따른다.
> `BattleTestFactory.CreateEngine`이 같은 `tick`을 엔진에 넘기고, 테스트 본문이 `engine.Start()`와 `tick.PumpTicks(220)`을 직접 호출한다(반복 패턴 통일 — `feedback_guide_code_rigor`).

### 5.2 `CastingDeterminismTests.cs` — `SkillCastPublished` 시퀀스 결정성

SSOT I-3.4 / Acceptance: *"같은 시드 + 같은 슬롯/적 구성 → SkillCastPublished 시퀀스 동일."* 같은 입력으로 두 번 돌려 `(tick에서의 skillId, targetId)` 시퀀스가 byte-equal인지.

```csharp
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
                casts.Add($"{skillCast.SkillId}->{skillCast.TargetId}");

            engine.Start();
            tick.PumpTicks(400);
            return casts;
        }
    }
}
```

> [!note]
> 결정 트리에 RNG가 없어 이 테스트는 "당연히 통과"처럼 보이지만, 의미가 있다 — 누군가 트리에 `IRngService` 호출(예: 무작위 슬롯 흔들기)을 끼워 넣으면 **이 테스트가 즉시 깨져** 결정성 위반을 잡는다.
>
> Phase 1 [[Completed_Phase_1_Guide]] §4.2가 "RNG 실사용 Phase에서 결정성 테스트 도입"이라 한 그 자리.

### 5.3 `ResourceMutatorTests.cs` — 기세 추가분

Phase 2 `ResourceMutatorTests`에 기세 2케이스 추가(Mana 케이스 패턴 그대로).

```csharp
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
```

> [!note]
> `PlayerStartData`는 기본값을 갖지 않는다.
> `BattleTestFactory`의 `PlayerStartData`에 `MaxMomentum = 10`을 명시한다(가이드↔산출물 양방향 확인).
>
> 테스트 메서드명 한글 시나리오 — 클래스·필드는 영문(`feedback_test_naming`).

---

## 6. 최종 검증 (Acceptance Checklist)

[[BATTLE_DESIGN]] §3 Phase 3 Acceptance:

- [x] EditMode: 슬롯[Single, NearbyPair] 모두 조건 충족 → 슬롯 0 무공 먼저 시전 (§5.1).
- [x] EditMode: 내공 부족 시 시전 X, 충전(테스트에선 `GainMana`로 강제) 후 다음 Tick에 시전. (§5.1 변형 — manaCost를 시작 내공보다 크게 두고 1케이스 추가.)
- [x] EditMode: 결정성 — 같은 시드/슬롯/적 → `SkillCastPublished` 시퀀스 동일 (§5.2).
- [x] EditMode: 기세 부족 시 실패 + 값 유지, maxMomentum 초과 방지 (§5.3, I-3.x 보강).
- [x] EditMode: Phase 1 회귀 2종(Approach/NearestEnemy) 기대값 `Engage`로 갱신 후 통과 (§5.0).
- [x] PlayMode: Engage 진입 후 슬롯 위 쿨 게이지가 돌고, 시전 시 해당 슬롯에 무공 id가 1초 표시 (§4.5).
- [x] PlayMode: VContainer 주입 정상 — Play 시 NullReferenceException 없이 진군→Engage 재현, 기세 게이지 표시.

> [!note]
> 2번째 항목(내공 부족→충전 후 시전)은 SSOT Acceptance 원문.
> §5.1에 manaCost > StartingMana인 무공 1슬롯으로 케이스를 더해 "처음엔 skip → 중간에 `((IResourceMutator)engine).GainMana(+N)` → 다음 Tick에 시전"을 단언한다.
>
> 본 Phase엔 회복원이 없으므로 테스트가 mutator를 직접 호출해 충전 상황을 만든다.

---

## 7. 흔한 함정

- **§2를 §1보다 먼저 함** — SkillData부터 만들면 결정 트리를 둘 곳(`CastingSystem`)이 없어 `BattleEngine`에 임시로 끼우게 되고, 그게 그대로 god class가 된다.
  SSOT가 "첫 작업 = 리팩토링"이라 한 이유.
- **`BattleContext`를 readonly struct로** — 시스템들이 `context.Player.State` 등을 **변경**한다. class여야 한다(참조 공유).
  Phase 1 `ActorView`가 readonly struct인 것과 정반대 — 그건 읽기 전용 사본, 이건 가변 공유 상태.
- **시스템에 상태를 들고 `Setup`마다 안 비움** — 시스템은 무상태(stateless)로 둔다.
  쿨다운·기세 같은 상태는 전부 `BattleContext`/`PlayerActor`에.
  시스템이 자체 필드를 가지면 `Setup` 재호출(재전투) 시 누수.
- **`PlayerActor.Mana`/`Momentum` 직접 쓰기** — `engine`/시스템이 `_context.Player.Momentum -= 5` 식으로 직접 쓰면 mutator 우회.
  CastingSystem은 값을 **읽기만** 하고 변경은 `ISkillExecutor.TryCast` → `IResourceMutator` 경유.
  리뷰 시 `\.(Mana|Momentum)\s*[-+]?=` grep이 `BattleEngine`의 mutator 메서드 밖에 나타나면 위반(Phase 2 함정 노트 연장).
- **한 Tick에 두 무공 시전** — 슬롯 순회에서 `break` 누락 시 한 Tick에 여러 슬롯이 발동.
  SSOT I-3.1 위반 + Phase 5 강공 동시발동 정책과 불일치. `break` 필수.
- **`SkillRange`를 거리 게이트로 재해석** — Phase 3에서 `SkillRange`는 시전 가능 거리 조건이 아니라 Phase 4 타겟 범위 예약값이다.
  자동 시전 조건에 다시 거리 게이트를 넣으면 `EngageDistance`가 큰 빌드에서 `Single` 무공이 죽는다.
- **`Resolve` 기대 테스트 방치** — §5.0 안 하면 Phase 1 테스트 2개가 빨갛게 남아 Acceptance를 못 닫는다.
  회귀는 "발견 즉시 기대값 동기화".
- **`SkillEffect` 빈 타입 미리 생성** — "TryCast가 effects를 참조하니까" 식으로 빈 클래스 만들지 말 것. Phase 3엔 효과가 없다(YAGNI). Phase 4에 데미지와 함께.
- **심법 중복 skip을 Phase 3에 구현** — 효과 활성 상태가 없어 항상 false인 죽은 분기. Phase 4.

---

## 8. Phase 3 → Phase 4 진입 조건

§6 체크리스트 7개 + 커밋 완료 + [[BATTLE_DESIGN]] §5 변경 이력에 Phase 3 완료 한 줄.

Phase 4는 [[BATTLE_DESIGN]] §3 Phase 4 — 적 일반 공격 cadence → Player HP 감소, `SkillEffect`(데미지) 도입 → 적 HP 감소 → 사망 → 전체 승패(Resolve).
본 Phase의 `IBattleSystem` 골격 위에 `CombatSystem`(또는 효과 적용)이 추가되고, `TryCast`의 "skill.effects 적용 (Phase 4)" 자리가 채워진다.

단일 Tick 순서가 (시스템 → **사망 판정** → 스냅샷)으로 확장된다.
