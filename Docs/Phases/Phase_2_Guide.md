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
> Phase 2는 자원 **컨테이너만** 만든다. 자원을 소비/회복하는 룰(SpendMana를 부르는 코드)은 Phase 3 이후. 본 Phase에선 mutator를 노출만 해두고 호출처가 없다.
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
        public int Mana;
        public int MaxMana;
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
        public int MaxMana = 100;
        public int StartingMana = 50;
    }
}
```

> SSOT는 `mana` 디폴트=50, `maxMana`=100. 시작값과 최댓값을 같은 자료에 두지 않으면 Phase 11+에서 메타 강화(maxMana +10)와 현재값을 별개 축으로 조작하기 어려워진다.

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
        public readonly int Mana;
        public readonly int MaxMana;

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
            }
            else
            {
                Mana = 0;
                MaxMana = 0;
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
        /// <summary>amount만큼 내공 소비. amount > 현재 mana면 false 반환, 값 불변(atomic).</summary>
        bool SpendMana(int amount);

        /// <summary>maxMana로 클램프. silent.</summary>
        void GainMana(int amount);
    }
}
```

> Phase 3에서 `SpendMomentum`/`GainMomentum`이 같은 인터페이스에 추가됨. Phase 2엔 내공 두 개만 노출.

### 2.2 `BattleEngine` — `IResourceMutator` 구현 + Setup 자원 초기화

**핵심 변경**:
1. `BattleEngine : IResourceMutator` 구현.
2. `Setup` 시 `PlayerStartData`의 내공 시작값을 `PlayerActor`에 복사.
3. `SpendMana`는 atomic — 부족 시 값 불변 + false 반환.
4. `GainMana`는 maxMana로 클램프.

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
            Mana = data.Player.StartingMana,
            MaxMana = data.Player.MaxMana,
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

    public bool SpendMana(int amount)
    {
        if (amount > _player.Mana)
            return false;
        _player.Mana -= amount;
        return true;
    }

    public void GainMana(int amount)
    {
        var nextMana = _player.Mana + amount;
        _player.Mana = Math.Min(nextMana, _player.MaxMana);
    }
}
```

> **왜 `Mathf`가 아니라 `System.Math`?** Engine asmdef는 UnityEngine 의존을 끊는다 — `Mathf.Min`/`Mathf.Clamp`는 UnityEngine이라 금지. 반면 `System.Math.Min(int, int)`은 .NET BCL이고 정수 정확 비교라 결정론도 삼항 비교와 동일하므로 안전. min을 손수 삼항으로 풀지 말고 `Math.Min`을 쓴다 — 이미 있는 추상화를 저수준으로 다시 풀지 않는다는 인지 부하 규칙(루트 CLAUDE.md §4).

---

## 3. View — 자원 2 표시

진행 순서: **Unity UI 추가 → 코드 수정 → Inspector 연결 → Play 확인**.

### 3.1 자원 UI 만들기

`Battle.unity` 열고 Canvas 아래에 만든다. 최종 계층:

```
ResourcePanel (RectTransform + Vertical Layout Group)
 ├ HpBar (RectTransform)
 │   ├ Label (TMP_Text)        "HP"
 │   ├ Background (Image)       회색
 │   │   └ Fill (Image)         빨강 — 이 너비를 코드가 조절
 │   └ ValueText (TMP_Text)    "50 / 50"
 └ ManaBar  (HpBar 복제, 라벨 "MP" + Fill 파랑)
