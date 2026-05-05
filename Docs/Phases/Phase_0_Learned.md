# Phase 0 — 배운 기술 / 개념 정리

> Phase 0의 **작업 절차**는 [Phase_0_Guide.md](Phase_0_Guide.md). 본 문서는 그 작업에서 등장한 **개념·용어·설계 결정의 이유**를 정리한다. 학습 노트.

---

## 1. asmdef (Assembly Definition)

**한 줄 정의**: Unity에서 C# 코드를 어떤 단위로 묶어 별도 DLL로 컴파일할지를 지정하는 파일.

**왜 쓰나**

기본적으로 Unity는 `Assets/` 아래 모든 스크립트를 `Assembly-CSharp.dll` 한 덩어리로 컴파일한다. 이게 다음 문제를 만든다:
1. 한 줄만 고쳐도 전체 재컴파일 → 큰 프로젝트일수록 느려진다.
2. 모든 스크립트가 서로를 자유롭게 참조 가능 → 아키텍처 위반(예: View가 Domain의 private 필드 직접 변경)을 컴파일러가 막을 수 없다.

asmdef로 코드를 작은 어셈블리로 나누면:
- 그 어셈블리만 다시 컴파일 → 빨라진다.
- `references` 필드에 의존 가능한 어셈블리를 명시 → **참조하지 않은 어셈블리의 타입은 사용 자체가 컴파일 에러**.

**이 프로젝트의 설계**

```
Domain  ←  Engine  ←  View
                ↖   ↑
                  Tests
```
- Domain은 아무것도 참조 안 함 → 순수 데이터/룰.
- Engine은 Domain만 참조 → 룰을 적용하지만 화면을 모름.
- View는 둘 다 참조 → 화면에 그리고 입력을 받지만, 룰을 결정하지 않음.
- Tests는 Domain·Engine만 → View 없는 환경에서 빠르게 단위 테스트.

이 단방향 구조가 [BATTLE_DESIGN §2.1 3계층 분리](../BATTLE_DESIGN.md)의 본질이다.

