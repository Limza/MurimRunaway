# 마일스톤 (Milestones) — 전투 시스템

> **본 문서의 위상**
> - 정식 게임의 **진행 추적용 마일스톤**. 일정이 아닌 **순서·상대 부담** 기록.
> - 의존: [BATTLE_DESIGN.md](BATTLE_DESIGN.md) (Phase 정의·Acceptance), [ASSET_PIPELINE.md](ASSET_PIPELINE.md) (에셋 생성 절차).
> - [TOTAL_DESIGN.md](TOTAL_DESIGN.md) 범위 중 **전투 외 시스템**(분파 정렬·경로·기연·결말 등)은 별도 마일스톤으로 추후 추가.
>
> **작성 규칙**
> - 캘린더 일정 금지 (시간 단위 비고정 솔로 개발). 부담은 S/M/L 상대 표기.
> - Phase 진입 조건: 직전 Phase의 모든 Acceptance + 컴파일 + 커밋.
> - 변경은 §7 변경 이력에 한 줄 추가.

---

## 0. 메타

| 항목 | 값 |
|------|----|
| 문서 종류 | 진행 추적용 마일스톤 |
| 대상 | 무림도망자 정식 게임 — 전투 시스템 |
| 버전 | 0.1 (초안) |
| 마지막 수정 | 2026-05-04 |

---

## 1. 전제 (Constraints)

- **솔로 개발, 시간 단위 비고정** — "시간 날 때마다" 진행. 캘린더 견적 금지.
- **에셋은 모두 AI 생성** — 자작·외주 없음. 도구·워크플로는 [ASSET_PIPELINE.md](ASSET_PIPELINE.md).
- **Placeholder 우선** — Phase 0~4까지 색+텍스트 placeholder로 진행. 실에셋은 M5 진입 직전부터.
- **한 Phase = 한 커밋 단위** — Acceptance 미달 상태로 다음 Phase 코드 시작 금지.

---

## 2. 부담 표기 (Effort)

| 표기 | 의미 (대략) |
|------|-------------|
| S | 작은 단위 — 1~3 작업 세션 (몇 시간) |
| M | 중간 — 4~10 작업 세션 (며칠 분량) |
| L | 큰 단위 — 10+ 작업 세션 (한 주 이상 누적) |

> 작업 세션 = 한 번에 앉아서 진행하는 단위. 본인 페이스에 맞춰 해석.

---

## 3. 마일스톤 일람

| # | Phase | 코드 | 에셋 | 주제 |
|---|-------|------|------|------|
| **M0** | P0 Foundation | S | — | 폴더·asmdef·Mock 서비스, 컴파일 골격 |
| **M1** | P1 Actor & 거리축 | S | — | Player/Enemy POCO + 진군·시간축 |
| **M2** | P2 자원 4종 | S | — | HP·내공·기세·오성 컨테이너 |
| **M3** | P3 무공 자동 시전 | M | — | SkillData SO + 자동 결정 트리 |
| **M4** | P4 일반 공격·데미지·승패 | M | — | 데미지 적용·사망·BattleResult |
| **M5** | P5 강공 분기 (방어 5택) | **L** | **M** | 매트릭스 + MindGame UI 첫 도입 |
| **M6** | P6 오의 분기 (공격 3택) | M | M | 핫키→타겟선택→3택 |
| **M7** | P7 오성 (Hint + 풀) | M | S | hint UI 차등 |
| **M8** | P8 재능 3종 | M | M | 재능 포트레이트 + 선택 화면 |
| **M9** | P9 회피 시스템 | S | S | 회피 RNG + 경공 보장 |
| **M10** | P10 UI 계약·Telemetry | S | **M** | UI 폴리시 패스 (색·폰트·여백 정합) |

> M5와 M10이 부담 피크 — M5는 첫 실에셋 + 매트릭스 UI 동시, M10은 폴리시 패스에 자잘한 일이 누적.

---

## 4. Phase별 상세

각 마일스톤의 코드는 [BATTLE_DESIGN.md](BATTLE_DESIGN.md) §3 Phase 본문을, 에셋은 [ASSET_PIPELINE.md](ASSET_PIPELINE.md) §4 워크플로를 참조한다.

### M0 — Phase 0 Foundation (S / —)
- **코드**: BATTLE_DESIGN §3 Phase 0 — 폴더/asmdef/네임스페이스, ITickService·IRngService Mock, 빈 BattleEngine.
- **에셋**: 없음 (텍스트 카운터 한 줄).
- **Done**: P0 Acceptance 4개 + git commit.

### M1 — Phase 1 Actor & 거리축 (S / —)
- **코드**: BATTLE_DESIGN §3 Phase 1.
- **에셋**: 없음 (위치를 숫자/막대로 표기).
- **Done**: P1 Acceptance 4개.

### M2 — Phase 2 자원 4종 (S / —)
- **코드**: BATTLE_DESIGN §3 Phase 2.
- **에셋**: 없음 (자원 4종 게이지를 UGUI Slider 디폴트로).
- **Done**: P2 Acceptance 3개.

