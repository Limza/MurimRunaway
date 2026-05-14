# Scripts/CLAUDE.md — 코드 규칙

`Assets/_Project/Scripts/` 하위 C# 코드에 적용되는 규칙. 프로젝트 전반 규칙은 루트 [CLAUDE.md](../../../CLAUDE.md) 참조.

---

## 1. 코드 원칙

### YAGNI (You Aren't Gonna Need It)
- **지금 필요한 것만 만든다.** "나중에 쓸 것 같아서" 미리 만들지 않는다.
- 빈 스텁·플레이스홀더 타입을 "인터페이스가 참조하므로" 식으로 미리 정의하지 않는다.
- 인터페이스는 두 번째 구현체가 생기는 시점, 또는 테스트 Mock이 필요한 시점에 추출한다.

### 단순하게
- 추상화는 중복이 세 번 이상 보일 때 도입한다.
- 에러 핸들링·폴백·검증은 실제로 발생 가능한 경계(사용자 입력·외부 API)에만 넣는다.
- 한 Phase에서 요구하지 않은 기능을 앞당겨 구현하지 않는다.

### 주석
- 기본적으로 주석 없음.
- WHY가 코드만으로 불분명할 때(숨겨진 제약·특정 버그 우회)만 한 줄.
- WHAT 설명 주석(코드가 이미 말하는 것) 금지.

---

## 2. 아키텍처 규칙

SSOT는 [Docs/BATTLE_DESIGN.md](../../../Docs/BATTLE_DESIGN.md). 전투 시스템 설계·인터페이스·Phase 정의는 그쪽을 따른다.

- **Domain ← Engine ← View** 단방향 의존. 역방향 참조는 컴파일 에러로 막혀 있음.
- `Time.deltaTime` Engine 레이어에서 직접 사용 금지 — `ITickService.Ticked` 이벤트 경유.
- 모든 RNG는 `IRngService` 경유 — `UnityEngine.Random`·`System.Random` 직접 호출 금지.
- 런타임 가변 값은 ScriptableObject에 저장 금지 — POCO 인스턴스에 복사 후 변경.

### 명명 규칙

- **`Service` 접미사 = 외부 의존성 게이트웨이.** Unity 전역 API(`Time`, `Random` 등)나 외부 자원을 인터페이스 뒤로 숨겨 결정론·테스트 가능성을 확보하는 얇은 추상화에만 붙인다. 예: `ITickService`, `IRngService`. 도메인 로직 모음에는 붙이지 않는다.
- **DTO/VO/Entity 같은 패턴 접미사는 붙이지 않는다.** 역할이 모호하면 역할이 드러나는 이름을 쓴다 (예: `BattleSnapshot`).
- **Enum은 `Enums/` 하위 폴더에 두고, 이름에 `Type`/`Enum` 접미사를 붙이지 않는다.** 네임스페이스는 부모와 동일하게 유지(폴더는 조직용). 이유: VS Code 파일 트리가 `.cs` 아이콘 통일이라 클래스와 enum이 시각적으로 안 구분됨 — 폴더로 가른다.
- **Enum 타입과 모든 멤버에 XML `///` 주석을 단다.** 도메인 용어(`Approach`, `Resolve` 등)는 코드만으로 의미가 안 드러나므로 호버 툴팁으로 즉시 확인할 수 있게 한다. WHAT 설명 금지 원칙의 예외 — enum 멤버는 식별자가 짧아 의미를 담기 어려운 도메인 어휘인 경우가 많음.
