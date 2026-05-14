# 전투 시스템 명세서 (Battle Design)

> **본 문서의 위상**
> - 정식 게임의 **전투 시스템 SSOT**.
> - 신규 설계 — 현 프로토타입 코드는 참고만, 본 문서의 코드 아키텍처를 따른다.
> - 본 문서는 **시스템 명세 + 구현 마일스톤** 두 역할을 겸한다. §3 "구현 단계 (Phases)"는 구현 순서대로 정의되며, 각 Phase의 Acceptance를 만족해야 다음 Phase로 진입한다.
> - [GAME_DESIGN.md](GAME_DESIGN.md)는 게임 비전·스코프·디자인 룰의 출발점이지만, 본 문서가 전투에 한해 정식 SSOT다. 충돌 시 본 문서 우선.
> - [MILESTONES.md](MILESTONES.md)는 프로토타입 진행 추적용으로 동결.
>
> **작성 규칙**
> - 결정된 것만 본문에 적는다. 미결은 §4 (Open Questions)에 모은다 — 본문에는 [OPEN] 마커를 두지 않는다.
> - Phase는 작은 단위로 나뉘며, 각 Phase가 끝나면 컴파일 + Unity 플레이 확인 + 커밋.
> - Phase 본문은 그 Phase에서 **새로 추가/변경되는 것**만 명시. 누적 SSOT는 §6 (Appendix).
> - 변경 사항은 §5 (변경 이력)에 한 줄 추가.

---

## 0. 메타

| 항목 | 값 |
|------|----|
| 문서 종류 | Technical Design Document (TDD) |
| 대상 | 무림도망자 정식 게임 — 전투 시스템 |
| 버전 | 0.4 (v4.1 스코프 컷 — PvP 보류, 재능 각성 모델) |
| 마지막 수정 | 2026-05-14 |
| 의존 문서 | GAME_DESIGN.md (디자인 출발점), CLAUDE.md (코드 컨벤션) |

---

## 0.5. v4 피벗 (2026-05-11) — 모바일 오토배틀 로그라이트

레퍼런스: **옵시디언 나이트** (ActFirst Games) — 사이드스크롤 오토배틀 + 레벨업 카드 드래프트 + 메타 진행. 1인 개발 적합성 평가 결과 v3(Steam 빌드크래프터) 대비 부담 피크 이동(전투 매트릭스 L → 모바일 UI L) + 백엔드 최소화로 결정.

**바뀌는 것 (요약)**:
- **플랫폼**: Steam(PC) → **Android 우선** (iOS는 v1.0 안정 후).
- **런 구조**: 15스테이지 단일 분기 → 절차 생성 스테이지 시퀀스. 런 길이 3~5분 기본, 후반 길어짐.
- **레벨업 시 카드 드래프트 (3택)** 신규 도입 — 매 레벨업 시 스킬/버프 카드 3장 중 1장.
- **메타 진행 신규** — 인벤토리·장비·영구 강화·골드 (런 간 누적).
- **비동기 PvP 신규** — 같은 시드 스테이지 도달 거리 비교 (BaaS 리더보드). *(※ v4.1 (2026-05-14)에서 v1.1+ 보류 — §5 변경 이력 참조)*
- **MindGame 매트릭스(P5 강공 / P6 오의)는 보스전 한정으로 스코프 축소** — 일반전은 풀 오토배틀, 보스전에서만 수싸움 발현. (결정: 2026-05-11, 옵나형 오토배틀 본질 유지 + v3 깊이 일부 보존)
- **재능 시스템 격상** — v3 백로그 → v4 차별점 핵심. 옵나의 약점(5~7h 정체감)을 메우는 본체.
- **컷**: 분파 정렬, 경로 분기, 기연 이벤트, 결말 5분기 — v1.0 범위 외(검토 보류).
- **컷**: 클랜/협동보스, 가챠/라이브옵스 — 1인 개발 불가능.

**그대로 유지**:
- 3계층 분리(Domain/Engine/View), 결정성, 시드 기반 RNG, ScriptableObject 데이터.
- Phase 0~4 본문 — 오토배틀 코어 자체로 살아남음.
- Phase 9 회피 — 자동전투에서도 시각 피드백 + RNG로 유효.

마일스톤은 [MILESTONES.md](MILESTONES.md) (v4)를 따른다.

---

## 1. Goals & Non-goals

### 1.1 Goals (v4)
1. **풀 오토배틀**(일반전 자동 시전) + **보스전 한정 수싸움**(강공 방어 4택 / 오의 공격 3택)이 함께 작동하는 전투 루프.
2. 보스전의 강공·오의는 **정보 기반 결정**(오성 hint) — 운이 결과를 100% 좌우하지 않는다.
3. 재능(천무지체/카피/대종사)에 따라 **자동 흐름의 모양과 카드 드래프트 가중치가 달라진다**. v4.1: 재능은 런 시작 선택이 아니라 **런 종료 시 확률 각성**으로 캐릭터에 0~3개 누적(동시 작동·가중치 스택). **첫 런 = 재능 0개 = 신참 베이스라인**(균일 가중치) — 재능 의존 메커니즘은 모두 0개 케이스를 default로 가져야 한다.
4. 코드는 **Domain / Engine / View 3계층 분리** — Domain 단위 테스트 가능.
5. 모든 RNG는 **시드 기반 결정적** — 같은 입력은 같은 결과 (디버깅·재현성·외부 시연 재현).

### 1.2 Non-goals (전투 시스템 외)
- 월드맵, 인카운트, 분파 정렬, 결말 분기 — v1.0 범위 외.
- 비동기 PvP — v4.1 스코프 컷, v1.1+ 보류 ([MILESTONES.md](MILESTONES.md) §5).
- 사운드, 세이브/로드, 설정 메뉴 — 폴리시 단계.
- 다국어 **콘텐츠 추가**(영어/일본어 등 실제 번역)는 v1.0 직전. 단 **L10n 인프라**(Unity Localization, NameKey 컨벤션)는 Phase 1부터 적용 — 후반 일괄 변환 비용 회피.
- PC/Steam 빌드 — v4는 Android 우선, iOS 후속.
- 파티클·카메라 워크·스프라이트 폴리시.
- 애니메이션 시스템 (placeholder 색+텍스트 유지).
- 클랜/협동보스/실시간 PvP/가챠 — 1인 개발 부담 회피.

### 1.3 v4 신규 책임 (전투 시스템 경계 안)
- 런 구조 (스테이지 시퀀스·절차 생성·런 상태 집계) — Phase 11에서 정의.
- 카드 드래프트 (레벨업 3택) — Phase 12에서 정의.
- 일반전과 보스전의 구분 — `EnemyData.isBoss` 플래그로 매트릭스 발동 게이팅.

---

## 2. 아키텍처 원칙

### 2.1 3계층 분리
```
[Domain]          POCO + ScriptableObject (값/룰 정의)
   ▲                    ▲ (인터페이스 계약)
[Engine]          시간축 진행 + 룰 적용 + RNG
   ▲                    ▲ (이벤트 송신 / 입력 수신)
[View]            MonoBehaviour, UI, VFX, Audio (read-only 출력)
```

| 계층 | 책임 | Unity 의존 | 룰 결정 권한 |
|------|------|-----------|-------------|
| Domain | 데이터 모델, SO 정의, POCO 상태 | SO에만 (CreateAssetMenu 한정) | 없음 (값 보유) |
| Engine | Tick, Skill 실행, MindGame 판정, 데미지 계산, RNG | 가능한 한 표준 C# | **있음** |
| View | 화면 표시, 입력 수집, 사운드/VFX | 풀 Unity | 없음 (이벤트 구독 + Engine 호출) |

### 2.2 의존 방향 (강제 규칙)
- **하위 계층은 상위 계층을 모른다.** Domain은 Engine을 참조하지 않고, Engine은 View를 참조하지 않는다.
- 통신은 **이벤트** 또는 **공개 read-only 상태(BattleSnapshot)** 로만.
- View → Engine 호출은 명시된 입력 인터페이스(`IBattleInput`)만 통한다.
- asmdef로 의존 방향을 컴파일 타임에 강제한다.

### 2.3 폴더 / 네임스페이스
| 네임스페이스 | 폴더 | 어셈블리 |
|-------------|------|---------|
| `MurimRunaway.Battle.Domain` | `Assets/_Project/Scripts/Battle/Domain/` | `_MurimRunaway.Battle.Domain.asmdef` |
| `MurimRunaway.Battle.Engine` | `Assets/_Project/Scripts/Battle/Engine/` | `_MurimRunaway.Battle.Engine.asmdef` (Domain 참조) |
| `MurimRunaway.Battle.View` | `Assets/_Project/Scripts/Battle/View/` | `_MurimRunaway.Battle.View.asmdef` (Domain + Engine 참조) |
| `MurimRunaway.Battle.Tests` | `Assets/_Project/Tests/Battle/` | `_MurimRunaway.Battle.Tests.asmdef` (Domain + Engine 참조, View 참조 금지) |

> 파일명 `_` prefix는 의도적 — Unity Project 창 정렬 시 폴더 상단에 위치시키기 위함. 어셈블리 정의(`name` 필드) 자체는 `MurimRunaway.Battle.*` 그대로.

### 2.4 결정성 (Determinism)
- 엔진 내 모든 RNG는 `IRngService` 한 곳만 통한다.
- 시드는 `(runSeed, battleIndex)` 의 결합으로 생성. 같은 시드 + 같은 BattleStartData + 같은 입력 시퀀스 = 같은 결과.
- `Time.deltaTime` 직접 사용 금지. Engine은 `ITickService` 가 전달하는 `dt` 만 사용한다.
- 고정 dt = **0.05초** (20Hz). View는 별도로 자기 프레임에 보간 표시.
- 부동소수 비교는 `<=` / `>=` 만 사용 (등가 비교 금지) — 거리·시간 오차 회피.

### 2.5 데이터 컨벤션
- 게임 콘텐츠(스킬·적·재능·스테이지)는 모두 **ScriptableObject**.
- 콘텐츠 SO는 `Assets/_Project/Data/{Skills,Enemies,Talents,Encounters,Stages}/`.
- 런타임 변하는 값은 SO에 저장 금지 — POCO 인스턴스에 복사 후 변경.

