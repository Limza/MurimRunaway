# 에셋 파이프라인 (Asset Pipeline) — AI 생성 기준

> **본 문서의 위상**
> - 정식 게임의 **AI 에셋 생성 워크플로 + 에셋 카탈로그 SSOT**.
> - 사용 도구·라이선스·프롬프트 패턴·Unity 임포트 스펙을 한 곳에서 결정.
> - 의존: [TOTAL_DESIGN.md](TOTAL_DESIGN.md) §9 (아트 디렉션), [MILESTONES.md](MILESTONES.md) (마일스톤별 에셋 진입 시점).
>
> **작성 규칙**
> - 결정된 도구/스펙만 본문. 미결은 §7 [검토 중].
> - 라이선스 변경은 즉시 반영 (도구 약관은 분기당 1회 재확인).

---

## 0. 메타

| 항목 | 값 |
|------|----|
| 문서 종류 | 에셋 생성 워크플로 |
| 대상 | 무림도망자 정식 게임 — 전 에셋 |
| 버전 | 0.3 |
| 마지막 수정 | 2026-05-11 |

---

## 1. 원칙

1. **기본 생성기는 GPT 이미지** — 현재 개발 단계에서는 별도 툴 학습 비용보다 빠른 시안 생성·대화식 수정의 이득이 더 크다.
2. **상업 라이선스 검증 의무** — Steam 판매 빌드에 들어가는 모든 자산은 상업 사용이 명시된 도구·모델·소스에서만. 모호하면 사용 금지.
3. **AI 생성 우선, 자작·외주 없음** — 일러스트·아이콘·SFX·BGM 모두 AI. UI 표준 요소는 무료 CC0 키트 픽업 허용.
4. **무협 톤·색 코드 준수** — TOTAL_DESIGN §9.2 (무공 4분류), §9.3 (분파 5색)을 프롬프트와 후처리에 반영.
5. **Placeholder는 가능한 한 오래** — 코드 Phase가 막히지 않게. 실에셋 진입 시점은 [MILESTONES.md](MILESTONES.md) §3 참조.
6. **수동 생성 기준의 현실적인 일관성 관리** — GPT 이미지는 SD식 시드 재현성보다 기준 이미지 재사용·동일 세션 작업·짧은 수정 루프가 더 중요하다.

---

## 2. 도구 스택

### 2.1 출시 빌드용 기본 스택

| 용도 | 도구 | 비고 |
|------|------|------|
| 이미지 (기본) | **ChatGPT Images / GPT 이미지** | 현재 프로젝트의 기본 생성기. 빠른 시안·수정·대화식 반복에 최적 |
| 이미지 (자동화/대량 반복 필요 시) | **GPT Image API** | 별도 API 과금. 파일명 규칙·폴더 적재·후처리 스크립트 연결용 |
| 이미지 후처리 | **GIMP** / **Krita** | 자르기, 배경 정리, 색 보정, 크기 정규화 |
| UI 키트 | **Kenney.nl** | CC0 — 무제한 |
| 효과음 (SFX) | **Pixabay** SFX (CC0) / **Freesound.org** (CC0/CC-BY 필터) | CC0 = 무제한 / CC-BY = 크레딧 표기 |
| BGM | **Pixabay Music** (CC0) | CC0 — 무제한 |
| 한국어 폰트 | **Pretendard**, **나눔체** 계열 | OFL — 상업 OK |

### 2.2 보조/대체 도구

| 용도 | 도구 | 사용 조건 |
|------|------|-----------|
| 이미지 (대량 배치·API 친화) | **Leonardo.Ai** / **FLUX API** | GPT 수동 작업이 병목일 때만 검토 |
| 이미지 (벡터/UI 보조) | **Recraft** | UI·벡터 자산 비중이 커질 때만 검토 |
| 효과음 2차 생성 | **ElevenLabs SFX 유료** | Pixabay에 적합한 소스가 없을 때 |
| BGM 2차 생성 | **Suno Pro 유료** | Pixabay Music으로 부족할 때 |

---

## 3. 에셋 카탈로그 (출시 범위)

