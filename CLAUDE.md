# CLAUDE.md — 무림도망자 개발 규칙

AI 어시스턴트와 작업할 때 따르는 프로젝트 전반 규칙. 코드 제안·문서 작성 모두 이 규칙을 우선한다.

코드 규칙(YAGNI·아키텍처·명명)은 [Assets/_Project/Scripts/CLAUDE.md](Assets/_Project/Scripts/CLAUDE.md)에 있다. Scripts 폴더에서 작업할 때 자동으로 로드된다.

---

## 1. Phase 진행 규칙

- 한 Phase의 Acceptance 체크리스트를 모두 통과한 후 커밋. 미달 상태로 다음 Phase 코드 시작 금지.
- Phase 종료 시 [Docs/BATTLE_DESIGN.md §5 변경 이력](Docs/BATTLE_DESIGN.md)에 한 줄 추가.
- 작업 중 설계 변경이 생기면 코드보다 SSOT 문서를 먼저 수정한다.

---

## 2. 문서 규칙

- `BATTLE_DESIGN.md` — 전투 시스템 SSOT. 충돌 시 최우선.
- `Docs/Phases/Phase_N_Guide.md` — 작업 절차 안내 (SSOT 아님).
- `Docs/Phases/Phase_N_Learned.md` — 학습 노트. 개념·설계 결정 이유.
- 새 Phase 시작 시 Guide + Learned 두 파일을 함께 만든다.

---

## 3. 코드 가독성 — 《프로그래머의 뇌》 기준

코드 제안(실제 소스 + 문서 내 코드 블록 모두)은 **인지 부하 3종**을 최소화하도록 작성한다.

- **지식 부족 부하 줄이기** — 같은 모듈에 이미 있는 추상화를 우선 사용한다. 같은 기능을 저수준 API로 다시 풀어 쓰지 않는다 (예: 클래스 안에 `NextFloat01()`이 있으면 `(float)_random.NextDouble()` 직접 호출 금지).
- **정보 부족 부하 줄이기** — 변수명에 도메인 의미를 담는다. `total`/`acc`/`tmp` 같은 일반 명사 대신 `weightSum`/`cumulative`/`target`처럼 역할이 드러나는 이름을 쓴다. 의도가 코드만으로 안 보이는 한 줄(부동소수점 보호, 경계 우회 등)에는 WHY 한 줄 주석을 허용한다.
- **처리 과부하 줄이기** — 한 표현식이 동시에 추적해야 할 개념을 3개 이하로 유지한다. 튜플·중첩 인덱싱은 루프 진입 직후 분해(`var (item, weight) = pool[i];`)해 청크화한다.

문서 내 코드 블록도 동일 기준. Phase Guide의 예제 코드가 이 기준에 어긋나면 **가이드를 먼저 고치고** 사용자에게 따라 치게 한다.