**Id 규약** (Sheets 이식 대비):
- 모든 콘텐츠 SO는 `Id: string` 필드를 가진다 — 한 카테고리 안에서 unique.
- 포맷: `[a-z0-9_]` snake_case. (예: `goblin_grunt`, `sword_slash`, `wave_01_intro`)
- 한글·공백·대문자 금지 — Sheets/CSV 컬럼 호환.
- SO → SO 참조는 Phase 1~2까지는 Inspector 직접 참조 OK. Sheets 도입 시점(미정)에 Id resolver 패턴으로 일괄 전환.

> **[리팩토링 트리거] Sheets 이식 시 숫자 PK 추가**
> 현재 `Id: string`(snake_case)이 (a) 외래키 타겟 + (b) 코드네임을 겸직. Sheets로 이식하는 시점에 한국 서버 업계 표준에 맞춰 **숫자 PK 컬럼을 추가**한다:
> - 기존 `Id: string` → `Code: string`으로 rename (의미: 코드네임)
> - 신규 `Id: int` 추가 (외래키 타겟)
> - `NameKey`는 그대로 (`enemy.<Code>` 자동 도출 유지)
> - `Actor.TemplateId`는 디버그 가독성 위해 `Code` 추적 (`string` 유지) — 숫자 Id는 데이터 레이어에만.
>
> **지금 미리 박지 않는 이유**: SO 시대엔 Unity가 자산 GUID로 참조하므로 숫자 Id는 잠자는 필드. 매 자산 생성 시 다음 번호 채워야 하는 사람 작업이 사고 위험. Sheets Importer가 일괄 부여하는 게 자연스러움.

**로컬라이제이션 규약**:
- 표시 문자열은 SO에 직접 저장 금지. `NameKey: string`(예: `enemy.goblin_grunt`) 형태의 키만 저장.
- 키 네임스페이스: `<category>.<id>` (예: `enemy.goblin_grunt`, `skill.sword_slash.name`, `skill.sword_slash.desc`).
- 실제 문자열은 **Unity Localization 패키지**의 String Table로 관리.
- Phase 1~2 시점엔 String Table에 한국어만 채워두고, 다국어 추가는 v1.0 직전. 인프라만 미리 박아 후반 일괄 변환 비용 회피.

---

## 3. 구현 단계 (Phases)

> 진행 원칙:
> 1. 한 Phase가 끝나면 모든 Acceptance 체크 + 컴파일 통과 + Unity 플레이 한 번 + 커밋.
> 2. EditMode 테스트가 가능한 Acceptance는 반드시 테스트로 작성.
> 3. 한 Phase 안에 두 가지 큰 책임을 합치지 않는다 — 합쳐졌다면 분할.

---

### Phase 0. 기반 (Foundation)

**Goal**: 빈 골격 컴파일. 폴더/asmdef/네임스페이스, ITickService·IRngService 인터페이스 + Mock 구현, 빈 BattleEngine. 화면에 카운터 1줄.

**Non-goals**: 액터, 스킬, UI, 룰, 데미지 — 전부 미존재.

**Data**: 없음.

**Interfaces (Engine)**:
```csharp
public interface ITickService
{
    event Action<float> Ticked;     // dt = 0.05f 고정
    void Pause();                   // 호출 시 Ticked 발생 중지
    void Resume();
    bool IsRunning { get; }
}

public interface IRngService
{
    void Reseed(int seed);
    int    NextInt(int minIncl, int maxExcl);
    float  NextFloat01();
    T      Pick<T>(IReadOnlyList<T> pool);
    T      PickWeighted<T>(IReadOnlyList<(T item, float weight)> pool);
}

public interface IBattleEngine
{
    void Setup(BattleStartData data, int seed);
    void Start();
    event Action<BattleSnapshot> SnapshotPublished;   // 매 틱 끝
    event Action<BattleResult>   OnResult;     // 종료 시 1회
}
```

**Behaviors**:
1. `TickService` 가 0.05s 간격으로 `Ticked(0.05f)` 호출 (Unity Update 내부에서 누산 후 호출).
2. `RngService` 는 `Reseed(seed)` 호출 시점부터 결정적.
3. `BattleEngine.Start()` 호출 시 빈 BattleSnapshot 송신만 함 (룰 X).

**Invariants**:
- I-0.1: Domain asmdef는 UnityEngine 참조가 SO 정의 외에 없다 (스크립트 grep으로 검증).
- I-0.2: Engine asmdef는 View asmdef를 참조하지 않는다 (asmdef references 검증).
- I-0.3: 같은 seed → `RngService.NextFloat01()` 시퀀스가 동일.

**Edge**:
- TickService.Pause()/Resume()이 Ticked 발생을 제어한다. Engine은 `Time.timeScale`에 의존하지 않는다 (Engine 계층의 Unity 무관 원칙). View의 게임 일시정지는 IBattleInput을 거쳐 Engine이 자체 결정한다.

**Acceptance**:
- [x] 폴더/asmdef/네임스페이스 생성, 컴파일 통과.
- [x] EditMode 테스트: TickService Mock 으로 N회 발동 시 dt 합 = N × 0.05 ± 1e-6.
- [x] EditMode 테스트: 동일 seed로 RngService 두 번 시뮬, NextFloat01() 100회 결과가 모두 동일.
- [x] PlayMode: BattleSceneController가 매 틱 화면 텍스트 카운터를 1씩 증가.

---

### Phase 1. Actor & 거리축 + 플레이어 진군

**Goal**: Player 1명, Enemy 1~4명. 1D 거리축. 시간이 흐르고 **플레이어가 적 웨이브 쪽으로 진군**해 교전 거리까지 도달. HP 정의(아직 데미지 없음).

**Non-goals**: 데미지, 스킬 시전, 자원.

**Data (추가)**:

`ActorState` enum (Domain):
- `Idle` / `Running` / `Casting` / `HeavyCharging` / `Stunned` / `Dead`
- 본 Phase에서 사용되는 상태: Idle, Running, Dead (남은 상태는 후속 Phase에서 활성화).

`Actor` POCO (Domain — 런타임 인스턴스):
| 필드 | 타입 | 단위 | 범위 | 설명 |
|------|------|------|------|------|
| id | int | — | unique/battle | 인스턴스 식별자 |
| side | ActorSide enum | — | Player / Enemy | 진영 |
| hp | int | HP | [0, maxHp] | 0이면 Dead |
| maxHp | int | HP | [1, ∞) | 최댓값 |
| position | float | distance unit | [0.0, ∞) | 0=런 시작 지점. Player는 진군에 따라 증가, Enemy는 spawnPosition 고정 |
| state | ActorState | — | — | 현재 FSM 상태 |
| sourceId | string | — | SO guid | EnemyData.id (Player는 "player") |

`EnemyData` SO (Domain) — 본 Phase에서 정의하는 필드만:
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| id | string | — | 고유 식별자 (snake_case) |
| nameKey | string | — | L10n 키 (`enemy.<id>`) — Unity Localization String Table 조회 |
| maxHp | int | 30 | 최대 HP |
| spawnPosition | float | 100.0 | 스폰 위치 (고정, 적은 이동하지 않음) |


`BattleStartData` (Engine 입력):
| 필드 | 타입 | 설명 |
|------|------|------|
| playerMaxHp | int | 플레이어 시작 HP |
| playerRunSpeed | float | 플레이어 진군 속도 (dist/s, 디폴트 5.0). Phase 9 경공이 일시 부스트 |
| playerAttackRange | float | 플레이어 공격 사거리 (디폴트 20.0). 가장 가까운 적과의 거리 ≤ 이 값이면 진군 정지. Phase 8/12에서 재능·카드로 가변 |
| enemies | EnemyData[] | 등장 적 |

> **AttackRange는 Actor 공통 속성** — Phase 1엔 Player만 사용, Phase 4+에서 Enemy도 능동 공격 시작 거리로 활용 (같은 필드 의미·다른 소유자).

`BattleSnapshot` (Engine → View):
| 필드 | 타입 | 설명 |
|------|------|------|
| tickIndex | long | 시뮬 스텝 ID — 결정론 비교 키·로그 식별자 (정수) |
| timeSec | float | 전투 시작 후 누적 경과(초) — UI/게임 로직 타이밍 (Engine 내부에서 dt 누적) |
| actors | ActorView[] | 액터 read-only 사본 |
| phase | BattlePhase enum | Setup/Approach/Engage/Resolve |

**State machine (Battle FSM, 추가)**:
```
Setup → Approach → Resolve(victory/defeat)
```
- 본 Phase에선 Engage 없이 Approach가 끝나면 자동으로 Resolve(victory) — 적이 stopPosition 도달만 보면 됨. Engage 페이즈는 P3에서 도입.

**State machine (Actor FSM, 추가)**:
- Player: Running → (가장 가까운 살아있는 Enemy와의 거리 ≤ player.attackRange) → Idle
- Enemy: Idle 고정 (적은 진군하지 않음, Phase 1에서는 사망도 없음).
- **Player.position은 0에서 시작해 Running 상태일 때 매 틱 증가.** 적은 spawnPosition에 고정. 경공(Phase 9)은 `playerRunSpeed`의 일시 부스트로 구현.

**Behaviors**:
1. `Setup(data, seed)` — Player Actor 1 생성 (position=0, state=Running). Enemy Actor[] 생성 (position=spawnPosition, state=Idle). RngService.Reseed(seed).
2. 매 Tick(dt): Player가 Running이면 `player.position += playerRunSpeed × dt`. 가장 가까운 살아있는 Enemy와의 거리(`enemy.position - player.position`)가 ≤ `player.attackRange`이면 Player → Idle.
3. Player가 Idle로 전이하면 BattlePhase = Resolve(victory) 송신 후 종료.
4. 매 Tick 끝에 BattleSnapshot 송신.

**Formulas**: 없음.

**Interfaces (추가)**:
```csharp
public interface IBattleInput
{
    // 본 Phase에선 사용 X (후속 Phase에서 사용)
}

public readonly struct ActorView
{
    public int Id;
    public ActorSide Side;
    public int Hp;
    public int MaxHp;
    public float Position;
    public ActorState State;
}
```

**Invariants**:
- I-1.1: 모든 시점에 `0 ≤ actor.hp ≤ actor.maxHp`.
- I-1.2: 모든 시점에 `actor.position ≥ 0`. Enemy.position은 spawnPosition으로 고정. Player.position은 단조 증가.
- I-1.3: 같은 (seed, BattleStartData) → 모든 BattleSnapshot 시퀀스가 byte-equal (결정성).
- I-1.4: actor.id는 한 전투 내 unique.

