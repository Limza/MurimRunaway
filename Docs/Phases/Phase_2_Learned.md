# Phase 2 — 배운 기술 / 개념 정리

> Phase 2의 **작업 절차**는 [Phase_2_Guide.md](Phase_2_Guide.md). 본 문서는 그 작업에서 등장한 **개념·용어·설계 결정의 이유**를 정리한다. 학습 노트.

---

## 1. 단일 진입점 (Single Entry Point) — `IResourceMutator`가 인터페이스로 분리된 이유

**한 줄 정의**: 어떤 데이터에 가해지는 모든 변경을 **하나의 인터페이스/메서드 집합**만 통과시켜, 변경 규칙(클램프·atomic·이벤트 발행)을 한 곳에서 강제하는 패턴.

Phase 2에서 Player의 자원(Mana)은 절대 직접 쓰지 않는다. 모든 변경은 `IResourceMutator.SpendMana`/`GainMana` 만 통과.

**왜 이렇게까지 강제하는가**:
1. **불변식 보장이 한 곳에서 끝난다** — `0 ≤ mana ≤ maxMana` 클램프 로직이 mutator 안에만 존재. 호출처 100곳에서 `if (next > max) next = max;`를 반복할 필요 없음.
2. **추적 가능성** — Phase 11+에서 "이번 런에 내공을 가장 많이 소비한 스킬은?" 같은 telemetry를 붙일 때 mutator 메서드에 로깅 한 줄만 추가하면 끝.
3. **Atomic 보장** — `SpendMana`가 부족 시 값을 안 바꾸고 false를 반환하는 atomic 규약은, 호출처에서 "비용 깎고 → 스킬 발동 실패 → 비용 롤백" 같은 두 단계 처리를 안 해도 되게 만든다.

**언제 이 패턴을 도입할까**: 같은 데이터에 변경 코드가 3곳 이상에서 보이거나, 변경마다 클램프/검증이 필요할 때. Phase 1엔 Player 자원이 없어 불필요했고, Phase 3에서 Skill 시전이 호출처가 되는 시점에 정확히 필요해진다 — Phase 2가 "한 단계 앞서 만들어두는" 자리.

---

## 2. Atomic 연산 — `SpendMana`의 all-or-nothing

```csharp
public bool SpendMana(int amount)
{
    if (amount > _player.Mana)
        return false;
    _player.Mana -= amount;
    return true;
}
```

이게 atomic이라고 부르는 이유: **변경이 성공하거나 아예 일어나지 않거나**. 부족 시 `_player.Mana`는 절대 안 바뀐다.

대안 — "있는 만큼만 깎기"도 가능:
```csharp
public int SpendManaBestEffort(int amount) {
    var spent = Math.Min(amount, _player.Mana);
    _player.Mana -= spent;
    return spent;
}
```

이건 상점 결제 같은 도메인에 적합하다. **무공 시전은 다르다** — "비용 5인데 3밖에 없어서 3만 내고 발동" 같은 게 없다. 발동하거나 안 하거나. 그래서 bool atomic이 자연스럽다.

**부산 효과**: 호출처가 if 한 줄로 깔끔해진다.
```csharp
if (mutator.SpendMana(skill.ManaCost))
    Cast(skill);
// 실패하면 자원도 그대로, 상태도 그대로.
```

---

## 3. 클램프 vs 거절 — `GainMana`는 클램프, `SpendMana`는 거절

같은 자원에 대해 한쪽은 silent 클램프(`GainMana` over max → max로), 다른쪽은 거절(`SpendMana` under 0 → false).

**왜 비대칭인가**:
- **GainMana** 호출처는 보통 "회복 효과" — `"드링크 사용 → 내공 +20"`. 만약 현재 90/100인데 +20을 시도하면, 호출처가 "10만 들어가고 10은 버려졌다"를 알아야 할 일이 거의 없다. **결과적으로 max에 닿았다는 사실만 중요**. 그래서 void + silent.
- **SpendMana** 호출처는 "시전 시도" — 비용을 못 내면 **시전 자체가 일어나면 안 된다**. 즉 "낼 수 있었나?"가 호출 흐름의 분기 조건이 된다. 그래서 bool 반환.