### M3 — Phase 3 무공 자동 시전 (M / —)
- **코드**: BATTLE_DESIGN §3 Phase 3 — 자동 시전 결정 트리.
- **에셋**: 없음 (슬롯 UI는 텍스트 라벨로).
- **Done**: P3 Acceptance 4개. 시각 검수: 슬롯 위 쿨 게이지 + 시전 시 스킬명 1초 표시.

### M4 — Phase 4 일반 공격·데미지·승패 (M / —)
- **코드**: BATTLE_DESIGN §3 Phase 4.
- **에셋**: 없음 (데미지 숫자는 TMP 텍스트 팝).
- **Done**: P4 Acceptance 4개.

### M5 — Phase 5 강공 분기 (L / M) ★ **첫 실에셋 마일스톤**
- **코드**: BATTLE_DESIGN §3 Phase 5 — 방어 매트릭스, MindGame FSM, FIFO 큐잉.
- **에셋** (ASSET_PIPELINE 워크플로):
  - 방어 5택 아이콘 (이화접목·보법·나려타곤·금강불괴·지패) — §4.1
  - 강공 차징 표시 (⚠ 머리 위) — §4.4 (UI 키트 활용)
  - 적 패턴 hint 표시 아이콘 3종 (직공·연환공·허실공) — §4.1
  - 매치업 결과 피드백 (Counter ★ / Normal / Weak ✗) — §4.4
- **Done**: P5 Acceptance 5개 + 5택 UI가 손으로 들어왔을 때 어색하지 않은 수준.

### M6 — Phase 6 오의 분기 (M / M)
- **코드**: BATTLE_DESIGN §3 Phase 6 — 공격 매트릭스, EnemyTargetSelect → MindGameOffense.
- **에셋**:
  - 오의 패턴 3 아이콘 (정면강타·관통·허초후공격) — §4.1
  - 적 방어 패턴 3 표시 (흘리기·강공방어·무방비) — §4.1
- **Done**: P6 Acceptance 4개.

### M7 — Phase 7 오성 (M / S)
- **코드**: BATTLE_DESIGN §3 Phase 7 — Hint·ChoicePool 등급 분기.
- **에셋**: hint 신뢰도 시각화 (Low/Mid/High 색·아이콘). UI 변형만, 새 일러스트 없음.
- **Done**: P7 Acceptance 3개.

### M8 — Phase 8 재능 3종 (M / M)
- **코드**: BATTLE_DESIGN §3 Phase 8 (시작값 차별화까지). 8.1·8.2는 후속 Phase.
- **에셋**:
  - 재능 포트레이트 3 (천무지체·카피·대종사) — §4.2
  - 재능 선택 화면 레이아웃 — §4.4 (UI 키트)
- **Done**: P8 Acceptance 3개.

### M9 — Phase 9 회피 시스템 (S / S)
- **코드**: BATTLE_DESIGN §3 Phase 9.
- **에셋**: 회피 이펙트 — 처음엔 텍스트 "회피!" 팝, 여유 있을 때 §4.5 SFX 한 개.
- **Done**: P9 Acceptance 4개.

### M10 — Phase 10 UI 계약·Telemetry·폴리시 (S / M)
- **코드**: BATTLE_DESIGN §3 Phase 10 (구조 정리).
- **에셋** (이번에 마무리):
  - 폰트 (한국어 본문·제목 2~3종) — ASSET_PIPELINE §2.3
  - SFX 4종 (UI 클릭·강공 알림·오의 발동·승/패) — §4.5
  - UI 색·여백 정합 패스 — §4.4
- **Done**: P10 Acceptance 3개 + 한 전투 풀 플레이를 외부인에게 보여줘도 이상하지 않은 수준.

---

## 5. 마일스톤 외 작업 (Phase에 묶이지 않음)

전투 외 시스템·자산. 본 문서 범위 외이며, 진입 시점에 별도 마일스톤으로 정의.

| 작업 | 출처 | 진입 시점 |
|------|------|----------|
| 분파 정렬 시스템 | TOTAL_DESIGN §8 | M10 이후 |
| 경로 분기 / 맵 | TOTAL_DESIGN §5.4, §7.3 | 〃 |
| 기연 이벤트 (15~20종) | TOTAL_DESIGN §6.4 | 〃 |
| 보상 시스템 (3중1) | TOTAL_DESIGN §5.3, §7.4 | 〃 |
| 보스 (추격대장 3페이즈) | TOTAL_DESIGN §5.9 | 분파/경로 후 |
| 결말 일러스트 5장 | TOTAL_DESIGN §5.10, §6.3 | 출시 직전 |
| BGM 0~5 트랙 | TOTAL_DESIGN §9.4, §12 C-8 | 출시 직전 |

---

## 6. [검토 중]

| # | 항목 | 결정 시점 |
|---|------|----------|
| MS-1 | M5의 첫 에셋 작업이 코드 진행을 막는다고 느껴지면 placeholder 유지 + M10에 일괄 배치 가능 | M5 진입 직후 결정 |
| MS-2 | M8 재능 포트레이트를 외주 일러스트(추후)로 갈아끼울지 | 출시 6개월 전 |

---

## 7. 변경 이력

| 날짜 | 버전 | 변경 |
|------|------|------|
| 2026-05-04 | 0.1 | 초안 — 11 Phase × 코드/에셋 상대 부담 + 마일스톤 외 작업 식별 |