**Edge**:
- 동일 spawnPosition에 적 N명을 배치할 경우, position은 같지만 id가 다르므로 ActorView 순서는 id 오름차순으로 직렬화한다 (스냅샷 결정성 보장).
- Tick dt가 큰 경우(에디터 일시정지 후 재개) Player가 가장 가까운 적의 공격 사거리 안쪽으로 들어가버릴 수 있음 → Idle 전이 시 `player.position = nearestEnemy.position - player.attackRange` 로 클램프.

**Acceptance**:
- [ ] Player 1 (runSpeed=5, attackRange=20), Enemy 1 (spawn=100) → 16.0s ± 0.05s 안에 Player Idle 전이 + Resolve(victory) 송신.
- [ ] Player(attackRange=20) vs Enemy 3명 spawn={60, 80, 100} → 가장 가까운 적(spawn=60)이 사거리 내가 되는 순간 Resolve. Player.position=40으로 클램프, 나머지 적 2명은 Idle 유지(Phase 1엔 Player만 진군).
- [ ] EditMode 테스트: Approach(1명) + NearestEnemy(3명) 통과. (결정성 테스트는 RNG 실사용 시작 Phase에서 도입.)
- [ ] PlayMode 시각 확인: 플레이어 마커가 거리 게이지에서 적 쪽으로 실시간 이동.

---

### Phase 2. 자원 (HP / 내공)

**Goal**: Player 자원 2종(HP·내공) 모델 + 변경 룰 + Snapshot 반영. 자원 변경 단일 진입점(`IResourceMutator`) 노출.

**Non-goals**: 자원을 소비/회복하는 스킬·룰은 Phase 3 이후. 본 Phase는 자원 컨테이너만.

> **스코프 결정 (2026-05-14, v0.4.2)**: 본 Phase에서 **기세(momentum)/오성(wisdom)을 제외**.
> - **기세**: 단독 자원으로는 의미 없음 — 스킬의 "쌓기/소비" 메커닉과 짝일 때만 자원으로 성립. Phase 3 SkillData 도입과 동반으로 이동 (Phase 3 Data에 포함).
> - **오성**: per-battle 자원으로 두면 키우기 게임 결에서 의미 잃음(곧 max). **메타 패시브 슬롯(PlayerData, Phase 11+)** 으로 이동 — Phase 7 hint 정확도는 메타 슬롯 값을 읽는 형태로 명세. Phase 11+ 도입 전엔 placeholder fixed 값으로 우회.

**Data (추가)**:

`PlayerActor` (Actor 상속, Domain):
| 필드 | 타입 | 단위 | 범위 | 디폴트 | 설명 |
|------|------|------|------|--------|------|
| mana | int | 내공 | [0, maxMana] | 50 | 시전 비용 자원 |
| maxMana | int | 내공 | [1, ∞) | 100 | 최대 내공 |

ActorView 확장 (Player 한정):
| 필드 추가 | |
|----------|--|
| Mana, MaxMana | (Enemy ActorView에는 없음) |

**State machine**: 변경 없음.

**Behaviors**:
1. `IResourceMutator` Engine 내부 인터페이스 — 자원 변경의 단일 진입점.
   - `SpendMana(int amount)` — `amount > mana` 시 `false` 반환, 변경 없음 (atomic).
   - `GainMana(int amount)` — `min(mana + amount, maxMana)` 로 클램프 (silent).
2. 모든 자원 변경은 `IResourceMutator`만 통과 — 다른 코드에서 직접 필드 쓰기 금지 (코드 리뷰 시 검증).
3. 자원이 변경되면 Snapshot의 다음 발행에 반영 (즉시 이벤트 X — Snapshot 일관성 우선).

**Formulas**: 없음 (변경 룰만).

**Invariants**:
- I-2.1: `0 ≤ mana ≤ maxMana`.
- I-2.2: SpendMana가 `false` 반환했을 때 mana는 변경되지 않음 (atomic).

**Edge**:
- GainMana가 maxMana를 초과하려 할 때 → `min(mana + amount, maxMana)` 로 클램프 (silent).

**Acceptance**:
- [ ] EditMode: SpendMana로 0 미만이 되는 시도가 false 반환 + 값 불변.
- [ ] EditMode: GainMana가 maxMana 초과 시 maxMana로 클램프.
- [ ] PlayMode: 화면에 HP·내공 게이지 표시.
- [ ] PlayMode: VContainer LifetimeScope에서 의존성 주입 동작 (Phase 1 흐름 회귀 없음).

---

### Phase 3. 무공 자동 시전 (단일 초식 → 4종 분류)

**Goal**: SkillData(SO) 정의. 자동 시전 결정 트리(거리·쿨·내공). 단일 초식부터 시작해 4종(초식/심법/경공/오의)을 모두 지원하되 본 Phase에선 **초식·심법** 두 종만 활성화.

**Non-goals**: 데미지(Phase 4), 강공/오의 분기(Phase 5/6).

> **[리팩토링 트리거]** 본 Phase 첫 작업으로 `IBattleSystem` 패턴 도입. Phase 1의 BattleEngine.HandleTick에 들어있던 진군·교전 로직을 `MovementSystem`(Player run) + `EngagementSystem`(교전 거리 검사 → Idle 전이)으로 추출하고, 본 Phase의 자동 시전 결정 트리를 `CastingSystem`으로 신설한다. 이유: tick mutation 개념이 1개(Phase 1)에서 2개+(P3)로 늘어나는 첫 시점 — N=2 트리거. BattleEngine은 `IBattleSystem[]`을 순서대로 호출하는 dispatcher로만 남기고, 액터/페이즈 상태는 공유 `BattleContext`에 둔다. 이 분리를 미루면 P5 강공·P6 오의 매트릭스가 모두 BattleEngine 안에 쌓여 god class가 된다.

**Data (추가)**:

`SkillType` enum (Domain):
- `Choseok` (초식 — 능동 공격)
- `Simbeop` (심법 — 패시브 버프)
- `Gyeonggong` (경공 — 이동/회피)
- `Ouui` (오의 — 핫키 발동, 기세 소비)

`SkillRange` enum (Domain):
- `Close` / `Mid` / `Long`
- 거리 게이팅 매핑 (디폴트):
  - Close: position ≤ 25
  - Mid: 25 < position ≤ 60
  - Long: 60 < position ≤ 100

`SkillData` SO (Domain):
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| id | string | — | 고유 식별자 (snake_case) |
| nameKey | string | — | L10n 키 (`skill.<id>.name`) — Unity Localization String Table 조회 |
| descKey | string | — | L10n 키 (`skill.<id>.desc`) |
| type | SkillType | Choseok | 분류 |
| manaCost | int | 5 | 시전 비용 |
| cooldownSec | float | 1.5 | 쿨타임 |
| preferredRange | SkillRange | Close | 선호 거리 |
| momentumGainOnCast | int | 1 | 시전 성공 시 기세 획득 |
| effects | SkillEffect[] | — | 발동 시 적용 효과 (Phase 4에서 정의) |

`PlayerActor` 확장:
| 필드 추가 | 타입 | 설명 |
|----------|------|------|
| skillSlots | SkillData[] | 학습한 무공 (시작 3, 최대 6) |
| skillCooldowns | float[] | 슬롯별 남은 쿨 (초) |
| momentum | int | 기세 (오의 자원 스택). 범위 `[0, maxMomentum]`, 디폴트 0 |
| maxMomentum | int | 최대 기세 스택. 디폴트 10 |

> **기세 자원은 Phase 2가 아닌 본 Phase에서 도입** (v0.4.2 스코프 컷). `IResourceMutator`에 `GainMomentum(int)`/`SpendMomentum(int)` 메서드 추가. Invariant: `0 ≤ momentum ≤ maxMomentum`, SpendMomentum atomic(부족 시 false + 값 불변). ActorView에 `Momentum`/`MaxMomentum` Player 한정 필드 추가.

**State machine (Actor FSM, 활성화)**:
- Player: Idle ↔ Casting (시전 직후 즉시 Idle 복귀, 본 Phase에선 시전 시간 = 0)

**State machine (Battle FSM, 활성화)**:
- `Approach → Engage` 전이: Player가 Idle 상태로 전이된 시점(= 가장 가까운 적의 교전 거리 내 도달)에 BattlePhase = Engage. 이전엔 자동 시전 결정 트리가 동작하지 않는다 (Approach 페이즈 동안 거리 게이팅에 어차피 걸리지만, 페이즈로도 명시적 게이팅).
- `Engage → Approach`: 현재 웨이브의 모든 적이 Dead가 된 시점. Player → Running 재개, 다음 적 웨이브로 진군 (Phase 11 런 구조에서 정식화).

**Behaviors — 자동 시전 결정 트리**:
매 Tick에서 다음을 순서대로 수행한다.
1. **쿨다운 감소**: `for each i: cooldowns[i] = max(0, cooldowns[i] - dt)`.
2. **타겟 선택**: `GetNearestAliveEnemy()` — 살아있는 적 중 position 최소. 없으면 step 종료.
3. **슬롯 순회 (slot order, 0..N-1)**:
   - 슬롯이 비었으면 skip.
   - `cooldowns[i] > 0` 이면 skip.
   - `mana < skill.manaCost` 이면 skip.
   - `IsInPreferredRange(target.position, skill.preferredRange) == false` 이면 skip.
   - skill.type == Simbeop 이고 효과가 이미 활성 중이면 skip (중복 방지).
   - **여기까지 통과한 첫 스킬을 시전하고 break.**
4. **시전 처리**:
   - SpendMana(skill.manaCost). 실패 시 step 종료 (race condition 안전).
   - cooldowns[i] = skill.cooldownSec.
   - GainMomentum(skill.momentumGainOnCast).
   - skill.effects 적용 (Phase 4에서 정의).
   - `OnSkillCast(actorId, skillId, targetId)` 이벤트 발생.

> **루프 제약**: 한 Tick에 한 슬롯만 시전 (위 break). 두 스킬 동시 발동 금지. 이는 §6.4 SSOT의 "강공 동시 발동 정책" 결정과 정합.

