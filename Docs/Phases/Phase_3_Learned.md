# Phase 3 — 배운 기술 / 개념 정리

> [!tip]
> Phase 3의 **작업 절차**는 [[Phase_3_Guide]]. 본 문서는 그 작업에서 등장한 **개념·용어·설계 결정의 이유**를 정리한다. 학습 노트.

---

## 1. 시스템 분리 (`IBattleSystem`) — Tick 로직을 왜 쪼개나

**한 줄 정의**: 매 Tick 한 단계씩 상태를 진행시키는 로직을, 한 `HandleTick` 안의 분기 더미가 아니라 **순서 있는 작은 시스템들의 배열**로 나누는 패턴(ECS의 system 개념을 축소판으로).

Phase 1: `HandleTick` → `TickMovement` + `TickEngagementCheck` (분기 2개, 한 메서드가 호출).
Phase 3: `MovementSystem` / `EngagementSystem` / `CastingSystem` 3개 + `BattleEngine`은 `foreach (system) system.Tick(...)` dispatcher.

**왜 지금 쪼개는가 (N=2→3 트리거)**:
- Phase 1엔 tick mutation이 2개라 메서드 분리로 충분했다. 메모리 `project_tick_split`이 잡아둔 임계 — *"Tick~~ 함수가 늘고 내부 상태를 보유하면 시스템 클래스로"* — 가 Phase 3에서 3번째(자동 시전)가 들어오며 넘는다.
- **god class 예방이 핵심**. Phase 5 강공, Phase 6 오의 매트릭스가 전부 `BattleEngine.HandleTick`의 if 더미로 쌓이면 한 클래스가 600줄+가 된다. 시스템 경계를 지금 그으면 이후 Phase는 "새 시스템 파일 + 배열에 한 줄"로 끝난다.

**대안 — partial class**: `BattleEngine`을 `partial`로 쪼개 파일만 나눌 수도 있다. 그런데 그건 *물리적* 분리지 *논리적* 분리가 아니다 — 여전히 한 클래스라 상태가 다 섞이고 테스트도 통으로만 가능. `IBattleSystem`은 각 시스템이 `BattleContext`만 받는 순수 함수꼴이라 추론 단위가 작아진다.

**언제 이 패턴이 과한가**: tick mutation이 1개뿐이면(Phase 1) 시스템 인터페이스는 비용만 크다. 추상화는 두세 번째가 보일 때 — Scripts/CLAUDE.md §1. Phase 1에서 안 한 게 맞고 Phase 3에서 하는 게 맞다.

---

## 2. `BattleContext` (가변 공유 상태) vs `ActorView` (읽기 전용 사본) — 정반대 두 객체

같은 전투 데이터를 두 가지 모양으로 들고 있다:

| | `BattleContext` | `ActorView`/`BattleSnapshot` |
|--|----------------|------------------------------|
| 타입 | `class` (참조) | `readonly struct` (값) |
| 가변성 | 시스템들이 매 Tick **변경** | 생성 후 **불변** |
| 방향 | Engine 내부에서 공유 | Engine → View 단방향 송신 |
| 목적 | 시스템들이 같은 상태를 같이 민다 | View가 그리는 동안 Engine이 다음 Tick 진행해도 안전 |

**왜 `BattleContext`는 class여야 하나**: `MovementSystem`이 `context.Player.Position`을 바꾸면 그 변경을 `EngagementSystem`·`CastingSystem`이 같은 Tick에 봐야 한다. struct면 시스템마다 사본이 복사돼 변경이 전파 안 된다. "여러 협력자가 한 상태를 순서대로 민다" = 참조 공유 = class.

**왜 `ActorView`는 struct여야 하나**: 정반대 요구. View로 넘어간 뒤엔 누구도 안 바꿔야 한다. Phase 1 [[Completed_Phase_1_Learned]]에서 이미 정리한 "스냅샷 일관성"의 연장.

이 대비 자체가 학습 포인트다 — **"가변성은 객체의 속성이 아니라 그 객체가 맡은 역할의 함수"**. 같은 액터 데이터가 한쪽(Context)에선 가변, 다른쪽(View)에선 불변이다.

---

## 3. 결정 vs 실행의 분리 — `CastingSystem` ≠ `ISkillExecutor`

`CastingSystem`은 **무엇을 쏠지** 결정하고(슬롯 순회·skip 조건), `ISkillExecutor.TryCast`는 **실제로 쏘는** 일(내공 차감·쿨 설정·기세 획득·이벤트)을 한다.

