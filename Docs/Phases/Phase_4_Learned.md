# Phase 4 — 오래 가져갈 프로그래밍 개념

> [!tip]
> Phase 4의 작업 절차는 [[Phase_4_Guide]]에 있다.
> 이 문서는 데미지, 사망, 승패 처리를 만들며 배운 장기 개념을 정리한다.

---

## 먼저 볼 것

Phase 4에서 Anki 카드로 가져갈 만한 장기 개념은 9개다.

| 번호 | 개념 | 카드로 바꿀 질문 |
|------|------|------------------|
| 1 | 결과가 있는 시스템 | 전투 시스템에 "결과"가 생기면 무엇이 달라지나? |
| 2 | 거리와 대상 범위 분리 | `EngageDistance`와 `SkillRange`는 왜 다른가? |
| 3 | 상태 변경의 출입문 | HP 변경은 왜 `ApplyDamage`를 통과해야 하나? |
| 4 | 계산과 적용 분리 | 왜 Phase 4에는 `IDamageResolver`가 아직 필요 없나? |
| 5 | 데미지와 사망 분리 | HP를 깎는 일과 죽음을 확정하는 일을 왜 나누나? |
| 6 | 이벤트 순서 | 이벤트 순서는 왜 게임 규칙인가? |
| 7 | 시스템 실행 순서 | 시스템 순서는 왜 단순한 구현 순서가 아닌가? |
| 8 | 필요한 효과만 만들기 | 왜 Phase 4에서는 `SkillDamageEffect`만 만드나? |
| 9 | 다형성 직렬화 | `[SerializeReference]`는 왜 필요한가? |

---

## 1. 전투 시스템에 "결과"가 생기면 무엇이 달라지나?

Phase 3까지 전투는 계속 진행되기만 했다.
무공은 시전됐지만 적 HP를 깎지 않았고, 적도 플레이어를 공격하지 않았다.

Phase 4부터 전투에는 결과가 생긴다.

| 흐름 | 결과 |
|------|------|
| 적 일반 공격 | Player HP 감소 |
| 무공 데미지 | Enemy HP 감소 |
| HP 0 | 사망 |
| 모든 적 사망 | 승리 |
| Player 사망 | 패배 |

결과가 생기면 시스템은 단순히 행동을 실행하는 데서 끝나지 않는다.
HP가 줄고, 죽고, 승패가 확정되는 흐름을 일관되게 보장해야 한다.

> [!important]
> 결과가 있는 시스템에서는 "무엇을 할까?"만큼 "그 결과를 어디서 확정할까?"가 중요하다.

---

## 2. `EngageDistance`와 `SkillRange`는 왜 다른가?

`EngageDistance`는 거리다.
`SkillRange`는 몇 명을 때리는지 나타내는 대상 범위다.

| 이름 | 뜻 |
|------|----|
| `EngageDistance` | 이 거리 안이면 교전하거나 공격할 수 있다 |
| `SkillRange.Single` | 가장 앞의 적 1명 |
| `SkillRange.NearbyPair` | 가장 앞의 적부터 최대 2명 |
| `SkillRange.All` | 살아있는 모든 적 |

적 일반 공격은 "플레이어가 내 공격 거리 안에 있는가?"를 보면 된다.
그래서 `EngageDistance`를 쓴다.

무공은 "몇 명을 때리는가?"를 보면 된다.
그래서 `SkillRange`를 쓴다.

> [!note]
> 이 둘을 나누면 거리 판단과 대상 선택이 섞이지 않는다.
> 이름이 정확해지면 잘못된 설계가 코드로 들어올 가능성도 줄어든다.

### LINQ로 대상 목록을 고르는 코드는 어떻게 읽나?

`GetAliveEnemiesByRange`는 살아있는 적을 고른 뒤, 앞쪽 순서로 정렬한다.
그 다음 `SkillRange`에 맞는 앞쪽 범위를 돌려준다.

```csharp
var aliveEnemies = Enemies
    .Where(enemy => enemy.State != ActorState.Dead && enemy.Hp > 0)
    .OrderBy(enemy => enemy.Position)
    .ThenBy(enemy => enemy.Id)
    .ToArray();
```

`var`는 오른쪽 식의 실제 자료형을 컴파일러가 대신 쓰게 하는 문법이다.
위 코드에서 `aliveEnemies`의 실제 자료형은 `EnemyActor[]`다.

`IOrderedEnumerable`은 C# enum이 아니다.
`IEnumerable` 계열 interface라서 "순서대로 하나씩 꺼낼 수 있는 것"에 가깝다.
`OrderBy`와 `ThenBy`를 지나 `ToArray()` 전까지의 중간 결과가 이 계열이다.