이 비대칭은 도메인 관찰 결과지 일반 규칙이 아니다. 다른 도메인(예: 재화 적립 한도 도달 시 알림 띄우기)에선 Gain에도 결과가 필요할 수 있다.

---

## 4. Snapshot 일관성 — 즉시 이벤트가 아니라 다음 스냅샷

SSOT [BATTLE_DESIGN §3 Phase 2](../BATTLE_DESIGN.md) Behavior 3:
> 자원이 변경되면 Snapshot의 **다음 발행**에 반영 (즉시 이벤트 X — Snapshot 일관성 우선).

대안 — 자원 변경마다 즉시 `OnManaChanged` 이벤트를 쏘는 것도 가능. 그런데 한 Tick 안에서 자원이 3번 바뀌면 View가 3번 갱신되고, 그 사이 중간 상태가 화면에 잠깐 보일 수 있다. **결정론 시뮬레이션은 한 Tick = 한 외부 관측**이 일관성을 가장 단순하게 만든다.

**다음 스냅샷에만 반영**한다는 약속이 있으면:
- View는 매 틱 끝의 단일 BattleSnapshot만 처리하면 됨.
- 자원이 같은 틱 안에서 +20 → -15로 변해도, View엔 net +5의 결과만 보임.
- 리플레이/디버그 재현 시 "이 틱의 자원값"이 명확히 하나로 정의된다.

성능 부산물도 있음 — 매 변경마다 이벤트 invoke + delegate allocation을 안 해도 되니 GC 압력이 줄어듦. Phase 11+ 적 다수 환경에서 의미 있어짐.

---

## 5. `ActorView`에 한쪽 전용 필드를 두는 비대칭

Phase 1의 [§1.6 ActorView](Phase_1_Guide.md)는 Player/Enemy가 같은 struct를 공유했다. Phase 2에서 Mana/MaxMana 2필드가 Player 한정으로 추가되면서 Enemy 케이스에선 0으로 떨어진다.

**왜 EnemyView를 따로 만들지 않았는가**:
1. View 측 코드가 단일 `ActorView[]`만 순회하면 단순. 두 종류면 `if (player) playerView else enemyView` 같은 분기가 매번 생긴다.
2. 메모리 낭비는 ~40B × actorCount × 매 틱. Phase 11에서 적 8명 가정해도 초당 6KB — GC에 무의미한 수준.
3. 미래에 양쪽 모두에 의미 있는 필드(예: `BuffSlots`)가 등장하면 다시 합쳐야 하는 비용이 더 크다.

**언제 분리할까**: 자원 필드가 3종 이상으로 늘면서 Enemy 전용 필드(예: `IsBoss`/`PatternType`)도 같이 증가하면, 그 시점에 base ActorView + PlayerView/EnemyView로 분리 고려. Phase 5(보스전 매트릭스) 진입이 자연스러운 검토 시점.

---

## 6. DI 컨테이너 — VContainer 도입 시점이 왜 Phase 2인가

**한 줄 정의**: DI(Dependency Injection) 컨테이너는 객체의 의존성을 직접 `new`로 엮지 않고 **컨테이너 설정에서 한 번에 선언**하게 만들어, 의존성 그래프 변경을 한 곳에 집중시키는 도구.

**도입 안 한 시점 (Phase 0~1)** — 의존성이 `BattleEngine ← (ITickService, IRngService)` 정도라 수동 조립 비용이 4줄. DI 도입 비용(LifetimeScope 작성 + 학습)이 더 컸음.

**도입 시점 (Phase 2)** — 의존성이 5개 근방. Phase 3에서 `IBattleSystem` 분리로 `MovementSystem`/`EngagementSystem`/`CastingSystem` 등 3~5개 인스턴스가 추가될 예정. 그때 도입하면:
- Phase 3 변경(시스템 분리) + DI 도입이 한 커밋에 섞임 → 리뷰 어려움.
- "왜 갑자기 LifetimeScope가 생겼는지" 결정 맥락이 흐려짐.

