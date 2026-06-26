# 튜토리얼 UI 인수인계 — 홍정옥님

작성: 정승우(아키텍처/매니저/시나리오 재생기) → 인수: 홍정옥(UI 연출/오버레이)
기준일: 2026-06-26 / 원본 스펙: 튜토리얼 기획서 v.1.04 · `Tutorial_Architecture.md`

---

## 0. 목적 + "네가 할 일" 한눈에 (우선순위순)

이 문서는 튜토리얼에서 **UI가 끼는 두 지점**을 네 손에 넘긴다. 하나는 이미 거의 다 만든 `ScenarioView`(시나리오 연출), 다른 하나는 골격만 있는 `TutorialView`(인게임 가이드 오버레이)다.

체크리스트:

- [ ] **(1순위) `TutorialView.ShowStep()` / `Hide()` 의 `[홍정옥 작성]` 구간 채우기** — 어두운 막 + 강조(마스크 구멍) + 말풍선 배치. (`TutorialView.cs:44`, `:56`)
- [ ] **(2순위) `highlightTargetId` → 실제 `RectTransform` 매핑** 을 씬별로 구성 (`TutorialStep.highlightTargetId` 값: `StageStartButton` / `SortTable` 등)
- [ ] **(3순위) 강조 영역 탭을 `TutorialView.OnHighlightTapped()` 에 배선** (버튼 OnClick 또는 탭 처리). 단, 풀스크린 딤이 입력을 먹는 Raycast 충돌부터 풀어야 한다(섹션 3-7).
- [ ] **(4순위) 각 씬(`_Lobby`/`_InGame`)에 `TutorialView` 배치 + `_overlayRoot`/`_guideText` 인스펙터 연결**
- [ ] **(5순위) 스펙 UI 요구(비대상 테이블 딤 / 드래그 화살표 / 드래그 완료 대기) 구현 위치 결정**
- [ ] **(협의) 더미 드라이버(`ScenarioDummyDriver`) → 정식 드라이버(`ScenarioDataDriver`) 교체** — API가 다르므로 단순 컴포넌트 교체가 아님(섹션 2-5). 정승우와 협의.

> `ScenarioView`는 네 기존 소유물이라 새로 만들 건 없다. 정승우가 만든 정식 드라이버가 거기에 데이터를 먹이는 구조만 확인하면 된다(섹션 2).

> **시작 트리거와 진행 훅이 아직 없다(중요).** `TryStart()`를 부르는 주체도, GameAction 단계(정렬 완성)를 `AdvanceStep()`으로 넘겨줄 게임 이벤트 훅도 현재 코드에 없다(둘 다 미착수). 그래서 지금 상태에서는 튜토리얼이 자동으로 뜨지도, stepId 1에서 다음으로 넘어가지도 않는다. 네 오버레이를 단독 검증하려면 임시 디버그 수단이 필요하다(섹션 5의 "최소 재현 시나리오" 참고).

---

## 1. 큰 그림: 튜토리얼 흐름과 UI가 끼는 두 지점

`Tutorial_Architecture.md` 기준 확정 흐름. **시작점은 로비가 아니라 "최초 실행 → 인트로 시나리오 → 바로 인게임"** 이다.

```
1. 최초 실행(세이브 없음)
2. 인트로 시나리오 재생            <-- [A] 시나리오 UI (ScenarioView)
        |  ScenarioDataDriver.Play(3001)
        v  OnFinished
3. 로비 안 거치고 바로 인게임 (몬스터X, 보드 잠금)
4. "책 3권 모으면 발동" 교육        <-- [B] 인게임 가이드 오버레이 (TutorialView)
        |  보드 일부 활성 + 비대상 딤 + 화살표 + 드래그 대기
        v  정렬 완성
5. 공격 발동 -> 몬스터 스폰 -> 경험치 -> 레벨업/인챈트 안내
6. 0챕터 강제 패배 -> 로비 복귀
7. 로비 UI 단계적 해금              <-- (후반, 미착수)
```

- **[A] 시나리오 UI** = 대사/초상화/배경 연출. 파일 `ScenarioView.cs` (네 소유, 거의 완성).
- **[B] 인게임 가이드 오버레이** = 어두운 막 + 특정 UI 강조 + 말풍선. 파일 `TutorialView.cs` (골격만, 네가 채울 stub).

---

## 2. [A] 시나리오 UI — `ScenarioView` (네 소유)

### 2-1. 정승우가 데이터로 먹이는 구조

정승우가 만든 **`ScenarioDataDriver`(데이터 기반 정식 재생기, 더미 교체본)** 가 동작 방식이다.

```
ScenarioDataDriver.Play(groupId)
  -> DataManager.Instance.StoryRepo.GetTalkGroup(groupId)   // 대사 묶음 조회
  -> 한 줄씩 ScenarioView.ShowLine(...)                       // 네 View가 그림
  -> 마지막 줄/스킵에서 OnFinished 발행
```

즉 **드라이버가 `ShowLine(...)` 을 호출**하고, 네가 만질 건 **View 안쪽의 보임새/연출/인스펙터 배선**뿐이다. 드라이버는 **내부 `Subscribe()`** 에서 `_view.OnAdvanceRequested += Next` / `_view.OnSkipRequested += Finish` 로 네 View의 이벤트를 구독한다(`ScenarioDataDriver.cs:199~200`). `Next`/`Finish`는 둘 다 드라이버 private 메서드라 네가 직접 부를 일은 없다.