`ToArray()`는 LINQ 중간 결과를 실제 배열로 만든다.
그래서 이후 switch에서는 `EnemyActor[]`의 앞쪽 몇 명을 쓸지만 정한다.

| 코드 | 쉬운 뜻 | 결과 자료형 |
|------|---------|-------------|
| `Where(...)` | 살아있는 적만 남긴다 | `IEnumerable<EnemyActor>` |
| `OrderBy(...)` | `Position`이 작은 순서로 정렬한다 | `IOrderedEnumerable<EnemyActor>` |
| `ThenBy(...)` | 같은 `Position` 안에서 `Id`가 작은 순서로 다시 정렬한다 | `IOrderedEnumerable<EnemyActor>` |
| `ToArray()` | 지금까지 고른 대상을 배열로 만든다 | `EnemyActor[]` |
| `Math.Min(2, aliveEnemies.Length)` | 앞에서 최대 2명을 쓰겠다고 정한다 | `int` |
| `AsMemory(0, targetCount)` | 배열을 새로 만들지 않고 범위만 감싼다 | `ReadOnlyMemory<EnemyActor>` |

`ThenBy`는 단독 정렬이 아니라 2차 정렬이다.
이 코드에서는 같은 위치에 적이 여럿 있을 때 항상 같은 순서로 고르기 위해 쓴다.
SSOT에는 같은 위치의 적 순서를 `id` 오름차순으로 고정하는 결정성 규칙이 있다.

`ReadOnlyMemory<EnemyActor>`는 배열 일부를 가리키는 값이다.
기존 배열, 시작 위치, 개수를 함께 들고 있다.

`Single`과 `NearbyPair`는 새 배열을 만들지 않는다.
같은 `aliveEnemies` 배열에서 앞쪽 1명 또는 2명 범위만 넘긴다.

`ReadOnlyMemory`의 `ReadOnly`는 범위 안의 슬롯 교체를 막는다는 뜻이다.
`targets.Span[0] = otherEnemy`처럼 다른 적으로 바꾸는 일은 할 수 없다.

하지만 `EnemyActor`는 class라서 슬롯 안의 객체는 그대로 수정할 수 있다.
그래서 `ApplyDamage`가 `skillTarget.Hp`나 `skillTarget.State`를 바꾸는 흐름은 가능하다.

| 코드 | 가능 여부 | 이유 |
|------|-----------|------|
| `targets.Span[0] = otherEnemy` | 불가 | 읽기 전용 범위라 슬롯 교체가 안 된다 |
| `targets.Span[0].Hp -= 5` | 가능 | 슬롯 안의 `EnemyActor` 객체는 mutable이다 |

이번 helper는 타겟 목록 자체를 바꾸려는 코드가 아니다.
타겟 범위를 고정한 뒤, 그 안의 적 객체에 데미지를 적용하는 코드다.
그래서 `Memory<EnemyActor>`보다 `ReadOnlyMemory<EnemyActor>`가 의도를 더 잘 드러낸다.

### `CastingSystem`도 범위 타겟을 알아야 하나?

`CastingSystem`은 어떤 슬롯을 쓸지만 고른다.
어떤 적들이 맞는지는 `BattleEngine.TryCast`가 `SkillRange`로 고른다.

이렇게 나누면 자동 시전 결정과 데미지 대상 선택이 섞이지 않는다.
`CastingSystem`이 가장 가까운 적 1명을 먼저 고르면,
`NearbyPair`와 `All` 같은 범위 무공의 뜻이 다시 흐려진다.

`SkillCastPublished`는 맞은 적마다 나가는 이벤트가 아니다.
무공을 한 번 성공적으로 시전했다는 이벤트다.
그래서 시전 1회에 한 번만 나간다.

대신 실제로 맞은 적들은 `DamagePublished`를 각각 받는다.
예를 들어 `NearbyPair`가 적 2명을 때리면 흐름은 이렇게 된다.

1. `SkillCastPublished` 1번.
2. 첫 번째 적 `DamagePublished` 1번.
3. 두 번째 적 `DamagePublished` 1번.

`SkillCastEvent`에는 대상 id를 담지 않는다.
피격 대상은 `DamageEvent.TargetId`로 확인한다.

---

## 3. HP 변경은 왜 `ApplyDamage`를 통과해야 하나?

