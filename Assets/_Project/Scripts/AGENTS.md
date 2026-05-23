# Scripts/AGENTS.md — 코드 규칙

`Assets/_Project/Scripts/` 하위 C# 코드에 적용되는 규칙. 프로젝트 전반 규칙은 루트 [AGENTS.md](../../../AGENTS.md) 참조.

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

- **`Service` 접미사 = 외부 의존성 게이트웨이.** Unity 전역 API(`Time`, `Random` 등)나 외부 자원(향후 저장·네트워크·광고 SDK)을 인터페이스 뒤로 숨겨 결정론·테스트 가능성을 확보하는 얇은 추상화에만 붙인다. 예: `ITickService`, `IRngService`. 도메인 로직 모음에는 붙이지 않는다.
- **`System` 접미사 = 내부 시뮬레이션 모듈.** `BattleContext` 같은 공유 상태를 매 Tick 변경하는 cohesive 도메인 로직 묶음. **외부 의존이 없고** 엔진(`BattleEngine`)이 등록 순서대로 호출한다. 예: `MovementSystem`/`EngagementSystem`/`CastingSystem`. ECS의 "System" 개념과 같은 결.
- **`Service` ↔ `System` 판별식**: *"이걸 mock으로 갈아 끼워도 게임 규칙은 그대로인가?"* → Yes면 Service(시간·난수·저장 등 환경 어댑터), No면 System(규칙 자체). 헷갈리면 Engine asmdef의 `UnityEngine` 의존 차단선에 걸치는지로 본다 — 그 선을 넘어 외부와 통신하면 Service, 안에서 상태만 만지면 System.
- **DTO/VO/Entity 같은 패턴 접미사는 붙이지 않는다.** 역할이 모호하면 역할이 드러나는 이름을 쓴다 (예: `BattleSnapshot`).
- **Enum은 `_Enums/` 하위 폴더에 두고, 이름에 `Type`/`Enum` 접미사를 붙이지 않는다.** 네임스페이스는 부모와 동일하게 유지(폴더는 조직용). 이유: VS Code 파일 트리에서 enum 묶음이 바로 보이게 하기 위함이다.
- **Enum의 첫 값은 `None = 0`으로 둔다.** 초기화를 빼먹었을 때 실제 값처럼 보이지 않게 하기 위함이다. 외부 파일/API에서 0이 정해진 값이면, 그 이유를 가까운 문서에 남긴다.
- **Enum 타입과 모든 멤버에 XML `///` 주석을 단다.** 도메인 용어(`Approach`, `Resolve` 등)는 코드만으로 의미가 안 드러나므로 호버 툴팁으로 즉시 확인할 수 있게 한다. WHAT 설명 금지 원칙의 예외 — enum 멤버는 식별자가 짧아 의미를 담기 어려운 도메인 어휘인 경우가 많음.

---

## 3. 개행 규칙

코드와 문서(이 저장소의 `.md` 산문 포함) 모두 한 줄에 정보를 몰아넣지 않는다. "한 호흡에 읽히는 단위"로 줄을 끊는다.

### 코드

- **한 줄에 한 문장.** `A = a; B = b;` 식 세미콜론 묶음 금지. `;`마다 줄바꿈.
- **`if (x) return/continue/break;` 한 줄 묶기 금지.** 본문은 다음 줄(중괄호 생략은 OK):
  ```csharp
  if (target == null)
      return;
  ```
- **조건·표현식이 길어지면** `&&`/`||`/`?.` 앞에서 줄을 끊어 들여쓰기로 정렬한다. 한 줄에 연산자 3개 이상 엉기지 않게.
- **튜플·중첩 인덱싱은 루프 진입 직후 분해.** `var (item, weight) = pool[index];` 같은 한 줄로 청크화 (루트 AGENTS.md §4 처리 과부하).

### 문서 산문 (Docs/**/*.md)

- **문장 단위로 개행.** 마침표·물음표·`—` 등 문장 끝마다 줄바꿈을 적극 고려. 한 줄에 4문장 이상 몰리면 무조건 쪼갠다.
- **콜아웃(`> [!note]` 등) 내부도 동일.** 근거 / 결론 / 보충 문단 사이에는 빈 `>` 줄로 단락을 나눈다.
- **핵심은 폴딩으로 숨기지 않는다** (Docs/AGENTS.md §2).
