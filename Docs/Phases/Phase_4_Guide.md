# Phase 4 작업 가이드 — 일반 공격 + 데미지 + 사망/승패

> [!abstract]- 문서 개요
> **목표 한 줄**: 적은 일정 주기로 Player HP를 깎고, 무공은 Enemy HP를 깎는다.
> HP가 0이 되면 사망 처리하고, 전투 결과를 한 번만 확정한다.
>
> **참조 SSOT**: [[BATTLE_DESIGN]] §3 Phase 4 + [[MILESTONES]] M4.
> 본 가이드는 작업 순서 안내다.
> 사양이 다르면 SSOT가 우선이다.
>
> **함께 보기**: [[Phase_4_Learned]].
> 이유와 개념 설명은 Learned에 있다.

---

## 0. 빠른 지도

### 목표

Phase 4는 전투가 처음으로 **끝나는** Phase다.

- 적 일반 공격 → Player HP 감소.
- 무공 데미지 → Enemy HP 감소.
- HP 0 → 사망.
- 모든 적 사망 → 승리.
- Player 사망 → 패배.

### 이번 Phase에서 하지 않는 것

- 강공, 오의, 회피는 만들지 않는다.
- 회복, 버프, 디버프는 만들지 않는다.
- 데미지 보정용 resolver는 만들지 않는다.
- 적 HP 전용 UI는 필수 아님. PlayMode에서 결과 확인만 가능하면 된다.

### 에셋 방향

Phase 4에서는 **새 전투 에셋을 따로 늘리지 않는다.**
이미 있는 확인용 `SkillData`와 `EnemyData` 에셋에 Phase 4 필드를 채운다.

| 에셋 | 작업 |
|------|------|
| `Assets/_Project/Data/Skills/tae_in_jang.asset` | `SkillDamageEffect` 추가 |
| `Assets/_Project/Data/Skills/cheonha_36_geom.asset` | `SkillDamageEffect` 추가 |
| `Assets/_Project/Data/Skills/simbeop_unki.asset` | 데미지 없음. `Effects`는 비워 둠 |
| `Assets/_Project/Data/Enemies/EnemyData.asset` | 일반 공격 값 입력 |

### 핵심 규칙

| 규칙 | 뜻 |
|------|----|
| HP 변경 단일 진입점 | HP를 깎는 코드는 반드시 `ApplyDamage`를 통과한다. |
| 거리와 범위 분리 | 적 일반 공격 거리는 `EngageDistance`, 무공 대상 수는 `SkillRange`가 맡는다. |
| 사망은 Tick 끝 정리 | 시스템이 모두 돈 뒤 HP 0 액터를 `Dead`로 바꾼다. |
| 결과는 한 번만 | `_result != BattleResult.None`이면 결과 이벤트를 다시 보내지 않는다. |

### 작업 순서

1. Domain 파일을 테마별 폴더로 정리한다.
2. Domain에 데미지 방식, 전투 결과, 이벤트 데이터, `SkillDamageEffect`를 추가한다.
3. `EnemyData`와 `EnemyActor`에 일반 공격 값을 추가한다.
4. `SkillData`에 효과 목록을 추가한다.
5. Engine에 `IDamageApplier`와 `ApplyDamage`를 추가한다.
6. `TryCast`에서 `SkillDamageEffect`를 적용한다.
7. `EnemyAttackSystem`을 추가한다.
8. Tick 끝에서 사망과 승패를 정리한다.
9. 기존 에셋을 갱신하고 View를 최소로 연결한다.
10. EditMode 테스트와 PlayMode 체크를 통과시킨다.

---

## 1. 실제 작업 절차

### 1.1 Domain — 파일 정리

Phase 4에서 Domain 파일이 더 늘어난다.
새 타입을 만들기 전에 기존 파일을 테마별 폴더로 옮긴다.

네임스페이스는 모두 `MurimRunaway.Battle.Domain`으로 유지한다.
폴더는 파일 탐색용이며 코드 계층을 새로 만들지 않는다.

| 폴더 | 파일 |
|------|------|
| `Domain/Setup/` | `BattleStartData.cs`, `PlayerStartData.cs`, `EnemyData.cs` |
| `Domain/Actors/` | `Actor.cs`, `PlayerActor.cs`, `EnemyActor.cs`, `ActorView.cs` |
| `Domain/Skills/` | `SkillData.cs`, `SkillSlot.cs`, `SkillSlotView.cs`, `SkillCastEvent.cs` |
| `Domain/Damage/` | `DamageEvent.cs` |
| `Domain/BattleFlow/` | `BattleSnapshot.cs` |
| `Domain/_Enums/` | 기존 enum 파일 |

이동 뒤에는 참조가 깨지지 않는지 빌드로 확인한다.
이 단계에서는 타입 이름, 네임스페이스, 동작을 바꾸지 않는다.

### 1.2 Domain — 전투 언어 추가