HP를 깎는 코드는 여기저기 흩어지면 위험하다.
어떤 곳은 HP를 0 아래로 내리고, 어떤 곳은 이벤트를 빼먹고, 어떤 곳은 죽은 대상을 또 때릴 수 있다.

그래서 HP 변경에는 하나의 출입문이 필요하다.
Phase 4에서는 그 출입문이 `ApplyDamage`다.

`ApplyDamage`가 맡는 일은 이렇다.

- HP를 0 아래로 내리지 않는다.
- 데미지가 실제로 적용됐을 때 `DamagePublished`를 보낸다.
- 죽은 액터에게 데미지를 다시 적용하지 않는다.
- 나중에 회피나 배율이 생길 때 넣을 위치를 만든다.

`source`와 `target`은 누가 누구에게 데미지를 줬는지 나타낸다.
`DamageKind`는 데미지 방식만 나타낸다.
예: `NormalAttack`, `Skill`.
그래서 `EnemyNormalAttack`처럼 진영과 방식을 한 enum 값에 섞지 않는다.

> [!important]
> 중요한 상태 변경은 한곳으로 모을수록 규칙을 빠뜨리기 어렵다.

---

## 4. 왜 Phase 4에는 `IDamageResolver`가 아직 필요 없나?

Phase 4의 데미지 공식은 단순하다.

```text
finalDamage = baseDamage
hp_new = max(0, hp_old - finalDamage)
```

회피, 방어, 상성 배율이 아직 없다.
그래서 "최종 데미지를 계산하는 전용 인터페이스"는 아직 이르다.

대신 HP에 실제로 반영하는 출입문은 필요하다.

| 역할 | 지금 필요한가 | 이유 |
|------|---------------|------|
| 데미지 계산기 | 아니오 | 계산할 규칙이 아직 없다 |
| 데미지 적용기 | 예 | HP 변경과 이벤트 발행은 이미 필요하다 |

> [!note]
> 계산기와 적용기는 다르다.
> "얼마나 아픈가?"를 계산하는 코드와 "HP에 반영한다"는 코드는 같은 문제가 아니다.

---

## 5. HP를 깎는 일과 죽음을 확정하는 일을 왜 나누나?

데미지를 적용하는 순간 바로 `State = Dead`로 바꾸고 싶을 수 있다.
하지만 Phase 4에서는 데미지 적용과 사망 처리를 나눴다.

흐름은 이렇다.

1. 시스템들이 HP를 깎는다.
2. Tick 끝에서 HP가 0인 액터를 한 번에 찾는다.
3. `State = Dead`로 바꾼다.
4. `ActorDeathPublished`를 한 번만 보낸다.
5. 승패를 확인한다.

이렇게 하면 사망 이벤트가 여러 번 나가거나, 시스템 중간에서 승패가 어중간하게 확정되는 일을 줄일 수 있다.

> [!warning]- 같은 Tick 처리에서 조심할 점
> 무공 데미지로 HP가 0이 된 적은 Tick 끝 사망 처리 전일 수 있다.
> 그래서 `EnemyAttackSystem`은 `State == Dead`뿐 아니라 `Hp <= 0`도 같이 봐야 한다.
> HP가 0인 적은 아직 `Dead` 상태가 아니어도 이미 공격할 수 없는 적이다.

---

## 6. 이벤트 순서는 왜 게임 규칙인가?

이벤트 순서는 단순한 로그 순서가 아니다.
View, 테스트, 나중에 붙을 연출이 모두 이 순서에 의존할 수 있다.

Phase 4의 큰 흐름은 이렇다.

1. `SkillCastPublished`
2. `DamagePublished`
3. `ActorDeathPublished`
4. `BattleResultPublished`
5. `SnapshotPublished`

이 순서가 있어야 "무공을 썼고, 데미지가 들어갔고, 그래서 죽었고, 그래서 승리했다"가 자연스럽게 읽힌다.

`OnDamage` 같은 이름보다 `DamagePublished`가 더 나은 이유도 여기에 있다.
외부로 공개된 일이 이미 발생했다는 뜻이 이름에 드러난다.

---

## 7. 시스템 순서는 왜 단순한 구현 순서가 아닌가?

시스템 순서는 게임 규칙이다.
같은 시스템들이 있어도 순서가 바뀌면 전투 감각과 결과가 달라질 수 있다.

Phase 4의 순서는 이렇다.

1. `MovementSystem`
2. `EngagementSystem`
3. `CastingSystem`
4. `EnemyAttackSystem`
5. `ResolveDeathsAndResult`
6. `PublishSnapshot`

이 순서에는 의미가 있다.

