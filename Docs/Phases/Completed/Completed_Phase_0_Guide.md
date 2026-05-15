# Phase 0 작업 가이드 — Foundation

>
> **목표 한 줄**: 빈 골격이 컴파일되고, EditMode 테스트 2개가 통과하고, Play 시 화면 카운터가 1씩 증가한다.
>
> **참조 SSOT**: [BATTLE_DESIGN.md §3 Phase 0](../BATTLE_DESIGN.md) — 본 가이드는 SSOT가 아니라 작업 절차 안내. 사양이 다르면 SSOT 우선.
>
> **함께 보기**: [Phase_0_Learned.md](Phase_0_Learned.md) — 본 Phase에서 사용된 개념 정리.
>

---

## 0. 사전 점검


| 항목           | 확인                                                                |
|------------------|-----------------------------------------------------------------------|
| Unity Editor     | 6000.3.14f1 (`ProjectSettings/ProjectVersion.txt`)                    |
| Test Framework   | `com.unity.test-framework@1.6.0` 설치됨 (`Packages/manifest.json`) |
| 작업 브랜치 | `main` 또는 `feature/phase-0`                                       |


>
> Unity 6 기준. 메뉴 명칭이 다른 버전과 다를 수 있음.
>

---

## 1. 폴더 구조 만들기

`Assets/_Project/` 아래에 다음을 생성:

```
Assets/_Project/  
├─ Scripts/  
│  └─ Battle/  
│     ├─ Domain/  
│     ├─ Engine/  
│     └─ View/  
├─ Tests/  
│  └─ Battle/  
└─ Scenes/
```

> 
> Unity Editor의 Project 창에서 우클릭 → Create → Folder 로 만들거나, 파일 시스템에서 만들고 Editor를 한 번 포커스(.meta 자동 생성).
> 

---

## 2. asmdef 4종 작성

각 폴더에 Assembly Definition 파일을 만든다. **Editor GUI에서**: 폴더 우클릭 → `Create` → `Scripting` ▶ → `Assembly Definition`.

> 
> Unity 6에서는 `Scripting` 서브메뉴 안에 있다. 구버전(2022 이하)에서는 `Create` 바로 아래.
> 

작성 후 Inspector에서 다음과 같이 설정.

### 2.1 `Scripts/Battle/Domain/MurimRunaway.Battle.Domain.asmdef`

```json
{  
"name": "MurimRunaway.Battle.Domain",  
"rootNamespace": "MurimRunaway.Battle.Domain",  
"references": [],  
"autoReferenced": false  
}
```

### 2.2 `Scripts/Battle/Engine/MurimRunaway.Battle.Engine.asmdef`

```json
{  
    "name": "MurimRunaway.Battle.Engine",  
    "rootNamespace": "MurimRunaway.Battle.Engine",  
    "references": ["MurimRunaway.Battle.Domain"],  
    "autoReferenced": false  
}
```

### 2.3 `Scripts/Battle/View/MurimRunaway.Battle.View.asmdef`

```json
{  
"name": "MurimRunaway.Battle.View",  
"rootNamespace": "MurimRunaway.Battle.View",  
"references": [  
"MurimRunaway.Battle.Domain",  
"MurimRunaway.Battle.Engine",  
"Unity.TextMeshPro"  
],  
"autoReferenced": false  
}
```

### 2.4 `Tests/Battle/MurimRunaway.Battle.Tests.asmdef`

```json
{  
    "name": "MurimRunaway.Battle.Tests",  
    "rootNamespace": "MurimRunaway.Battle.Tests",  
    "references": [  
        "MurimRunaway.Battle.Domain",  
        "MurimRunaway.Battle.Engine"  
    ],  
    "includePlatforms": ["Editor"],  
    "optionalUnityReferences": ["TestAssemblies"],  
    "precompiledReferences": ["nunit.framework.dll"],  
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],  
    "autoReferenced": false  
}
```

> 
> Test asmdef는 Inspector의 **Test Assemblies** 체크박스를 켜는 게 가장 안전. JSON을 직접 편집하면 위 형태가 된다.
> 
> View가 Domain·Engine을 둘 다 참조해도, **Engine은 View를 참조하지 못함** → 의존 방향이 단방향으로 강제된다.
> 

**검증**: Unity가 자동 컴파일 → Console에 에러 없으면 OK.

---

## 3. Domain 타입 — Phase 0에서 실제로 쓰이는 것만

> 
> **YAGNI**: "나중에 쓸 것 같아서" 미리 만들지 않는다 ([CLAUDE.md §1](../../CLAUDE.md)).
> 
> Phase 0에서 실제로 쓰이는 Domain 타입은 `BattleSnapshot` 하나.
> `BattleStartData`(Phase 1), `BattleResult`(Phase 4)는 그 Phase에서 만든다.
> 

