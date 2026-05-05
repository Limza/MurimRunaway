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
| 버전 | 0.1 (초안) |
| 마지막 수정 | 2026-05-04 |

---

## 1. 원칙

1. **무료 우선** — 솔로 개발 비용 디폴트는 0. 유료는 무료 대비 명확한 품질·시간 이득이 있을 때만 도입 (§7 의사결정 트리).
2. **상업 라이선스 검증 의무** — Steam 판매 빌드에 들어가는 모든 자산은 상업 사용이 명시된 도구·모델·소스에서만. 모호하면 사용 금지.
3. **AI 생성 우선, 자작·외주 없음** — 일러스트·아이콘·SFX·BGM 모두 AI. UI 표준 요소는 무료 CC0 키트 픽업 허용.
4. **무협 톤·색 코드 준수** — TOTAL_DESIGN §9.2 (무공 4분류), §9.3 (분파 5색)을 프롬프트와 후처리에 반영.
5. **Placeholder는 가능한 한 오래** — 코드 Phase가 막히지 않게. 실에셋 진입 시점은 [MILESTONES.md](MILESTONES.md) §3 참조.

---

## 2. 도구 스택

### 2.1 무료 + 상업 라이선스 명확 (출시 빌드용)

| 용도 | 도구 | 라이선스 |
|------|------|---------|
| 이미지 (전체) | **Stable Diffusion 로컬** (ComfyUI / Automatic1111) + SDXL/SD1.5 base | CreativeML Open RAIL-M — 상업 OK (커스텀 체크포인트는 개별 확인) |
| 이미지 (전체, 브라우저) | **Flux.1 Schnell** (HuggingFace Spaces 또는 fal.ai 무료 티어) | Apache 2.0 — 상업 OK |
| 이미지 후처리 | **GIMP** / **Krita** | GPL — 상업 OK |
| 효과음 (SFX) | **Pixabay** SFX (CC0) / **Freesound.org** (CC0/CC-BY 필터) | CC0 = 무제한 / CC-BY = 크레딧 표기 |
| BGM | **Pixabay Music** (CC0) | CC0 — 무제한 |
| UI 키트 | **Kenney.nl** | CC0 — 무제한 |
| 한국어 폰트 | **Pretendard**, **나눔체** 계열 | OFL — 상업 OK |

### 2.2 무료 사용 가능, 단 조건 있음

| 도구 | 조건 | 권장 용도 |
|------|------|----------|
| **Leonardo.ai** 무료 (일 150 토큰) | 상업 OK이지만 결과물이 공개 갤러리에 노출됨 (다른 사용자가 볼 수 있음). Leonardo가 결과물 재배포 라이선스 보유. | 빠른 시안·실험. 출시 빌드에 들어갈 결정본은 Pro 또는 §2.1 도구로 재생성 권장 |
| **ElevenLabs SFX** 무료 티어 | 월 생성 한도. 무료 티어는 attribution 요구. | 실험 후 Pro 결정 |
| **Suno** / **Udio** 무료 | **상업 사용 불가** — Pro/Premier 필요 | 무료 티어는 BGM 톤 탐색에만, 출시 빌드 사용 금지 |

### 2.3 명시적으로 사용 금지

| 도구·모델 | 이유 |
|----------|------|
| **Flux.1 Dev** 모델 | FLUX.1 [dev] Non-Commercial License — 상업 빌드 금지. Schnell 또는 Pro API만. |
| **Microsoft Designer / Bing Image Creator** (DALL-E 3) | MSA가 상업 사용을 명시 보장하지 않음. 책임이 사용자에게 전가됨 — 출시 빌드 사용 비권장. 프로토타이핑만. |
| **Suno / Udio 무료 티어** | 상업 사용 불가 (위) |
| 라이선스 불명 Civitai 커스텀 체크포인트 | 모델 카드의 "Commercial use" 항목이 No 또는 Image Sales Only인 경우 게임 빌드 사용 금지 |

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

### 4.1 아이콘 (무공·방어·오의·적 패턴 등 — 60+개)

**도구**: Flux.1 Schnell (HuggingFace Spaces 무료) 또는 SD 로컬 SDXL.

**프롬프트 템플릿**:
```
A martial arts skill icon, {무공명} - {효과 한 문장},
ink wash painting on parchment, single centered subject,
high contrast, {색} accent (#XXXXXX),
square 1:1, no text, no watermark, no border
```

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

**예상 반복**: 1 아이콘당 3~5 generation → 1 채택. 60+개 = ~250회 생성.

### 4.2 캐릭터 포트레이트 (적·재능·보스)

**도구**: SD 로컬 + 일관성을 위한 LoRA 또는 Flux Schnell + reference image.

**프롬프트 템플릿** (적):
```
Wuxia character portrait, {잡적|자객|무림고수|...},
ink wash painting style, traditional East Asian martial arts setting,
half body, neutral background,
{분파 색} ambient lighting, no text
```