**도입 안 했을 미래** — Phase 11(런 구조)에선 `RunSession`/`StageRunner`/`BattleEngine`이 동시에 살아있고, 한 런 안에서 BattleEngine만 재생성·교체된다. 컨테이너 없이는 scope 관리가 손수 어려워짐.

>
> Zenject가 아니라 **VContainer**를 고른 이유: IL2CPP·AOT 호환이 더 매끄럽고, reflection 기반 모드가 학습용으로 단순. 코드 생성(`InstallerCodeGen`) 없이도 기본 동작.

---

## 7. ScriptableObject는 자원 컨테이너로 쓰면 안 된다

`EnemyData`는 SO. 그런데 `PlayerActor.Mana`/`MaxMana`는 SO가 아니라 런타임 POCO 필드.

**왜 SO에 자원을 두지 않는가**:
- SO는 **에셋 파일**. 런타임에 필드를 변경하면 에디터에선 디스크에 반영된다 (Play 모드 종료 후에도 남음).
- Phase 2 학습 노트의 핵심 — **런타임 가변 값은 절대 SO에 두지 않는다**. 시작값(maxMana의 디폴트=100)은 `PlayerStartData`(POCO) 또는 향후 메타 강화 SO의 baseline 필드로.

이건 [Phase_1_Learned.md §3](Phase_1_Learned.md)에서 EnemyData를 직접 mutate하지 않고 EnemyActor로 복사한 것과 같은 원리. **"읽기 전용 데이터" vs "런타임 상태"의 분리**.

---

## 8. `is PlayerActor`로 타입 분기 — pattern matching의 좋은 사례

ActorView 생성자:
```csharp
if (actor is PlayerActor player)
{
    Mana = player.Mana;
    ...
}
```

C# 7+의 type pattern matching. `is`로 타입 검사 + 변수 캐스팅을 한 번에. 대안:
```csharp
if (actor is PlayerActor)
{
    var player = (PlayerActor)actor;
    Mana = player.Mana;
    ...
}
```
두 줄을 한 줄로 줄임. **WHY**: 두 번 캐스팅하면 JIT가 type check를 두 번 할 수도 있고, 가장 큰 건 가독성 — "타입이면서 그 타입으로 받는다"가 한 표현식.

**언제 안 쓸까**: 진영 분기가 3개 이상으로 늘어나면 `switch` pattern matching이나 가상 메서드(virtual `ToView()`)가 깔끔하다. Phase 2엔 2개라 `is` 두 갈래로 충분.

---

## 9. 학습 체크 — Phase 2 마치고 답할 수 있어야 할 것들

- [ ] `IResourceMutator`로 모든 자원 변경을 통과시키는 이유 3가지?
- [ ] `SpendMana`가 atomic이고 `GainMana`가 silent clamp인 비대칭의 이유는?
- [ ] 자원 변경이 즉시 이벤트가 아니라 다음 스냅샷에 실리는 이유는?
- [ ] `ActorView`에 Enemy가 안 쓰는 자원 필드를 두는 게 왜 합리적인가? 언제 분리?
- [ ] VContainer를 Phase 1이 아닌 Phase 2에서 도입한 이유는?
- [ ] 자원 컨테이너를 ScriptableObject에 두면 안 되는 이유는?

---

## 10. 더 깊이 파고 싶다면

| 주제 | 추천 키워드 |
|------|------------|
| 단일 진입점 패턴 | "Mediator pattern", "Aggregate root" (DDD) |
| Atomic 연산 / Transactional state | "Software transactional memory", "Optimistic locking" |
| DI 컨테이너 비교 | "VContainer vs Zenject" — github.com/hadashiA/VContainer |
| Pattern matching (C#) | "C# pattern matching tutorial", Microsoft Learn |
| ScriptableObject 안티패턴 | Unity 공식 — "Mutable ScriptableObject pitfalls" |