```

#### 먼저 알아둘 개념 4개

| 용어 | 한 줄 설명 |
|------|-----------|
| **Canvas** | 모든 UI가 올라가는 판. 씬에 1개 있으면 그 아래에 UI를 넣는다 |
| **RectTransform** | UI용 Transform. 위치·크기를 **앵커/피벗/sizeDelta**로 잡는다 |
| **앵커(Anchor)** | 부모 사각형 안에서 어디에 매달릴지. 한 점에 모으면(min=max) 크기가 `sizeDelta`로 **고정**, 벌리면 부모 따라 늘어남 |
| **피벗(Pivot)** | 자기 크기·회전의 기준점. `x=0`이면 왼쪽 변 기준 → 너비를 늘리면 **오른쪽으로만** 자란다 |

게이지가 "왼쪽 고정 + 오른쪽으로 차오름"이 되려면 **Fill = 좌측 한 점 앵커 + 피벗 x=0**. 이게 §3.2 코드와 맞물리는 핵심 ([아래 "왜 이 앵커여야 하나"](#왜-fill-앵커가-중요한가) 참조).

#### 단계별 절차

1. **ResourcePanel**
   - Hierarchy `Canvas` 우클릭 → UI → Empty, 이름 `ResourcePanel`.
   - 앵커 프리셋(Inspector 좌상단 네모) → 화면 top-left. Pos X=20, Y=-20.
   - `Add Component → Vertical Layout Group` (Spacing 8, Child Alignment Upper Left, Control Child Size 해제).
2. **HpBar (컨테이너)**
   - `ResourcePanel` 우클릭 → UI → Empty, 이름 `HpBar`. Width 240, Height 30.
3. **Label**
   - `HpBar` 우클릭 → UI → Text - TextMeshPro (첫 사용 시 "Import TMP Essentials" Import).
   - 이름 `Label`, 내용 `HP`. 앵커 왼쪽-중앙, Width 40.
4. **Background**
   - `HpBar` 우클릭 → UI → Image, 이름 `Background`. Color 회색(80,80,80,255).
   - Width 200, Height 20. Label 오른쪽에 배치.
   - **이 Background 폭이 게이지 최대 길이.** `ResourceBar`가 이 폭을 `_track.rect.width`로 런타임에 읽으므로 코드와 따로 동기화할 값은 없다 — 폭을 바꾸면 게이지가 자동으로 따라간다.
5. **Fill (제일 중요)**
   - `Background` 우클릭 → UI → Image, 이름 `Fill`. Color 빨강.
   - 앵커 프리셋 **왼쪽-중앙** 클릭 → `anchorMin = anchorMax = (0, 0.5)`.
   - **Pivot** 직접 입력 `X=0, Y=0.5`.
   - `Pos X=0, Pos Y=0` (Background 왼쪽 변에 딱 붙음), `Width=200, Height=20`.
6. **ValueText**
   - `HpBar` 우클릭 → UI → Text - TextMeshPro, 이름 `ValueText`, 내용 `50 / 50`. Background 위에 겹치거나 오른쪽.

#### 왜 Fill 앵커가 중요한가

§3.2 [ResourceBar.cs](#scriptsbattleviewresourcebarcs)는 `_fill.sizeDelta.x = _track.rect.width * ratio`로 너비를 직접 만진다.

- `sizeDelta`는 "앵커 사각형 대비 크기 차"다. Fill 앵커를 **한 점**(min=max)으로 모으면 앵커 사각형이 0 → `sizeDelta.x`가 **그대로 실제 픽셀 너비**가 된다. 앵커를 좌우로 벌려놓으면(stretch) `sizeDelta.x`는 너비가 아니라 여백이 돼 코드가 안 먹는다.
- 피벗 `x=0`이라 너비가 줄어도 **왼쪽 변 고정, 오른쪽만 깎임** → 게이지가 오른쪽부터 빈다. 피벗 0.5면 가운데로 줄어든다.

→ **Fill 앵커=(0,0.5) 한 점 / 피벗 x=0 / Pos=(0,0)**. 이 셋이 어긋나면 게이지가 안 움직이거나 엉뚱하게 움직인다 — Phase 2에서 가장 흔히 막히는 지점.

#### Prefab화 + ManaBar 복제

1. `HpBar`를 `Assets/_Project/Prefabs/`로 드래그 → Prefab 생성.
2. `ResourcePanel` 아래에 Prefab 한 번 더 배치(또는 `HpBar` Ctrl+D).
3. 복제본 이름 `ManaBar`, `Label` 텍스트 `HP→MP`, `Fill` Color 빨강→파랑.
4. 이 시점엔 UI만 있다. `ResourceBar` 컴포넌트 부착·슬롯 연결은 §3.2에서 스크립트를 만든 뒤 §3.4에서 한다.

> Phase 2 placeholder 수준이라 정교한 UI는 Phase 14에서 다시 함. Phase 3에 기세 게이지가 추가되면 이 Prefab을 한 번 더 복제(라벨/색만).

> **참고**: Unity Image에 `Image Type=Filled` + `Fill Amount`(0~1) 내장 게이지도 있다. 더 간단해 보이지만 가이드가 `sizeDelta` 방식을 쓰는 이유 — (1) RectTransform 이해에 학습상 도움 (2) Phase 14 정식 UI에서 fill 외 데코(테두리·눈금)를 붙이기 쉬움. 코드가 이미 `sizeDelta`로 쓰여 있으니 앵커만 정확히 잡으면 된다.

### 3.2 `ResourceBar` MonoBehaviour (작은 헬퍼)

**역할**: `(current, max)`를 받아 Fill width + ValueText를 갱신. View 쪽 유틸.

### `Scripts/Battle/View/ResourceBar.cs`

```csharp
using UnityEngine;
using TMPro;