**프롬프트 템플릿** (재능 — 천무지체/카피/대종사):
```
Wuxia protagonist portrait, {재능 한 문장 묘사},
ink wash painting, half body, dignified expression,
parchment background, no text
```

**스펙**:
- 생성: 768×1024 PNG (세로 비율)
- Unity 임포트: Sprite, 384×512로 다운샘플 (UI 표시 사이즈)

**일관성 확보**: 동일 generation 세션 + 동일 시드 변형으로 batch. 다른 세션에서 만들면 톤이 어긋남 — 모든 캐릭터를 한 시점에 묶어 생성.

### 4.3 결말 일러스트 (5장)

**도구**: SD 로컬 SDXL (또는 Flux Schnell). **반드시 한 세션에서 batch 생성** — 5장의 톤 통일이 핵심.

**프롬프트 템플릿** (분파별):
```
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

**검수**: 5장을 한 화면에 늘어놓고 톤 비교 — 색온도·붓터치·디테일 수준이 어긋난 장이 있으면 그 장만 재생성.

### 4.4 UI 요소 (버튼·패널·게이지·차징 표시)

**선택**: 직접 생성 대신 **CC0 UI 키트 픽업** + 색만 무협 팔레트로 변경. 시간 효율 압도적.

- **Kenney.nl UI Pack** (CC0): https://kenney.nl/assets/ui-pack
- 9-slice 처리: Unity Sprite Editor에서 Border 설정 후 UI Image의 Image Type = Sliced.

**색 변경**: GIMP/Krita에서 Hue 시프트 또는 Unity Material로 tint.

**필요 요소**:
- 직사각 패널 (방어 5택용)
- 원형 슬롯 (무공 슬롯용)
- 가로 게이지 (HP/내공)
- 스택 바 (기세 0~10)
- 차징 표시 (⚠ 머리 위) — Kenney 또는 §4.1 워크플로로 자체 생성

### 4.5 SFX (4종 + 회피 등 추가)

**1차 시도**: Pixabay SFX 무료 픽업 (CC0).
- "wood click", "gong alert", "whoosh impact", "victory chime", "defeat tone"

**2차 (Pixabay에 적합한 게 없을 때)**: ElevenLabs SFX 무료 티어로 텍스트→SFX 생성.

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
- 분파별 1트랙 결정 시: Pixabay에 적합한 게 없으면 Suno **Pro 유료** ($10/월)로 전환 — 무료 티어 상업 사용 불가.

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

```
새 에셋 필요
   │
   ├─ UI 표준 요소? ─ Kenney.nl 픽업 → 색만 변경 (90% 케이스)
   │
   ├─ 한정 일러스트(결말 5장)? ─ SD 로컬 SDXL batch (한 세션에 묶음)
   │
   ├─ 반복 아이콘(무공·방어·오의)? ─ Flux Schnell HF Space (브라우저)
   │                                  GPU 있으면 SD 로컬 더 빠름
   │
   ├─ 캐릭터 포트레이트? ─ SD 로컬 (LoRA로 일관성) > Flux Schnell
   │
   ├─ SFX? ─ Pixabay 픽업 → 없으면 ElevenLabs SFX
   │
   └─ BGM? ─ Pixabay 픽업 → 출시 직전 부족하면 Suno Pro 검토
```

---

## 7. [검토 중]

| # | 항목 | 결정 시점 |
|---|------|----------|
| AP-1 | SD 로컬 셋업 가능한 GPU 여부 | M5 진입 전 — 안 되면 Flux Schnell HF로 단일화 |
| AP-2 | Leonardo.ai Pro ($10~12/월) 도입 여부 | 무료로 일관성 확보가 한계일 때 재고 |
| AP-3 | Suno Pro ($10/월) BGM 5트랙 외주 대체로 도입 | 출시 6개월 전 |
| AP-4 | 결말 일러스트만 외주 (Krea·Recraft 유료 또는 인간 작가) | 출시 6개월 전 |
| AP-5 | 무공 아이콘 신규 생성 vs CC0 무협 아이콘 팩 픽업 비율 | M5 진입 직후 |

---

## 8. 라이선스 점검 체크리스트 (출시 빌드 전 매 자산 1회)

- [ ] 도구 약관에 상업 사용 명시
- [ ] 모델/체크포인트 라이선스 명시 (특히 SD 커스텀)
- [ ] CC-BY 자산은 크레딧 표기 준비됨
- [ ] CC0 자산은 출처 기록 (의무 아님이지만 추적용)
- [ ] 폰트 라이선스 OFL/Apache 등 명시
- [ ] 도구 약관 변경분 분기당 1회 재확인 (특히 Microsoft, Suno, Leonardo)

---

## 9. 변경 이력

| 날짜 | 버전 | 변경 |
|------|------|------|
| 2026-05-04 | 0.1 | 초안 — 무료 우선 도구 스택, 카테고리별 워크플로, MS Designer/Flux Dev/Suno 무료를 출시 빌드 금지로 명시 |
