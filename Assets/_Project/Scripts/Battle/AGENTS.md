# Battle/AGENTS.md — 전투 코드 배치 규칙

`Assets/_Project/Scripts/Battle/` 하위 C# 코드에 적용한다.
상위 `Assets/_Project/Scripts/AGENTS.md`를 우선 따르고, 이 파일은 전투 폴더의 레이어 배치 판단만 보강한다.

---

## 1. 레이어 배치

- **Domain**: 전투 규칙의 데이터 언어.
  `BattleSnapshot`, `ActorView`, 시전 이벤트 데이터처럼 Engine 밖으로 공개되고 View/Test가 읽는 전투 데이터는 여기에 둔다.
- **Engine**: 전투 시뮬레이션 실행.
  Tick 처리, 시스템 순서, 자원 변경, 시전 실행처럼 상태를 바꾸는 로직과 진입점은 여기에 둔다.
- **View**: Unity 표현 계층.
  UI, 프리팹 연결, 표시용 코루틴, Unity 컴포넌트는 여기에 둔다.

판단 기준: **Engine 밖으로 전달되는 읽기 전용 전투 결과인가?**
그렇다면 생성 위치가 Engine이어도 타입은 Domain에 둔다.

---

## 2. 전투 시작 입력

- `BattleStartData`와 `PlayerStartData`는 이미 정해진 한 전투의 시작값만 담는다.
- HP, 내공, 이동 속도, 사거리 같은 밸런스 기본값을 `StartData` 필드 초기값으로 넣지 않는다.
- 기본값은 Config, ScriptableObject, 씬 임시 입력, 테스트 빌더 중 한 곳에서 정하고 `StartData`에는 모두 채워서 넘긴다.
- 이렇게 해야 나중에 Config 로더가 들어와도 Domain 타입을 다시 뜯지 않는다.

---

## 3. 이벤트 데이터

- 값이 2개 이상인 공개 이벤트에 `Action<int, string, int>` 같은 원시 타입 나열을 쓰지 않는다.
- 필드 이름으로 의미가 읽히는 `readonly struct`를 만든다.
- 공개 이벤트 데이터 타입은 기본적으로 `Domain`에 둔다.
- `Dto`/`VO` 접미사는 쓰지 않는다.
  역할이 드러나는 도메인 이름을 쓴다.

예:

```csharp
public event Action<SkillCastEvent> SkillCastPublished;
```

---

## 4. Domain 타입 이름

- `Data`: 전투 시작 입력값 또는 ScriptableObject 정적 콘텐츠.
  예: `BattleStartData`, `PlayerStartData`, `EnemyData`, `SkillData`.
- `Snapshot`: 한 시점의 전체 읽기 전용 상태 묶음.
  예: `BattleSnapshot`.
- `View`: View/Test가 읽는 액터 단위 읽기 전용 사본.
  예: `ActorView`.
- `Event`: 플레이 중 한 번 발생한 알림의 데이터.
  예: `SkillCastEvent`.

`Data`는 "저장/입력/정적 정의"에 붙인다.
시전 성공처럼 플레이 중 한 번 발생한 알림은 `Data`가 아니라 `Event`로 부른다.

---

## 5. 이벤트 이름

- 외부로 발행되는 이벤트는 `Published` 계열 이름을 우선한다.
  예: `SnapshotPublished`, `SkillCastPublished`.
- `OnSkillCast` 같은 `OnX` 이름은 이벤트를 발생시키는 protected/private 메서드처럼 읽히므로 공개 이벤트 이름으로 쓰지 않는다.

---

## 6. 주석과 문서 언어

- 주석과 문서는 쉬운 단어를 우선한다.
- 수학·아키텍처 용어는 꼭 필요할 때만 쓴다.
- `payload`, `이산 사건`처럼 뜻을 한 번 더 해석해야 하는 말은 피한다.
  각각 `데이터`, `한 번 발생한 알림`처럼 바로 읽히는 말로 쓴다.
- 코드 주석에는 `Phase 3`, `Phase 9 활성` 같은 작업 단계 정보를 쓰지 않는다.
  주석은 현재 코드의 뜻만 설명하고, 단계 정보는 Guide/Learned 문서 산문에 둔다.