**Formulas**:
- `IsInPreferredRange(pos, range)` — `SkillRange` 정의 참조.

**Interfaces (추가)**:
```csharp
public interface ISkillExecutor
{
    bool TryCast(Actor caster, int slotIndex, Actor target);
}

// Engine 이벤트
event Action<int /*casterId*/, string /*skillId*/, int /*targetId*/> OnSkillCast;
```

**Invariants**:
- I-3.1: 한 Tick에 한 actor당 시전 1회 이하.
- I-3.2: 시전이 일어났다면 mana는 정확히 `manaCost` 만큼 감소했다.
- I-3.3: 쿨다운 중인 스킬은 시전되지 않는다 (cooldowns[i] > 0).
- I-3.4: 결정성: 같은 seed/BattleStartData/SkillSlots → 같은 OnSkillCast 시퀀스.

**Edge**:
- 슬롯 순서가 우선순위다 — 사용자가 슬롯 순서로 우선순위를 표현한다.
- `GetNearestAliveEnemy()` 동률(같은 position) 시 id 오름차순.
- 시전 직후 적이 즉사하는 경우(Phase 4)에는 이벤트가 먼저 발생하고 사망 처리는 이후 Tick에서 적용 — 단일 Tick 내 순서 정의: (1) 쿨다운 감소 → (2) 시전 → (3) 효과 적용 → (4) 사망 판정 → (5) 스냅샷.

**Acceptance**:
- [ ] EditMode: 슬롯[태인장(Close), 천하삼십육검(Mid)] 적이 Mid 거리면 천하삼십육검만 시전.
- [ ] EditMode: 내공 부족 시 시전 X, 충전 후 다음 Tick에 시전.
- [ ] EditMode: 결정성 — 같은 시드 + 같은 슬롯/적 구성 → OnSkillCast 시퀀스 동일.
- [ ] PlayMode: 슬롯 위에 쿨 게이지 + 시전 시 skill 이름이 1초 표시.

---

### Phase 4. 일반 공격 + 데미지 + 사망/승패

**Goal**: 적 일반 공격(deterministic cadence) → 플레이어 HP 감소. 스킬 효과(데미지) 적용 → 적 HP 감소 → 사망. 전체 승/패 판정.

**Non-goals**: 강공·오의·회피. 회피는 Phase 9에서 도입.

**Data (추가)**:

`EnemyData` 확장:
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| normalAttackDamage | int | 3 | 일반 공격 데미지 |
| normalAttackPeriod | float | 2.5 | 공격 주기 (초) |
| normalAttackRange | SkillRange | Close | 사거리 |

`SkillEffect` (Domain — sealed class 다형성):
- `DamageEffect { int amount }`
- `HealEffect { int amount }`  (본 Phase에선 미사용, 정의만)
- `BuffEffect`, `DebuffEffect` (Phase 6+)

`Actor` 확장:
| 필드 추가 | 타입 | 디폴트 | 설명 |
|----------|------|--------|------|
| normalAttackCooldown | float | 0 | 다음 공격까지 남은 시간 |

**Behaviors**:
1. **적 일반 공격 (Idle 상태에서만)**:
   - `normalAttackCooldown > 0` → `cd -= dt`.
   - `cd ≤ 0` 이고 사거리 안에 플레이어 있음 → 데미지 적용 + `cd = normalAttackPeriod`.
2. **데미지 적용**: `target.hp = max(0, target.hp - amount)`. 0이 되면 다음 step에서 `state = Dead` 전이.
3. **사망 처리**: 매 Tick 끝에 hp=0인 액터를 Dead로 전이 + `OnActorDeath(actorId)` 이벤트.
4. **승/패 판정**:
   - 모든 적이 Dead → BattlePhase = Resolve(victory) + 종료.
   - Player가 Dead → BattlePhase = Resolve(defeat) + 종료.

**Formulas — 데미지 (단순 1차)**:
```
finalDamage = baseDamage      // 본 Phase에선 매트릭스/회피/방어/공격 보정 없음
hp_new      = max(0, hp_old - finalDamage)
```
> 매트릭스 배율은 Phase 5/6에서 들어옴. 능력치 압도 보정은 §4 Open Question.

**Interfaces (추가)**:
```csharp
public interface IDamageResolver
{
    int Resolve(Actor source, Actor target, int baseDamage, DamageContext ctx);
}

public readonly struct DamageContext
{
    public readonly DamageSource Kind;   // NormalAttack / Skill / HeavyAttack / Ohi
    public readonly string SkillId;      // null 가능
}

// Engine 이벤트
event Action<int srcId, int dstId, int dmg, DamageContext ctx> OnDamage;
event Action<int actorId> OnActorDeath;
```

**Invariants**:
- I-4.1: `finalDamage ≥ 0` (음수 데미지 = 회복은 별도 effect로).
- I-4.2: hp는 절대 0 미만이 되지 않는다.
- I-4.3: Dead 상태 액터는 어떤 이벤트도 받지 않는다 (공격·피격·시전·이동 X).
- I-4.4: BattleResult는 정확히 한 번 송신된다.

**Edge**:
- 같은 Tick에 두 액터가 서로를 죽이는 경우 — 양측 모두 Dead, BattleResult는 player의 사망이 우선(defeat).
- 적 사망 시점에 적의 진행 중 normalAttack은 취소된다.
- 적이 사거리 밖으로 이탈하면 normalAttackCooldown은 그대로 흐른다 (재진입 시 즉시 발동 가능).

**Acceptance**:
- [ ] EditMode: 적 1 (normalAtk=3, period=2.5s) vs Player(HP=100) → 25초 = 10회 공격, Player HP=70 ± 0.
- [ ] EditMode: 스킬 데미지 5 시전 → 적 HP 정확히 감소.
- [ ] EditMode: 결정성 — 같은 시드 + 같은 BattleStartData → OnDamage/OnActorDeath 시퀀스 동일.
- [ ] PlayMode: 시각 확인 — 적이 일정 주기로 플레이어 HP를 깎고, 스킬이 적 HP를 깎고, 누군가 0 되면 전투 종료.

---

### Phase 5. 강공 분기 (방어 수싸움) — **보스전 한정 (v4)**

**Goal**: **보스 적의** 강공(Heavy Attack) 차징 → UI 4택 → 매트릭스 판정 → 데미지/효과 적용. GAME_DESIGN.md §6.4 매트릭스를 정식 SSOT로 본 Phase에 흡수.

**v4 스코프 (2026-05-11 결정)**: 일반 적은 강공을 트리거하지 않는다. `EnemyData.isBoss == true` 인 적만 강공 사이클 진행. 일반전은 풀 오토배틀(P0~P4 + P9 회피만)로 동작.

**Non-goals**: 오의 공격 분기는 Phase 6.

**Data (추가)**:

`AttackPattern` enum (Domain — 강공 종류, 방어 매트릭스의 행):
- `Direct` (직공) / `Combo` (연환공) / `Feint` (허실공)

`DefenseChoice` enum (Domain — 방어 선택지, 방어 매트릭스의 열):
- `Ihwajeopmok` (이화접목) / `Bobeop` (보법) / `Naryeotagon` (나려타곤) / `Geumgang` (금강불괴)

`DefenseMatchup` enum:
- `Counter` (★ — 받는 데미지 = 0, 강공 무력화) — 카운터 발생
- `Normal` (보통 — 데미지 절반)
- `Weak` (✗ — 데미지 1.5배)

`EnemyData` 확장:
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| heavyAttackPeriod | float | 12.0 | 강공 사이클 (초) |
| heavyChargingDuration | float | 2.0 | 차징 지속 시간 (UI 노출 시간) |
| heavyAttackDamage | int | 10 | 강공 base damage |
| attackPatterns | AttackPattern[] | { Direct } | 풀 — 강공 시 RNG로 1개 선택 |

`Actor` 확장 (Enemy):
| 필드 추가 | 타입 | 초기값 | 설명 |
|----------|------|--------|------|
| heavyAttackCooldown | float | heavyAttackPeriod | 다음 강공까지 남은 시간. 스폰 시 첫 강공도 1사이클 대기 후 발동. |

`PlayerActor` 확장:
| 필드 추가 | 타입 | 설명 |
|----------|------|------|
| availableDefenseChoices | DefenseChoice[] | 보유 방어 선택지 (오성 등급별 — Phase 7에서 갱신) |

`DefenseMindGameState` (Engine 내부):
| 필드 | 타입 | 설명 |
|------|------|------|
| sourceEnemyId | int | 강공 발동 적 |
| pattern | AttackPattern | 풀에서 선택된 패턴 (사용자에겐 hint로만 노출) |
| choices | DefenseChoice[] | UI에 노출되는 4개 |
| timeoutSec | float | 미선택 시 자동 결정 |

**State machine (추가)**:
```
Engage ──(enemy.heavyChargingStart)── HeavyCharging
                                          │
                                  (timeout / playerChoice)
                                          ▼
                                   ResolveDefense ── Engage 복귀
```
- 적 actor.state: Idle → HeavyCharging (차징 동안 일반 공격 중단) → Idle.

**Behaviors**:
1. **강공 트리거**: 매 Tick, 적의 `heavyAttackCooldown -= dt`. ≤0이고 사거리 안 + Idle이면:
   - actor.state = HeavyCharging.
   - RNG로 attackPatterns 풀에서 1개 선택 (시드 결정적).
   - DefenseMindGameState 생성. timeoutSec = heavyChargingDuration.
   - `OnHeavyAttackTrigger(enemyId, patternHint)` 이벤트 (hint는 Phase 7에서 정확도 조정).
   - BattleEngine은 `BattlePhase.MindGameDefense` 진입 — 게임 시간 일시정지 (TickService.Pause). 단, 차징 타임아웃은 별도 real-time 카운트다운으로 진행.
2. **방어 선택지 풀 결정**: `choices = availableDefenseChoices` 중 4개 (오성에 따른 풀 차이는 Phase 7에서, 본 Phase에선 4개 고정 노출).
3. **사용자 입력 (View → IBattleInput.SubmitDefense)**:
   - choice 적용. 매트릭스 판정으로 매치업 결정 → 데미지 계산 → Player HP 감소.
4. **타임아웃 (시간 초과)**:
   - 사용자가 선택 안 함 → choice = `null`로 간주, 매트릭스의 "타임아웃" 행렬을 적용 (전 패턴 ✗ 판정).