> 초기화 의존성: `Play(groupId)`는 `DataManager.Instance.StoryRepo`가 준비돼 있어야 동작한다. `DataManager`가 없거나 `StoryRepo`가 비어 있으면 바로 `FinishImmediate()`로 빠진다(`ScenarioDataDriver.cs:84~90`, `:132`). 즉 인트로 재생 시점에 어느 부팅 단계에서 `DataManager`가 초기화돼 있어야 하는지가 A 검증의 전제다. 현재 문서엔 그 보장 지점이 명시돼 있지 않으니, 정식 드라이버를 실제 씬에서 돌릴 때 가장 먼저 확인할 것.

### 2-2. `ScenarioView` 인스펙터 배선 표 (실제 `[SerializeField]` 전부)

| 필드명 | 타입 | Header | Tooltip / 꽂는 것 |
|---|---|---|---|
| `_boxFrame` | `GameObject` | 텍스트 박스 | "박스 프레임/배경만 — TextBox=0이면 숨김(텍스트는 계속 표시)" |
| `_textboxRoot` | `GameObject` | 텍스트 박스 | "이름+대사 묶음(텍스트 페이드용, 항상 표시)" — CanvasGroup 자동 부착됨 |
| `_nameText` | `TMP_Text` | 텍스트 박스 | 화자 이름 TMP |
| `_dialogueText` | `TMP_Text` | 텍스트 박스 | 대사 본문 TMP |
| `_portraitLeft` | `Image` | 초상화 (좌/중/우) | 왼쪽 화자 초상화 |
| `_portraitCenter` | `Image` | 초상화 (좌/중/우) | 중앙 화자 초상화 |
| `_portraitRight` | `Image` | 초상화 (좌/중/우) | 오른쪽 화자 초상화 |
| `_bgImage` | `Image` | 배경 / 컷씬 | BG |
| `_cgImage` | `Image` | 배경 / 컷씬 | CG (전체 화면) |
| `_grayscaleShader` | `Shader` | 라이팅 (회색 실루엣) | "UI/Grayscale 셰이더 — 말 안 하는 초상화를 흑백 처리" |
| `_litColor` | `Color` | 라이팅 (회색 실루엣) | 화자 색 (기본 `Color.white`) |
| `_dimColor` | `Color` | 라이팅 (회색 실루엣) | "회색 초상화 명도(어둡게)" 기본 `(0.6, 0.6, 0.6, 1)` |
| `_skipButton` | `Button` | 스킵 버튼 (선택) | 스킵 버튼 (`OnSkipRequested` 발행) |

> `_grayscaleShader` 주의: 이 슬롯이 비어 있으면 `Awake`에서 `_grayMaterial`이 만들어지지 않아(`ScenarioView.cs:115~116`), 비화자 초상화의 **흑백 처리가 안 되고 `_dimColor`로 어둡게만** 된다(`ApplyPortrait`의 `if (_grayMaterial != null)` 가드, `:360`). 회색 실루엣 연출을 쓰려면 실제 `UI/Grayscale` 셰이더 에셋을 꽂아야 한다. (해당 셰이더 에셋의 프로젝트 내 경로/존재 여부는 이 문서로 확인 못 했으니 직접 확인 필요.)

### 2-3. 연출 옵션 필드 + 기본값

| 필드명 | 타입 | Header | 기본값 | Tooltip |
|---|---|---|---|---|
| `_useTypewriter` | `bool` | 텍스트 연출 | `true` | 타자기 효과 사용 |
| `_charsPerSecond` | `float` | 텍스트 연출 | `30f` | "초당 출력 글자 수" |
| `_useTextFade` | `bool` | 텍스트 연출 | `false` | "타자기 대신 텍스트 박스를 페이드로 표시" |
| `_textFadeDuration` | `float` | 텍스트 연출 | `0.2f` | 텍스트 페이드 시간 |
| `_autoPlay` | `bool` | 자동 진행 | `false` | 오토플레이 |
| `_autoDelay` | `float` | 자동 진행 | `1.5f` | "텍스트 출력 완료 후 다음으로 넘어가기까지 대기(초)" |
| `_usePortraitSlide` | `bool` | 초상화 슬라이드 인 | `true` | 초상화 등장 슬라이드 |
| `_slideOffset` | `float` | 초상화 슬라이드 인 | `60f` | "등장 시 이동 거리(px)" (좌=왼쪽에서/우=오른쪽에서/중앙=아래에서) |
| `_slideDuration` | `float` | 초상화 슬라이드 인 | `0.25f` | 슬라이드 시간 |
| `_useSceneFade` | `bool` | 배경/컷씬 페이드 | `true` | BG/CG 전환 페이드 |
| `_sceneFadeDuration` | `float` | 배경/컷씬 페이드 | `0.3f` | 씬 페이드 시간 |

### 2-4. `ShowLine` 파라미터 의미

```csharp
public void ShowLine(
    string name, string text, bool showTextbox,
    Sprite portraitLeft, Sprite portraitCenter, Sprite portraitRight,
    ScenarioSpeakerSlot speaker,
    Sprite background, Sprite cutscene)
```

| 파라미터 | 의미 |
|---|---|
| `name` | 화자 이름 (`_nameText`) |
| `text` | 대사 본문 (타자기/페이드로 출력) |
| `showTextbox` | `_boxFrame` 토글. **텍스트(이름/대사)는 항상 표시**, 박스 프레임만 켜고 끔 (`SetTextbox` 참고) |
| `portraitLeft/Center/Right` | 좌/중/우 초상화 스프라이트. `null`이면 해당 슬롯 숨김 |
| `speaker` | 누가 말하는가 → 그 슬롯만 컬러(`_litColor`), 나머지는 그레이스케일(`_dimColor`) + 화자를 맨 앞으로(`SetAsLastSibling`) |
| `background` | BG 스프라이트 (`_bgImage`) |
| `cutscene` | CG 스프라이트 (`_cgImage`, 전체 화면) |