| 카테고리 | 수량 | 마일스톤 | 워크플로 |
|----------|------|---------|---------|
| 무공 아이콘 | 30~40 | M5+ 누적 | §4.1 |
| 방어 선택 아이콘 | 5 | M5 | §4.1 |
| 적 강공 패턴 hint 아이콘 | 3 | M5 | §4.1 |
| 오의 패턴 아이콘 | 3 | M6 | §4.1 |
| 적 방어 패턴 표시 | 3 | M6 | §4.1 |
| 적 포트레이트 | 8~10 | M5+ | §4.2 |
| 보스 포트레이트 (3페이즈) | 1 + 2 변형 | 보스 마일스톤 | §4.2 |
| 재능 포트레이트 | 3 | M8 | §4.2 |
| 분파 아이콘·라벨 | 5 | TOTAL 범위 | §4.1 |
| 결말 일러스트 | 5 | 출시 직전 | §4.3 |
| 강공 차징 표시 (⚠) | 1 | M5 | §4.4 (UI 키트) |
| 매치업 결과 피드백 (★/Normal/✗) | 3 | M5 | §4.4 |
| HP·자원 게이지 4종 | 세트 | M2 placeholder → M10 | §4.4 |
| SFX | 4종 (UI 클릭·강공 알림·오의 발동·승/패) | M10 | §4.5 |
| BGM | 0~5 트랙 | 출시 직전 | §4.6 |
| 한국어 폰트 | 본문/제목 2~3 | M10 | §2.1 픽업 |

---

## 4. 에셋 타입별 워크플로

### 4.0 GPT 이미지 공통 작업 규칙

1. **한 번에 한 카테고리만 작업** — 아이콘 세션, 포트레이트 세션, 결말 세션을 분리한다.
2. **첫 결과를 기준 이미지로 삼아 같은 대화/세션에서 이어 수정** — 새 채팅으로 흩어지면 톤이 흔들린다.
3. **프롬프트를 매번 새로 쓰기보다 수정 지시를 누적** — "같은 스타일 유지, 배경만 단순화" 같은 식으로 단계적으로 다듬는다.
4. **기준 샘플을 먼저 1~3장 확보** — 수량을 늘리기 전 톤을 고정한다.
5. **최종본만 Final로 이동** — 시안은 `Generated/`, 채택본은 `Final/`.

### 4.1 아이콘 (무공·방어·오의·적 패턴 등 — 60+개)

**도구**: ChatGPT Images.

**기본 프롬프트 템플릿**:
```text
A martial arts skill icon, {무공명} - {효과 한 문장},
ink wash painting on parchment, single centered subject,
high contrast, {색} accent (#XXXXXX),
subtle rough pixel texture only on the surface and edges,
not full pixel art, keep it as a sharp readable 2D ink icon,
square 1:1, no text, no watermark, no border
```

**권장 수정 루프**:
- 1차: 모티프와 톤 확인
- 2차: 배경 단순화, 아이콘 중심 피사체 강조
- 3차: 작은 크기에서도 식별되도록 형태 대비 강화
- 4차: 필요할 때만 표면과 가장자리에 아주 약한 픽셀 질감 추가, 전체를 정통 픽셀아트처럼 낮은 해상도로 만들지 않기

**색 매핑** (TOTAL_DESIGN §9.2):
| 분류 | 색 |
|------|----|
| 초식 | `#C03030` (적색) |
| 심법 | `#7030A0` (보라) |
| 경공 | `#3080C0` (청색) |
| 오의 | `#D0A030` (금색) |

**스펙**:
- 생성: 1024×1024 PNG
- Unity 임포트: Sprite (2D and UI), Pixels Per Unit 100, Filter Bilinear, Compression Normal Quality
- 게임 내 표시: 128×128 (UI 슬롯) 또는 256×256 (강조)

**검수 기준**:
- [ ] 4분류 색이 한 화면에서 즉시 구분되는가
- [ ] 같은 분류 내 형태가 너무 비슷하지 않은가 (실루엣 테스트 — 흑백 변환 후 구분되는가)
- [ ] 256×256으로 축소해도 의미 전달되는가
- [ ] 거친 픽셀 질감이 "표면 텍스처"로만 느껴지고, 정통 도트 아이콘처럼 가독성을 해치지 않는가

