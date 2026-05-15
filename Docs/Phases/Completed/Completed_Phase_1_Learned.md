# Phase 1 — 배운 기술 / 개념 정리

> Phase 1의 **작업 절차**는 [Phase_1_Guide.md](Phase_1_Guide.md). 본 문서는 그 작업에서 등장한 **개념·용어·설계 결정의 이유**를 정리한다. 학습 노트.

---

## 1. 1D 거리축이란?

**한 줄 정의**: 캐릭터의 위치를 (x, y) 2D 좌표가 아니라 **거리 하나의 숫자**로만 표현하는 좌표계.

```
Player(0) ─────────────────── Enemy(100)
            거리 0 ~ 100
```

- Player는 `position = 0`에서 시작해 적 쪽(큰 position)으로 진군. 적은 `SpawnPosition`(예: 100)에 고정. Player가 가장 가까운 적과의 거리가 `AttackRange` 이하가 되는 순간 정지.
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

Phase 1에서 실제로 발생하는 전이는 **Player의 한 줄짜리**:
```
Player: Running ──(거리 ≤ AttackRange)──> Idle
Enemy:  Idle (고정, 전이 없음)
```

[BattleEngine.cs](../../Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs)에서:
- Setup 시 Player는 `Running`, Enemy는 모두 `Idle`로 시작.
- 매 틱 `TickMovement`는 `Player.State == Running`일 때만 Position을 증가.
- `TickEngagementCheck`가 nearest enemy와의 거리가 AttackRange 이하가 되는 순간 `Player.State = Idle`로 한 번 전이하고, BattlePhase를 `Resolve`로 보냄.

상태 전이가 사실상 1회뿐이라 "FSM"이라 부르기엔 과한 면이 있다. 그런데도 `ActorState`를 enum 6개(`Idle`/`Running`/`Casting`/`HeavyCharging`/`Stunned`/`Dead`)로 선언해 둔 이유:

**왜 if 플래그 더미가 아니라 enum + 상태인가**

순진하게 짜면 액터마다 `isMoving`/`isAttacking`/`isCasting`/`isStunned`/`isDead` 같은 bool 플래그가 늘어난다.
```csharp
if (actor.isMoving && !actor.isAttacking && !actor.isStunned && actor.hp > 0) { ... }
```
플래그 N개면 조합이 2^N. **"동시에 켜질 수 없는 상태"는 한 enum으로 묶는 게 안전**:
- 컴파일러가 "Running이면서 Stunned"같은 불가능 조합을 원천 차단.
- 디버그: `actor.State` 한 값만 보면 액터가 뭘 하는지 안다.
- 후속 Phase에서 `Casting`/`HeavyCharging`을 채울 때 분기 지점이 명확 (`switch (state)`).

**무림도망자에서의 위치**
- Actor 상태(이 Phase): 액터 한 명의 행동 모드.
- Battle 페이즈([BATTLE_DESIGN.md](../BATTLE_DESIGN.md)): 전투 전체 흐름(Setup → Approach → Engage → Resolve).

둘은 독립이지만 연결됨 — Phase 1에서는 Player가 `Idle`로 전이하는 그 순간 BattlePhase가 `Resolve`로 넘어간다.

**enum 미리 선언 vs YAGNI**
`Casting`/`HeavyCharging`/`Stunned`/`Dead`는 Phase 1에서 쓰지 않는데 미리 선언했다. 원칙상 YAGNI 위반처럼 보이지만, enum 멤버 추가는 **switch 망라 검사를 깨므로** 후속 Phase에서 추가될 때 일괄 컴파일 에러로 호출처를 강제로 갱신하게 만드는 게 의도. 빈 스텁 클래스를 미리 만드는 것(진짜 YAGNI 위반)과 다르다.

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

## 4. dt 누적과 결정성 — 왜 `position += speed * dt`가 안전한가

진군 코드 ([BattleEngine.TickMovement](../../Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs)):
```csharp
_player.Position += _player.MoveSpeed * deltaTime;
```