`ScenarioSpeakerSlot` enum (`ScenarioView.cs:18`):

```csharp
public enum ScenarioSpeakerSlot
{
    None   = 0,
    Left   = 1,   // 왼쪽 화자
    Center = 2,   // 중앙 화자
    Right  = 3,   // 오른쪽 화자
}
```

> 정식 드라이버의 `speaker` 변환 규칙: `ScenarioDataDriver`는 데이터의 `line.speaker`(int)를 `Mathf.Clamp(line.speaker, 0, 3)`로 강제 변환해 enum으로 캐스팅한다(`ScenarioDataDriver.cs:157`). 데이터에 **4 이상 값이 들어오면 전부 `Right`(3)로 뭉개진다**. 현재 데이터는 전부 0이라 당장 문제는 없으나, 초상화 라이팅 검증 시 인지할 것.

진행 관련 공개 멤버(드라이버가 쓰는 계약): 이벤트 `OnAdvanceRequested` / `OnSkipRequested` / `OnLineLogged`, 메서드 `CompleteText()` / `SetAutoPlay(bool)` / `ToggleAutoPlay()` / `PauseAuto()` / `ResumeAuto()`. 터치 진행은 `OnPointerClick`(타이핑 중이면 `CompleteText`, 아니면 `OnAdvanceRequested` 발행)으로 이미 처리돼 있다.

### 2-5. 더미 → 정식 드라이버 교체 (단순 컴포넌트 교체 아님 — 주의)

- **더미**: `ScenarioDummyDriver`(네 작성, 하드코딩). `FillDefault()` 에 3001/3002 대사가 박혀 있고, 조규민이 다시보기 분기(`FillReplayStory`)를 얹어둠.
- **정식**: `ScenarioDataDriver`(정승우 작성, 데이터 기반).

**둘은 같은 API가 아니다. `OnFinished` 이벤트 이름만 같을 뿐, 진입점·자동재생 기본값·구독 방식이 다르다.** `ScenarioDataDriver`는 `ScenarioDummyDriver`를 상속하지 않는 별개 클래스라 인스펙터 드래그/타입 캐스팅으로 갈아끼울 수 없다. 차이:

| 항목 | 더미 `ScenarioDummyDriver` | 정식 `ScenarioDataDriver` |
|---|---|---|
| 재생 진입점 | `Begin()` (인자 없음, 내부 `_index=0` 초기화) | `Play(int groupId)` (GroupID 인자) |
| 자동재생 기본값 | `_playOnStart = true` (Start에서 `Begin()`) | `_playOnStart = false` (켜면 Start에서 `Play(_startGroupId)`, 기본 `_startGroupId=3001`) |
| 데이터 출처 | `FillDefault`/`FillReplayStory` 하드코딩(Awake/Begin) | `StoryRepo.GetTalkGroup(groupId)` |
| 구독 방식 | `OnEnable`에서 직접 구독 | `Subscribe()` 가드(중복 방지 `_subscribed`) |
| `OnFinished` 타입 | `event System.Action`(:50) | `event Action`(:47, `using System`) — 동작 동일 |

교체 시 해야 할 일:
1. 호출부에서 `Begin()` → `Play(3001)` 로 진입점을 바꾸거나, 정식 드라이버에서 `_playOnStart=true` + `_startGroupId=3001`로 켜서 Start 자동재생에 맡긴다.
2. **`TempStoryToGameFlow` 코드 수정 필수.** `_driver` 필드 타입이 `[SerializeField] private ScenarioDummyDriver _driver`(`TempStoryToGameFlow.cs:12`)로 강타입이고, `Awake`에서 `FindFirstObjectByType<ScenarioDummyDriver>()`(`:19`)로 찾는다. 정식으로 가려면 이 **필드 타입과 `FindFirstObjectByType<>` 제네릭 인자**까지 `ScenarioDataDriver`로 바꿔야 한다(인스펙터 참조도 끊김). 장기적으로는 `OnFinished`를 가진 공통 인터페이스로 빼는 것이 안전.

> 더미는 아직 삭제 안 함. `TempStoryToGameFlow`가 현재 **`ScenarioDummyDriver.OnFinished`** 를 구독해 씬 전환한다(`TempStoryToGameFlow.cs:12,25`). **교체 시점·방식은 정승우와 협의.**

### 2-6. `ScenarioDataDriver` 인스펙터 필드 (정식 드라이버를 씬에 배치할 때)

| 필드명 | 타입 | 기본값 | 의미 |
|---|---|---|---|
| `_view` | `ScenarioView` | — | 먹일 View. 비우면 `Awake`에서 `GetComponent`→`FindFirstObjectByType`로 자동 탐색 |
| `_startGroupId` | `int` | `3001` | `_playOnStart`일 때 Start에서 재생할 GroupID(인트로) |
| `_playOnStart` | `bool` | `false` | Start에서 `Play(_startGroupId)` 자동 호출 여부 |
| `_useEnglish` | `bool` | `false` | 켜면 `name_EN`/`Text_EN`, 끄면 한국어 |
| `_portraitPath` | `string` | `"Story/Portraits/"` | 초상화 Resources 경로 접두 |
| `_backgroundPath` | `string` | `"Story/Backgrounds/"` | 배경 Resources 경로 접두 |
| `_cutscenePath` | `string` | `"Story/Cutscenes/"` | 컷씬 Resources 경로 접두 |