### 무엇을 만드는가 — `BattleSnapshot`

**역할**: Engine이 매 틱마다 "이 시점의 전투 상태는 이렇다"고 View에 **읽기 전용으로 던져주는 데이터 묶음**.

- Engine 내부 변수(`_tickIndex` 등)를 View가 직접 만지지 못하게 하기 위한 **경계용 타입**.
- `readonly struct` — 한번 만들면 못 바꿈. View가 받아서 그리는 동안 Engine이 다음 틱을 진행해도 충돌 없음.
- Phase 0에서는 `TickIndex` 하나뿐이지만, 이후 Phase에서 HP·거리·상태이상 등 필드가 추가될 예정.
- 흐름: `Engine이 매 틱 BattleSnapshot 생성` → `SnapshotPublished 이벤트로 통보` → `View가 받아서 UI 갱신`.

### `Scripts/Battle/Domain/BattleSnapshot.cs`

```csharp
namespace MurimRunaway.Battle.Domain
{
    /// <summary>매 틱 Engine이 View에 던지는 읽기 전용 상태 스냅샷.</summary>
    public readonly struct BattleSnapshot
    {
        public readonly long TickIndex;
        public BattleSnapshot(long tickIndex) { TickIndex = tickIndex; }
    }
}
```

---

## 4. Engine 인터페이스 작성

`ITickService`·`IRngService`는 Phase 0에서 Mock이 필요하므로 지금 인터페이스를 만든다.`IBattleEngine`은 두 번째 구현체가 생기는 시점(Phase 1 이후)에 추출한다 — 지금은 YAGNI.

### 무엇을 만드는가 — `ITickService`

**역할**: "매 0.05초마다 한 번씩 신호를 쏴주는 시계"의 **추상화**.

- 실제 게임에서는 Unity의 `Update()`가 매 프레임 호출되며 dt를 누적해 0.05초가 차면 `Ticked(0.05f)`을 호출 (§6.1 `UnityTickService`).
- 테스트에서는 Unity 없이 동작해야 하므로 수동으로 똑딱이는 가짜 구현 (§7.1 `MockTickService`).
- 인터페이스로 묶어두면 Engine은 "시간이 어디서 오는지" 모르고 그냥 `Ticked`을 구독만 하면 됨 → **Engine이 Unity에 직접 의존하지 않게 됨**.

### `Scripts/Battle/Engine/ITickService.cs`

```csharp
using System;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>0.05초 고정 간격 틱 신호의 추상화. Engine을 Unity 시간에서 분리.</summary>
    public interface ITickService
    {
        event Action<float> Ticked;   // dt = 0.05f 고정
    }
}
```

> 
> Pause/Resume/IsRunning은 Phase 0 호출자가 0이라 뺐다. 실제로 일시정지가 필요한 Phase에서 추가한다 (YAGNI).
> 

### 무엇을 만드는가 — `IRngService`

**역할**: 모든 난수 호출을 통과시키는 **단일 창구**.

- `UnityEngine.Random`·`System.Random`을 코드 곳곳에서 직접 부르면 시드 통제가 불가능 → 같은 입력에 다른 결과 → 디버깅·리플레이 불가.
- 모든 RNG를 이 인터페이스로 모으고 `Reseed(seed)`로 초기화하면, 같은 시드는 같은 시퀀스를 보장 → **결정적 시뮬레이션** 가능.
- Phase 0에서는 `RngService`(System.Random 래퍼) 하나만 구현. 테스트에서 시드 고정으로 재현성 검증.
- Phase 0이 실제로 쓰는 메서드는 `Reseed`/`NextFloat01` 둘뿐. `NextInt`/`Pick`/`PickWeighted`는 호출자가 생기는 Phase에서 추가한다 (YAGNI).

### `Scripts/Battle/Engine/IRngService.cs`

```csharp
namespace MurimRunaway.Battle.Engine
{
    /// <summary>모든 난수의 단일 창구. 시드 고정으로 결정적 시뮬레이션 보장.</summary>
    public interface IRngService
    {
        void Reseed(int seed);
        float NextFloat01();
    }
}
```

---

## 5. Engine 구현체

### 5.1 무엇을 만드는가 — `RngService`

**역할**: `IRngService`의 실구현. 내부적으로 `System.Random`을 들고 있으면서 `Reseed`/`NextFloat01`을 제공.

- Unity 의존 없음 → Engine asmdef에 둠.

### `Scripts/Battle/Engine/RngService.cs` — 결정적 RNG

```csharp
namespace MurimRunaway.Battle.Engine
{
    /// <summary>System.Random 기반 IRngService 구현.</summary>
    public sealed class RngService : IRngService
    {
        private System.Random _random = new(0);
        public void Reseed(int seed) => _random = new System.Random(seed);
        public float NextFloat01() => (float)_random.NextDouble();
    }
}
```