새 파일을 만든다.

| 파일 | 역할 |
|------|------|
| `Domain/_Enums/DamageKind.cs` | 데미지가 어떤 방식으로 들어왔는지 구분 |
| `Domain/_Enums/BattleResult.cs` | 승리/패배 결과 |
| `Domain/Damage/DamageEvent.cs` | 데미지 적용 알림 |
| `Domain/BattleFlow/ActorDeathEvent.cs` | 사망 알림 |
| `Domain/Skills/SkillEffect.cs` | 무공 효과 base |
| `Domain/Skills/SkillDamageEffect.cs` | HP를 깎는 무공 효과 |

`SkillEffect`는 base만 둔다.
Phase 4에서 실제로 쓰는 하위 타입은 `SkillDamageEffect` 하나다.

> [!example]- Domain 예제 코드
> 파일: `Assets/_Project/Scripts/Battle/Domain/_Enums/DamageKind.cs`.
>
> 타입: `DamageKind`.
>
> 역할: `DamageEvent`와 `ApplyDamage`가 데미지 출처를 구분할 때 쓴다.
>
> ```csharp
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>데미지가 어떤 방식으로 들어왔는지 구분한다.</summary>
>     public enum DamageKind
>     {
>         /// <summary>아직 데미지 방식이 정해지지 않음.</summary>
>         None = 0,
>         /// <summary>일반 공격.</summary>
>         NormalAttack,
>         /// <summary>무공.</summary>
>         Skill,
>     }
> }
> ```
>
> 공격자와 피격자는 `source`/`target` 액터가 맡는다.
>
> `DamageKind`는 데미지 방식만 나타낸다.
> 예: `NormalAttack`, `Skill`.
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/_Enums/BattleResult.cs`.
>
> 타입: `BattleResult`.
>
> 역할: `BattleEngine`이 승리/패배를 한 번만 확정하고 View와 테스트에 알릴 때 쓴다.
>
> ```csharp
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>전투 종료 결과.</summary>
>     public enum BattleResult
>     {
>         /// <summary>아직 결과가 정해지지 않음.</summary>
>         None = 0,
>         /// <summary>모든 적을 처치함.</summary>
>         Victory,
>         /// <summary>플레이어가 사망함.</summary>
>         Defeat,
>     }
> }
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/Damage/DamageEvent.cs`.
>
> 타입: `DamageEvent`.
>
> 역할: `ApplyDamage`가 HP를 바꾼 뒤 View와 테스트에 데미지 결과를 알릴 때 쓴다.
>
> ```csharp
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>HP 데미지가 적용됐을 때 View와 테스트에 알리는 사건.</summary>
>     public readonly struct DamageEvent
>     {
>         public readonly int SourceId;
>         public readonly int TargetId;
>         public readonly int Amount;
>         public readonly DamageKind Kind;
>         public readonly string SkillId;
>
>         public DamageEvent(int sourceId, int targetId, int amount, DamageKind kind, string skillId)
>         {
>             SourceId = sourceId;
>             TargetId = targetId;
>             Amount = amount;
>             Kind = kind;
>             SkillId = skillId;
>         }
>     }
> }
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/BattleFlow/ActorDeathEvent.cs`.
>
> 타입: `ActorDeathEvent`.
>
> 역할: Tick 끝 사망 정리에서 액터가 처음 사망 처리될 때 발행한다.
>
> ```csharp
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>액터가 사망했을 때 View와 테스트에 알리는 사건.</summary>
>     public readonly struct ActorDeathEvent
>     {
>         public readonly int ActorId;
>
>         public ActorDeathEvent(int actorId)
>         {
>             ActorId = actorId;
>         }
>     }
> }
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/Skills/SkillEffect.cs`.
>
> 타입: `SkillEffect`.
>
> 역할: `SkillData.Effects` 배열에 들어가는 무공 효과들의 공통 base다.
>
> ```csharp
> using System;
>
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>무공이 발동했을 때 적용되는 효과의 base.</summary>
>     [Serializable]
>     public abstract class SkillEffect
>     {
>     }
> }
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/Skills/SkillDamageEffect.cs`.
>
> 타입: `SkillDamageEffect`.
>
> 역할: `TryCast`가 무공 대상에게 적용할 HP 데미지 값을 담는다.
>
> ```csharp
> using System;
>
> namespace MurimRunaway.Battle.Domain
> {
>     /// <summary>대상 HP를 줄이는 무공 효과.</summary>
>     [Serializable]
>     public sealed class SkillDamageEffect : SkillEffect
>     {
>         public int Amount;
>     }
> }
> ```

### 1.3 Domain — 기존 데이터 확장

`EnemyData`에는 적 SO의 정적 값을 추가한다.

| 필드 | 추천 기본값 | 뜻 |
|------|-------------|----|
| `NormalAttackDamage` | `3` | 일반 공격 데미지 |
| `NormalAttackPeriod` | `2.5f` | 공격 주기 |
| `EngageDistance` | `20f` | Player가 이 거리 안이면 공격 가능 |

`EnemyActor`에는 런타임 값을 추가한다.

| 필드 | 뜻 |
|------|----|
| `NormalAttackDamage` | 전투 중 쓰는 일반 공격 데미지 |
| `NormalAttackPeriod` | 전투 중 쓰는 공격 주기 |
| `NormalAttackCooldown` | 다음 공격까지 남은 시간 |

`SkillData`에는 효과 목록을 추가한다.

| 필드 | 뜻 |
|------|----|
| `Effects` | 무공 발동 시 적용할 효과 목록. Phase 4에서는 `SkillDamageEffect`만 사용 |

> [!example]- 기존 데이터 확장 코드 조각
> 파일: `Assets/_Project/Scripts/Battle/Domain/Setup/EnemyData.cs`.
>
> 타입: `EnemyData`.
>
> 역할: 적 SO가 들고 있는 일반 공격 기본값이다.
> `BattleEngine.Setup`에서 `EnemyActor`로 복사한다.
>
> ```csharp
> [Tooltip("일반 공격 데미지")]
> public int NormalAttackDamage = 3;
>
> [Tooltip("일반 공격 주기 (초)")]
> public float NormalAttackPeriod = 2.5f;
>
> [Tooltip("플레이어가 이 거리 안에 있으면 일반 공격 가능")]
> public float EngageDistance = 20f;
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/Actors/EnemyActor.cs`.
>
> 타입: `EnemyActor`.
>
> 역할: 전투 중 실제로 변하는 적 일반 공격 값과 쿨다운을 가진다.
> `EnemyAttackSystem`이 읽고 갱신한다.
>
> ```csharp
> public int NormalAttackDamage;
> public float NormalAttackPeriod;
> public float NormalAttackCooldown;
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Domain/Skills/SkillData.cs`.
>
> 타입: `SkillData`.
>
> 역할: 무공이 발동했을 때 실행할 효과 목록이다.
> `BattleEngine.TryCast`가 이 배열을 순회한다.
>
> ```csharp
> using System;
> using UnityEngine;
>
> // ...
>
> [SerializeReference]
> [Tooltip("무공 발동 시 적용 효과. Phase 4에서는 SkillDamageEffect만 사용")]
> public SkillEffect[] Effects = Array.Empty<SkillEffect>();
> ```

> [!warning]- Unity Inspector 주의
> `[SerializeReference]` 배열은 Unity 기본 Inspector에서 다루기가 어색할 수 있다.
> Phase 4에서는 테스트가 먼저라 큰 문제는 아니다.
> PlayMode 확인용 에셋은 Inspector에서 managed reference를 추가하거나, 임시 에디터 작업으로 채워도 된다.

### 1.4 Engine — `ApplyDamage` 단일 진입점

`IDamageApplier`를 추가하고 `BattleEngine`이 구현한다.

`ApplyDamage`가 맡는 일:

- Dead 상태 액터는 데미지를 주거나 받지 않는다.
- 데미지는 0 미만이 되지 않는다.
- HP는 0 아래로 내려가지 않는다.
- `DamagePublished`를 발행한다.

> [!example]- `IDamageApplier`와 `ApplyDamage`
> 파일: `Assets/_Project/Scripts/Battle/Engine/IDamageApplier.cs`.
>
> 타입: `IDamageApplier`.
>
> 역할: 적 일반 공격과 무공 데미지가 같은 HP 변경 입구를 쓰게 한다.
>
> ```csharp
> using MurimRunaway.Battle.Domain;
>
> namespace MurimRunaway.Battle.Engine
> {
>     public interface IDamageApplier
>     {
>         void ApplyDamage(Actor source, Actor target, int amount, DamageKind damageKind, string skillId);
>     }
> }
> ```
>
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs`.
>
> 타입: `BattleEngine`.
>
> 역할: `IDamageApplier`를 구현하고 데미지/사망/결과 이벤트를 발행한다.
>
> ```csharp
> public sealed class BattleEngine : IResourceMutator, ISkillExecutor, IDamageApplier
> {
>     public event Action<DamageEvent> DamagePublished;
>     public event Action<ActorDeathEvent> ActorDeathPublished;
>     public event Action<BattleResult> BattleResultPublished;
>
>     private BattleResult _result = BattleResult.None;
> }
> ```
>
> `Setup`을 다시 부를 수 있으므로 시작 데이터 세팅 때 결과도 초기화한다.
> 위치는 `BattleEngine.Setup(...)` 안이다.
>
> ```csharp
> _result = BattleResult.None;
> ```
>
> 아래 메서드는 `BattleEngine` 클래스 안에 추가한다.
>
> ```csharp
> public void ApplyDamage(Actor source, Actor target, int amount, DamageKind damageKind, string skillId)
> {
>     if (source.State == ActorState.Dead || target.State == ActorState.Dead)
>         return;
>
>     var finalDamage = Math.Max(0, amount);
>     target.Hp = Math.Max(0, target.Hp - finalDamage);
>     DamagePublished?.Invoke(new DamageEvent(source.Id, target.Id, finalDamage, damageKind, skillId));
> }
> ```