---

## 3. [B] 튜토리얼 오버레이 — `TutorialView` (네가 채울 stub)

### 3-1. `ITutorialView` 계약 (매니저 ↔ 뷰 약속)

`ITutorialView.cs` 전체 시그니처(절대 바꾸지 말 것):

```csharp
public interface ITutorialView
{
    // 매니저 -> 뷰 : 이 단계를 그려라(어두운 막 + step.highlightTargetId 강조 + step.guideText 말풍선)
    void ShowStep(TutorialStep step);

    // 매니저 -> 뷰 : 오버레이 숨겨라(이 씬엔 보여줄 단계 없음 / 튜토리얼 종료)
    void Hide();

    // 뷰 -> 매니저 : 유저가 이 단계 동작 완료(주로 강조 영역 탭). 매니저가 다음 단계로.
    event Action OnStepActionCompleted;
}
```

매니저는 씬 로드 시 `RegisterView(this)` 로 받은 뷰의 `OnStepActionCompleted` 를 `AdvanceStep`에 연결한다(`TutorialManager.cs:66`). 즉 **네가 `OnStepActionCompleted` 를 쏘면 다음 단계로 넘어간다.**

**등록 → 자동 그리기 흐름(왜 등록만 했는데 ShowStep이 불리나):** `RegisterView`는 마지막에 `RefreshView()`를 부른다(`TutorialManager.cs:67`). `RefreshView()`(private, `:106`)가 현재 단계가 있고 그 단계의 `scene`이 활성 씬과 맞으면 `ShowStep(step)`을, 아니면 `Hide()`를 호출한다(`:106~115`). 즉 네 View는 **씬 로드 시 자기 등록만 하면, 현재 단계에 맞춰 매니저가 알아서 `ShowStep`/`Hide`를 골라 호출**한다. `RefreshView`/`IsStepForActiveScene`은 private이라 네가 직접 부를 수 없다.

### 3-2. `TutorialView.cs` 의 `[홍정옥 작성]` 구간이 해야 할 일

**`ShowStep(TutorialStep step)` — `TutorialView.cs:38`**
이미 돼 있는 것: `_currentStep = step` 저장, `_overlayRoot.SetActive(true)`, `_guideText.text` 설정. 텍스트 대입은 **null 가드가 있어**(`:42`: `step != null ? step.guideText : string.Empty`) null 단계가 들어와도 안전하다.
네가 채울 곳 (`:44` `[홍정옥 작성]`):
- `step.highlightTargetId` 로 강조할 UI를 찾아 **그 영역만 밝게(마스크 구멍)** 만들기
- `step.guideText` 말풍선을 그 대상 **근처에 배치**
- `step.advanceMode == TapHighlight` 면 그 영역 **탭 받기** 활성화

**`Hide()` — `TutorialView.cs:51`**
이미 돼 있는 것: `_currentStep = null`, `_overlayRoot.SetActive(false)`.
네가 채울 곳 (`:56` `[홍정옥 작성]`):
- 강조/말풍선 정리(마스크 구멍 복구, 화살표/딤 해제 등)

> `Hide()`는 "이 씬엔 보여줄 단계 없음"일 때뿐 아니라 **튜토리얼 완전 종료 시에도** 호출된다. `AdvanceStep`이 마지막 단계를 넘기면 `Complete()`(`TutorialManager.cs:93`)가 실행되고, 그 안에서 `_view.Hide()`를 부른다(`:98`). 즉 네 `Hide()` 정리 코드가 종료 경로에서도 돌아야 하므로, 남은 화살표/딤/탭 핸들을 빠짐없이 정리할 것.

### 3-3. 뷰 → 매니저 진행: `OnHighlightTapped()`

`TutorialView.cs:64`:

```csharp
public void OnHighlightTapped()
{
    // TapHighlight 단계만 탭으로 진행. GameAction 단계는 게임 이벤트 훅이 TutorialManager.AdvanceStep 호출.
    if (_currentStep != null && _currentStep.advanceMode == TutorialAdvanceMode.TapHighlight)
        OnStepActionCompleted?.Invoke();
}
```

- 이 메서드를 **강조 대상 버튼의 OnClick** 또는 네가 만든 **투명 탭 영역(IPointerClickHandler 등)** 에 연결하면 된다.
- `TapHighlight` vs `GameAction` 차이:
  - **TapHighlight**: 강조 영역을 **탭**하면 진행. → `OnHighlightTapped()` 경로. (예: stepId 0 `StageStartButton`, stepId 2)
  - **GameAction**: 정렬/인챈트 선택 등 **실제 게임 동작**이 일어나면 진행. → 게임 이벤트 훅이 `TutorialManager.AdvanceStep()` 을 직접 호출(정승우/게임 로직 담당). 네 탭 처리는 무시됨. (예: stepId 1 `SortTable` 정렬 완성)

> **(중요) GameAction 진행 훅은 아직 없다.** "정렬 완성 → `AdvanceStep`" 을 부르는 게임 이벤트 훅은 현재 코드에 존재하지 않는다(아키텍처 3절 "인게임 튜토 모드 … 미착수"). 그래서 지금 상태에서 stepId 1(`GameAction`)은 **진행시킬 주체가 없어 그 단계에서 영구 정지**한다. 네가 [B] 오버레이를 만들어 화면에 띄워도 다음으로 못 넘어가므로, 단독 검증을 위해선 임시로 `AdvanceStep()`을 부르는 디버그 수단이 필요하다(섹션 5).