namespace MurimRunaway.Battle.View
{
    /// <summary>current/max를 받아 Fill width + 텍스트를 갱신하는 자원 게이지 헬퍼.</summary>
    public sealed class ResourceBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _track;   // Fill의 부모(Background) — 게이지 최대 폭
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _valueText;

        public void SetValue(int current, int max)
        {
            var ratio = max <= 0 ? 0f : (float)current / max;
            var size = _fill.sizeDelta;
            size.x = _track.rect.width * ratio;
            _fill.sizeDelta = size;

            if (_valueText != null)
                _valueText.text = $"{current} / {max}";
        }
    }
}
```

> `_maxWidth` 상수 대신 `_track.rect.width`를 읽는다 — Background 폭이 유일한 진실의 출처가 되어 수동 동기화가 사라진다. §3.3 `BattleSceneController`가 `_gauge.rect.width`를 쓰는 패턴과 동일. `rect.width`는 레이아웃 이후에만 유효하나 `SetValue`는 스냅샷 시점(레이아웃 이후) 호출이라 안전 — `Awake`에서 캐싱 금지.

### 3.3 `BattleSceneController` — 자원 표시 연결

추가 슬롯과 `HandleSnapshot` 분기.

```csharp
public sealed class BattleSceneController : MonoBehaviour
{
    // ... 기존 [SerializeField] 슬롯 유지 ...

    [SerializeField] private ResourceBar _hpBar;
    [SerializeField] private ResourceBar _manaBar;

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
                _manaBar.SetValue(actor.Mana, actor.MaxMana);
            }
        }
    }
}
```

### 3.4 컴포넌트 부착 + Inspector 연결

§3.2에서 `ResourceBar.cs`를 만들었으니 이제 씬에 붙인다. **연결은 2단** — 먼저 각 Bar가 자기 `ResourceBar`를 갖게 하고(A), 그 다음 `BattleSceneController`가 두 `ResourceBar`를 잡게 한다(B).

#### (A) `ResourceBar` 부착 + 자체 슬롯 — **Prefab에 1회**

`ManaBar`가 `HpBar` Prefab의 인스턴스이므로, Prefab 에셋에 **한 번만** 부착하면 두 Bar가 함께 받는다.

1. Project 창 `HpBar` Prefab 더블클릭 → **Prefab Mode** 진입 (또는 Hierarchy 인스턴스 우클릭 → Prefab → Open).
2. 루트 `HpBar`에 `Add Component → ResourceBar`.
3. 슬롯 연결 (Prefab **자신의** 자식을 가리킴):
   - `_track` ← Prefab의 **Background** 드래그(게이지 최대 폭을 여기서 읽음).
   - `_fill` ← Prefab의 **Fill** 드래그(RectTransform로 들어감).
   - `_valueText` ← Prefab의 **ValueText** 드래그.
4. Prefab Mode 나가기(저장). → HpBar·ManaBar 인스턴스 둘 다 컴포넌트+슬롯 반영.

> **왜 Prefab 슬롯이 인스턴스마다 따로 먹히나**: 같은 Prefab 내부 참조(`HpBar`→`HpBar/Fill`)는 Unity가 인스턴스별로 자동 재해석한다 — ManaBar 인스턴스의 `ResourceBar`는 `ManaBar/Fill`을 알아서 가리킨다. 슬롯을 인스턴스에서 다시 만질 필요 없음.
>
> **단, ManaBar가 진짜 Prefab 인스턴스일 때만.** Hierarchy에서 ManaBar 아이콘이 파란 박스(Prefab 인스턴스)인지 확인. §3.1에서 Ctrl+D로 만들어 Prefab 연결이 끊긴 일반 사본이면 ManaBar에 따로 `Add Component → ResourceBar` + 슬롯 수동 연결.
>
> 라벨 텍스트("MP")·Fill 색(파랑)은 ManaBar 인스턴스의 **오버라이드**로 둔다 — Prefab 공통값을 인스턴스에서만 덮는 정상 동작.

#### (B) `BattleSceneController` 슬롯 연결

`BattleSceneController` 컴포넌트(§3.3) Inspector에서:

| 슬롯 | 연결할 대상 |
|------|------------|
| `_hpBar` | `ResourcePanel/HpBar`에 붙인 `ResourceBar` |
| `_manaBar` | `ResourcePanel/ManaBar`에 붙인 `ResourceBar` |

`PlayerStartData` 시작값은 일단 디폴트(`Mana=50/100`) 그대로. SerializeField로 노출할지는 Phase 3에서 Skill 비용 튜닝 시작할 때 결정.

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
    [SerializeField] private ResourceBar _manaBar;
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
- SpendMana 부족 시 false + 값 불변 (I-2.2).
- GainMana가 maxMana로 클램프 (Edge).

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
                    MaxMana = 100, StartingMana = 50,
                },
                Enemies = new EnemyData[0],
            });
            return engine;
        }

        [Test]
        public void 내공이_부족하면_SpendMana는_false를_반환하고_값은_그대로다()
        {
            IResourceMutator mutator = CreateEngine();

            var ok = mutator.SpendMana(60); // 시작 50, 60 시도

            Assert.IsFalse(ok);
            // 스냅샷으로 검증
            ((BattleEngine)mutator).Start();
            BattleSnapshot snapshot = default;
            ((BattleEngine)mutator).SnapshotPublished += s => snapshot = s;
            Assert.AreEqual(50, snapshot.Actors[0].Mana);
        }

        [Test]
        public void GainMana는_maxMana로_클램프된다()
        {
            var engine = CreateEngine();
            IResourceMutator mutator = engine;

            mutator.GainMana(80); // 50 + 80 = 130 → 100으로 클램프

            BattleSnapshot snapshot = default;
            engine.SnapshotPublished += s => snapshot = s;
            engine.Start();

            Assert.AreEqual(100, snapshot.Actors[0].Mana);
        }
    }
}
```

