# Phase 1 — 배운 기술 / 개념 정리

> Phase 1의 **작업 절차**는 [Phase_1_Guide.md](Phase_1_Guide.md). 본 문서는 그 작업에서 등장한 **개념·용어·설계 결정의 이유**를 정리한다. 학습 노트.

---

## 1. 1D 거리축이란?

**한 줄 정의**: 캐릭터의 위치를 (x, y) 2D 좌표가 아니라 **거리 하나의 숫자**로만 표현하는 좌표계.

```
Player(0) ─────────────────── Enemy(100)
            거리 0 ~ 100
```

- 플레이어는 항상 `position = 0`에 고정. 적은 100에서 출발해 정지 위치(stopPosition, 보통 20)까지 다가옴.
- 사이드스크롤 오토배틀(옵시디언 나이트 형)과 호환되는 가장 단순한 모델.

**왜 1D인가**
1. **결정성 보장이 쉽다** — 2D 좌표는 부동소수점 누적 오차가 두 축에서 동시에 생긴다. 1D는 한 축만 신경 쓰면 됨.
2. **자동 전투에 충분** — "거리에 따라 사거리 안인가" 판단이 모든 무공 룰의 본체. xy 자유도는 오토배틀에선 의미 없음.
3. **시각화는 별개** — 화면에는 2D로 그리되 시뮬레이션은 1D. View와 Domain의 분리를 유지하는 자연스러운 결과.

**한계**
- 측면 회피·집단 진영 같은 표현은 못 함. v1.0 범위에서는 불필요 — 이런 게 필요한 시점에서 좌표계를 확장.

---

## 2. FSM (Finite State Machine, 유한 상태 기계)

**한 줄 정의**: 객체가 가질 수 있는 상태를 **유한한 enum 값들**로 정의하고, 그 사이 **전이 규칙**을 명시한 구조.

Phase 1의 Actor FSM:
```
spawn → Approaching → (pos ≤ stopPosition) → Idle
```

- `ActorState.Approaching`과 `ActorState.Idle`만 사용. `Casting`/`HeavyCharging`/`Stunned`/`Dead`는 enum에 선언만 — 후속 Phase가 채움.

**왜 if문 더미가 아니라 FSM인가**

순진하게 짜면:
```csharp
if (enemy.isMoving && !enemy.isAttacking && enemy.hp > 0 && ...) { ... }
```
복잡해질수록 조합 폭발. **상태가 N개, 전이가 M개**라고 명시하면:
- 코드: `switch (state)` 한 번으로 분기.
- 디버그: 현재 상태 한 값만 보면 액터가 뭘 하는지 안다.
- 테스트: 상태별 단위 테스트 가능.

**무림도망자에서의 위치**
- Actor FSM(이 Phase): 액터 한 명의 상태.
- Battle FSM([BATTLE_DESIGN §3 Phase 1](../BATTLE_DESIGN.md)): 전투 전체의 흐름(Setup → Approach → Engage → Resolve).

두 FSM은 독립이지만 연결됨 — 예: 모든 적이 `Idle`이면 Battle FSM이 `Resolve`로 전이.

---

## 3. ScriptableObject — 왜 EnemyData가 SO인가

Phase 0 Learned §2에서 셋의 차이는 다뤘다. 본 Phase에서 처음으로 **실제로 SO를 만든다** — EnemyData. 다시 한 번:

**MonoBehaviour는 씬 안에 살아있는 것** — 코루틴, 입력, 렌더링.
**ScriptableObject는 에셋 파일** — 적 종류, 무공 정의, 카드 효과 같은 **콘텐츠 데이터**.
**POCO는 룰** — `Actor`, `BattleEngine` 등 게임 로직.

**EnemyData가 SO여야 하는 이유**:
1. Inspector에서 수치를 바꾸면 **코드 빌드 없이** 반영. 밸런스 작업 시 결정적.
2. 같은 적 종을 여러 전투·여러 스테이지에서 공유 가능 (`Enemy_Goblin.asset` 하나, 사용처 N곳).
3. 디스크에 저장되므로 git diff로 변경 이력이 따라옴.

**주의**: 런타임에 SO 필드를 직접 변경하면 에디터에서는 **디스크에 반영**된다 (플레이 모드 종료 후에도 남음). 그래서 Engine은 SO 값을 Actor 인스턴스에 복사한 뒤 Actor만 mutate한다.

---

## 4. dt 누적과 결정성 — 왜 `position -= speed * dt`가 안전한가

진군 코드:
```csharp
enemy.Position -= ApproachSpeedFor(enemy) * dt;
```

여기서 `dt = 0.05f` 고정. `Time.deltaTime`이 아니다 — 그래서 결정적이다.

**부동소수점 누적 오차는 어떻게 되는가**
- `position`은 매 틱 `0.25` (= 5 × 0.05) 씩 감소. 누적은 정확히 같은 곱셈을 N번 반복 → **같은 시드는 같은 결과**.
- 다른 머신에서 같은 코드를 돌려도 IEEE 754가 보장하는 한 결과 일치.

**Edge — 큰 dt 보호**
에디터 일시정지 후 재개하면 한 프레임에 dt가 크게 누적될 수 있다. 그래서 가이드에 두 줄:
```csharp
if (enemy.Position <= stopPosition)
{
    enemy.Position = stopPosition;  // 클램프
    enemy.State = ActorState.Idle;
}
```
position이 stopPosition 아래로 흘러내려가는 걸 막는다. 클램프 없이는 같은 시드라도 dt 패턴에 따라 다른 position이 기록 → 결정성 깨짐.

>
> 그런데 우리는 `ITickService`가 dt를 0.05초 단위로 잘게 쪼개 펌프하므로, 실제론 한 번의 `HandleTick(dt)` 호출의 dt는 항상 0.05f다. 클램프는 안전망.