**왜 한 곳에서 다 안 하나**:
1. **단일 진입점 재등장**. Phase 2 `IResourceMutator`가 "자원 변경의 단일 진입점"이었듯, `ISkillExecutor`는 "시전의 단일 진입점". Phase 6 오의는 결정 트리가 아니라 핫키로 시전하지만 **같은 `TryCast`를 통과**해야 비용·이벤트 규약이 한 곳에서 강제된다. 호출처(트리/핫키) ≠ 실행처(executor).
2. **atomic 경계가 명확**. `TryCast`는 "내공 부족 시 아무 일도 안 일어남"을 한 메서드 안에서 보장(Phase 2 `SpendMana` atomic의 연장). 결정 코드가 비용까지 만지면 "깎고 → 실패 → 롤백"이 흩어진다.
3. **테스트 가능성**. 결정 트리 검증은 `OnSkillCast` 시퀀스만 보면 된다 — 실행 디테일과 분리돼 있어 "Mid 거리면 Mid 무공" 같은 단언이 깔끔.

`BattleEngine`이 `IResourceMutator`·`ISkillExecutor`를 **둘 다** 구현하고 같은 인스턴스로 노출하는 것도 Phase 2 패턴 그대로(`.AsSelf().As<IResourceMutator>().As<ISkillExecutor>()`).

---

## 4. 결정 트리 = 우선순위 선택 + 한 Tick 한 시전

자동 시전의 본질은 "여러 후보 중 **첫 적격자**를 고른다"이다.

- **슬롯 순서 = 우선순위**. 사용자가 슬롯을 재배치해 우선순위를 표현한다(SSOT Edge). 정렬·점수화 없음 — 순회하다 처음 통과하는 슬롯에서 `break`.
- **한 Tick 한 시전(`break`)**. 두 무공이 동시에 조건을 만족해도 한 Tick엔 하나만. 이유: (a) 결정성 — "동시"의 순서를 정의해야 하는데 슬롯 순서가 그 정의, (b) Phase 5 강공 동시발동 정책과 정합, (c) 시각적으로도 한 박자에 한 무공이 자연스럽다.

**대안 — 점수 기반 선택**(각 스킬에 효용 점수 매겨 최대 선택)도 오토배틀에서 흔하다. 안 쓴 이유: Phase 3 범위에선 "슬롯 순서"라는 사용자가 직접 쥐는 우선순위가 더 단순하고 예측 가능. 점수화는 카드/시너지(Phase 12)가 들어와 "상황 의존 우선순위"가 필요할 때 재검토.

---

## 5. 결정성이 RNG 없이 "구조적으로" 성립한다는 것

Phase 3 결정 트리엔 난수가 한 줄도 없다. 그래서 같은 (seed, 슬롯, 적) → 같은 `OnSkillCast` 시퀀스가 **자동으로** 성립한다(쿨다운·내공·거리는 전부 결정론적 누적).

그럼 [[Phase_3_Guide]] §5.2 결정성 테스트는 "당연한 걸 검사"하는 무의미한 테스트인가? **아니다.** 이 테스트의 가치는 *현재*가 아니라 *미래*에 있다:
- 누군가 트리에 "가끔 슬롯을 무작위로 흔들기"(`IRngService` 호출)를 끼워 넣는 순간 이 테스트가 **즉시 빨개진다**.
- 즉 이 테스트는 결정성 검증이라기보다 **"결정 트리에 비결정성이 침투했다"는 회귀 알람**이다.

Phase 1 [[Completed_Phase_1_Guide]] §4.2가 "결정성 테스트는 RNG 실사용 Phase에서 도입"이라 미뤄둔 그 자리가 여기다. `IRngService`가 `BattleContext`에 들어왔지만(회피=Phase 9 대비) 트리에선 안 쓴다 — 들고만 있는 의존성.

> [!note]
> 교훈: **"통과가 보장된 테스트"와 "무의미한 테스트"는 다르다.** 불변식을 고정하는 테스트는, 그 불변식이 깨질 변경이 들어올 때 비로소 일한다.

---

## 6. 기세(momentum) 도입 시점이 왜 Phase 3인가

[[Completed_Phase_2_Learned]]는 "자원 변경 단일 진입점"을 Phase 2에 만들면서 기세는 의도적으로 뺐다(v0.4.2 스코프 컷).