5. **Engage 복귀**: TickService.Resume. enemy.state = Idle. heavyAttackCooldown = heavyAttackPeriod 재설정.
6. **동시 강공 정책 (결정)**: MindGameDefense 진행 중 다른 적이 강공 트리거를 만나면 **FIFO 큐**에 적재한다. 큐는 한 번에 하나씩 순차 처리하며, 큐가 빌 때까지 Engage로 복귀하지 않는다. 동시 다중 강공으로 인한 정보 오버로드를 방지하기 위함.
7. **MindGame 시간 정지 (결정)**: MindGameDefense 페이즈 동안 TickService는 Pause된다. 이는 (a) 사용자 결정의 인지 부담 보장, (b) 결정 외 변수(다른 적의 진군·공격) 차단을 위함. 단, `heavyChargingDuration` 의 timeout 카운트다운은 일시정지된 게임 시간이 아니라 별도 real-time 타이머로 진행한다.

**Formulas — 방어 매트릭스 (SSOT)**:

| 적 패턴 \ 방어 | 이화접목 | 보법 | 나려타곤 | 금강불괴 |
|---------------|---------|------|---------|---------|
| 직공 (Direct) | ★ Counter | Normal | Weak | Normal |
| 연환공 (Combo) | Weak | ★ Counter | Normal | Weak |
| 허실공 (Feint) | Normal | Weak | Normal | ★ Counter |

(타임아웃 시 모든 패턴에 대해 Weak 적용)

**데미지 배율**:
```
matchupMultiplier =
    Counter   → 0.0   (+ 적에게 reverseDamage = enemy.heavyAttackDamage × 0.5 반사)
    Normal    → 0.7   (방어자 절반~보통 — 0.7로 통일)
    Weak      → 1.5

finalDamage = round(enemy.heavyAttackDamage × matchupMultiplier × statSurpressionMod)
```
> `statSurpressionMod`은 §4 Open Question. 본 Phase에선 `1.0`.

**Interfaces (추가)**:
```csharp
public interface IBattleInput
{
    bool SubmitDefense(DefenseChoice choice);   // MindGameDefense 페이즈에서만 유효
}

public interface IMatchupResolver
{
    DefenseMatchup ResolveDefense(AttackPattern pattern, DefenseChoice choice);
}

// Engine 이벤트
event Action<int enemyId, AttackPattern hintedPattern, DefenseChoice[] choices> OnDefenseMindGameStart;
event Action<DefenseChoice chosen, DefenseMatchup result, int damageDealt, int counterDamage> OnDefenseResolved;
```

**Invariants**:
- I-5.1: MindGameDefense 페이즈 진입 시 게임 시간(TickService) 일시정지.
- I-5.2: 한 강공 차징은 정확히 한 번만 ResolveDefense 한다 (이중 처리 방지).
- I-5.3: 매트릭스는 12칸 모두 정의되어 있다 (테스트로 검증).
- I-5.4: Counter 발생 시 적은 stunned 1.0초 (state = Stunned, normalAttack/heavyAttack 둘 다 진행 중단).
- I-5.5: 결정성: 같은 시드 + 같은 입력 시퀀스 → 같은 매치업 결과.

**Edge**:
- Player HP가 강공 데미지로 0이 되면 ResolveDefense 후 즉시 BattleResult(defeat).
- attackPatterns 풀이 빈 배열이면 강공 자체 발동 X (콘텐츠 검증).
- 큐잉된 강공의 트리거 적이 큐 대기 중 다른 스킬·이벤트로 사망한 경우 큐에서 제거.

**Acceptance**:
- [ ] EditMode: 매트릭스 12칸 모두 기대 결과 반환 (단위 테스트).
- [ ] EditMode: Counter 시 데미지 0 + 적 stunned 1초.
- [ ] EditMode: 타임아웃 시 데미지 1.5배.
- [ ] EditMode: 동시 강공 큐잉 — 적 두 명이 같은 Tick에 강공 트리거 → 두 번의 MindGame이 순차 처리.
- [ ] PlayMode: 적 머리 위 ⚠ 차징 표시 + 4택 UI 노출 + 선택 결과 화면 피드백.

---

### Phase 6. 오의 분기 (공격 수싸움) — **보스전 한정 (v4)**

**Goal**: 보스전에서 플레이어가 **오의 핫키** 발동 → 적 선택 → UI 3택 → 매트릭스 판정 → 데미지 적용. §6.5 매트릭스 SSOT.

**v4 스코프 (2026-05-11 결정)**: 오의 핫키 발동은 현재 전투가 보스전(`BattleStartData.isBossBattle == true`)일 때만 활성화. 일반전에서는 오의 스킬도 카드 드래프트 시너지/일반 데미지원으로 동작하되 매트릭스 분기를 거치지 않는다 (`momentumCost` 소비 후 즉시 `Normal` 매치업으로 처리하거나, 자동 시전 결정 트리 안에 흡수 — Phase 6 진입 시 결정).

**Non-goals**: 카피·대종사 재능 특수 효과(Phase 8).

**Data (추가)**:

`OuuiPattern` enum (오의 운용, 공격 매트릭스의 행 — 플레이어 측 선택):
- `Pierce` (관통) / `Precision` (정밀) / `Lure` (유인)

`DefensePattern` enum (적 방어 패턴, 공격 매트릭스의 열):
- `Deflect` (흘리기) / `BraceHeavy` (강공방어) / `Defenseless` (무방비)

`OffenseMatchup` enum:
- `Counter` (★ — 데미지 1.5배 + 카운터 추가 효과)
- `Normal` (1.0배)
- `Weak` (✗ — 데미지 0.5배 + 적 카운터)

`EnemyData` 확장:
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| defensePatterns | DefensePattern[] | { Deflect, BraceHeavy } | 풀 |

`SkillData` 확장 (오의용):
| 필드 | 타입 | 설명 |
|------|------|------|
| isOuui | bool | type==Ouui이면 true |
| momentumCost | int | 오의 발동 비용 (디폴트 5) |
| ouuiPatternChoices | OuuiPattern[] | 이 오의가 사용 가능한 패턴 (보통 3개 모두) |

`OffenseMindGameState` (Engine 내부):
| 필드 | 타입 | 설명 |
|------|------|------|
| skillSlot | int | 발동된 오의 슬롯 |
| targetEnemyId | int | 사용자가 선택한 대상 |
| enemyDefensePattern | DefensePattern | 풀에서 RNG 선택 (사용자에겐 hint) |
| choices | OuuiPattern[] | UI 3개 |
| timeoutSec | float | 디폴트 5.0 |

**State machine (추가)**:
```
Engage ──(player presses Ouui hotkey, momentum ≥ cost)── EnemyTargetSelect
                                                              │
                                                       (user clicks enemy)
                                                              ▼
                                                       MindGameOffense
                                                              │
                                                  (timeout / playerChoice)
                                                              ▼
                                                       ResolveOffense ── Engage
```
- 게임 시간 정지 정책은 Phase 5와 동일.

**Behaviors**:
1. **오의 핫키 입력 (View → IBattleInput.PressOuui(slotIndex))**:
   - 슬롯이 isOuui=true 인지 확인.
   - momentum ≥ momentumCost 인지 확인 — 부족 시 거부 + ON_OuuiRefused 이벤트.
   - 거리 게이팅 통과 검증.
   - 통과 시 BattlePhase = EnemyTargetSelect (live 적이 1명이면 자동 선택, 2+이면 사용자 선택).
2. **적 선택 (IBattleInput.SubmitOuuiTarget)**:
   - target enemy 결정. RNG로 defensePatterns 풀에서 1개 선택. choices = ouuiPatternChoices.
   - BattlePhase = MindGameOffense, TickService.Pause.
3. **사용자 입력 (IBattleInput.SubmitOuui(pattern))**:
   - 매트릭스 판정 → 데미지 계산 → 적 HP 감소.
4. **타임아웃 (결정)**: 자동 = `Pierce` (관통). 3개 패턴 중 1Counter·1Weak·1Normal 분포로 가장 보수적이며, 0정보 사용자에게 페널티 최소화.
5. **MindGame 시간 정지 (결정)**: MindGameOffense 페이즈 동안 TickService는 Pause. P5와 동일 정책.
6. **종료**: SpendMomentum(momentumCost). cooldowns[slot] = skill.cooldownSec. Engage 복귀.

**Formulas — 공격 매트릭스 (SSOT)**:

| 오의 패턴 \ 적 방어 | 흘리기 (Deflect) | 강공방어 (BraceHeavy) | 무방비 (Defenseless) |
|--------------------|-----------------|----------------------|---------------------|
| 관통 (Pierce) | Weak | ★ Counter | Normal |
| 정밀 (Precision) | ★ Counter | Weak | Normal |
| 유인 (Lure) | Normal | Normal | ★ Counter |

**데미지 배율**:
```
matchupMultiplier =
    Counter   → 1.5   (+ 추가 효과: 적 stunned 1.5초)
    Normal    → 1.0
    Weak      → 0.5   (+ 적 카운터 = 적 normalAttackDamage 즉시 반사)

finalDamage = round(skill.baseDamage × matchupMultiplier × statSurpressionMod)
```

**Interfaces (추가)**:
```csharp
public interface IBattleInput
{
    bool PressOuui(int slotIndex);
    bool SubmitOuuiTarget(int enemyId);
    bool SubmitOuui(OuuiPattern choice);
}

OffenseMatchup ResolveOffense(OuuiPattern pattern, DefensePattern enemyDefense);

event Action<int slotIndex, int enemyId, DefensePattern hintedPattern, OuuiPattern[] choices> OnOffenseMindGameStart;
event Action<OuuiPattern chosen, OffenseMatchup result, int damageDealt, int counterDamage> OnOffenseResolved;
```

**Invariants**:
- I-6.1: 오의 발동 시 momentum은 정확히 momentumCost만큼 감소했다.
- I-6.2: 적이 모두 Dead인 상태에서는 오의 핫키가 거부된다.
- I-6.3: MindGameOffense 페이즈 도중에는 적의 일반 공격 / 강공이 발동되지 않는다.
- I-6.4: 매트릭스 9칸 모두 정의 (테스트).