**예상 반복**: 1 아이콘당 2~4회 수정 루프 → 1 채택.

### 4.2 캐릭터 포트레이트 (적·재능·보스)

**도구**: ChatGPT Images.

**프롬프트 템플릿** (적):
```text
Wuxia character portrait, {잡적|자객|무림고수|...},
ink wash painting style, traditional East Asian martial arts setting,
half body, neutral background,
{분파 색} ambient lighting, parchment background, no text
```

**프롬프트 템플릿** (재능 — 천무지체/카피/대종사):
```text
Wuxia protagonist portrait, {재능 한 문장 묘사},
ink wash painting, half body, dignified expression,
parchment background, no text
```

**스펙**:
- 생성: 768×1024 PNG (세로 비율)
- Unity 임포트: Sprite, 384×512로 다운샘플 (UI 표시 사이즈)

**일관성 확보**:
- 같은 대화에서 연속 생성
- 첫 채택본을 기준 이미지처럼 삼아 "이전 인물과 같은 화풍 유지"를 반복 지시
- 적/재능/보스는 세션을 섞지 않는다

### 4.3 결말 일러스트 (5장)

**도구**: ChatGPT Images.

**프롬프트 템플릿** (분파별):
```text
Wuxia ending scene, {분파별 묘사},
cinematic ink wash, painterly composition, 16:9,
no text, no characters facing camera, no watermark
```

| 분파 | 묘사 |
|------|------|
| 귀환 (정파) | 정파 사문 전경, 새벽 안개, 깃발 |
| 마교 | 흑색 깃발 산채, 횃불, 황혼 |
| 개방 | 강호 떠돌이 거지의 뒷모습, 길가 |
| 소림 | 절 풍경, 종소리, 목탁, 평정 |
| 은퇴 | 평민 마을, 농부 차림, 무공 봉인 |

**스펙**:
- 생성: 1920×1080 PNG (16:9)
- Unity 임포트: Sprite, Default settings (UI 또는 Background로 사용)

**검수**:
- 5장을 같은 작업 세션 안에서 생성
- 기준 장면 1장을 먼저 정하고 나머지 4장을 그 톤으로 유도
- 5장을 한 화면에 늘어놓고 색온도·붓터치·디테일 수준 비교

### 4.4 UI 요소 (버튼·패널·게이지·차징 표시)

**선택**: 직접 생성 대신 **CC0 UI 키트 픽업** + 색만 무협 팔레트로 변경. 시간 효율 압도적.

- **Kenney.nl UI Pack** (CC0): https://kenney.nl/assets/ui-pack
- 9-slice 처리: Unity Sprite Editor에서 Border 설정 후 UI Image의 Image Type = Sliced.

**색 변경**: GIMP/Krita에서 Hue 시프트 또는 Unity Material로 tint.

**필요 요소**:
- 직사각 패널 (방어 4택용)
- 원형 슬롯 (무공 슬롯용)
- 가로 게이지 (HP/내공)
- 스택 바 (기세 0~10)
- 차징 표시 (⚠ 머리 위) — Kenney 또는 §4.1 워크플로로 자체 생성

### 4.5 SFX (4종 + 회피 등 추가)

**1차 시도**: Pixabay SFX 무료 픽업 (CC0).
- "wood click", "gong alert", "whoosh impact", "victory chime", "defeat tone"

**2차 (Pixabay에 적합한 게 없을 때)**: ElevenLabs SFX **유료** 티어로 텍스트→SFX 생성.

**프롬프트 예**:
- UI 클릭: `soft wood click, single tap, 0.2s`
- 강공 알림: `ominous low gong, tense alert, 1s`
- 오의 발동: `powerful impact whoosh, energetic, 0.8s`
- 승리: `bright victory chime, soft, 1.5s`
- 패배: `low descending tone, somber, 2s`

**스펙**: 44.1kHz mono WAV → Unity AudioClip, Compression Vorbis Quality 70.

### 4.6 BGM (0~5 트랙)