### 3-4. `TutorialStep` 데이터 필드 (각 단계에서 네가 읽는 값)

`TutorialStepData.cs:25`:

| 필드 | 타입 | 의미 (네가 쓰는 법) |
|---|---|---|
| `stepId` | `int` | 식별/디버그용 순번(0부터) |
| `scene` | `TutorialScene` (`Lobby`/`InGame`) | 이 단계가 일어나는 씬. **매니저가 활성 씬과 비교해 노출을 게이트한다** |
| `advanceMode` | `TutorialAdvanceMode` (`TapHighlight`/`GameAction`) | **위 3-3 분기 기준** |
| `highlightTargetId` | `string` | **강조 대상 식별자.** 네가 이 id로 `RectTransform`을 찾아 강조 |
| `guideText` | `string` (TextArea) | 말풍선 문구. `ShowStep`에서 이미 `_guideText`에 들어감 |
| `note` | `string` (TextArea) | 기획/개발 메모. 동작 안 함, 참고용 (`TutorialStepData.cs:44` Tooltip) |

enum 값 (`TutorialStepData.cs`): `TutorialScene { Lobby=0, InGame=1 }`, `TutorialAdvanceMode { TapHighlight=0, GameAction=1 }`.

현재 `TutorialStepData.asset` 실제 데이터 3단계:

| stepId | scene | advanceMode | highlightTargetId | guideText |
|---|---|---|---|---|
| 0 | Lobby(0) | TapHighlight(0) | `StageStartButton` | "스테이지를 눌러 전투를 시작해보세요" |
| 1 | InGame(1) | GameAction(1) | `SortTable` | "같은 색 유닛 3개를 정렬해보세요" |
| 2 | InGame(1) | TapHighlight(0) | (비어있음) | "정렬하면 공격이 나갑니다!" |

> **(함정) `scene` 필드가 노출 게이트다.** `TutorialManager.IsStepForActiveScene`(`:118~127`)는 `step.scene`을 활성 씬 이름과 비교해 `ShowStep` 여부를 정한다(`Lobby`→`"_Lobby"`, `InGame`→`"_InGame"`). 현재 asset의 **stepId 0은 `scene=Lobby`** 라, `_InGame` 씬에만 `TutorialView`를 두고 테스트하면 stepId 0은 **영원히 안 뜬다**. 확정 흐름(섹션 1)은 "로비 안 거치고 인게임"이지만 데이터는 아직 Lobby로 돼 있어 어긋난다(데이터/흐름 정합은 정승우가 시퀀스 디렉터로 진화시키며 정리 예정). 그 전까지 테스트하려면 **어느 씬에 View를 둘지 + asset의 `scene` 값**을 함께 고려해야 한다. 강조 대상 매핑 자체는 `highlightTargetId` 문자열 기준으로 잡되, "왜 안 뜨나"의 1순위 원인이 `scene` 게이트임을 기억할 것.

### 3-5. `highlightTargetId` → `RectTransform` 매핑 권장 방법

매니저/데이터는 **문자열 id**만 안다. 실제 `RectTransform`은 씬마다 다르므로, **씬별 매핑은 `TutorialView` 쪽에서** 갖는 게 깔끔하다(인터페이스 변경 불필요).

권장: `TutorialView`에 인스펙터 매핑 리스트를 추가.

```csharp
[System.Serializable]
public class HighlightTarget
{
    public string id;            // TutorialStep.highlightTargetId 와 일치 (예: StageStartButton)
    public RectTransform target; // 이 씬의 실제 대상
}

[Header("강조 대상 매핑 (홍정옥)")]
[SerializeField] private List<HighlightTarget> _highlightTargets = new();

private RectTransform ResolveTarget(string id)
{
    foreach (var t in _highlightTargets)
        if (t.id == id) return t.target;
    return null;   // 못 찾으면 강조 없이 말풍선만(graceful)
}
```

각 씬의 `TutorialView` 프리팹/인스턴스에 그 씬의 `StageStartButton`/`SortTable` 등을 드래그해 채운다. `_Lobby`엔 로비 대상만, `_InGame`엔 인게임 대상만 넣으면 됨.

> 미해결: `StageStartButton`(로비)·`SortTable`(인게임)이 실제 씬 계층 어디의 어떤 오브젝트인지(이름/하이어라키 경로)는 이 문서로 확정 못 했다. `_highlightTargets`에 무엇을 드래그할지 씬을 열어 직접 찾아야 한다. 정승우/인게임 담당에게 대상 오브젝트 경로를 받아두면 빠르다.

### 3-6. 이미 배선된 SerializeField + 추가로 필요한 것

이미 있음(`TutorialView.cs:13~17`):

| 필드 | 타입 | Tooltip |
|---|---|---|
| `_overlayRoot` | `GameObject` | "화면을 어둡게 덮는 마스크 루트. 단계 표시 중에만 켜짐." |
| `_guideText` | `TMPro.TextMeshProUGUI` | "말풍선 텍스트(TMP 등). 홍정옥이 연결." |

추가로 네가 만들/연결할 것(권장):
- 강조 매핑 리스트 `_highlightTargets` (위 3-5)
- 마스크 구멍 연출 수단(딤 패널/셰이더 구멍/Mask+Sprite — 기법 결정은 3-7)
- 말풍선 RectTransform(대상 근처로 옮길 핸들)
- 드래그 안내용 **화살표** 오브젝트(섹션 4)
- (TapHighlight용) 강조 영역 탭을 받는 투명 버튼/핸들 → `OnHighlightTapped()` 연결

