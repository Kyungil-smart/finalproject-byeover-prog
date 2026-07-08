# SFX 훅 연결 가이드 (담당별 전달용)

사운드 시스템이 들어갔습니다. 각자 도메인의 이벤트 지점에 **한 줄**만 추가하면 됩니다.

## 사용법 (전원 공통)

```csharp
AudioManager.Play(SfxId.ButtonClick);   // 효과음
AudioManager.Bgm(SfxId.HousingBgm);     // BGM 교체 (같은 곡이면 무시)
```

- 배선/씬 배치 불필요 — AudioManager가 없으면 자동 생성됩니다.
- **파일명 문자열 사용 금지.** 클립·볼륨·중복재생 정책은 `Resources/SoundLibrary.asset` 인스펙터에서 관리합니다(사운드 밸런스 튜닝도 여기서, 코드 수정 없음).
- 가이드의 "중복재생 X" 항목은 라이브러리에 이미 반영돼 있어 연타해도 겹치지 않습니다.

## 이미 연결된 것 (참고)

로비/챕터 BGM, 정산 클리어·게임오버, 자동공격 사출, sort 성공, 유닛 선택/드롭(성공 시 드롭음 제외 규칙 포함), 조커, 인챈트 선택/리롤/교체, 원소별 피격음 5종, 강공 5종(메테오/파도/허리케인/뇌격/얼음결정).

## 남은 훅 — 아웃게임 (아티팩트/상점/UI 담당)

| SfxId | 시점 | 추천 위치 |
|---|---|---|
| `CharacterLevelUp` | 캐릭터 레벨 업 성공 시 | 레벨업 비용 차감 성공 직후 (LobbyCharacterLevelUI 계열) |
| `ArtifactGachaDraw` | 뽑기 진행 시 | ShopGachaPresenter의 뽑기 실행 지점 |
| `ArtifactGachaTen` | 10회 뽑기 시 추가 재생 | 위와 같은 곳, count==10 분기 |
| `ArtifactAscension` | 돌파 성공 시 | ArtifactManager.AttemptAscension의 AscensionCount++ 직후 |
| `ArtifactDisassemble` | 분해 시 | ArtifactManager.ManualDisassemble의 GiveReward 직후 |
| `ArtifactCraft` | 제조 시 | ArtifactCraftService 제조 확정 지점 |
| `ButtonClick` | 공용 버튼 클릭 | 버튼마다 달지 말고 공용 버튼 컴포넌트/ScreenNavigator 한 곳에서 |
| `PopupOpen` | 모든 팝업 등장 시 | ScreenNavigator.OpenMenu 한 곳 |
| `EnterInGame` | 스타트 버튼 클릭 시 | 로비 PLAY 버튼 핸들러 (GameManager.LoadInGame에 달면 Retry/다음챕터에서도 울리니 버튼 쪽에) |

## 남은 훅 — 하우징 (조규민)

| SfxId | 시점 | 비고 |
|---|---|---|
| `HousingBgm` | 하우징 화면 입장 시 | 이탈 시 `AudioManager.Bgm(SfxId.LobbyBgm)`으로 복귀 |
| `HousingText` | 에이프릴 대사 출력 시 | |
| `HousingFootstep` | 에이프릴 이동 시 | 라이브러리에 재생 간격 0.2초 설정돼 있어 연타 안전 |
| `HousingBookshelf` | 책장 터치 시 | |
| `HousingBed` | 침대 터치 시 | **가이드에 파일 미지정** — 기획 확정 후 SoundLibrary.asset에 클립만 꽂으면 됨(ID 예약됨) |
| `HousingCoffee` | 커피머신 터치 시 | |
| `HousingCurrencyMaker` | 재화 생산기 터치 시 | |

## 기획 확인 필요

- 침대(HousingBed) 사운드 파일 지정
- `OutgameCast`(Neutral_Medium_Cast_06) — 가이드 아웃게임 10.0 항목의 용도가 비어 있음
