# Phase 4 — 배운 개념 정리

> [!tip]
> 작업 순서는 [[Phase_4_Guide]]에 있다.
> 이 문서는 Phase 4에서 왜 그런 구조를 쓰는지 설명하는 학습 노트다.

---

## 1. Phase 4의 핵심은 "결과"다

Phase 3까지 전투는 계속 진행되기만 했다.
무공은 시전됐지만 적 HP를 깎지 않았고, 적도 플레이어를 공격하지 않았다.

Phase 4부터 전투에는 결과가 생긴다.

- 적 일반 공격 → Player HP 감소.
- 무공 데미지 → Enemy HP 감소.
- HP 0 → 사망.
- 모든 적 사망 → 승리.
- Player 사망 → 패배.

그래서 Phase 4의 중심 질문은 "어떻게 때릴까?"보다 "HP가 줄고 죽고 끝나는 흐름을 어디서 보장할까?"에 가깝다.

---

## 2. 거리와 타겟 범위는 다른 말이다

Phase 3에서 `SkillRange`는 `Single / NearbyPair / All`로 정리됐다.
이제 `SkillRange`는 거리 등급이 아니다.
무공이 몇 명을 때리는지 나타낸다.

그래서 적 일반 공격에 `SkillRange`를 쓰면 의미가 흐려진다.
적 일반 공격은 "플레이어가 내 공격 거리 안에 있는가?"를 보면 된다.
이 값은 `EngageDistance`가 맡는다.

| 이름 | 뜻 |
|------|----|
| `EngageDistance` | 이 거리 안이면 교전/공격 가능 |
| `SkillRange.Single` | 가장 앞의 적 1명 |
| `SkillRange.NearbyPair` | 가장 앞의 적부터 최대 2명 |
| `SkillRange.All` | 살아있는 모든 적 |

> [!note]
> 이 구분 덕분에 "플레이어 기본 공격 거리를 넓게 잡으면 Mid 무공만 계속 나간다" 같은 문제가 다시 생기지 않는다.
> 교전 거리와 무공 타겟 범위가 서로 다른 축이 되었기 때문이다.

---

## 3. `ApplyDamage`는 계산기가 아니라 출입문이다

Phase 4의 데미지 공식은 아주 단순하다.

```text
finalDamage = baseDamage
hp_new = max(0, hp_old - finalDamage)
```

이 단계에는 회피도 없고, 방어도 없고, 상성 배율도 없다.
그래서 `IDamageResolver` 같은 "최종 데미지 계산 인터페이스"는 아직 이르다.

하지만 HP를 깎는 **출입문**은 필요하다.
그게 `ApplyDamage`다.

`ApplyDamage`가 한 곳에 있으면 다음이 쉬워진다.

- HP가 0 아래로 내려가지 않는다.
- `DamagePublished` 이벤트가 빠지지 않는다.
- Dead 상태 액터가 다시 맞는 일을 막을 수 있다.
- 나중에 회피나 배율이 생길 때 넣을 위치가 분명하다.

> [!note]
> resolver는 "얼마나 아픈가?"를 계산한다.
> applier는 "그 데미지를 실제 HP에 반영한다."
> Phase 4에는 applier만 필요하다.

---

## 4. 죽음은 데미지와 분리한다

데미지를 적용하는 순간 바로 `State = Dead`로 바꾸고 싶을 수 있다.
하지만 Phase 4에서는 데미지 적용과 사망 처리를 분리하는 편이 읽기 쉽다.

흐름은 이렇다.

1. 시스템들이 HP를 깎는다.
2. Tick 끝에서 HP가 0인 액터를 한 번에 찾는다.
3. `State = Dead`로 바꾼다.
4. `ActorDeathPublished`를 한 번만 보낸다.
5. 승패를 확인한다.

이렇게 하면 이벤트 순서가 선명해진다.

- `SkillCastPublished`
- `DamagePublished`
- `ActorDeathPublished`
- `BattleResultPublished`
- `SnapshotPublished`

> [!warning]- 같은 Tick 처리에서 조심할 점
> 무공 데미지로 HP가 0이 된 적은 아직 Tick 끝 사망 처리 전일 수 있다.
> 그래서 `EnemyAttackSystem`은 `State == Dead`뿐 아니라 `Hp <= 0`도 같이 봐야 한다.
> HP가 0인 적은 이미 공격할 수 없는 적이다.

---

## 5. 이벤트 이름은 `Published`로 맞춘다

전투 폴더 규칙은 외부로 나가는 이벤트 이름을 `Published` 계열로 둔다.
Phase 3도 `SnapshotPublished`, `SkillCastPublished`를 쓴다.

Phase 4도 같은 결을 따른다.

| 이벤트 | 뜻 |
|--------|----|
| `DamagePublished` | HP 데미지가 실제로 적용됨 |
| `ActorDeathPublished` | 액터가 Dead로 전이됨 |
| `BattleResultPublished` | 전투 결과가 확정됨 |

`OnDamage`처럼 이름을 붙이면 "이벤트를 발생시키는 메서드"처럼 읽힐 수 있다.
공개 이벤트는 `Published`가 더 명확하다.

---

## 6. 시스템 순서가 룰이다

Phase 4의 추천 순서:

1. `MovementSystem`
2. `EngagementSystem`
3. `CastingSystem`
4. `EnemyAttackSystem`
5. Tick 끝 `ResolveDeathsAndResult`
6. `PublishSnapshot`

이 순서에는 의미가 있다.

- 먼저 교전 거리에 들어간다.
- 그 다음 플레이어 무공이 시전된다.
- 살아있는 적만 일반 공격한다.
- 마지막에 사망과 결과를 정리한다.
- View는 정리된 Snapshot을 받는다.

> [!note]
> "플레이어 무공이 먼저냐, 적 일반 공격이 먼저냐"는 게임 감각에도 영향을 준다.
> Phase 4에서는 자동 시전이 이미 Phase 3의 중심 기능이므로, 시전 후 적 공격 순서가 가장 자연스럽다.

---

## 7. `DamageEffect`만 먼저 만든다

SSOT에는 스킬 효과라는 자리가 있다.
하지만 Phase 4에서 실제로 필요한 효과는 데미지 하나다.

그래서 `DamageEffect`만 만든다.

- `HealEffect`는 회복 룰이 생길 때.
- `BuffEffect`는 버프 지속 시간과 중복 규칙이 생길 때.
- `DebuffEffect`는 적 상태 이상 규칙이 생길 때.

미리 타입만 만들어 두면 코드는 "있는 것처럼 보이지만 아무 일도 안 하는" 상태가 된다.
이 프로젝트 규칙에서는 그런 빈 미래 코드를 만들지 않는다.

---

## 8. 학습 체크

- [ ] `SkillRange`와 `EngageDistance`의 차이를 설명할 수 있다.
- [ ] `ApplyDamage`가 왜 필요한지 설명할 수 있다.
- [ ] `IDamageResolver`를 Phase 4에서 만들지 않는 이유를 설명할 수 있다.
- [ ] 데미지 적용과 사망 처리를 분리하는 이유를 설명할 수 있다.
- [ ] `DamagePublished` → `ActorDeathPublished` → `BattleResultPublished` 순서를 설명할 수 있다.
- [ ] HP가 0인 적이 같은 Tick에 다시 공격하지 않게 막는 조건을 알고 있다.