여기서 `deltaTime = 0.05f` 고정. `Time.deltaTime`이 아니다 — 그래서 결정적이다.

**부동소수점 누적 오차는 어떻게 되는가**
- `Position`은 매 틱 `0.25` (= 5 × 0.05) 씩 증가. 누적은 정확히 같은 곱셈을 N번 반복 → **같은 시드는 같은 결과**.
- 다른 머신에서 같은 코드를 돌려도 IEEE 754가 보장하는 한 결과 일치.

**Edge — 큰 dt 보호 (클램프)**
에디터 일시정지 후 재개하면 한 프레임에 dt가 크게 누적될 수 있다. `TickEngagementCheck`의 마지막 세 줄:
```csharp
_player.Position = nearest.Position - _player.AttackRange;  // 클램프
_player.State = ActorState.Idle;
_phase = BattlePhase.Resolve;
```
거리가 AttackRange 이하가 된 그 틱에 Player를 **정확히** `nearest.Position - AttackRange` 위치로 스냅한다. 클램프 없이 그냥 멈추기만 하면, 큰 dt가 들어왔을 때 Player가 적 사거리 안쪽으로 살짝 더 들어가서 멈춤 → 같은 시드라도 dt 패턴에 따라 최종 Position이 흔들림 → 결정성 깨짐.

>
> 그런데 우리는 `ITickService`가 dt를 0.05초 단위로 잘게 쪼개 펌프하므로, 실제론 한 번의 `HandleTick(dt)` 호출의 dt는 항상 0.05f다. 클램프는 안전망 — Tick 정책이 바뀌어도 시뮬레이션은 결정적으로 남는다.

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
> `actors`는 id 오름차순 정렬 보장.

실제 [PublishSnapshot](../../Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs)은 `Array.Sort` 같은 정렬 호출이 없다. 그런데도 "id 오름차순"이 성립하는 이유:
- Setup에서 `_enemies`를 만들 때 `Id = index + 1`로 매김. 배열 인덱스 순 = id 순.
- PublishSnapshot은 `actors[0] = _player.ToView()` 이후 `_enemies`를 인덱스 순으로 그대로 복사. → 자연스럽게 Player(id=0), Enemy(id=1, 2, 3, …) 순.

즉 **정렬 알고리즘으로 보장하는 게 아니라, 생성 시점의 invariant로 보장**한다. 이게 깨질 수 있는 시나리오:
- 전투 도중 적 추가(`Array.Resize` + 새 id) 후 다시 `_enemies`를 어떤 기준으로 재배치하면 인덱스 순 ≠ id 순이 될 수 있음.
- `_enemies`를 List로 바꾸고 Remove를 쓰면 빈 슬롯이 메꿔지며 마찬가지로 순서가 흔들림.

후속 Phase에서 이런 변화가 들어올 때는 PublishSnapshot에 명시적 `OrderBy(actor => actor.Id)`를 넣거나, 배열 구조 자체를 id-stable하게 유지(예: Dead는 제거 대신 상태만 마킹)해야 한다. 현재 코드는 후자를 택함 — Dead 액터도 배열에서 빼지 않고 `State = Dead`로만 마킹할 예정이라 자연 정렬이 유지된다.

**일반화**: 결정성 시뮬레이션은 "값이 같다"뿐 아니라 "**같은 순서로 직렬화된다**"까지 요구한다. 컬렉션 순회·해시 기반 자료구조를 통과시킬 때마다 이 점을 의식.

---

## 7. 진군 종료 = 게임 종료의 임시 정의

Phase 1의 Resolve(victory) 조건:
> Player가 가장 가까운 살아있는 적과의 거리가 AttackRange 이하가 되는 순간 `Player.State = Idle` + `BattlePhase = Resolve` 송신 후 종료.

이건 **임시 정의**다. 실제 게임에선:
- Player가 적 사거리에 들어오면 → **전투 시작** (Engage 페이즈, Phase 3).
- 적이 모두 죽으면 → Resolve(victory) (Phase 4).
- Player HP가 0이면 → Resolve(defeat) (Phase 4).

