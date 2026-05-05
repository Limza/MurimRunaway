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