- 먼저 교전 거리에 들어간다.
- 그 다음 플레이어 무공이 시전된다.
- HP가 남은 적만 일반 공격한다.
- 마지막에 사망과 결과를 정리한다.
- View는 정리된 Snapshot을 받는다.

> [!note]
> "플레이어 무공이 먼저냐, 적 일반 공격이 먼저냐"는 게임 감각에도 영향을 준다.
> 실행 순서는 코드 편의가 아니라 의도한 규칙으로 정해야 한다.

---

## 8. 왜 Phase 4에서는 `SkillDamageEffect`만 만드나?

SSOT에는 스킬 효과라는 자리가 있다.
하지만 Phase 4에서 실제로 필요한 효과는 데미지 하나다.

그래서 `SkillDamageEffect`만 만든다.

| 만들지 않은 효과 | 나중에 필요한 조건 |
|------------------|-------------------|
| `HealEffect` | 회복 규칙 |
| `BuffEffect` | 지속 시간과 중복 규칙 |
| `DebuffEffect` | 상태 이상 규칙 |

미리 타입만 만들어 두면 코드는 있는 것처럼 보이지만 아무 일도 안 한다.
이 프로젝트 규칙에서는 그런 빈 미래 코드를 만들지 않는다.

---

## 9. `[SerializeReference]`는 왜 필요한가?

`[SerializeReference]`는 부모 타입 필드에 실제 자식 타입 객체를 담기 위한 Unity 직렬화 속성이다.
이번 Phase에서는 `SkillEffect` 배열 안에 `SkillDamageEffect`를 넣기 위해 필요하다.

일반 Unity 직렬화는 필드 타입 중심으로 값을 저장한다.
그래서 `SkillEffect`처럼 base 타입으로 선언된 필드에
`SkillDamageEffect` 같은 하위 타입을 안정적으로 담기 어렵다.

```csharp
[SerializeReference]
public SkillEffect[] Effects = Array.Empty<SkillEffect>();
```

이렇게 쓰면 Unity가 배열 원소의 실제 타입 정보를 함께 저장한다.
덕분에 `SkillData`는 "효과 목록"만 알고,
각 효과의 구체적인 동작은 `SkillDamageEffect` 같은 하위 타입이 맡을 수 있다.

> [!warning]- Inspector에서 바로 편해지는 것은 아니다
> `[SerializeReference]`는 직렬화 방식이다.
> Inspector에서 하위 타입을 고르는 UI까지 항상 보기 좋게 만들어 주지는 않는다.
> 필요하면 Unity의 managed reference 선택 UI나 임시 에디터 작업으로 에셋을 채운다.

---

## 학습 체크

Phase 4를 마치고 아래 질문에 답할 수 있으면 충분하다.

- [ ] 전투 시스템에 결과가 생기면 설계에서 무엇이 달라지는지 설명할 수 있다.
- [ ] `EngageDistance`와 `SkillRange`의 차이를 설명할 수 있다.
- [ ] `Where`/`OrderBy`/`ThenBy`/`ToArray`/`ReadOnlyMemory`의 자료형 흐름을 설명할 수 있다.
- [ ] HP 변경을 `ApplyDamage`로 모으는 이유를 설명할 수 있다.
- [ ] `IDamageResolver`를 Phase 4에서 만들지 않는 이유를 설명할 수 있다.
- [ ] 데미지 적용과 사망 처리를 분리하는 이유를 설명할 수 있다.
- [ ] 이벤트 순서가 왜 게임 규칙인지 설명할 수 있다.
- [ ] 시스템 실행 순서가 전투 결과에 영향을 주는 이유를 설명할 수 있다.
- [ ] Phase 4에서 `SkillDamageEffect`만 만드는 이유를 설명할 수 있다.
- [ ] `[SerializeReference]`가 필요한 이유를 설명할 수 있다.

---

## 나중에 더 깊이 볼 단어

> [!info]- 지금 외울 필요는 없는 단어들
> 아래 단어들은 장기적으로 도움이 되지만, Phase 4를 이해하려고 당장 깊게 팔 필요는 없다.
>
> | 단어 | 쉬운 뜻 |
> |------|---------|
> | state transition | 상태가 다른 상태로 바뀌는 것 |
> | event ordering | 이벤트가 외부로 나가는 순서를 정하는 것 |
> | damage pipeline | 데미지 계산부터 HP 반영까지의 흐름 |
> | single entry point | 중요한 처리를 한곳으로만 지나가게 하는 구조 |
> | deferred resolution | 바로 확정하지 않고 정해진 시점에 한 번에 처리하는 방식 |