**Edge**:
- 오의 차징 중 적이 사망한 경우 (스킬·일반공격으로) → MindGameOffense 취소 + momentum 환불.
- 동일 적에게 동시 다발 오의는 막는다 — MindGameOffense 진행 중엔 추가 핫키 거부.
- 거리 게이팅에 어긋나는 적은 SubmitOuuiTarget이 거부한다.

**Acceptance**:
- [ ] EditMode: 매트릭스 9칸 모두 기대 결과 (단위 테스트).
- [ ] EditMode: Weak 시 적 카운터 데미지 = enemy.normalAttackDamage 가 플레이어에 적용.
- [ ] EditMode: momentum 부족 시 핫키 거부 + ON_OuuiRefused 발생.
- [ ] PlayMode: 핫키 → 적 선택 UI → 3택 UI → 결과 피드백.

---

### Phase 7. 오성 (Wisdom) — Hint + 선택지 풀

**Goal**: 오성 등급(1~10)에 따라 (a) hint 정확도, (b) 방어/오의 선택지 풀 품질, (c) 디버그 표시 풍부도가 달라진다. §6.4·§6.5의 hint 노출 부분을 본 Phase에서 정식 명세로 들여온다.

**Non-goals**: 재능별 시작 오성값(Phase 8).

> **Wisdom 값 출처 (v0.4.2)**: 오성은 per-battle 자원이 아니라 **메타 패시브 슬롯**(PlayerData, Phase 11+)에서 읽는 영구 stat. Phase 7 진입 시점에 Phase 11+가 미도입이면 placeholder fixed 값(예: 5) 사용. Phase 11+ 도입 후 `PlayerData.metaPassives` 조회로 교체. `PlayerActor`에 wisdom 필드를 두지 않는다.

**Data (추가)**:

`WisdomTier` enum:
- `Low` (1~4) / `Mid` (5~7) / `High` (8~10)

`HintAccuracy` 매핑 (Wisdom → 강공/오의 패턴 hint 정확도):
| Tier | Wisdom | Hint 정확도 | hint 형태 |
|------|--------|------------|----------|
| Low  | 1~4 | 33% (전 무작위 후보 3개 중 하나로 균등 표시) | "??" 또는 잘못된 패턴까지 후보 |
| Mid  | 5~7 | 66% (실제 패턴 + 1개 오답 노출) | 실제 + 가짜 1개 |
| High | 8~10 | 100% (실제 패턴 정확 표시) | 실제 패턴만 |

`DefenseChoicePool` (오성 등급별 노출되는 4택):
- Low: 약한 4개 고정 (예: 보법, 나려타곤, 흘리기 무용, 무방비) — 카운터 패가 없음
- Mid: 4개 중 카운터 2개 포함
- High: 4개 모두 카운터 가능 (이상적인 풀)
- > 정확한 4개 매핑은 §4 Open Question

`OuuiPatternChoicePool` 동일 원리.

**Behaviors**:
1. MindGameDefense 진입 시:
   - 적 패턴 RNG 선택 직후, `HintGenerator(wisdom, actualPattern)` → hint 노출 패턴.
   - `DefenseChoiceGenerator(wisdom)` → 노출 5개.
2. MindGameOffense도 동일 구조 (적 defensePattern + 오의 3택).
3. hint RNG는 시드 결정적.

**Interfaces (추가)**:
```csharp
public interface IHintProvider
{
    AttackPattern[] HintForDefense(int wisdom, AttackPattern actualPattern);
    DefensePattern[] HintForOffense(int wisdom, DefensePattern actualPattern);
}

public interface IChoicePoolProvider
{
    DefenseChoice[] DefensePool(int wisdom);
    OuuiPattern[]   OuuiPool(int wisdom, SkillData skill);
}
```

**Invariants**:
- I-7.1: Wisdom == 10일 때 hint는 항상 정확.
- I-7.2: Wisdom 변경 없이 같은 시드 → 같은 hint 시퀀스 (결정성).
- I-7.3: 오성 1~4 구간엔 매트릭스에서 ★ Counter를 만들 수 있는 선택지가 한 개 이하만 포함된다 (밸런스 의도).

**Edge**:
- Wisdom < 1 또는 > 10이 들어오면 클램프.
- HintForDefense의 fake 후보가 actual과 동일하지 않도록 보장.

**Acceptance**:
- [ ] EditMode: Wisdom=10 시 hint == 실제 패턴 (1000회 반복).
- [ ] EditMode: Wisdom=1 시 hint 후보 3개 중 actual 포함 비율 ~33% (1000회).
- [ ] PlayMode: 오성 슬라이더 1↔10 변경 시 UI 표시 변화 시각 확인.

---

### Phase 8. 재능 3종 (Talents)