---

## 5. `BattleSnapshot`이 매 틱 새 배열을 할당해도 되는가

가이드의 `PublishSnapshot`은 매 틱:
```csharp
var actors = new ActorView[1 + _enemies.Length];
```

Phase 0 Learned §9에서 다뤘듯, struct 본체는 매 틱 `new`가 무료에 가깝다. 그런데 **배열은 힙 할당**이다. 이건 다른 얘기.

**Phase 1 기준 판단**
- 적 4명 = 5크기 배열, 0.05초마다 = 초당 20회. → 초당 ~100 ActorView 슬롯. GC에 부담 없음.
- Phase 11(런 구조)에서 적 수가 늘거나 1틱당 여러 스냅샷이 필요해지면 **Object Pool** 검토.

**현재 임계점**
- 적 ≤ 8, 틱 0.05초 → 그대로 가도 됨.
- 적 ≥ 30 또는 1틱당 N스냅샷 필요 → Pool 도입.

판단 기준은 항상 **프로파일러 측정값**. "느낄 것 같아서" 미리 Pool 도입하지 않는다 (YAGNI).

---

## 6. id 오름차순 직렬화 — 결정성의 디테일

가이드 §1.7:
> `actors`는 id 오름차순 정렬 보장 (Edge 케이스 — 동일 position 적 N명의 결정성).

왜 정렬해야 하는가:
- 적 4명이 같은 spawnPosition에 있으면, `Dictionary` 같은 컬렉션은 **삽입 순서가 보장되지 않음**. 같은 시드라도 호출 순서가 미세하게 달라지면 직렬화 순서가 흔들림.
- 스냅샷 byte-equal 보장(I-1.3)은 actors 배열의 순서까지 포함. 따라서 **명시적 정렬 키**가 필요.
- 가장 단순한 키 = id. Setup 시점에 enemies를 i+1로 매기므로 자연스럽게 1, 2, 3, … 순.

**일반화**: 결정성 시뮬레이션은 "값이 같다"뿐 아니라 "**같은 순서로 직렬화된다**"까지 요구한다. 컬렉션 순회·해시 기반 자료구조를 통과시킬 때마다 이 점을 의식.

---

## 7. 진군 종료 = 게임 종료의 임시 정의

Phase 1의 Resolve(victory) 조건:
> 모든 적이 Idle 상태가 되면 BattlePhase = Resolve(victory) 송신 후 종료.

이건 **임시 정의**다. 실제 게임에선:
- 모든 적이 도착하면 → **전투 시작** (Engage 페이즈, Phase 3).
- 적이 모두 죽으면 → Resolve(victory) (Phase 4).
- 플레이어 HP가 0이면 → Resolve(defeat) (Phase 4).

Phase 1에서 "도착=승리"로 둔 이유는 **데미지·HP 변화가 아직 없기 때문**. Acceptance를 만들 무언가가 필요. 다음 Phase에서 자연스럽게 교체될 자리 — YAGNI 원칙의 응용.

---

## 8. `ref` 키워드 — `_enemies[i]`를 왜 `ref var`로 받았는가

가이드:
```csharp
for (int i = 0; i < _enemies.Length; i++)
{
    ref var enemy = ref _enemies[i];
    ...
    enemy.Position -= ...;
}
```

`Actor`는 `class`(참조 타입)다. 그러면 그냥 `var enemy = _enemies[i];`로 받아도 같은 인스턴스를 가리키므로 `enemy.Position` 변경이 원본에 반영된다.

→ **이 경우 `ref`는 불필요**. 가이드 코드에 `ref var`를 쓴 건 잘못. 일반적으로:
- `class`: 그냥 `var x = arr[i]` — 참조 복사.
- `struct`: 변경하려면 `ref var x = ref arr[i]` 필수. 안 그러면 값 복사본만 바뀜.

**기억할 것**: `Actor`를 나중에 `struct`로 바꾼다면 `ref var` 패턴이 필수가 된다. 지금은 class라 둘 다 동작하지만, **습관적으로 `ref var`를 쓰면 struct로 바뀐 날 자동으로 안전하다**. 트레이드오프는 "필요 없는 곳에서 ref를 쓰는 인지 부하". 본 프로젝트는 **명시적 단순함**을 우선해 class일 때는 그냥 `var`로 가는 게 맞다.

---

## 9. 학습 체크 — Phase 1 마치고 답할 수 있어야 할 것들

- [ ] 1D 거리축을 쓰는 이유 두 가지를 설명할 수 있는가?
- [ ] Actor FSM과 Battle FSM은 어떻게 연결되는가?
- [ ] EnemyData가 SO여야 하는 이유는?
- [ ] `pos = max(pos, stopPosition)` 클램프가 왜 필요한가? 없으면 무엇이 깨지는가?
- [ ] 매 틱 `new ActorView[N]` 배열 할당이 Phase 1에선 왜 문제 없는가? 언제 문제가 되는가?
- [ ] actors 배열을 id 정렬해야 하는 이유는?
- [ ] `class` 필드를 변경할 때 `ref var`가 필요한 경우와 그렇지 않은 경우는?

---

## 10. 더 깊이 파고 싶다면

| 주제 | 추천 키워드 |
|------|------------|
| FSM in games | "Game programming patterns — State", Robert Nystrom |
| ScriptableObject 활용 패턴 | Unity 공식 강의 — "Pluggable AI with ScriptableObjects" |
| 결정성 시뮬레이션 심화 | "Lockstep simulation", "Fixed timestep integration" |
| 부동소수점 누적 오차 | "What Every Computer Scientist Should Know About Floating-Point Arithmetic" |
| Object Pool | "Game programming patterns — Object Pool" |