### 3-7. (가장 큰 "어떻게" 공백) 마스크 구멍 / 말풍선 배치 / 탭 통과 — 결정해야 할 기법

이 부분은 스펙이 "강조 영역만 밝게 + 나머지 딤"이라고만 말하고 **구현 기법은 비어 있다.** 네가 결정해서 채워야 하며, 아래는 결정해야 할 항목과 함정이다.

- **마스크 구멍 만드는 법(택1)**: ① 강조 대상 사각형을 피해 딤 패널을 4분할로 까는 방식(상/하/좌/우 패널), ② 딤 머티리얼/셰이더로 구멍 뚫기, ③ `Mask`+Sprite로 구멍 모양 마스킹. 단, **이 프로젝트는 URP 2D 환경**이라 일부 렌더 컴포넌트(예: `LineRenderer`)가 정상 동작 안 하는 전례가 있으니, 셰이더/머티리얼 경로를 택할 땐 URP 2D에서 실제로 렌더되는지 먼저 검증할 것. 가장 안전한 1순위는 ①(4분할 딤 패널).
- **좌표 변환**: 강조 대상 `RectTransform`의 화면 위치를 딤/말풍선 좌표로 옮겨야 한다. `Canvas`의 RenderMode가 **Overlay냐 Camera냐**에 따라 변환식이 다르다(`RectTransformUtility.WorldToScreenPoint` / `ScreenPointToLocalPointInRectangle` 등). View 프리팹의 Canvas 설정을 먼저 확정하고 그에 맞춰 변환할 것.
- **말풍선 배치**: 대상 `RectTransform` → 말풍선 `RectTransform` 좌표 변환 + **화면 밖으로 안 나가게 클램핑** + 대상 기준 위/아래 방향 결정(말풍선 꼬리 방향 포함) 규칙이 필요하다. 말풍선 프리팹 구조(꼬리 등)도 네가 설계.
- **(핵심 충돌) 딤이 입력을 다 먹는 문제**: 풀스크린 딤 패널이 Raycast를 막으면 강조한 버튼(`StageStartButton` 등)이 안 눌린다. **구멍 영역만 클릭이 통과**하도록 처리해야 한다 — 4분할 딤이면 가운데(대상 위)에는 딤을 깔지 않거나, 대상 위에 올린 투명 버튼만 Raycast를 받게 하고 그 버튼 OnClick을 `OnHighlightTapped()`(TapHighlight 단계) 또는 실제 게임 버튼으로 라우팅한다. 이 충돌을 풀지 않으면 TapHighlight 단계 진행 자체가 막힌다.
- **오버레이 최상단 보장**: `TutorialView`의 Canvas 계층/`sorting order`를 인게임 HUD보다 위로 둬야 하고, 씬에 `EventSystem`이 있어야 탭이 동작한다. View 프리팹 구성 시 함께 챙길 것.

---

## 4. 스펙 UI 요구 매핑 (어느 메서드/단계에 구현하나)

스펙 흐름(`Tutorial_Architecture.md` 2절): "4. 책 3권 교육 → 보드 일부만 활성 + 딤 + 화살표 + 드래그 대기".

| 스펙 UI 요구 | 구현 위치 | 비고 |
|---|---|---|
| **비대상 테이블 어둡게(딤)** | `TutorialView.ShowStep()` 의 `[홍정옥 작성]` 구간 — `highlightTargetId`로 찾은 대상만 밝게, 나머지는 `_overlayRoot` 딤으로 덮기 | `Hide()` 에서 복구 |
| **드래그 화살표** | 같은 `ShowStep()` — 대상 근처에 화살표 오브젝트 표시(트윈 가능). 정렬 단계(`SortTable`, stepId 1)에서 노출 | `Hide()` 에서 끄기. **단, 방향/경로 데이터 출처가 없음(아래)** |
| **드래그 완료까지 대기** | UI는 **대기만** 한다. 진행은 stepId 1이 `advanceMode=GameAction` 이므로 **게임의 정렬 완성 이벤트 훅이 `TutorialManager.AdvanceStep()` 호출** | **그 훅이 아직 미구현이라 현재는 stepId 1에서 멈춤(3-3 참고)** |

요약: **딤/화살표/말풍선 = 네 `ShowStep`/`Hide`**, **드래그 완료 판정/진행 = 게임 이벤트 → 매니저**(아직 미구현). 네가 할 일은 "보여주고 대기"까지.

> **(공백) 드래그 화살표의 방향·경로 데이터가 없다.** 화살표가 "어디서 어디로"(드래그 시작 타일 → 목표 위치) 향할지를 정할 입력이 `TutorialStep`에 없다. `highlightTargetId="SortTable"` 하나로는 방향/경로를 알 수 없다. 화살표 좌표/방향/트윈을 무슨 입력으로 계산할지(데이터 필드 추가가 필요한지, 인게임 보드에서 런타임으로 가져올지)는 정승우/인게임 담당과 협의해 정해야 한다.

---

## 5. 씬 / 인스펙터 셋업 체크리스트