> **v4.1 (2026-05-14)**: 재능은 런 시작 선택이 아니라 **런 종료 시 확률 각성**으로 캐릭터에 0~3개 누적(동시 작동·가중치 스택). 본 Phase 8은 **재능이 적용된 상태의 효과**(시작값 차별화)만 정의 — 각성 트리거 자체는 Phase 11+에서. **첫 런 = 재능 0개**: 본 Phase의 재능 효과가 전혀 적용되지 않은 균일 베이스라인 (시작값은 [Phase 2 `PlayerStartData`](#phase-2-자원--hp--내공--기세--오성) 디폴트 사용). Phase 8의 모든 메커니즘은 0개 케이스에서 자연 통과해야 한다. 다중 재능 가중치 합산 방식·Pity·캐릭터 단일성은 §4 Open Q (Q-8~Q-10).

**Goal**: 천무지체 / 카피 / 대종사 — startingMomentum, startingWisdom, startingSkillSlots, 재능별 특수 능력 훅을 정의. 본 Phase는 **시작값 차별화**까지만 활성화. 카피의 "적 무공 카피" / 대종사의 "강화 추가 효과"는 Phase 8.x로 후속.

**Non-goals**: 카피의 적 무공 학습, 대종사의 무공 강화 보너스 — Phase 8.1 / 8.2로 후속 정의.

**Data (추가)**:

`TalentData` SO (Domain):
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| id | string | — | tianmu / copy / daejongsa |
| nameKey | string | — | L10n 키 (`talent.<id>.name`) |
| descKey | string | — | L10n 키 (`talent.<id>.desc`) |
| startingMaxHp | int | 100 | 시작 최대 HP |
| startingMaxMana | int | 100 | 시작 최대 내공 |
| startingMomentum | int | 0 | 매 전투 시작 시 기세 |
| startingSkills | SkillData[] | — | 시작 무공 슬롯 |
| specialBehaviorId | string | "" | 재능 특수 능력 식별자 (Phase 8.1+) |

> **`startingWisdom` 제거 (v0.4.2)**: 오성은 메타 패시브 슬롯(Phase 11+)으로 이관. 재능별 wisdom 차별화는 "재능이 메타 패시브 풀에 가하는 가중치"로 표현 (Phase 8/Phase 12 연계, Open Q-18). 본 Phase에선 wisdom 시작값을 다루지 않음.

**3재능 시작값** (SSOT):
| 재능 | maxHp | maxMana | startingMomentum | 시작 무공 풀 (요지) |
|------|-------|----------|------------------|---------------------|
| 천무지체 (tianmu) | 100 | 100 | 0 | 균형 (초식 1 + 심법 1 + 오의 1) |
| 카피 (copy)       | 100 | 100 | 0 | 빈 슬롯 많음 (시작 무공 1) |
| 대종사 (daejongsa)| 100 | 100 | 5 | 강한 오의 1 + 초식 1 |

> 카피의 "정보 핸디캡" 의도(기존 wisdom=3)는 메타 패시브 풀 가중치로 재현. 카피 재능 보유 시 오성 패시브 슬롯의 강화 확률이 낮아지는 식 — 정확한 수치는 Phase 8/12 연계로 결정 (Open Q-18).

**Behaviors**:
1. Setup 시 PlayerActor 초기화에 TalentData 적용.
2. specialBehaviorId 가 있으면 해당 ITalentSpecialBehavior 활성화 (Phase 8.1+).

**Interfaces (추가)**:
```csharp
public interface ITalentSpecialBehavior
{
    void OnBattleStart(BattleContext ctx);
    void OnSkillCast(SkillCastContext ctx);
    void OnDefenseResolved(DefenseResolveContext ctx);
    void OnOffenseResolved(OffenseResolveContext ctx);
}
```

**Invariants**:
- I-8.1: 매 전투 Setup 시 player.momentum = talent.startingMomentum (이전 전투 잔여 X).
- I-8.2: TalentData는 Setup 시 적용된 후 변경되지 않는다 (불변).

**Edge**:
- v4.1: 재능 0개(첫 런)인 경우 `PlayerStartData` 디폴트 baseline 적용 — 재능 효과 미적용(헤더 노트 참조). 본 항목 이전 wording("재능 미선택 → 천무지체 디폴트")은 v4 가설(런 시작 선택)의 잔재.
- 시작 무공이 maxSlot(6)을 초과하면 컨텐츠 검증 단계에서 거부.

**Acceptance**:
- [ ] EditMode: 3재능 각각 Setup 후 startingMomentum 일치.
- [ ] PlayMode: 재능 선택 → 3가지 시작 자원/슬롯 시각 확인.

#### Phase 8.1 카피 재능 — 적 무공 카피 [후속, 본 Phase에선 명세만]
- 트리거: 적이 강공 발동 시 ResolveDefense 결과가 Counter일 때 일정 확률로 그 적의 attackPattern을 플레이어 슬롯에 학습.
- 슬롯이 가득찬 경우 학습 거부.
- 상세 확률·획득 무공 매핑은 §4 Open Question.

#### Phase 8.2 대종사 재능 — 무공 강화 보너스 [후속]
- 트리거: 같은 무공 두 번 학습 시 자동 강화에 추가 효과(데미지 +20% 또는 쿨 -10%).
- 상세 효과는 §4 Open Question.

---

### Phase 9. 회피 시스템

**Goal**: 회피율 / 회피 RNG / 강제 회피(경공). 단순한 형태로 도입.

**Non-goals**: 회피와 매트릭스의 상호작용 (Phase 5/6 결과는 회피 전 단계로 처리).

**Data (추가)**:

`PlayerActor` 확장:
| 필드 | 타입 | 디폴트 | 설명 |
|------|------|--------|------|
| dodgeBaseRate | float | 0.10 | 회피 기본 확률 |
| guaranteedDodgeAvailable | bool | false | 경공 트리거 시 True |

**Behaviors**:
1. 적 일반 공격 또는 적 강공이 플레이어에 도달할 때:
   - `guaranteedDodgeAvailable == true` → 데미지 0, 플래그 false. (경공 1회 보장)
   - 아니면 `RngService.NextFloat01() < dodgeBaseRate` → 데미지 0.
   - 둘 다 아니면 데미지 정상 적용.
2. **경공 트리거 (HP 30% 이하 진입 첫 번째 1회)**:
   - **트리거 조건 (결정)**: PlayerActor의 skillSlots 중 SkillType.Gyeonggong 무공이 1개 이상 존재할 때만 활성화. 경공 무공이 없는 빌드는 보장 회피 자체가 없음.
   - 조건 충족 + HP 30% 이하 진입 시 다음 데미지 1회를 보장 회피.
3. 회피 발생 시 `OnDodge(targetId, sourceKind)` 이벤트.

**Formulas**:
- `damageAfterDodge = isDodged ? 0 : finalDamage`

**Invariants**:
- I-9.1: 경공 보장 회피는 한 전투에 한 번만 사용된다 (HP 30% 재진입에는 재발동 X — 단순 룰).
- I-9.2: 회피 RNG는 IRngService를 통한다.
- I-9.3: 회피된 공격은 OnDamage 이벤트에 amount=0 으로 송신 (UI 표시용).

**Edge**:
- 같은 Tick에 여러 공격이 도달하는 경우 각각 독립 회피 판정.
- 강공 매트릭스에서 Counter(데미지 0)가 나온 경우엔 회피 판정 자체가 없다 (이미 0).

**Acceptance**:
- [ ] EditMode: dodgeBaseRate=0.0 + guaranteed=false 시 회피 0회.
- [ ] EditMode: dodgeBaseRate=1.0 시 모든 공격 회피.
- [ ] EditMode: HP 30% 진입 시 다음 1회 회피 보장.
- [ ] EditMode: 결정성 — 같은 시드 시 회피 시퀀스 동일.

---

### Phase 10. UI 계약 + Telemetry

**Goal**: View 계층의 입력/출력 인터페이스 정식 분리. Telemetry hook(전투 로그·디버그 dump). 사운드/VFX는 명시적 Non-goal.

**Non-goals**: 사운드/VFX 폴리시.

**Data**: 없음 (구조 정리).

**Behaviors**:
1. View는 Engine으로부터 다음만 받는다:
   - `BattleSnapshot` (매 틱)
   - `OnSkillCast`, `OnDamage`, `OnActorDeath`, `OnDefenseMindGameStart`, `OnDefenseResolved`, `OnOffenseMindGameStart`, `OnOffenseResolved`, `OnDodge`, `OnHeavyAttackTrigger`, `OnOuuiRefused`, `BattleResult`.
2. View는 Engine에 다음만 호출한다:
   - `IBattleInput.SubmitDefense(choice)`
   - `IBattleInput.PressOuui(slotIndex)`, `SubmitOuuiTarget(enemyId)`, `SubmitOuui(pattern)`
3. Telemetry: `ITelemetrySink` — 모든 이벤트를 JSON-line 파일에 append (옵션). 결정성 검증·재현용.

**Interfaces (정리)**:
```csharp
public interface ITelemetrySink
{
    void Record(string eventName, IReadOnlyDictionary<string, object> payload);
}
```

**Invariants**:
- I-10.1: View가 Engine에 호출하는 메서드는 IBattleInput 4개로 제한 (코드 grep 검증).
- I-10.2: View는 Domain의 mutable 필드를 직접 변경하지 않는다 (BattleSnapshot은 readonly struct).
- I-10.3: Telemetry는 Engine 결정성에 영향 X (sink 없어도 같은 결과).

**Edge**:
- Telemetry sink가 IO 예외를 던지면 swallow (로그만, 게임 진행 영향 X).

**Acceptance**:
- [ ] 코드 grep: View 어셈블리에서 Engine 인터페이스 호출이 IBattleInput 4개 + 이벤트 구독 외엔 없다.
- [ ] EditMode: Telemetry sink에 기록된 이벤트 시퀀스로 전투 재구성 가능 (재현성 테스트).
- [ ] PlayMode: 한 전투의 telemetry 파일이 사람이 읽을 수 있는 JSON line.

---

### Phase 11~14. v4 신규 Phase (개요 — 상세 명세는 진입 시점에)

> 본 절은 v4 피벗으로 추가된 Phase의 **개요**만 기록한다. 각 Phase의 본격 명세(Data/Behaviors/Invariants/Acceptance)는 해당 Phase 진입 직전에 본 문서에 추가한다 — CLAUDE.md §1 "코드보다 SSOT 먼저" 규칙.

#### Phase 11. 런 구조 (RunSession)
- 한 런의 상태 집계: 현재 스테이지 index, 누적 골드/경험치, 적용된 카드 목록, 사망/클리어 조건.
- 스테이지 시퀀스 정의 (`StageDefinition` SO 또는 절차 생성기) + 적 절차 생성 (`EnemySpawnTable` + RNG).
- 보스 스테이지 게이팅 (`EnemyData.isBoss` 플래그) — Phase 5/6 매트릭스 발동 조건이 됨.

#### Phase 12. 카드 드래프트 (하이브리드 풀)

**플레이어 캐릭터 (v4 단순화)**
- 단일 클래스 = 검사. 평타: 자동 세로 베기.
- 액티브 무공 발동 시 검기 이펙트로 평타와 시각 차별화.

**이중 풀 구조**

| 풀 | 획득 시점 | 빈도 | 효과 지속 | 비고 |
|---|---|---|---|---|
| **계승 풀 (메타)** | 보스/엘리트 처치 보상 | 1~3장/런 | 런 간 영구 누적 | 슬롯제 |
| **휘발 풀** | 스테이지 중 레벨업 3택 | ~5장/스테이지 | 한 런 내 임시 (종료 시 소멸) | 슬롯 없음, 캐릭터에 누적 |

**계승 풀 — 무공 슬롯 (4개)**
- 액티브 무공 × 2 (예: 연속 베기 → 환영검무, 철벽 막기 → 금강불괴)
- 패시브 무공 × 2 (예: 내공심법(체력 회복), 호신강기(피해 감소))
- 슬롯별 카드 누적 → 단계 진행:
  - **1~6장**: 모션 진화 (세로 베기 → +가로 → +대각 → 환영검무)
  - **7~17장**: 1~12성 위력 % 증가 (단순 누산, **12성만 특별 연출**)
- 4슬롯 × 17장 = 68장이 메타 만렙. 한 런 1~3장 → 수십 런 단위 장기 목표.

**휘발 풀 카드**
- 무공 카드 (한 런 동안 액티브 효과) + 패시브 카드 (한 런 동안 스탯 % 증가) 둘 다 포함.
- 옵나 톤의 빈번한 드래프트 체감 담당.

**구현 메모**
- `CardData` SO (효과 + 시너지 태그 + 풀 종류), `CardDraftService` — 풀별 추첨 분리.
- 효과 적용은 기존 자원 4종 / 무공 시스템에 후킹 (자원 max 증가, 시전 가속, 데미지 보너스, 특정 SkillType 강화 등).
- 오성(Phase 7)이 풀 등급 게이팅에 영향 — Wisdom Tier별 카드 풀 차등.

#### Phase 13. 메타 진행 (Meta Progression)
- 계승 풀 4슬롯의 무공 상태 영구 저장 (슬롯별: 진화 단계 1~6 / 성 단계 1~12).
- `Inventory`, `Equipment` (슬롯제), `PermanentUpgrade` (MaxHP·기본 공격력 등 영구 스탯), 골드/재화 시스템.
- 런 사망/클리어 후 획득물 → 인벤토리 반영 → 강화 → 다음 런 통계 차이.

#### Phase 14. 모바일 UI/UX
- 세로 레이아웃 캔버스, 터치 입력 매핑, 화면 전환(메인→런→인벤→강화→정산).
- 한국어 폰트 풀세트, UI 키트 폴리시.

---

## 4. 미해결 질문 (Open Questions)

> 답이 나오면 해당 Phase 본문에 흡수 + §5 변경 이력에 한 줄.

| # | 질문 | 영향 Phase | 결정 필요 시점 |
|---|------|----------|---------------|
| Q-1 | 능력치 압도 보정 (`statSurpressionMod`) 공식 — 플레이어 평균 stat과 적 stat 비율 기반? | P5/P6 | P5 진입 전 |
| Q-2 | 플레이어 일반 공격 존재 여부 (없으면 무공 자동 시전만 데미지원) | P3/P4 | P4 진입 전 |
| Q-3 | 회복원 (HP/내공) — 시간/이벤트/스킬 어디서 회복? 기세는 P3 시전 보상으로만 | P2/P3/P9 | P3 진입 전 |
| Q-4 | 카피 재능의 적 무공 학습 확률·매핑 | P8.1 | P8 완료 전 |
| Q-5 | 대종사 재능의 강화 추가 효과 수치 | P8.2 | P8 완료 전 |
| Q-6 | 오성 등급별 4택 풀의 정확한 무공 매핑 | P7 | P7 진입 전 |
| Q-7 | 탈주 메커니즘 존재 여부 (경공 무공으로 전투 이탈) | P9+ | P9 진입 후 |
| Q-8 | v4.1 다중 재능 가중치 합산 방식 — multiplier(곱) vs boost(가산) | P11~12 | P11 진입 전 |
| Q-9 | v4.1 Pity 시스템 — 첫 보스 클리어 = 재능 1개 보장, 미획득 시 상승 확률 | P11~12 | P11 진입 전 |
| Q-10 | v4.1 캐릭터 단일성 — **현행 단일 캐릭터(0~3 재능 누적) 전제 유지 vs 다중 캐릭터 슬롯 도입 검토** | P12~13 | P12 완료 전 |
| Q-11 | v4.1 각성 매커니즘 vs 카드 강화·조합(뱀서식형) 매커니즘 선택 | P12 | P12 진입 전 |
| Q-12 | v4.1 런 자원 / 메타 자원 분류표 — 사망 시 무엇이 계승되는지 | P11~13 | P13 진입 전 |
| Q-13 | 적 에셋 다양성 전략 — 같은 휴머노이드 + 무기/색 변형(무협 표준) vs 별개 적 종 | P5~6 | P5 진입 전 |
| Q-14 | 휘발 풀 카드와 계승 풀 카드의 효과 강도 비율 (한 런 5장 휘발 vs 1~3장 계승) | P12 | P12 진입 전 |
| Q-15 | 휘발 풀에서 동일 무공 카드 중복 획득 시 처리 (스택/강화/거절) | P12 | P12 진입 전 |
| Q-16 | 계승 슬롯 12성 도달 후 추가 카드 처리 (잉여 / 골드 전환 / 풀 제외) | P12~13 | P12 진입 전 |
| Q-17 | 액티브 무공 2슬롯 선택 방식 — 런 시작 시 결정 / 첫 카드로 자동 결정 / 풀에 여러 무공 다양성 | P12 | P12 진입 전 |
| Q-18 | 재능 가중치가 어느 풀에 영향 — 휘발만 / 계승만 / 둘 다 | P8/P12 | P8 또는 P12 진입 전 |

---

## 5. 변경 이력

| 날짜 | 버전 | 변경 |
|------|------|------|
| 2026-05-04 | 0.1 | 초안 작성 — 신규 설계, 11 Phase 명세, Open Questions 10건 |
| 2026-05-04 | 0.2 | 자기 규칙 정리 — 잠정 결정을 결정으로 확정 (P5 강공 큐잉 FIFO, P5/P6 MindGame 시간 정지, P6 타임아웃=Pierce). Q-4/8/9 흡수 후 삭제 + 잔여 Q 번호 재정렬. P3에 Approach→Engage 전이 명시. P9 경공 트리거 조건에 Gyeonggong 무공 슬롯 보유 추가. P1 Player.position=0 고정 명시. P0 Time.timeScale 의존 제거. P5 EnemyActor.heavyAttackCooldown 초기값 명시. |
| 2026-05-11 | 0.3 | **v4 피벗 — 모바일 오토배틀 로그라이트** (옵시디언 나이트 레퍼런스). §0.5 피벗 섹션 신설, §1 Goals/Non-goals v4 갱신(모바일 우선, PC 제외, 분파/경로 v1.0 외). **Phase 5/6 강공·오의 매트릭스를 보스전 한정으로 스코프 축소** (`EnemyData.isBoss` 게이팅). Phase 11~15 신규 추가(런 구조, 카드 드래프트, 메타 진행, 모바일 UI, 비동기 PvP) — 개요만, 상세는 진입 시점에. |
| 2026-05-11 | 0.3.1 | **Phase 0 완료** — Foundation 산출물: asmdef 4종(Domain/Engine/View/Tests), `ITickService`/`IRngService` 인터페이스 + Mock·실구현, 빈 `BattleEngine`, `BattleSceneController` 카운터 표시, EditMode 테스트 2종(TickServiceTests, RngServiceTests). Phase 1(Actor & 거리축) 진입 가능. |
| 2026-05-11 | 0.3.2 | **방어 5택 → 4택 축소** — `지패(Jipae)` 슬롯 제거(금강불괴와 "받되 감소" 카테고리 중복). 매트릭스 9칸 → 12칸으로 갱신, 허실공 Counter를 지패 → 금강불괴로 재배정. P5/P7 본문, MILESTONES M5, Phase_1_AssetQueue, TOTAL_DESIGN §4.1/§5.1, ASSET_PIPELINE §UI 동시 반영. |
| 2026-05-11 | 0.3.3 | **Phase 1 거리축 모델 전환 — 적 진군 → 플레이어 진군** (옵시디언 나이트 정합성). `ActorState.Approaching` → `Running`(주체 = Player). `EnemyData.stopPosition` → `engagementDistance`, `EnemyData.approachSpeed` 제거. `BattleStartData.playerRunSpeed` 신설. Enemy.position은 spawnPosition 고정, Player.position이 단조 증가. `Approach↔Engage` 전이 트리거 갱신(Player Idle 진입 / 웨이브 클리어). Player.position=0 고정 제약 해제 — Phase 9 경공이 mutable position 도입이 아니라 속도 부스트로 단순화. Phase_1_Guide.md 동시 갱신 예정. |
| 2026-05-11 | 0.3.5 | **§2.5 리팩토링 트리거 추가** — Sheets 이식 시점에 숫자 PK(`Id: int`) 추가 + 기존 string Id를 `Code`로 rename 계획 명시. 지금은 잠자는 필드 안 박음(YAGNI). |
| 2026-05-11 | 0.3.4 | **데이터 컨벤션 확장 — Id 규약 + L10n 정책 명시**. §2.5에 Id snake_case 규약, SO→SO 참조는 Inspector 직접 참조 OK(Sheets 도입 시 Id resolver로 일괄 전환) 추가. **로컬라이제이션 인프라 Phase 1부터 적용** — `Unity Localization` 패키지 사용, `DisplayName`/표시 문자열 SO 직접 저장 금지, `NameKey`/`DescKey`(`<category>.<id>` 네임스페이스)로 String Table 조회. §1.2 Non-goals에서 "멀티언어"를 "콘텐츠 번역은 v1.0 직전, 인프라는 Phase 1부터"로 분리. EnemyData/SkillData/TalentData 표 `displayName` → `nameKey`(+`descKey`)로 갱신. Phase_1_Guide.md EnemyData 코드 동기화. |
| 2026-05-13 | 0.3.6 | **Phase 1 사거리 모델 정정 — `EnemyData.engagementDistance` → `Actor.attackRange`**. 적이 능동 행동하지 않는 Phase 1에서 "교전 거리"는 플레이어의 공격 사거리(`player.attackRange`)가 의미상 정답. `AttackRange`는 Actor 공통 속성에 두고 Phase 1엔 Player만 사용, Phase 4+에 Enemy도 능동 공격 시작 거리로 활용. EnemyData에서 engagementDistance 제거, PlayerStartData에 AttackRange 추가. State machine·Behaviors·Edge 클램프식·Acceptance 동시 갱신. |
| 2026-05-14 | 0.3.7 | **Phase 1 Acceptance #2 현실화 — "4명 동일 spawn=100" → "3명 다른 spawn={60,80,100}"**. 옵나 스타일에서 적이 같은 위치에 겹쳐 등장하지 않음을 반영. 새 케이스는 `FindNearestAliveEnemyIndex` 로직과 nearest-stop 동작을 실제로 검증함(이전 케이스는 거리 동일이라 nearest 분기가 안 돌았음). |
| 2026-05-14 | 0.3.8 | **Phase 1 완료** — 1D 거리축 + 플레이어 진군 + Resolve(victory). Domain 10타입(`Actor`/`PlayerActor`/`EnemyActor`/`EnemyData`/`BattleStartData`/`PlayerStartData`/`BattleSnapshot`/`ActorView`/`ActorState`/`BattlePhase`) + Engine(`BattleEngine` 재작성 — `HandleTick` 오케스트레이션 + `TickMovement`/`TickEngagementCheck` 서브루틴) + View(거리 게이지 + EnemyMarker prefab). EditMode 테스트 2종(`BattleEngineApproachTests`, `BattleEngineNearestEnemyTests`) 통과. Phase 1 Guide/Learned/AssetQueue 산출. 커밋 `afe00ac`. |
| 2026-05-14 | 0.4 | **v4.1 스코프 컷 + 서사 통일** (피벗 아님). (a) **PvP 컷** — Phase 15 stub 제거, §1.2 Non-goals/§0.5 PvP 표기 재프레이밍, §1.1 Goal 5 "외부 시연 재현"으로 정정, MILESTONES §5로 이동. (b) **재능 매커니즘** — 런 시작 선택 → 런 종료 확률 각성, 캐릭터에 0~3개 누적·동시 작동·가중치 스택. 첫 런 = 재능 0개 = 신참 베이스라인. §1.1 Goal 3 + Phase 8 헤더 노트 갱신. (c) **서사 통일** — 카드=무공 습득/메타=수련 누적/각성=깨달음 (narrative 본문은 TOTAL_DESIGN v4 리프레시 시 적용). (d) §4 Open Q에 v4.1 보류 6건 추가(Q-8~Q-13). |
| 2026-05-14 | 0.4.1 | **카드 시스템 구조 결정** — 하이브리드 이중 풀(계승+휘발). 계승 풀에 무공 4슬롯(액티브 2 + 패시브 2), 슬롯별 1~6장 모션 진화 + 7~17장 1~12성 위력 누산(12성 특별 연출). 휘발 풀은 슬롯 없이 한 런 내 누적, 무공+패시브 둘 다. 단일 클래스=검사, 평타 세로 베기 자동. Phase 12/13 개요 갱신, Open Q-14~Q-18 추가. |
| 2026-05-14 | 0.4.2 | **Phase 2 스코프 컷 — 기세/오성 제외**. (a) **기세**: 단독 자원으로 의미 부족(쌓기·소비 메커닉과 짝일 때만 성립) → Phase 3 SkillData 도입과 동반 이동(P3 PlayerActor 확장에 momentum/maxMomentum, IResourceMutator에 GainMomentum/SpendMomentum). (b) **오성**: per-battle 자원으로 두면 키우기 게임 결에서 곧 max → 메타 패시브 슬롯(PlayerData, Phase 11+)으로 이관. P7 Wisdom 출처 = 메타 슬롯(Phase 11+ 미도입 시 placeholder), P8 talent 표에서 startingWisdom 제거(카피 정보 핸디캡 의도는 메타 풀 가중치로 재현). Q-3 회복원에서 기세 제거, Phase 2 Acceptance에 VContainer 항목 추가. Phase_2_Guide.md 동시 갱신. |

---

## 6. Appendix — 누적 통합 모델

> 이 섹션은 Phase 진행에 따라 갱신된다. 각 Phase 종료 시 그 Phase가 추가/변경한 부분을 반영.

### 6.1 데이터 모델 통합 (Phase 진행 시 갱신)
- (P0): 비어있음.

### 6.2 FSM 통합
- (P0): 비어있음.

### 6.3 인터페이스 통합 시그니처
- (P0): `ITickService`, `IRngService`, `IBattleEngine`.

### 6.4 이벤트 카탈로그
- (P0): `SnapshotPublished`, `OnResult`.

> 위 4개 부록은 Phase 1 종료 시점부터 누적 갱신.