### 1.5 Engine — `Setup`에서 적 값 복사

`EnemyData`의 값을 `EnemyActor`에 복사한다.
SO는 정적 데이터이고, 전투 중 쿨다운은 `EnemyActor`가 가진다.

> [!example]- `EnemyActor` 생성부에 추가할 값
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs`.
>
> 클래스: `BattleEngine`.
>
> 메서드: `Setup(...)`.
>
> 역할: `EnemyData`의 정적 값을 전투용 `EnemyActor`에 복사한다.
> 아래 코드는 `new EnemyActor { ... }` 초기화 블록 안에 들어간다.
>
> ```csharp
> // ...
> NormalAttackDamage = enemyData.NormalAttackDamage,
> NormalAttackPeriod = enemyData.NormalAttackPeriod,
> NormalAttackCooldown = 0f,
> EngageDistance = enemyData.EngageDistance,
> // ...
> ```

### 1.6 Engine — 무공 타겟 범위 적용

Phase 3의 `GetNearestAliveEnemy()`는 Single용으로 충분했다.
Phase 4부터는 `NearbyPair`와 `All`이 실제 데미지를 가진다.

`BattleContext`에 살아있는 적을 앞에서부터 고르는 helper를 둔다.

| `SkillRange` | 데미지 대상 |
|--------------|-------------|
| `Single` | 가장 앞의 살아있는 적 1명 |
| `NearbyPair` | 가장 앞의 살아있는 적부터 최대 2명 |
| `All` | 모든 살아있는 적 |

> [!example]- 타겟 선택 helper
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleContext.cs`.
>
> 클래스: `BattleContext`.
>
> 역할: `SkillRange`에 맞춰 살아있는 적만 앞쪽 순서로 고른다.
> `TryCast`는 이 helper가 돌려준 대상들에게 `SkillDamageEffect`를 적용한다.
>
> `ThenBy`는 `OrderBy`로 정렬한 뒤 같은 값끼리 한 번 더 정렬한다.
> 여기서는 `Position`이 같으면 `Id`가 작은 적을 먼저 고른다.
> SSOT의 동률 규칙과 맞춰 같은 입력이면 같은 타겟 순서가 나오게 하기 위해서다.
>
> `aliveEnemies`의 실제 자료형은 `EnemyActor[]`다.
> `ToArray()` 전까지는 LINQ가 "어떻게 고르고 정렬할지"를 들고 있다가,
> `ToArray()`에서 실제 배열을 만든다.
>
> 반환값은 `ReadOnlyMemory<EnemyActor>`다.
> 새 배열을 더 만들지 않고, `aliveEnemies` 배열의 앞쪽 범위만 넘긴다.
> 범위의 슬롯은 바꿀 수 없지만, 슬롯 안의 `EnemyActor` 객체에는 데미지를 적용할 수 있다.
>
> 필요한 using:
>
> ```csharp
> using System;
> using System.Linq;
> ```
>
> ```csharp
> public ReadOnlyMemory<EnemyActor> GetAliveEnemiesByRange(SkillRange range)
> {
>     var aliveEnemies = Enemies
>         .Where(enemy => enemy.State != ActorState.Dead && enemy.Hp > 0)
>         .OrderBy(enemy => enemy.Position)
>         .ThenBy(enemy => enemy.Id)
>         .ToArray();
>
>     var targetCount = range switch
>     {
>         SkillRange.Single => Math.Min(1, aliveEnemies.Length),
>         SkillRange.NearbyPair => Math.Min(2, aliveEnemies.Length),
>         SkillRange.All => aliveEnemies.Length,
>         _ => 0,
>     };
>
>     return aliveEnemies.AsMemory(0, targetCount);
> }
> ```
>
> 적 수는 1~4명이라 이 단계에서는 명료함을 우선한다.