- [ ] **`TutorialManager` 배치**: 시작 진입점 씬(부팅/최초 진입)에 1개. `Awake`에서 `DontDestroyOnLoad` 되므로(`TutorialManager.cs:41`) **씬마다 두지 말 것**(중복 시 자기 자신 `Destroy`, `:37`).
- [ ] **`TutorialStepData(.asset)` 연결**: `TutorialManager._stepData` 에 `_Project/Data/SO/TutorialStepData.asset` 드래그.
- [ ] **각 씬에 `TutorialView` 1개씩** (`_Lobby`, `_InGame`). `Start`에서 자동으로 `RegisterView` 함(`TutorialView.cs:24`).
- [ ] **`TutorialView` 배선**: `_overlayRoot`, `_guideText`, (추가)`_highlightTargets` 매핑.
- [ ] **강조 탭 배선**: 대상 버튼 OnClick 또는 탭 핸들 → `OnHighlightTapped()`. (딤 Raycast 충돌은 3-7대로 먼저 해결.)

> **(graceful 동작) `TutorialManager`가 씬에 없으면 오버레이는 조용히 숨겨질 뿐 에러가 안 난다.** `TutorialView.Start`는 `TutorialManager.Instance`가 null이면 그냥 `Hide()` 처리한다(`TutorialView.cs:24~27`). 단독 씬에서 `TutorialView`만 띄워 테스트하면 "왜 안 뜨지"가 될 수 있으니, 매니저가 그 씬까지 살아 있는지(DontDestroyOnLoad로 진입 씬부터 따라오는지) 확인할 것.

> 매니저가 활성 씬 이름을 `"_Lobby"` / `"_InGame"` 문자열로 비교한다(`TutorialManager.cs:123~124`, `SceneManager.GetActiveScene().name`). 씬 이름이 이와 다르면 단계가 안 뜨니 주의.

### 5-1. 완료 플래그 리셋 / 최소 재현 시나리오

- 완료 플래그: `PlayerPrefs "Tutorial_Completed"`(`TutorialManager.cs:24` `DONE_KEY`). 매니저 컴포넌트 우클릭 → **ContextMenu "Reset Tutorial Flag"**(`TutorialManager.cs:132`, 메서드 `ResetTutorialFlag` `:133`)로 지운다.
- **그러나 리셋만으로는 튜토리얼이 자동으로 안 뜬다.** `TryStart()`(`:47`)를 부르는 시작 트리거가 현재 코드 어디에도 없기 때문이다(미착수). 또 stepId 1(GameAction)을 넘길 정렬 완성 훅도 없다(3-3).
- **최소 재현(현 상태에서 한 번 끝까지 돌려보려면)**: 임시 디버그 수단이 필요하다.
  1. 진입 씬을 Play 모드로 연다(매니저가 살아 있는 씬).
  2. `ResetTutorialFlag`로 완료 플래그를 지운다.
  3. `TutorialManager.Instance.TryStart()` 를 부르는 **임시 디버그 버튼/ContextMenu**를 직접 만들어 호출한다(시작 트리거 대용).
  4. stepId 0(TapHighlight): 강조 영역 탭 → 진행.
  5. stepId 1(GameAction): 정렬 완성 훅이 없으므로, `AdvanceStep()` 을 부르는 **임시 디버그 수단**으로 강제 진행(실제 훅이 들어오기 전까지).
  6. stepId 2(TapHighlight): 탭 → `Complete()` 까지 가서 종료(`Hide()` 호출 확인).

  이 임시 트리거/강제 진행 수단의 정식화는 정승우+조규민 도메인(시작 트리거·인게임 튜토 모드)이다.

---

## 6. 주의 / 협의 사항

- **★데이터 갭 (지금 텍스트만 나옴)**: `Resources/Data/Tables/3_npc_talk.json` 의 인트로(GroupID 3001, ID 100001~) 행들은 **`portrait1/2/3`, `direction`, `speaker`, `BG`, `CG`, `BGM`, `SFX` 가 전부 0**이다. 그래서 정식 드라이버를 붙여도 **현재는 이름 + 대사 텍스트만** 나온다. 그림이 붙으려면 ①데이터 담당이 ID 채우기 ②아트가 아래 경로에 스프라이트 배치 — **코드 수정 없이 자동으로 붙음**.
- **스프라이트 경로 규칙** (`ScenarioDataDriver` 인스펙터 기본값, 변경 가능):
  - 초상화: `Resources/Story/Portraits/<Story_CharacterData.Resource_ID>`
  - 배경: `Resources/Story/Backgrounds/<BG>`
  - 컷씬: `Resources/Story/Cutscenes/<CG>`
  - 못 찾으면 `null` → `ScenarioView`가 알아서 숨김(텍스트만 진행, graceful).