### 5.2 무엇을 만드는가 — `BattleEngine`

**역할**: 전투 시뮬레이션의 **심장**. Phase 0 시점에서는 단순히 "틱이 올 때마다 카운터를 +1 하고 `BattleSnapshot`를 발행"하는 정도.

- 생성자에서 `ITickService.Ticked`을 구독 → 시간 진행은 외부에서 주입.
- 매 틱 `_tickIndex++` → `SnapshotPublished` 이벤트로 View에 통보.
- `using UnityEngine` 없음에 주의 — Engine은 순수 C#만 사용 ([CLAUDE.md §2](../../CLAUDE.md)).
- 이후 Phase에서 Actor·HP·거리축·기술 시스템이 여기로 들어옴.

### `Scripts/Battle/Engine/BattleEngine.cs`

```csharp
using System;
using MurimRunaway.Battle.Domain;

namespace MurimRunaway.Battle.Engine
{
    /// <summary>전투 시뮬레이션 본체. 틱마다 상태를 진행시키고 SnapshotPublished으로 통보.</summary>
    public sealed class BattleEngine
    {
        private readonly ITickService _tick;
        private long _tickIndex;

        public event Action<BattleSnapshot> SnapshotPublished;

        public BattleEngine(ITickService tick)
        {
            _tick = tick;
            _tick.Ticked += HandleTick;
        }

        public void Start() { _tickIndex = 0; }

        private void HandleTick(float dt)
        {
            _tickIndex++;
            SnapshotPublished?.Invoke(new BattleSnapshot(_tickIndex));
        }
    }
}
```

>
> `IBattleEngine` 인터페이스·`Setup(BattleStartData)`·`OnResult`는 Phase 1 이후 필요해지면 추출한다.
>

---

## 6. View 측

### 6.1 무엇을 만드는가 — `UnityTickService`

**역할**: `ITickService`의 Unity 실구현. `MonoBehaviour`라서 씬 GameObject에 붙여 사용.

- `Update()`에서 `Time.deltaTime`을 누적하다 0.05초가 차면 `Ticked(0.05f)` 호출.
- 프레임 레이트가 들쑥날쑥해도 **틱은 항상 0.05초 간격**으로 일정 → 결정적 시뮬레이션 보장.
- `while` 루프인 이유: 한 프레임에 0.1초가 흘렀다면 틱을 두 번 호출해야 함 (스파이크 보정).

### `Scripts/Battle/View/UnityTickService.cs` — Update 누산 실구현

```csharp
using System;
using UnityEngine;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.View
{
    /// <summary>Time.deltaTime 누적으로 0.05초 틱을 발생시키는 ITickService의 Unity 구현.</summary>
    public sealed class UnityTickService : MonoBehaviour, ITickService
    {
        private const float TickIntervalSeconds = 0.05f;
        private float _elapsedSinceLastTick;

        public event Action<float> Ticked;

        private void Update()
        {
            _elapsedSinceLastTick += Time.deltaTime;
            // while: 프레임 드랍 시 밀린 틱을 따라잡아 시뮬레이션 시간 ≈ 실시간 보장
            while (_elapsedSinceLastTick >= TickIntervalSeconds)
            {
                _elapsedSinceLastTick -= TickIntervalSeconds;
                Ticked?.Invoke(TickIntervalSeconds);
            }
        }
    }
}
```

### 6.2 무엇을 만드는가 — `BattleSceneController`

**역할**: 씬에서 Engine·View를 **조립하고 연결하는 접착제**.

- Inspector에서 `UnityTickService`와 `TMP_Text`를 슬롯에 꽂아둠.
- `Start()`에서 `BattleEngine`을 생성하며 TickService를 주입 (의존성 주입).
- `SnapshotPublished` 이벤트를 구독해 매 틱 카운터 텍스트 갱신.
- Engine은 Unity를 모르고, View는 Engine 내부를 모름 → 둘을 잇는 책임만 이 클래스가 짐.

### `Scripts/Battle/View/BattleSceneController.cs` — 카운터 표시

```csharp
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

        private BattleEngine _engine;

        private void Start()
        {
            _engine = new BattleEngine(_tickService);
            _engine.SnapshotPublished += HandleSnapshot;
            _engine.Start();
        }

        private void HandleSnapshot(BattleSnapshot state)
        {
            if (_counterText != null)
                _counterText.text = $"Tick: {state.TickIndex}";
        }
    }
}
```

---

## 7. EditMode 테스트 2개

### 7.1 무엇을 만드는가 — `MockTickService`

**역할**: 테스트에서 Unity 없이 `Ticked`을 수동으로 호출하는 가짜 TickService.