### 1.7 Engine — `TryCast`에 데미지 적용 연결

`CastingSystem`은 더 이상 가장 가까운 적 1명을 `TryCast`에 넘기지 않는다.
슬롯 조건만 확인하고 시전을 요청한다.
실제 대상 목록은 `BattleEngine.TryCast`가 `SkillRange`로 고른다.

`TryCast` 순서는 아래처럼 둔다.

1. `SkillRange`로 대상 목록을 고른다.
2. 대상이 없으면 아무 변경 없이 실패한다.
3. 내공 소비.
4. 쿨다운 설정.
5. 기세 획득.
6. `SkillCastPublished`.
7. `SkillDamageEffect` 적용.

`SkillCastPublished`가 먼저 나가는 이유는 Phase 3의 이벤트 순서를 유지하기 위해서다.
단, 맞은 적마다 보내는 이벤트는 아니다.
시전은 한 번이고, 데미지는 대상 수만큼 발생한다.
따라서 `SkillCastPublished`는 한 번, `DamagePublished`는 맞은 대상마다 한 번 나간다.
`SkillCastEvent`에는 피격 대상 id를 넣지 않는다.

> [!example]- `TryCast` 데미지 적용 조각
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs`.
>
> 클래스: `BattleEngine`.
>
> 메서드: `TryCast(...)`.
>
> 역할: `SkillRange`로 대상 목록을 고른 뒤, 시전 이벤트를 한 번 보내고,
> 대상마다 `SkillDamageEffect`를 적용한다.
> 맞은 대상 id는 `DamagePublished`가 담당한다.
>
> ```csharp
> public bool TryCast(Actor caster, int slotIndex)
> {
>     var slot = _context.Player.Skills[slotIndex];
>     var skill = slot.Data;
>     var targets = _context.GetAliveEnemiesByRange(skill.PreferredRange);
>
>     if (targets.Length == 0)
>         return false;
>
>     if (!SpendMana(skill.ManaCost))
>         return false;
>
>     slot.CooldownRemaining = skill.CooldownSec;
>     GainMomentum(skill.MomentumGainOnCast);
>
>     var skillCastEvent = new SkillCastEvent(caster.Id, slotIndex, skill.Id);
>     SkillCastPublished?.Invoke(skillCastEvent);
>
>     foreach (var skillTarget in targets.Span)
>     {
>         foreach (var effect in skill.Effects)
>         {
>             if (effect is SkillDamageEffect damageEffect)
>                 ApplyDamage(caster, skillTarget, damageEffect.Amount, DamageKind.Skill, skill.Id);
>         }
>     }
>
>     return true;
> }
>
> // ...
> ```

### 1.8 Engine — `EnemyAttackSystem`

`EnemyAttackSystem`은 Engage 중 살아있는 적이 Player를 일정 주기로 공격하게 한다.

조건:

- `context.Phase == BattlePhase.Engage`
- Player가 살아 있음.
- Enemy가 살아 있음.
- Enemy가 `Idle`.
- 일반 공격 쿨다운이 0.
- Player와 Enemy의 거리 ≤ `enemy.EngageDistance`

> [!example]- `EnemyAttackSystem.cs`
> 파일: `Assets/_Project/Scripts/Battle/Engine/Systems/EnemyAttackSystem.cs`.
>
> 클래스: `EnemyAttackSystem`.
>
> 역할: Engage 중 살아있는 적이 Player를 일정 주기로 공격하게 한다.
> HP 변경은 직접 하지 않고 `IDamageApplier.ApplyDamage`로 넘긴다.
>
> ```csharp
> using System;
> using MurimRunaway.Battle.Domain;
>
> namespace MurimRunaway.Battle.Engine
> {
>     public sealed class EnemyAttackSystem : IBattleSystem
>     {
>         private readonly IDamageApplier _damageApplier;
>
>         public EnemyAttackSystem(IDamageApplier damageApplier)
>         {
>             _damageApplier = damageApplier;
>         }
>
>         public void Tick(BattleContext context, float deltaTime)
>         {
>             if (context.Phase != BattlePhase.Engage)
>                 return;
>
>             var player = context.Player;
>             if (player.State == ActorState.Dead || player.Hp <= 0)
>                 return;
>
>             foreach (var enemy in context.Enemies)
>             {
>                 if (enemy.State == ActorState.Dead || enemy.Hp <= 0)
>                     continue;
>                 if (enemy.State != ActorState.Idle)
>                     continue;
>
>                 if (enemy.NormalAttackCooldown > 0f)
>                     enemy.NormalAttackCooldown = Math.Max(0f, enemy.NormalAttackCooldown - deltaTime);
>                 if (enemy.NormalAttackCooldown > 0f)
>                     continue;
>
>                 var distance = Math.Abs(enemy.Position - player.Position);
>                 if (distance > enemy.EngageDistance)
>                     continue;
>
>                 _damageApplier.ApplyDamage(enemy, player, enemy.NormalAttackDamage, DamageKind.NormalAttack, null);
>                 enemy.NormalAttackCooldown = enemy.NormalAttackPeriod;
>             }
>         }
>     }
> }
> ```

`EnemyAttackSystem.cs`는 파일을 만들기만 하면 실행되지 않는다.
`BattleEngine.Setup(...)`에서 `_systems` 배열에 등록해야 Tick마다 호출된다.

> [!example]- `EnemyAttackSystem` 등록
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs`.
>
> 클래스: `BattleEngine`.
>
> 메서드: `Setup(...)`.
>
> 역할: Engine이 매 Tick 실행할 시스템 목록에 `EnemyAttackSystem`을 추가한다.
> `new EnemyAttackSystem(this)`에서 `this`는 `IDamageApplier`를 구현한 `BattleEngine`이다.
>
> ```csharp
> // ...
> _systems = new IBattleSystem[]
> {
>     new MovementSystem(),
>     new EngagementSystem(),
>     new CastingSystem(this),
>     new EnemyAttackSystem(this),
> };
> ```