Phase 1에서 "사거리 도달=승리"로 둔 이유는 **데미지·HP 변화가 아직 없기 때문**. Acceptance를 만들 무언가가 필요했고, "Player가 적 앞에 멈춘다"는 상태 전이가 가장 단순한 종료 신호다. Phase 3에서 Engage가 활성화되는 순간 이 자리는 자연스럽게 "사거리 도달 → 전투 시작"으로 교체된다 — YAGNI 원칙의 응용.

---

## 8. `ref` 키워드 — 왜 가이드는 `var enemy = _enemies[index]`로 받는가

[BattleEngine.FindNearestAliveEnemyIndex](../../Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs):
```csharp
for (var index = 0; index < _enemies.Length; index++)
{
    var enemy = _enemies[index];
    ...
}
```

`Actor`는 [class](../../Assets/_Project/Scripts/Battle/Domain/Actor.cs)(참조 타입)다. 그래서 `var enemy = _enemies[index];`로 받으면 같은 인스턴스를 가리키는 참조 복사 — `enemy.Position = ...` 같은 변경도 원본 배열의 객체에 그대로 반영된다.

요약:
- `class`: 그냥 `var x = arr[i]` — 참조 복사. 필드 변경 OK.
- `struct`: 변경하려면 `ref var x = ref arr[i]` 필수. 안 그러면 값 복사본만 바뀜.

**왜 이걸 짚어두는가**: C# 입문서가 종종 "성능 위해 `ref var`를 써라"는 식으로 가르치지만, **참조 타입에 `ref`를 추가해도 성능 이득은 거의 없고 인지 부하만 늘어난다** (포인터의 포인터를 추가로 따라가는 형태). 본 프로젝트는 **명시적 단순함**을 우선해 class일 때는 그냥 `var`로 간다.

**기억할 것**: `Actor`를 나중에 `struct`로 바꾼다면 `ref var` 패턴이 필수가 된다. 그 시점에 일괄 변환.

---

## 9. 학습 체크 — Phase 1 마치고 답할 수 있어야 할 것들

- [x] 1D 거리축을 쓰는 이유 두 가지를 설명할 수 있는가?
- [x] Phase 1에서 진군하는 건 누구인가? (Player? Enemy?) 도착 후 클램프되는 Position 값은 어떻게 계산되는가?
- [x] Player의 `Running → Idle` 전이는 어떤 조건에서 일어나고, BattlePhase의 `Resolve` 전이와 어떻게 연결되는가?
- [x] Phase 1에서 쓰지 않는 `Casting`/`Stunned` 같은 enum 값을 미리 선언해 둔 이유는? (YAGNI와 충돌하지 않는가?)
- [x] EnemyData가 SO여야 하는 이유는?
- [x] `Player.Position = nearest.Position - AttackRange` 클램프가 왜 필요한가? 없으면 무엇이 깨지는가?
- [x] 매 틱 `new ActorView[N]` 배열 할당이 Phase 1에선 왜 문제 없는가? 언제 문제가 되는가?
- [x] actors 배열의 id 오름차순은 정렬 호출 없이 어떻게 보장되는가? 그 invariant가 깨지는 시나리오는?
- [x] 배열 요소를 변경할 때 `ref var`가 필요한 경우와 그냥 `var`로 충분한 경우의 기준은?

---

## 10. 더 깊이 파고 싶다면

| 주제 | 추천 키워드 |
|------|------------|
| FSM in games | "Game programming patterns — State", Robert Nystrom |
| ScriptableObject 활용 패턴 | Unity 공식 강의 — "Pluggable AI with ScriptableObjects" |
| 결정성 시뮬레이션 심화 | "Lockstep simulation", "Fixed timestep integration" |
| 부동소수점 누적 오차 | "What Every Computer Scientist Should Know About Floating-Point Arithmetic" |
| Object Pool | "Game programming patterns — Object Pool" |