- `PumpTicks(100)` 호출 시 `Ticked(0.05f)`을 100번 즉시 호출 → 테스트가 1초를 기다리지 않음.
- `ITickService`를 구현하므로 `BattleEngine`은 진짜와 가짜를 구분하지 못함 (인터페이스의 힘).

### `Tests/Battle/MockTickService.cs` — 테스트용 수동 펌프

```csharp
using System;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    /// <summary>테스트용 수동 펌프. PumpTicks(n)로 Ticked을 즉시 n번 호출.</summary>
    public sealed class MockTickService : ITickService
    {
        private const float TickIntervalSeconds = 0.05f;

        public event Action<float> Ticked;

        public void PumpTicks(int count)
        {
            for (int i = 0; i < count; i++)
                Ticked?.Invoke(TickIntervalSeconds);
        }
    }
}
```

### 7.2 `Tests/Battle/TickServiceTests.cs`

```csharp
using NUnit.Framework;

namespace MurimRunaway.Battle.Tests
{
    public class TickServiceTests
    {
        [Test]
        public void PumpTicks_AccumulatesDtCorrectly()
        {
            var tick = new MockTickService();
            float total = 0f;
            tick.Ticked += dt => total += dt;

            tick.PumpTicks(100);

            Assert.AreEqual(100 * 0.05f, total, 1e-5f);
        }
    }
}
```

### 7.3 `Tests/Battle/RngServiceTests.cs`

```csharp
using NUnit.Framework;
using MurimRunaway.Battle.Engine;

namespace MurimRunaway.Battle.Tests
{
    public class RngServiceTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new RngService();
            var b = new RngService();
            a.Reseed(12345);
            b.Reseed(12345);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(a.NextFloat01(), b.NextFloat01(), 0f);
        }
    }
}
```

**검증**: Unity 메뉴 → Window → General → Test Runner → EditMode 탭 → Run All. 두 테스트 모두 초록색.

---

## 8. 씬 셋업

1. `Assets/_Project/Scenes/Battle.unity` 생성 (File → New Scene → Empty → Save).
2. Hierarchy에서:3. `TickService` (Empty GameObject) — `UnityTickService` 컴포넌트 부착.
4. `BattleSceneController` (Empty GameObject) — `BattleSceneController` 컴포넌트 부착, Inspector에서 `_tickService` 슬롯에 위 GameObject 드래그.
5. `Canvas` — UI → Canvas 생성. 하위에 `Text - TextMeshPro` 추가. 처음 추가 시 TMP Essentials 임포트 다이얼로그 → Import.
6. Canvas 하위 Text를 `BattleSceneController._counterText` 슬롯에 드래그.
7. Build Settings에 씬 추가 (File → Build Profiles → Scene List → Add Open Scenes).


---

## 9. 최종 검증 (Acceptance Checklist)

[BATTLE_DESIGN §3 Phase 0 Acceptance](../BATTLE_DESIGN.md):

- 폴더/asmdef/네임스페이스 생성, **Console에 컴파일 에러 0**.
- EditMode `TickServiceTests.PumpTicks_AccumulatesDtCorrectly` 통과.
- EditMode `RngServiceTests.SameSeed_ProducesSameSequence` 통과.
- PlayMode: `Battle.unity` 재생 → 화면 텍스트가 매 틱 1씩 증가.

---

## 10. 커밋

```bash
git add Assets/_Project Packages/manifest.json  
git commit -m "Phase 0: foundation (asmdefs, tick/rng services, empty engine)"
```

>
> 이미 `Phase_0_Guide.md` / `Phase_0_Learned.md` / `Docs/Phases/` 도 같이 추가되어 있을 것.
>

---

## 11. 흔한 함정

- **TMP_Text 못 찾음** → TextMeshPro Essentials 미임포트. Canvas에 Text-TMP 처음 추가하면 자동 다이얼로그 뜸.
- **Engine asmdef에서 `using UnityEngine`** → Engine은 표준 C# 의존만. `Time.deltaTime`을 직접 쓰고 싶어지면 멈추고 `ITickService`로 받는 구조인지 다시 확인.
- **테스트 어셈블리에서 `View` 타입 사용** → Tests asmdef는 View를 참조하지 않음. View 의존 테스트는 PlayMode에서.
- **`autoReferenced: false` 설정 안 함** → 4개 asmdef 모두 `false`여야 함. 하나라도 `true`로 두면 떠도는 스크립트(asmdef 밖)가 그 어셈블리를 우회해서 호출 가능 → 의존 방향 강제 효과 약화. View도 예외 아님.

---

## 12. Phase 0 → Phase 1 진입 조건

위 §9 체크리스트 4개 + 커밋 완료. Phase 1은 [BATTLE_DESIGN §3 Phase 1](../BATTLE_DESIGN.md)에서 Actor와 거리축을 도입한다.

 