### 1.9 Engine — Tick 끝 사망/승패 정리

시스템이 모두 돈 뒤 한 번만 정리한다.

`HandleTick` 마지막 흐름:

1. 모든 system Tick 실행.
2. `ResolveDeathsAndResult()`.
3. `PublishSnapshot()`.

시스템 등록 순서:

1. `MovementSystem`
2. `EngagementSystem`
3. `CastingSystem`
4. `EnemyAttackSystem`

> [!example]- 사망/승패 정리 코드
> 파일: `Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs`.
>
> 클래스: `BattleEngine`.
>
> 역할: 모든 시스템 Tick이 끝난 뒤 HP 0 액터를 사망 처리하고,
> 승리/패배 결과를 한 번만 발행한다.
>
> 아래 두 메서드는 `BattleEngine` 클래스 안에 추가한다.
>
> ```csharp
> private void ResolveDeathsAndResult()
> {
>     if (_result != BattleResult.None)
>         return;
>
>     if (_context.Player.Hp <= 0 && _context.Player.State != ActorState.Dead)
>     {
>         _context.Player.State = ActorState.Dead;
>         ActorDeathPublished?.Invoke(new ActorDeathEvent(_context.Player.Id));
>     }
>
>     foreach (var enemy in _context.Enemies)
>     {
>         if (enemy.Hp > 0 || enemy.State == ActorState.Dead)
>             continue;
>
>         enemy.State = ActorState.Dead;
>         ActorDeathPublished?.Invoke(new ActorDeathEvent(enemy.Id));
>     }
>
>     if (_context.Player.State == ActorState.Dead)
>     {
>         PublishBattleResult(BattleResult.Defeat);
>         return;
>     }
>
>     if (_context.Enemies.All(enemy => enemy.State == ActorState.Dead))
>         PublishBattleResult(BattleResult.Victory);
> }
> ```
>
> ```csharp
> private void PublishBattleResult(BattleResult result)
> {
>     _result = result;
>     _context.Phase = BattlePhase.Resolve;
>     BattleResultPublished?.Invoke(result);
> }
> ```
>
> 아래 조각은 `HandleTick(...)`의 시스템 Tick 루프 바로 뒤에 넣는다.
>
> ```csharp
> // ...
> foreach (var system in _systems)
>     system.Tick(_context, deltaTime);
>
> ResolveDeathsAndResult();
> PublishSnapshot();
> ```