> 테스트 메서드명은 한글 시나리오 묘사 — `feedback_test_naming` 메모리 규칙. 클래스명·필드명은 영문.

---

## 6. 최종 검증 (Acceptance Checklist)

[BATTLE_DESIGN §3 Phase 2 Acceptance](../BATTLE_DESIGN.md) + M2 추가:

- [ ] EditMode: SpendMana로 0 미만이 되는 시도가 false 반환 + 값 불변. (§5.1)
- [ ] EditMode: GainMana가 maxMana 초과 시 maxMana로 클램프. (§5.1)
- [ ] PlayMode: 화면에 HP·내공 게이지 표시.
- [ ] PlayMode: VContainer LifetimeScope에서 의존성 주입 동작 확인 — Play 시 NullReferenceException 없이 Phase 1 흐름 그대로 재현.

---

## 7. 흔한 함정

- **PlayerActor.Mana 직접 쓰기** — `engine._player.Mana -= 5` 같은 직접 쓰기는 mutator를 우회. 코드 리뷰 시 grep으로 검출 (`\.Mana\s*[-+*/]?=` 패턴이 BattleEngine.cs의 mutator 메서드 외에 나타나면 위반).
- **GainMana의 음수 인자** — `GainMana(-5)`는 silent로 mana를 5 깎는다. 의도된 동작이 아니라면 호출처 버그. SpendMana로 명시할 것. 본 Phase에선 가드 안 추가(Phase 4 데미지 계산이 들어올 때 통합 검토).
- **Enemy ActorView의 Mana 필드를 게이지에 묶기** — Enemy는 Mana 0. `IsPlayer` 게이팅 누락하면 Enemy 마커 옆에 빈 게이지가 그려짐.
- **LifetimeScope에 Engine을 트랜션트로 등록** — Singleton이어야 매 틱 같은 인스턴스. `Lifetime.Transient`로 두면 `Construct`가 받는 Engine과 `Setup`/`Start`를 호출한 Engine이 달라질 수 있음.
- **`[Inject]` 메서드를 `private`으로** — VContainer는 public/private 모두 reflect하지만 IL2CPP 빌드(모바일)에서 stripping될 위험. `public void Construct(...)`로 두면 안전.

---

## 8. Phase 2 → Phase 3 진입 조건

§6 체크리스트 4개 + 커밋 완료. Phase 3는 [BATTLE_DESIGN §3 Phase 3](../BATTLE_DESIGN.md) — `SkillData` SO + 자동 시전 결정 트리 + **기세(momentum) 자원 컨테이너 도입**(v0.4.2 스코프 컷으로 P2에서 옮겨옴). **첫 작업으로 `IBattleSystem` 패턴 도입**(SSOT 리팩토링 트리거)이 들어와 `BattleEngine.HandleTick`이 dispatcher로 줄어든다. 본 Phase의 `IResourceMutator`는 그 때 `GainMomentum`/`SpendMomentum`이 추가되며 Skill 효과의 호출처가 된다.