**왜 Phase 2가 아니라 Phase 3인가**: 기세는 **단독으론 의미 없는 자원**이다. "쌓아서 무언가에 쓴다"의 짝이 있어야 자원으로 성립한다. Phase 2엔 그 짝(스킬 시전)이 없어 기세는 그냥 안 변하는 0이었을 것. Phase 3에서 `SkillData.MomentumGainOnCast`(시전 시 +1) + Phase 6 오의 소비가 짝을 이루며 비로소 자원이 된다. **자원은 그것을 움직이는 메커닉과 한 Phase에 같이 태어나야 한다** — 컨테이너만 먼저 만들면 검증할 행동이 없다.

`IResourceMutator`에 `SpendMomentum`/`GainMomentum`을 더하는 건 Phase 2 `SpendMana`/`GainMana`의 비대칭(소비 atomic 거절 / 획득 silent 클램프)을 그대로 복제 — 같은 모양의 자원은 같은 규약을 갖는 게 인지 부하를 줄인다.

---

## 7. SSOT 산문 ≠ 코드 — 충돌 3종을 어떻게 판단했나

SSOT는 사양의 진실이지만 **표기는 산문**이라 코드 컨벤션과 어긋날 수 있다. Phase 3에서 3건([[Phase_3_Guide]] §0.1):

| | SSOT 산문 | 판단 | 근거 |
|--|-----------|------|------|
| C-1 | 병렬 배열 `skillSlots[]`+`skillCooldowns[]` | `SkillSlot`로 번들 | `feedback_no_parallel_arrays` — 인덱스 동기화 주석은 침묵 버그 |
| C-2 | `SkillType` enum | `SkillCategory`로 개명 | `feedback_enum_convention` — `Type`/`Enum` 접미사 금지 |
| C-3 | `IsInPreferredRange(target.position…)` | **거리**로 재해석 | 0~100 축 의미 — 절대 좌표론 게이팅 붕괴 |

**판단 원칙**: SSOT가 정하는 건 *동작*(병렬 배열로 적힌 두 값이 슬롯·쿨이라는 사실, Type enum이 4분류라는 사실, 거리대로 게이팅한다는 사실)이지 *표현*이 아니다. 표현은 프로젝트 컨벤션이 정한다. 그래서:
- C-1/C-2는 **동작 보존 + 표현만 컨벤션화** → 가이드가 조용히 컨벤션을 따르고 노트만 남김(루트 CLAUDE.md §1: 표기 변경은 추후 SSOT도 같은 방향으로 정합).
- C-3은 **산문이 모호**(`target.position`이 절대 좌표인지 거리인지)했고, 0~100 축에서 적이 멀리 spawn하고 Player가 진군하는 모델상 "거리"로만 의미가 산다. 이건 단순 표기가 아니라 *의미 해석*이라 가이드에 가장 크게 박아 두고 SSOT 정합을 명시 권고.

> [!note]
> 교훈: 가이드는 SSOT의 *필사*가 아니다. "사양은 SSOT, 표현은 컨벤션, 모호하면 해석을 명시하고 SSOT에 되돌린다."

---

## 8. ActorView 분리 — "숫자는 충족, 이득은 음수"

메모리 `project_actorview_split` 트리거: *진영 전용 필드 2개+ 또는 IsPlayer 분기 3곳+*. Phase 3에서 Player 전용 필드가 6개(Mana·MaxMana·Momentum·MaxMomentum·Skills…)가 돼 **숫자상 트리거는 충족**된다. 그런데 [[Phase_3_Guide]] §2.7은 분리를 **보류**했다.

**왜 트리거 충족인데 안 하나**: 트리거는 "검토하라"는 신호지 "무조건 하라"가 아니다. 검토해 보니 *지금 분리의 순이득이 음수*다:
- `EnemyView`에 들어갈 Enemy 전용 필드가 **0개** — 분리해도 `EnemyView`는 base와 동일, 이득 없음.
- 미래에 양쪽 공통 필드(예: `BuffSlots`)가 생기면 분리된 둘을 **재병합**해야 하는 역비용.
- 진짜 이득이 양수가 되는 지점 = Phase 5 보스전(Enemy 전용 `IsBoss`/`PatternType` 등장). 그때 양쪽 다 전용 필드를 가져 분리가 비로소 값을 한다.

이건 [[Completed_Phase_2_Learned]] §5가 이미 내린 판단을 잇는 것 — Phase 2 때 "Phase 5가 자연스러운 검토 시점"이라 적어둔 약속을 Phase 3에서 지킨다.

> [!note]
> 교훈: **트리거는 알람이지 명령이 아니다.** "조건 충족 → 즉시 실행"이 아니라 "조건 충족 → 이득/비용 재계산 → 결정". 숫자만 보고 기계적으로 분리하면 YAGNI 위반.

