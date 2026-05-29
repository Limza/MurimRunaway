# MurimRunaway

무림도망자는 Unity 기반 모바일 세로형 오토배틀 로그라이트 프로토타입입니다.

이 저장소는 완성된 콘텐츠 양보다 **테스트 가능한 전투 시뮬레이션 구조**와 **재현 가능한 게임 규칙 구현 방식**을 보여주는 데 초점을 둡니다. 클라이언트 프로젝트이지만, 전투 룰 엔진은 서버 검증이나 리플레이 디버깅을 염두에 둔 방식으로 분리하고 있습니다.

## 채용자가 볼 포인트

- Unity `6000.3.14f1` 기반 C# 프로젝트
- `Domain / Engine / View` 3계층 분리
- asmdef로 계층 의존 방향을 컴파일 타임에 제한
- 고정 Tick, 시드 기반 RNG, Snapshot 발행으로 재현 가능한 전투 흐름 설계
- EditMode 테스트로 이동, 교전, 자원 변경, 자동 시전, 쿨다운, 결정성 검증
- SSOT 문서와 Phase별 Acceptance 체크리스트로 설계 변경과 구현 범위 추적

## 현재 구현 범위

현재는 전투 코어를 검증하는 초기 단계입니다.
그래픽과 연출은 의도적으로 단순하게 두고, 전투 규칙과 테스트 구조를 먼저 닫고 있습니다.

| 구분 | 구현 상태 |
|------|-----------|
| Phase 0 | 폴더, asmdef, Tick/RNG 서비스, 전투 흐름 골격 |
| Phase 1 | Actor 모델, 1D 거리축, 플레이어 진군, 가장 가까운 적 교전 |
| Phase 2 | HP, 내공, 기세 자원 컨테이너와 변경 진입점 |
| Phase 3 | SkillData, 스킬 슬롯, 자동 시전 우선순위, 쿨다운, 결정성 |
| Phase 4+ | 데미지, 사망, 승패 판정, 보스전 수싸움 시스템은 문서화된 다음 단계 |

## 아키텍처

| 계층 | 책임 | 참조 가능 |
|------|------|-----------|
| `Domain` | 전투 데이터 언어. POCO 상태, ScriptableObject 정의, Snapshot/Event 타입 | 없음 |
| `Engine` | 전투 시뮬레이션 실행. Tick, 이동, 교전, 자동 시전, RNG 처리 | `Domain` |
| `View` | Unity 표현 계층. MonoBehaviour, UI, 표시용 보간 | `Engine`, `Domain` |
| `Tests` | 전투 규칙과 결정성 검증 | `Engine`, `Domain` |

의존 방향은 아래 원칙을 따릅니다.

- `Domain`은 `Engine`을 모릅니다.
- `Engine`은 `View`를 모릅니다.
- `View`는 Snapshot을 읽어 화면에 표시하고, 보간 값을 Engine 상태로 되돌려 쓰지 않습니다.
- 전투 난수는 `IRngService`를 통해서만 사용합니다.
- Engine 시간은 `Time.deltaTime` 직접 사용 대신 `ITickService` 이벤트로 진행합니다.

## 먼저 보면 좋은 파일

| 목적 | 경로 |
|------|------|
| 전투 시스템 SSOT | [Docs/BATTLE_DESIGN.md](Docs/BATTLE_DESIGN.md) |
| 마일스톤 계획 | [Docs/MILESTONES.md](Docs/MILESTONES.md) |
| 전투 엔진 진입점 | [Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs](Assets/_Project/Scripts/Battle/Engine/BattleEngine.cs) |
| Tick 단위 시스템 | [Assets/_Project/Scripts/Battle/Engine/Systems](Assets/_Project/Scripts/Battle/Engine/Systems) |
| Domain 데이터와 Snapshot | [Assets/_Project/Scripts/Battle/Domain](Assets/_Project/Scripts/Battle/Domain) |
| EditMode 테스트 | [Assets/_Project/Tests/Battle](Assets/_Project/Tests/Battle) |
| 전투 코드 배치 규칙 | [Assets/_Project/Scripts/Battle/AGENTS.md](Assets/_Project/Scripts/Battle/AGENTS.md) |

## 테스트가 보여주는 신호

현재 EditMode 테스트는 아래 흐름을 검증합니다.

- 같은 시드의 RNG 시퀀스가 동일한지
- TickService의 Pause/Resume이 Tick 발행을 제어하는지
- 플레이어가 가장 가까운 적의 교전 거리까지 진군하는지
- 같은 입력의 전투 Snapshot 시퀀스가 동일한지
- 내공과 기세가 정해진 진입점을 통해서만 변경되는지
- 자동 시전이 슬롯 우선순위와 쿨다운을 지키는지
- 같은 입력의 시전 이벤트 시퀀스가 재현되는지

테스트는 작게 유지합니다. 각 Phase는 Acceptance 체크리스트를 닫는 데 필요한 검증만 추가합니다.

## 로컬에서 확인하는 방법

1. Unity `6000.3.14f1`로 프로젝트를 엽니다.
2. 코드 탐색은 [MurimRunaway.slnx](MurimRunaway.slnx)를 사용합니다.
3. Unity Test Runner에서 `Assets/_Project/Tests/Battle` 아래 EditMode 테스트를 실행합니다.
4. 설계 의도를 먼저 보고 싶다면 [Docs/BATTLE_DESIGN.md](Docs/BATTLE_DESIGN.md)부터 읽으면 됩니다.

## 설계 메모

이 프로젝트는 전투 규칙을 나중에 서버 검증이나 리플레이 디버깅에 사용할 수 있다는 가정으로 설계하고 있습니다.

그래서 전투 코어는 Unity 컴포넌트에 직접 묶기보다, 명시적인 입력값, 고정 Tick, 시드 기반 난수, 읽기 전용 Snapshot, 테스트 가능한 서비스 경계를 우선합니다.

또한 솔로 개발 프로젝트이기 때문에 Phase를 작게 나누고, 각 Phase가 끝날 때 문서와 Acceptance 기준으로 범위를 닫는 방식을 사용합니다.