> [!warning]- HP 0 적의 같은 Tick 공격
> `EnemyAttackSystem`은 `enemy.State == Dead`뿐 아니라 `enemy.Hp <= 0`도 함께 본다.
> 무공 데미지로 HP가 0이 된 적은 아직 Tick 끝 사망 처리 전일 수 있기 때문이다.

### 1.10 기존 에셋 갱신과 View 연결

Phase 4에서는 새 무공이나 새 적 에셋을 만들지 않는다.
목표는 이미 있는 플레이 확인용 에셋이 새 데미지 규칙을 쓰게 만드는 것이다.

먼저 `SkillEffect` 추상화가 Unity Inspector에서 실제로 편하게 설정되는지 확인한다.
현재 구조는 `SkillEffect` base 클래스 하나를 두고,
`SkillDamageEffect` 같은 하위 클래스를 `SkillEffect[]`에 넣는 다형성 방식이다.

확인할 것:

1. `SkillData.Effects`의 `+` 버튼을 누른다.
2. 새 Element에서 `SkillDamageEffect` 같은 구체 타입을 선택할 수 있는지 본다.
3. 타입 선택 후 `Amount` 필드가 Inspector에 보이는지 본다.

이 셋 중 하나라도 안 되면,
에셋 연결 전에 구조 선택을 먼저 해야 한다.
기본 Inspector에서 타입 선택이 안 되면 플레이어가 에셋을 안정적으로 만들 수 없기 때문이다.

SkillData 에셋에는 `SkillDamageEffect`를 붙인다.

| 파일 | 추천 효과 |
|------|-----------|
| `Assets/_Project/Data/Skills/tae_in_jang.asset` | `SkillDamageEffect.Amount = 5` |
| `Assets/_Project/Data/Skills/cheonha_36_geom.asset` | `SkillDamageEffect.Amount = 5` |
| `Assets/_Project/Data/Skills/simbeop_unki.asset` | 비워 둠 |

EnemyData 에셋에는 일반 공격 값을 채운다.

| 파일 | 필드 | 추천값 |
|------|------|--------|
| `Assets/_Project/Data/Enemies/EnemyData.asset` | `NormalAttackDamage` | 3 |
| `Assets/_Project/Data/Enemies/EnemyData.asset` | `NormalAttackPeriod` | 2.5 |
| `Assets/_Project/Data/Enemies/EnemyData.asset` | `EngageDistance` | 20 |