---

## 9. YAGNI 적용 3사례 — "안 만든 것"의 목록

Phase 3에서 *의도적으로 안 만든* 것들. 각각 "지금 만들면 데드 코드"라는 공통 이유.

1. **`SkillEffect` 타입** — SSOT 표에 `effects: SkillEffect[]`가 있으나 "Phase 4 정의" 단서. 빈 타입을 지금 만들면 `TryCast`가 참조만 하고 아무것도 안 하는 죽은 코드. Phase 4 데미지와 함께.
2. **심법 중복 skip 분기** — 결정 트리의 *"Simbeop이고 효과 활성이면 skip"* 은 "효과 활성"을 판정할 효과 시스템(Phase 4)이 있어야 의미. 지금 넣으면 항상 false인 분기.
3. **시스템의 VContainer 등록** — 세 시스템을 컨테이너에 안 올렸다. 독립 교체·독립 Mock 수요가 아직 없고(엔진 통합 테스트로 검증), 올리면 `BattleEngine ↔ CastingSystem` DI 순환만 생긴다. 인터페이스/등록은 두 번째 구현이나 Mock이 필요할 때.

공통 원리: **"인터페이스가 참조하니까/SSOT에 적혔으니까 미리 만든다"는 YAGNI 위반.** 참조처가 *실제로 동작*할 Phase에 같이 만든다.

---

## 10. 전이 의미 변경(Resolve→Engage)은 회귀의 "정상 동작"

Phase 1/2에서 적 도달 = `Resolve`(즉시 승리, 전투 끝). Phase 3에서 적 도달 = `Engage`(교전 시작, 전투 계속). 이 변경으로 Phase 1 테스트 2개가 깨진다.

**이걸 어떻게 보나**: "테스트가 깨졌으니 기대값을 무르게 고친다"가 아니다. SSOT가 **사양 자체를 바꿨다**(Phase 1의 Resolve는 "할 게 없어서 끝"이라는 placeholder였고, Phase 3에서 진짜 의미인 Engage로 승격). 테스트 기대값이 새 사양을 따라가는 것 = **회귀 추적의 정상 동작**.

> [!note]
> 교훈: 테스트가 빨개졌을 때 두 가지가 있다 — (a) 코드가 사양을 어겼다(고칠 건 코드), (b) 사양이 바뀌었다(고칠 건 기대값). 구분 기준은 "SSOT가 뭐라 하는가". Phase 3은 (b). 단, (b)일 때도 기대값을 *조용히* 바꾸지 말고 "왜 바뀌었나"를 커밋/노트에 남긴다(`feedback_guide_code_rigor`).

---

## 11. 학습 체크 — Phase 3 마치고 답할 수 있어야 할 것들

- [ ] `IBattleSystem`으로 Tick 로직을 쪼갠 이유와, Phase 1에서 안 한 이유?
- [ ] `BattleContext`가 class이고 `ActorView`가 struct인 — 정반대인 이유?
- [ ] `CastingSystem`(결정)과 `ISkillExecutor`(실행)를 나눈 3가지 이유?
- [ ] "슬롯 순서 = 우선순위"와 "한 Tick 한 시전(break)"이 결정성과 어떻게 엮이나?
- [ ] RNG가 없어 당연히 통과할 결정성 테스트를 그래도 두는 이유?
- [ ] 기세를 Phase 2가 아니라 Phase 3에 도입한 이유?
- [ ] SSOT 산문과 코드 컨벤션이 충돌할 때(C-1~C-3) 무엇을 보존하고 무엇을 바꾸나?
- [ ] ActorView 분리 트리거가 숫자상 충족인데 보류한 판단의 근거?
- [ ] Phase 3에서 *안 만든* 3가지(SkillEffect/심법skip/시스템DI)와 그 공통 이유?

---

## 12. 더 깊이 파고 싶다면

| 주제 | 추천 키워드 |
|------|------------|
| 시스템 분리 / ECS | "Entity Component System", "system update order", Unity DOTS 개념 |
| 결정 트리 / 행동 선택 | "Behavior Tree", "utility AI" (점수 기반 대안), "priority list AI" |
| 결정론 시뮬레이션 | "deterministic lockstep", "fixed timestep simulation" |
| 단일 진입점 / Command | "Command pattern", "Aggregate root" (DDD) |
| 가변/불변 상태 모델링 | "value vs reference semantics", "snapshot isolation" |