- **빌드/실행 경고**: `ScenarioDataDriver`는 portrait/BG/CG를 `Resources.Load<Sprite>`로 **즉시 동기 로드**한다(`ScenarioDataDriver.cs:187`). 해당 `Resources/Story/Portraits|Backgrounds|Cutscenes` 폴더가 아직 없으면 **줄마다 `Debug.Log` 경고**가 찍힌다(`:190`). 에러는 아니고 텍스트는 정상 진행되니, A 연출만 만질 때 로그가 시끄러워도 당황하지 말 것.
- **`Story_CharacterTable` ID 10010 중복 행** 존재 → `StoryRepo` dup-key 경고. 데이터 도메인(김영찬) 정리 대상. UI에는 영향 적지만 초상화 매핑 검증 시 인지.
- **더미 → 정식 드라이버 교체**: 단순 컴포넌트 교체가 아님(섹션 2-5). 진입점(`Begin()`→`Play(3001)`)·`_playOnStart`·`TempStoryToGameFlow` 코드까지 함께 손봐야 한다. 시점은 정승우와 협의.
- **완료 플래그 저장 위치**: 현재 `PlayerPrefs`(기기 단위, `TutorialManager.cs:24` `DONE_KEY`). 계정 단위로 가려면 `GameManager.CloudData`에 필드 추가 후 PlayerPrefs를 직접 읽고 쓰는 두 지점만 교체하면 된다 — `IsCompleted` getter(`:29`, `PlayerPrefs.GetInt`)와 `Complete()`(`:96`, `PlayerPrefs.SetInt`). (조규민 도메인.)
  - 용어 주의: `TutorialManager` 헤더 주석(`:12`)과 `Tutorial_Architecture.md`는 교체 지점을 "`IsCompleted/MarkCompleted`"라고 적은 흔적이 있었으나, 실제 완료 처리 메서드명은 **`Complete()`**(`TutorialManager.cs:93`)다. (헤더 주석의 `MarkCompleted` 오기는 정정함.)
- **인터페이스 고정**: `ITutorialView` 시그니처와 `OnHighlightTapped` 의 advanceMode 분기는 매니저가 의존하니 바꾸지 말 것. 내부 연출만 자유롭게.
- **씬 배치도 미확정(A 검증 전제)**: `ScenarioView`/`ScenarioDataDriver`/`TempStoryToGameFlow`가 어느 씬(`_Story`?)에 어떻게 함께 배치되는지, 인트로(3001)를 누가 `Play`하는지(시작 트리거 미착수), 그 씬/Canvas/프리팹이 이미 존재하는지(파일 표엔 `.cs`만 있고 씬/프리팹 경로는 미확인)가 정리돼 있지 않다. A를 실제 씬에서 돌려보려면 이 배선도를 정승우와 먼저 맞춰야 한다.

---

## 7. 빠른 참조

### 파일 위치

| 역할 | 경로 | 담당 / 상태 |
|---|---|---|
| 시나리오 UI (연출) | `AprilLog/Assets/_Game/UI/Scenario/ScenarioView.cs` | 홍정옥 / 기존 |
| 시나리오 정식 드라이버 | `AprilLog/Assets/_Game/UI/Scenario/ScenarioDataDriver.cs` | 정승우 / 완료 |
| 시나리오 더미 드라이버(임시) | `AprilLog/Assets/_Game/UI/Scenario/ScenarioDummyDriver.cs` | 홍정옥(+조규민) / 임시 |
| 시나리오→게임 전환(임시) | `AprilLog/Assets/_Game/UI/Scenario/TempStoryToGameFlow.cs` | 홍정옥(+조규민) / 임시 |
| 오버레이 뷰 계약 | `AprilLog/Assets/_Game/Features/Tutorial/ITutorialView.cs` | 정승우 / 골격 |
| 오버레이 뷰 (stub) | `AprilLog/Assets/_Game/Features/Tutorial/TutorialView.cs` | 정승우+홍정옥 / 골격 |
| 단계 데이터 정의 | `AprilLog/Assets/_Game/Features/Tutorial/TutorialStepData.cs` | 정승우 / 골격 |
| 진행 매니저(두뇌) | `AprilLog/Assets/_Game/Features/Tutorial/TutorialManager.cs` | 정승우 / 골격 |
| 단계 데이터 에셋 | `AprilLog/Assets/_Project/Data/SO/TutorialStepData.asset` | 정승우 / 3단계 입력됨 |
| 스토리 저장소 | `AprilLog/Assets/_Game/Data/Repositories/StoryRepo.cs` | 김영찬 |
| 대사 테이블 데이터 | `AprilLog/Assets/Resources/Data/Tables/3_npc_talk.json` | 데이터 / 갭(리소스 ID 0) |
| 아키텍처 문서 | `AprilLog/Assets/_Project/Docs/Tutorial_Architecture.md` | 정승우 |

### 데이터 접근 API

```csharp
// 스토리 대사 (드라이버가 이미 호출 — 초상화 경로/매핑 검증 시 참고용)
StoryRepo repo = DataManager.Instance.StoryRepo;
List<Story_TalkData>  group = repo.GetTalkGroup(3001);        // GroupID -> 대사 리스트(ID 순 정렬)
Story_CharacterData   ch    = repo.GetCharacterData(charId);  // 캐릭터 -> Resource_ID 등

// 튜토 단계 데이터 (TutorialStepData)
IReadOnlyList<TutorialStep> steps = stepData.Steps;   // 전체 목록
int count = stepData.Count;                           // 개수
TutorialStep s = stepData.Get(index);                // 인덱스 접근(범위 밖이면 null)
```

### `TutorialManager` 공개 멤버 (디버그/상태 확인용)

```csharp
TutorialManager.Instance.TryStart();        // 시작(완료/데이터없음이면 무시) — ★호출 주체 아직 없음
TutorialManager.Instance.AdvanceStep();     // 다음 단계(탭/게임동작 완료 시) — GameAction 훅 아직 없음
TutorialManager.Instance.Complete();        // 강제 완료(저장+Hide)
TutorialManager.Instance.ResetTutorialFlag(); // ContextMenu, 완료 플래그 초기화(테스트)

bool        running = TutorialManager.Instance.IsRunning;    // 진행 중(:30)
bool        done    = TutorialManager.Instance.IsCompleted;  // 완료 플래그(:29)
TutorialStep cur    = TutorialManager.Instance.CurrentStep;  // 현재 단계(:31, Get(_currentIndex))
```