`BattleSceneController`는 최소 확인만 붙인다.

- Player HP 게이지가 적 공격으로 줄어드는지 본다.
- Enemy가 Dead가 되면 마커를 숨기거나 흐리게 표시한다.
- `BattleResultPublished`를 받으면 임시 디버그 텍스트에 코드 결과를 표시한다.

> [!important] UI 문구 정책
> `Defeat`는 코드 판정명으로만 쓴다.
> 플레이어에게 보이는 UI에는 "패배" 단어를 쓰지 않는다.
> 실제 결과 화면은 "재도전", "다시 도전" 계열 문구를 우선 검토한다.

> [!warning]- `SkillDamageEffect`가 Inspector에서 불편할 때
> `SkillData.Effects`가 `[SerializeReference]` 배열이면 Unity 기본 Inspector에서 추가하기 불편할 수 있다.
> `+` 버튼을 눌러 Element가 생겨도 타입 선택 메뉴가 안 나오면 기본 Inspector만으로는 설정할 수 없다.
>
> 이 경우 바로 코드를 추가하지 말고 먼저 선택지를 정한다.
> Phase 4에서는 아래 둘 중 하나를 고른다.
>
> 1. 임시 Editor 도구를 만든다.
>    `SkillData`를 선택했을 때 `SkillDamageEffect.Amount = 5`를 넣어주는 버튼을 만든다.
>    장점은 현재 `SkillEffect[]` 구조를 유지한다는 점이다.
>    단점은 런타임 규칙과 무관한 Editor 보조 코드가 생긴다는 점이다.
> 2. Phase 4 동안 `SkillData`를 단순화한다.
>    예를 들어 `DamageAmount` 같은 직렬화 필드를 직접 둔다.
>    장점은 Inspector에서 바로 보인다는 점이다.
>    단점은 나중에 여러 효과 구조가 필요할 때 다시 `SkillEffect[]`로 되돌려야 한다는 점이다.
>
> 선택 전에는 에셋을 손으로 YAML 수정하지 않는다.
> Unity 직렬화 포맷을 직접 고치면 `SerializeReference` 참조 ID가 깨질 수 있다.

---

## 2. 테스트 계획

### 2.1 필수 EditMode 테스트

| 테스트 | 확인 |
|--------|------|
| `EnemyNormalAttackTests` | 적 1명이 2.5초마다 Player HP를 3씩 깎는다. |
| `SkillDamageTests` | 데미지 5 무공이 적 HP를 정확히 5 깎는다. |
| `SkillRangeDamageTests` | `Single`/`NearbyPair`/`All` 대상 수가 맞다. |
| `ActorDeathTests` | HP 0 액터는 사망 이벤트를 한 번만 낸다. |
| `BattleResultTests` | 승리/패배 결과 이벤트는 한 번만 난다. |
| `DamageDeterminismTests` | 같은 입력이면 데미지/사망 이벤트 시퀀스가 같다. |

### 2.2 `BattleTestFactory` 보강

테스트 예시가 빌드되려면 factory도 같이 넓힌다.

- `CreateEnemy`에 `maxHp`, `normalAttackDamage`, `normalAttackPeriod`, `engageDistance` 선택 인자를 추가한다.
- `CreateSkill`에 `damageAmount` 선택 인자를 추가한다.
- `damageAmount > 0`이면 `SkillDamageEffect` 1개를 `Effects`에 넣는다.