**참고**: Unity 공식 문서 — [Assembly Definitions](https://docs.unity3d.com/Manual/assembly-definition-files.html).

---

## 2. POCO / MonoBehaviour / ScriptableObject

Unity에 클래스를 만들 때 셋 중 하나로 시작하게 된다. 차이를 정리.

| 종류 | 정체 | Unity 의존 | 용도 | 예 |
|------|------|-----------|------|----|
| **POCO** | Plain Old C# Object — 그냥 `class`/`struct` | 없음 | 게임 로직, 데이터 모델 | `Actor`, `BattleEngine` |
| **MonoBehaviour** | Unity의 컴포넌트 베이스 | 강함 (씬·GameObject 필요) | 입력·렌더·코루틴, **씬 안에서 살아있는 것** | `BattleSceneController` |
| **ScriptableObject** | Unity의 에셋 베이스 | 중간 (.asset 파일로 직렬화) | 콘텐츠 데이터(스킬/적/재능) | `SkillData`, `EnemyData` (Phase 3+) |

**왜 가르나**

MonoBehaviour는 `new`로 만들 수 없고(반드시 `AddComponent`), 테스트 시 GameObject·Scene 셋업이 필요하다. 즉 **단위 테스트가 느리고 번거롭다**. 핵심 룰을 MonoBehaviour에 넣으면 EditMode 테스트로 검증할 수 없다.

→ 그래서 룰은 **POCO**에, Unity와의 접점만 **MonoBehaviour**에 둔다. 이 프로젝트의 `BattleEngine`은 POCO이고, `BattleSceneController`만 MonoBehaviour인 이유.

ScriptableObject는 **데이터 자산**이다. 코드 안에 적의 HP를 하드코딩하지 않고 `EnemyData.asset` 파일에서 읽어 디자이너(혹은 본인)가 코드 빌드 없이 수정할 수 있게 한다.

---

## 3. 결정적 RNG (Deterministic RNG)

> **사용자 질문에 대한 답**.

**RNG**는 Random Number Generator(난수 생성기)의 약자.

**결정적(deterministic)** = "같은 입력이면 항상 같은 출력". 컴퓨터의 난수는 사실 진짜 난수가 아니라 **수학 공식으로 만들어진 숫자열**이다. 공식의 시작점(=**시드**, seed)이 같으면 매번 같은 수열이 나온다.

```csharp
var a = new System.Random(12345);
var b = new System.Random(12345);

a.Next();  // 예: 1387523891
b.Next();  // 예: 1387523891  ← 항상 같음
```

이걸 일부러 활용하는 설계가 **결정적 RNG**이다. "결정적"이라는 단어가 어색하면 **재현 가능한 RNG** / **시드 고정 RNG** 정도로 풀어도 된다.

**왜 쓰나 (이 프로젝트의 이유)**

1. **버그 재현** — "그 전투에서 갑자기 죽었어!" 했을 때 시드만 알면 정확히 같은 전투를 다시 돌려볼 수 있다. 시드 없으면 매번 다른 결과라 버그가 사라진다.
2. **테스트** — `Assert.AreEqual(expected, actual)` 류 테스트가 가능. RNG가 매번 다르면 "기댓값"을 적을 수 없다.
3. **리플레이/저장** — 시드와 입력 시퀀스만 저장하면 전투 전체를 재생 가능 (디스크 절약, 디버깅 무기).
4. **밸런스 분석** — 같은 시드로 1000번 시뮬레이션 돌려도 같은 결과 → 코드 변경의 영향을 격리해서 측정 가능.

**규칙 (이 프로젝트가 정한 것)**
- 모든 무작위는 `IRngService` 한 곳만 통과한다 ([BATTLE_DESIGN §2.4](../BATTLE_DESIGN.md)).
- `UnityEngine.Random`, `System.Random` 직접 호출 금지 — 시드 통제가 안 되기 때문.
- `Time.deltaTime`도 같은 이유로 금지 (실행 환경에 따라 dt가 달라지면 결정성 깨짐) → `ITickService`가 0.05f 고정값을 흘려보낸다.

**용어 표**
| 용어 | 의미 |
|------|------|
| 시드 (seed) | 난수 생성기의 시작점. 같은 시드 → 같은 수열. |
| PRNG | Pseudo-Random Number Generator. 우리가 쓰는 모든 컴퓨터 RNG는 사실 PRNG. |
| 결정적 (deterministic) | 같은 입력 → 같은 출력 보장. |
| 비결정적 (non-deterministic) | 매번 다른 결과 (예: `new Random()` 무인자 — 시간 기반 시드). |

---

## 4. 의존성 역전 / 인터페이스 기반 설계

`BattleEngine`은 `UnityTickService`를 직접 만들지 않는다. 대신 `ITickService` **인터페이스**만 받는다:

```csharp
public BattleEngine(ITickService tick) { ... }
```

이 덕분에:
- 런타임에는 `UnityTickService`(Update 누산) 주입.
- 테스트에는 `MockTickService`(수동 펌프) 주입 → Unity 없이도 Engine을 돌릴 수 있음.

이 패턴은 **의존성 역전 원칙(DIP, Dependency Inversion Principle)**의 적용이다. "고수준 모듈은 저수준 모듈에 의존하지 않고, 둘 다 추상(인터페이스)에 의존한다." 클린 아키텍처의 핵심 아이디어 중 하나.

**관용구**: "구체 클래스 대신 인터페이스를 받아라" = "Mock으로 갈아끼워 테스트할 수 있게 하라".

---

## 5. Unity Test Framework — EditMode vs PlayMode

| 구분 | 실행 환경 | 속도 | 용도 |
|------|----------|------|------|
| **EditMode** | Editor 안, Play 모드 안 들어감. GameObject/Scene 없음. | 매우 빠름 (밀리초) | POCO 단위 테스트, 룰 검증 |
| **PlayMode** | 실제 Play 모드 진입, Scene 로드, MonoBehaviour 살아있음 | 느림 (초 단위) | 통합 테스트, 입력·렌더 |

Phase 0 테스트가 EditMode인 이유 — `BattleEngine`/`RngService`는 POCO라 Scene이 필요 없고, MockTickService로 Tick을 수동 펌프하면 Unity 없이도 충분.

**핵심 어트리뷰트**:
- `[Test]` — 일반 테스트 (EditMode 기본).
- `[TestCase(arg1, arg2)]` — 매개변수화 테스트.
- `[UnityTest]` — 코루틴 기반 PlayMode 테스트 (필요 시).

---

## 6. Tick 추상화 — 왜 `Time.deltaTime`을 직접 안 쓰는가

게임 코드에서 가장 자연스러운 한 줄:
```csharp
position += speed * Time.deltaTime;
```

문제는 `Time.deltaTime`이:
- **실행 시점마다 다른 값** (프레임레이트에 따라).
- Unity 런타임에서만 의미가 있음 — 단위 테스트에서 호출하면 0이거나 예외.
- Pause/Resume·시간 정지(MindGame)·결정성 시뮬레이션의 통제권을 빼앗음.

해결: 시간을 **인터페이스로 추상화**한다.

```csharp
public interface ITickService { event Action<float> OnTick; ... }
```

- 런타임 구현은 `Update` 안에서 0.05초마다 OnTick 호출.
- 테스트 구현은 `PumpTicks(100)` 호출로 100번 호출 → 100×0.05=5초 시뮬레이션.
- MindGame 페이즈 일시정지는 `Pause()` 한 줄로 끝.

이게 [BATTLE_DESIGN §2.4 결정성](../BATTLE_DESIGN.md)의 핵심 도구 중 하나.

---

## 7. readonly struct — 왜 쓰나

`BattleSnapshot`은 `readonly struct`로 선언했다.

- **`struct`** — 클래스가 아닌 값 타입. 복사로 전달됨. 불변하게 만들면 동시성/이벤트 발생 시 받는 쪽이 마음대로 수정 못 함.
- **`readonly`** — 모든 필드가 `readonly`임을 컴파일러가 강제. 나중에 누가 필드 추가하면서 가변으로 만드는 사고 방지.

View가 `BattleSnapshot.actors[i].hp = 0`을 시도해도 컴파일 에러 → 단방향 데이터 흐름이 컴파일러로 강제됨.

> 단점: 큰 struct는 복사 비용. 그래서 `BattleSnapshot`은 가능한 한 가볍게 유지하고, `ActorView` 배열은 read-only 구조 사본으로.

---

## 8. 네임스페이스 컨벤션

이 프로젝트:
```
MurimRunaway.Battle.Domain
MurimRunaway.Battle.Engine
MurimRunaway.Battle.View
MurimRunaway.Battle.Tests
```

**`Company.Module.Layer`** 패턴. 일반적인 .NET 컨벤션을 따른다. asmdef 이름과 네임스페이스가 일치하면 IDE의 자동 import가 깔끔하다 (`rootNamespace` 필드가 그 역할).

---

## 9. 상태 객체 전달 방식 — 왜 매 틱 `new`인가, 언제 바꾸는가

### 결정
`BattleState`는 `readonly struct`로 만들고, Engine이 매 틱 `new BattleState(...)`로 새 인스턴스를 만들어 `event Action<BattleState>`로 View에 전달한다.

### 대안과 트레이드오프
"엔진이 단일 mutable struct를 들고, `in`(`ref readonly`) 파라미터를 가진 커스텀 delegate로 호출" 패턴도 문법적으로 가능하다.

```csharp
public delegate void BattleStateHandler(in BattleState state);
public event BattleStateHandler OnStateChanged;
// ...
OnStateChanged?.Invoke(in _state);   // 복사 0, 박싱 0
```

| 항목 | 매 틱 `new` (현재) | 단일 인스턴스 + `in` 전달 |
|------|------------------|-------------------------|
| 매 틱 비용 | struct 크기만큼 복사 | 포인터(8B) 전달 |
| 힙 할당 | 0 | 0 |
| 코드 단순성 | `Action<T>` 표준 | 커스텀 delegate 정의 필요 |
| 구독자가 값을 **저장**할 수 있나? | 가능 (값 타입이라 안전) | 불가 (`in`은 그 호출 동안만 유효) |
| 멀티 구독자·로깅·비동기 | 안전 (각자 독립 사본) | 즉시 처리해야 함 |

### 임계점 — 언제 바꿀 가치가 있는가
**프레임 레이트가 빠르다고 자동으로 바꿔야 하는 게 아니다.** 60Hz든 240Hz든 8B struct 복사는 프로파일러에 잡히지 않는다. 결정 요인은:

1. **struct 크기**: 대략 ~64B (필드 ~8개) 이하면 매 틱 복사가 사실상 무료. 100B를 넘기 시작하면 `in` 전달이 의미를 가짐.
2. **이벤트 발생 빈도 × 구독자 수**: 한 틱 한 구독자에 8B는 0이지만, 100명에게 1KB를 60Hz로 뿌리면 다른 얘기.
3. **프로파일러 측정값**: 추측이 아니라 빨간불이 켜진 후에 바꾼다.

### 한 줄 요약
> 현재 구조는 도메인 객체가 작은 동안(대략 ~64바이트, 필드 8개 이하) 옳다. 깨지는 신호는 "프레임 레이트가 빠르다"가 아니라 **"struct가 커졌다"** 또는 **"프로파일러에 잡힌다"** 이다.

### 미래의 본인을 위한 메모
필드 수가 8개를 넘기 시작하거나, View 외에 로깅/리플레이/네트워크 구독자가 추가되는 시점에 이 섹션을 다시 읽어볼 것. 그때도 "구독자가 값을 저장하느냐"가 결정의 축이 된다 — 저장이 필요하면 값 타입 유지, 즉시 처리만 하면 `in` 전달 검토.

---

## 10. 학습 체크 — Phase 0 마치고 답할 수 있어야 할 것들

- [ ] asmdef를 안 쓰면 어떤 문제가 생기는가? (둘 이상)
- [ ] POCO와 MonoBehaviour를 가르는 기준은?
- [ ] "결정적 RNG"가 무슨 뜻인지 다른 사람에게 설명할 수 있는가?
- [ ] `BattleEngine`이 `UnityTickService` 대신 `ITickService`를 받는 이유는?
- [ ] EditMode 테스트와 PlayMode 테스트의 선택 기준은?
- [ ] `BattleState`를 `readonly struct`로 만든 이유는?
- [ ] `BattleState`를 매 틱 `new`로 만드는 게 왜 비싸지 않은가? 언제 비싸지는가?

---

## 11. 더 깊이 파고 싶다면

| 주제 | 추천 키워드 |
|------|------------|
| 클린 아키텍처 일반 | "Clean Architecture" — Robert C. Martin |
| Unity asmdef 심화 | Unity Manual — Assembly Definitions, Roslyn Analyzers |
| 결정성 시뮬레이션 | "Lockstep simulation", "Deterministic game logic" |
| TDD in Unity | "Unity Test Framework", "EditMode test patterns" |
| DI in Unity | "Zenject", "VContainer" — 필요해지면. Phase 0~4 단계에선 수동 주입으로 충분. |