- **출시 직전 단계**. M10까지 BGM 없이 진행해도 됨 (TOTAL_DESIGN §12 C-8 미결).
- 1차: **Pixabay Music**에서 동양/명상/타악 키워드로 CC0 트랙 픽업.
- 분파별 1트랙 결정 시: Pixabay에 적합한 게 없으면 Suno **Pro 유료**로 전환.

---

## 5. Unity 임포트 표준

| 항목 | 값 |
|------|----|
| Sprite 폴더 | `Assets/_Project/Art/Skills/`, `Art/Enemies/`, `Art/Talents/`, `Art/UI/`, `Art/Endings/` |
| 오디오 폴더 | `Assets/_Project/Audio/SFX/`, `Audio/BGM/` |
| 폰트 폴더 | `Assets/_Project/Fonts/` |
| Sprite 디폴트 | Pixels Per Unit 100, Filter Bilinear, Mip Maps Off, Compression Normal |
| AudioClip 디폴트 (SFX) | Force Mono, Decompress on Load, Vorbis Quality 70 |
| AudioClip 디폴트 (BGM) | Streaming, Vorbis Quality 80 |
| 폰트 | TextMeshPro Asset 생성, 한국어 한글 글리프 셋 포함 |

---

## 6. 의사결정 트리 (새 에셋 필요할 때)

```text
새 에셋 필요
   │
   ├─ UI 표준 요소? ─ Kenney.nl 픽업 → 색만 변경 (90% 케이스)
   │
   ├─ 아이콘/포트레이트/결말 시안? ─ ChatGPT Images에서 먼저 생성
   │
   ├─ 같은 톤으로 후속 수정 필요? ─ 같은 대화에서 기준 이미지 유지하며 수정
   │
   ├─ 수십 장 반복 생성/자동 저장 필요? ─ GPT Image API 검토
   │
   ├─ GPT 수동 작업이 병목? ─ Leonardo.Ai / FLUX API 재검토
   │
   ├─ SFX? ─ Pixabay 픽업 → 없으면 ElevenLabs SFX Pro
   │
   └─ BGM? ─ Pixabay 픽업 → 출시 직전 부족하면 Suno Pro 검토
```

---

## 7. [검토 중]

| # | 항목 | 결정 시점 |
|---|------|----------|
| AP-1 | GPT Image API를 실제 파이프라인에 붙일지 여부 | M5 진입 전 — 수동 작업량이 병목이면 검토 |
| AP-2 | Leonardo.Ai / FLUX API 보조 도입 여부 | GPT 일관성 또는 작업 속도가 한계일 때 재고 |
| AP-3 | Suno Pro BGM 5트랙 외주 대체 도입 | 출시 6개월 전 |
| AP-4 | 결말 일러스트만 외주 (Krea·Recraft 유료 또는 인간 작가) | 출시 6개월 전 |
| AP-5 | 무공 아이콘 신규 생성 vs CC0 무협 아이콘 팩 픽업 비율 | M5 진입 직후 |

---

## 8. 라이선스 점검 체크리스트 (출시 빌드 전 매 자산 1회)

- [ ] 도구 약관에 상업 사용 명시
- [ ] API 사용 시 별도 과금/보관 정책 확인
- [ ] CC-BY 자산은 크레딧 표기 준비됨
- [ ] CC0 자산은 출처 기록 (의무 아님이지만 추적용)
- [ ] 폰트 라이선스 OFL/Apache 등 명시
- [ ] 도구 약관 변경분 분기당 1회 재확인 (특히 OpenAI, Suno, Leonardo)

---

## 9. 변경 이력

| 날짜 | 버전 | 변경 |
|------|------|------|
| 2026-05-04 | 0.1 | 초안 — 무료 우선 도구 스택, 카테고리별 워크플로 |
| 2026-05-11 | 0.2 | 상업 사용 불가/조건부 도구(§2.3 금지 목록, Suno/Udio 무료, Leonardo 무료, ElevenLabs 무료) 삭제 — 출시 빌드 후보만 남김 |
| 2026-05-11 | 0.3 | **기본 생성기를 GPT 이미지로 전환**. SD/Flux 중심 워크플로 삭제, ChatGPT Images 수동 생성 + GPT Image API 보조 구조로 재정리 |