> [!example]- 테스트 예제
> 파일: `Assets/_Project/Tests/Battle/EnemyNormalAttackTests.cs`.
>
> 클래스: `EnemyNormalAttackTests`.
>
> 역할: `EnemyAttackSystem`이 주기마다 Player HP를 깎는지 확인한다.
> `BattleTestFactory` 보강이 먼저 되어 있어야 빌드된다.
>
> ```csharp
> using NUnit.Framework;
> using MurimRunaway.Battle.Domain;
>
> namespace MurimRunaway.Battle.Tests
> {
>     public class EnemyNormalAttackTests
>     {
>         [Test]
>         public void 적은_주기마다_플레이어_HP를_깎는다()
>         {
>             var tick = new MockTickService();
>             var enemy = BattleTestFactory.CreateEnemy(
>                 spawnPosition: 20f,
>                 normalAttackDamage: 3,
>                 normalAttackPeriod: 2.5f,
>                 engageDistance: 20f);
>             var engine = BattleTestFactory.CreateEngine(
>                 tick,
>                 moveSpeed: 0f,
>                 engageDistance: 20f,
>                 maxHp: 100,
>                 enemies: new[] { enemy });
>
>             BattleSnapshot snapshot = default;
>             engine.SnapshotPublished += next => snapshot = next;
>
>             engine.Start();
>             tick.PumpTicks(500);
>
>             Assert.AreEqual(70, snapshot.Actors[0].Hp);
>         }
>     }
> }
> ```
>
> 파일: `Assets/_Project/Tests/Battle/SkillDamageTests.cs`.
>
> 클래스: `SkillDamageTests`.
>
> 역할: `BattleEngine.TryCast`가 `SkillDamageEffect`를 읽고 적 HP를 깎는지 확인한다.
> 이 테스트가 통과하면 `GetAliveEnemiesByRange`와 `ApplyDamage` 연결도 같이 검증된다.
>
> ```csharp
> using NUnit.Framework;
> using MurimRunaway.Battle.Domain;
>
> namespace MurimRunaway.Battle.Tests
> {
>     public class SkillDamageTests
>     {
>         [Test]
>         public void 데미지_무공은_적_HP를_깎는다()
>         {
>             var tick = new MockTickService();
>             var enemy = BattleTestFactory.CreateEnemy(spawnPosition: 20f, maxHp: 30);
>             var skill = BattleTestFactory.CreateSkill(
>                 "tae_in_jang",
>                 SkillRange.Single,
>                 manaCost: 0,
>                 damageAmount: 5);
>             var engine = BattleTestFactory.CreateEngine(
>                 tick,
>                 moveSpeed: 0f,
>                 enemies: new[] { enemy },
>                 skills: new[] { skill });
>
>             BattleSnapshot snapshot = default;
>             engine.SnapshotPublished += next => snapshot = next;
>
>             engine.Start();
>             tick.PumpTicks(1);
>
>             Assert.AreEqual(25, snapshot.Actors[1].Hp);
>         }
>     }
> }
> ```

---

## 3. 최종 검증

[[BATTLE_DESIGN]] §3 Phase 4 Acceptance 기준.

- [ ] EditMode: 적 1명(`normalAttackDamage=3`, `normalAttackPeriod=2.5`) vs Player HP 100 → 25초 후 Player HP 70.
- [ ] EditMode: 데미지 5 무공 시전 → 적 HP 정확히 5 감소.
- [ ] EditMode: 결정성 — 같은 seed + 같은 `BattleStartData` → `DamagePublished`/`ActorDeathPublished` 시퀀스 동일.
- [ ] EditMode: 사망 이벤트는 액터마다 한 번만 발생.
- [ ] EditMode: 전투 결과 이벤트는 한 번만 발생.
- [ ] PlayMode: 적이 일정 주기로 Player HP를 깎는다.
- [ ] PlayMode: 스킬이 적 HP를 깎고, 적이 죽으면 전투가 `Victory`로 끝난다.
- [ ] PlayMode: Player HP가 0이면 전투가 `Defeat`로 끝난다.
- [ ] 에셋: 기존 `SkillData`/`EnemyData` 에셋에 Phase 4 값이 들어 있다.
- [ ] `dotnet build MurimRunaway.Battle.Tests.csproj` 통과.
- [ ] `dotnet build MurimRunaway.Battle.View.csproj` 통과.
- [ ] `dotnet test MurimRunaway.Battle.Tests.csproj --no-build --verbosity normal` 통과.
- [ ] [[BATTLE_DESIGN]] §5 변경 이력에 Phase 4 완료 한 줄 추가.

---

## 4. 접힌 상세

> [!note]- 설계 메모
> `SkillRange`는 이제 거리 등급이 아니다.
> `Single`/`NearbyPair`/`All`은 무공이 몇 명을 때리는지 나타낸다.
> 적 일반 공격의 거리 판정은 `EnemyData.EngageDistance`와 `Actor.EngageDistance`가 맡는다.
>
> `IDamageResolver`는 아직 만들지 않는다.
> Phase 4의 데미지 공식은 `finalDamage = baseDamage`뿐이다.
> 회피, 방어, 매트릭스 배율처럼 두 번째 규칙이 생길 때 resolver를 추출한다.

> [!warning]- 흔한 함정
> - **`SkillRange`를 거리로 다시 쓰기**  
>   `SkillRange`는 타겟 수다.
>   적 일반 공격 거리는 `EngageDistance`다.
>
> - **HP를 직접 깎기**  
>   `target.Hp -= amount`를 여러 곳에 흩뿌리면 이벤트와 clamp가 깨진다.
>   반드시 `ApplyDamage`를 통과한다.
>
> - **사망 즉시 `Resolve`를 여러 번 발행하기**  
>   `_result != BattleResult.None`이면 더 발행하지 않는다.
>
> - **HP 0인데 아직 `State`가 Dead가 아니라고 공격시키기**  
>   `EnemyAttackSystem`은 `enemy.Hp <= 0`도 함께 본다.
>
> - **회복·버프를 미리 만들기**  
>   Phase 4는 `SkillDamageEffect`만 쓴다.
>   다음 규칙이 생길 때 효과 타입을 추가한다.
