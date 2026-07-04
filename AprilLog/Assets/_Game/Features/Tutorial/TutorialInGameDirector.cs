//작성자 : 홍정옥
// 인게임 튜토리얼 0챕터 시퀀스를 직접 구동한다
// 튜토리얼 진행 중일 때만 동작한다.
// 씬에 두면 Start에서 시스템을 자가 탐색하고, 현재 단계 시퀀스를 시작한다.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TutorialInGameDirector : MonoBehaviour
{
    private static readonly string[] RequiredStep0RedSlotNames = { "sort (0)", "sort (12)", "sort (13)" };
    private const float Step1TutorialSpawnInterval = 2f;
    private const int Step1TutorialSpawnAmount = 1;
    private const int Step1TutorialTargetKillCount = 10;
    private const int TutorialCombinationSkillNameId = 71;

    public static bool AllowsPausedSortInput { get; private set; }
    private static bool _hasTutorialSpawnOverride;
    private static float _tutorialSpawnInterval;
    private static int _tutorialSpawnAmount;
    private static bool _hasTutorialMonsterExpOverride;
    private static int _tutorialMonsterExp;

    public static bool TryGetTutorialSpawnOverride(out float interval, out int amount)
    {
        interval = _tutorialSpawnInterval;
        amount = _tutorialSpawnAmount;
        return _hasTutorialSpawnOverride;
    }

    public static bool TryGetTutorialMonsterExpOverride(out int exp)
    {
        exp = _tutorialMonsterExp;
        return _hasTutorialMonsterExpOverride;
    }

    [System.Serializable]
    private struct Step0BoardSlot
    {
        [SerializeField] private string _slotName;
        [SerializeField] private int _unitType;

        public string SlotName => _slotName;
        public int UnitType => _unitType;

        public Step0BoardSlot(string slotName, int unitType)
        {
            _slotName = slotName;
            _unitType = unitType;
        }
    }

    [Header("step0 강조 슬롯 이름 목록 (예: sort (0), sort (1), sort (2))")]
    [SerializeField] private string[] _step0HighlightNames = { "sort (0)", "sort (1)", "sort (2)" };

    [Header("step0 드래그 화살표 (출발/도착 슬롯 오브젝트 이름, 비우면 화살표 없음)")]
    [SerializeField] private string _step0DragFromName;
    [SerializeField] private string _step0DragToName;

    [Header("step0 고정 시드 (대기열용)")]
    [SerializeField] private int _step0Seed = 12345;

    [Header("step0 커스텀 보드: 강조 슬롯들에 깔 유닛 타입")]
    [SerializeField] private int _step0BoardUnitType = 0;

    [Header("step0 커스텀 보드 슬롯")]
    [SerializeField] private Step0BoardSlot[] _step0BoardSlots = CreateDefaultStep0BoardSlots();

    [Header("step2 최초 인챈트 안내")]
    [SerializeField] private ScenarioDataDriver _firstEnchantScenarioDriver;
    [SerializeField] private int _tutorialScenarioSourceGroupId = 3002;
    [Tooltip("도입: 레벨업/인챈트 개념 안내")]
    [SerializeField] private int _firstEnchantScenarioGroupId = 100030;
    [SerializeField] private int _firstEnchantScenarioEndId = 100034;
    [Tooltip("자동공격 스킬만 남기고 딤 후 재생")]
    [SerializeField] private int _enchantAutoAttackStartId = 100035;
    [SerializeField] private int _enchantAutoAttackEndId = 100036;
    [Tooltip("조합 스킬만 남기고 딤 후 재생")]
    [SerializeField] private int _enchantCombinationId = 100037;
    [Tooltip("팝업 숨기고 버블로 조합 스킬 설명 연출")]
    [SerializeField] private int _enchantCombinationGuideId = 100038;
    [Tooltip("콤보 스킬만 남기고 딤 후 재생")]
    [SerializeField] private int _enchantComboStartId = 100039;
    [SerializeField] private int _enchantComboEndId = 100040;
    [Tooltip("스킬 딤 해제 후 마무리 재생")]
    [SerializeField] private int _enchantWrapupId = 100041;
    [Tooltip("인챈트 설명 중 말풍선을 화면 어디에 고정할지(0~1 뷰포트 좌표).")]
    [SerializeField] private Vector2 _enchantBubbleViewportPosition = new Vector2(0.5f, 0.78f);
    [Tooltip("인챈트 설명 중 말풍선 고정 위치에 더할 픽셀 오프셋.")]
    [SerializeField] private Vector2 _enchantBubbleScreenOffset;

    // 고정 3카드 순서: 자동공격(50)/조합(71)/콤보(54) = TutorialFirstEnchantSelectionOverride와 동일
    private const int EnchantCardAutoAttack = 0;
    private const int EnchantCardCombination = 1;
    private const int EnchantCardCombo = 2;

    [Header("step0 전투 연출")]
    [Tooltip("0챕터 진입 대사(몬스터 없는 화면). 100019 앞에 먼저 재생")]
    [SerializeField] private int _step0EntryScenarioStartId = 100009;
    [SerializeField] private int _step0EntryScenarioEndId = 100018;
    [Tooltip("몬스터 스폰 대사")]
    [SerializeField] private int _step0IntroScenarioId = 100019;
    [Tooltip("몬스터 강조 복귀 후 대사")]
    [SerializeField] private int _step0PostEmphasisScenarioStartId = 100020;
    [SerializeField] private int _step0PostEmphasisScenarioEndId = 100025;
    [Tooltip("퍼즐 조작 유도 대사(이후 딤+화살표)")]
    [SerializeField] private int _step0PuzzleGuideScenarioId = 100026;
    [Tooltip("콤보 학습 대사(3정렬 후 공격 순간)")]
    [SerializeField] private int _step0ComboScenarioStartId = 100027;
    [SerializeField] private int _step0ComboScenarioEndId = 100029;
    [SerializeField] private int[] _step0MonsterIds = { 5011, 5012, 5013 };
    [Tooltip("몬스터가 멈춰서 등장하는 위치(월드). 좌->우 3개")]
    [SerializeField] private Vector2[] _step0MonsterSpawnPositions =
    {
        new Vector2(-2f, 3f), new Vector2(0f, 3f), new Vector2(2f, 3f)
    };
    [Tooltip("강조 연출 후 몬스터가 내려와 다시 멈추는 목표 Y(월드)")]
    [SerializeField] private float _step0MonsterStopY = 1.5f;
    [Tooltip("하강 소요 시간(초, unscaled)")]
    [SerializeField] private float _step0MonsterDescendDuration = 0.6f;
    [Tooltip("몬스터 강조 시 확대 배율(1=원래 크기)")]
    [SerializeField] private float _step0MonsterEmphasisScale = 1.5f;
    [Tooltip("확대/복귀 각각 소요 시간(초, unscaled)")]
    [SerializeField] private float _step0MonsterEmphasisDuration = 0.4f;
    [Tooltip("확대 상태를 잠깐 유지하는 시간(초)")]
    [SerializeField] private float _step0MonsterEmphasisHold = 0.3f;
    [SerializeField] private bool _step0GuaranteeOneShot = true;

    [Header("step3 한계 체감 러시")]
    [SerializeField] private float _step3RushWarningDelay = 38f;
    [SerializeField] private int _step3RushWarningScenarioStartId = 100042;
    [SerializeField] private int _step3RushWarningScenarioEndId = 100042;
    [SerializeField] private int _step3DefeatScenarioStartId = 100043;
    [SerializeField] private int _step3DefeatScenarioEndId = 100046;
    [SerializeField] private int[] _step3RushMonsterIds = { 5011, 5012, 5013 };
    [SerializeField] private int _step3RushBatchAmount = 6;
    [SerializeField] private float _step3RushSpawnInterval = 0.35f;
    [SerializeField] private float _step3ForcedDefeatDelay = 12f;

    [Header("step14 0-1챕터 재진입")]
    [SerializeField] private int _step14EntryScenarioStartId = 100075;
    [SerializeField] private int _step14EntryScenarioEndId = 100078;
    [SerializeField] private int _step14BossScenarioId = 100079;
    [Tooltip("100079 이후 조커 사용을 기다리는 동안 몬스터를 다시 멈추는 주기")]
    [SerializeField] private float _step14JokerProtectionRefreshInterval = 0.1f;
    [Tooltip("조커 사용 유도 중 몬스터에게 반복 적용할 짧은 스턴 시간")]
    [SerializeField] private float _step14JokerProtectionStunDuration = 0.25f;

    [Header("인게임 대화 버블")]
    [Tooltip("인게임 다이제틱 대화를 재생할 프리젠터. 오버레이 프리팹 안에 두고 연결.")]
    [SerializeField] private InGameTalkPresenter _talkPresenter;
    [Tooltip("이 이름과 일치하는 대사는 에이프릴(플레이어), 나머지는 래리로 매핑.")]
    [SerializeField] private string _aprilSpeakerName = "에이프릴";

    [Header("런타임 참조")]
    [SerializeField] private InGameGrowthSystem _growth;

    private MonsterSpawner _spawner;
    private SortSystem _sortSystem;
    private SortInputHandler _inputHandler;
    private PlayerModel _player;
    private StageBootstrapper _stageBootstrapper;
    private EnchantSelectView _firstEnchantSelectView;
    private TutorialDragArrow _step0DragArrow;
    private TutorialFingerGuide _step0Finger;
    private TutorialDimMask _step0DimMask;
    private TutorialDimMask _enchantDimMask;
    private CanvasGroup _firstEnchantCanvasGroup;
    private CombinationView _combinationView;
    private EnchantCombinationModel _enchantCombinationModel;
    private static FieldInfo _enchantSpawnedCardsField;

    private bool _active;
    private bool _step0DragArrowHidden;
    private bool _isGameplayPausedForGuide;
    private bool _firstEnchantScenarioPlayed;
    private bool _isWaitingFirstEnchantChoice;
    private bool _isFirstEnchantSelectionLocked;
    private bool _isGrowthLevelUpSubscribed;
    private bool _isStep3Running;
    private bool _isStep3RushActive;
    private bool _isStep3DefeatHandled;
    private bool _isStep3ScenarioPaused;
    private bool _step14EntryScenarioPlayed;
    private bool _step14BossScenarioPlayed;
    private bool _isStep14ScenarioPaused;
    private bool _wasStep14BossWavePopupVisible;
    private bool _previousFirstEnchantBlocksRaycasts;
    private bool _temporaryCombinationRecipeShown;
    private bool _temporaryFusionDataAdded;
    private int _runningStepId = -1;
    private float _previousTimeScale = 1f;
    private float _step3PreviousTimeScale = 1f;
    private float _step14PreviousTimeScale = 1f;
    private Coroutine _step3Routine;
    private Coroutine _step3RushRoutine;
    private Coroutine _step3ForceDefeatRoutine;
    private Coroutine _step14Routine;
    private Coroutine _step14BossScenarioRoutine;
    private Coroutine _step14JokerProtectionRoutine;

    private Coroutine _step0Routine;
    private Coroutine _step1Routine;
    private bool _step0ComboLessonPlayed;
    private readonly List<MonsterAI> _step0Monsters = new List<MonsterAI>(3);
    private int _step0KillCount;
    private bool _step0PuzzlePhase;
    private GameObject _bossWavePopup;
    private bool _bossWavePopupResolved;

    private static readonly BindingFlags ScenarioDriverMemberFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo _scenarioLinesField;
    private static FieldInfo _scenarioIndexField;
    private static FieldInfo _scenarioIsPlayingField;
    private static FieldInfo _scenarioFinishedField;
    private static MethodInfo _scenarioSubscribeMethod;
    private static MethodInfo _scenarioShowMethod;
    private static FieldInfo _bossWavePopupField;

    private void Start()
    {
        var tm = TutorialManager.Instance;
        _active = tm != null && tm.IsRunning && IsInGameStep(tm.CurrentStep) && IsTutorialChapterRun();
        if (!_active) return;

        ResolveSystems();
        if (_spawner != null) _spawner.OnMonsterDied += HandleStep0MonsterDied;
        if (_inputHandler != null) _inputHandler.OnDragStarted += HandleDragStarted;
        TrySubscribeGrowthLevelUp();
        if (tm.CurrentStep != null && tm.CurrentStep.stepId == 0) HoldStageForTutorialGuide();
        StartCoroutine(SuppressThenBegin());
    }

    private void OnDestroy()
    {
        if (_spawner != null) _spawner.OnMonsterDied -= HandleStep0MonsterDied;
        if (_step0Routine != null)
        {
            StopCoroutine(_step0Routine);
            _step0Routine = null;
            // 연출 도중 이탈 시 멈춰둔 시간을 복구한다.
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }
        if (_step1Routine != null)
        {
            StopCoroutine(_step1Routine);
            _step1Routine = null;
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }
        if (_step14Routine != null)
        {
            StopCoroutine(_step14Routine);
            _step14Routine = null;
            ResumeStep14ScenarioPause();
        }
        if (_step14BossScenarioRoutine != null)
        {
            StopCoroutine(_step14BossScenarioRoutine);
            _step14BossScenarioRoutine = null;
            ResumeStep14ScenarioPause();
        }
        if (_step14JokerProtectionRoutine != null)
        {
            StopCoroutine(_step14JokerProtectionRoutine);
            _step14JokerProtectionRoutine = null;
        }
        _step0PuzzlePhase = false;
        ClearEnchantDim();
        ClearTemporaryCombinationRecipe();
        SetFirstEnchantPopupVisible(true);
        ClearEnchantDialogueBubblePosition();
        if (_inputHandler != null) _inputHandler.OnDragStarted -= HandleDragStarted;
        UnsubscribeGrowthLevelUp();
        UnsubscribePlayerDeath();
        ResumeGameplayAfterGuide();
        ResumeStep3ScenarioPause();
        ResumeStep14ScenarioPause();
        UnlockFirstEnchantSelection();
        ReleaseStageForTutorialPractice();
        ClearTutorialPracticeOverrides();
        TutorialFirstEnchantSelectionOverride.ClearFixedChoiceState();
    }

    private void Update()
    {
        if (!_active) return;

        TrySubscribeGrowthLevelUp();

        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (step == null || !tm.IsRunning || !IsInGameStep(step)) return;
        if (step.stepId == _runningStepId) return;

        BeginCurrentStep();
    }

    // 튜토리얼 중에는 기획상 필요한 14단계를 제외하고 보스 경고 팝업을 같은 프레임에 눌러 억제한다.
    private void LateUpdate()
    {
        if (!_active) return;

        if (!_bossWavePopupResolved) ResolveBossWavePopup();
        if (_bossWavePopup == null) return;

        if (IsCurrentStep(14))
        {
            WatchStep14BossWavePopup();
            return;
        }

        if (_bossWavePopup.activeSelf)
            _bossWavePopup.SetActive(false);
    }

    private void WatchStep14BossWavePopup()
    {
        if (_step14BossScenarioPlayed || _step14BossScenarioRoutine != null) return;

        bool isVisible = _bossWavePopup != null && _bossWavePopup.activeInHierarchy;
        if (isVisible)
        {
            _wasStep14BossWavePopupVisible = true;
            StartStep14JokerProtection();
            return;
        }

        if (_wasStep14BossWavePopupVisible)
            _step14BossScenarioRoutine = StartCoroutine(RunStep14BossScenario());
    }

    // ScreenNavigator의 보스 웨이브 팝업 참조를 런타임에 리플렉션으로 확보한다(런타임 스폰이라 인스펙터 연결 불가).
    private void ResolveBossWavePopup()
    {
        var nav = FindFirstObjectByType<ScreenNavigator>();
        if (nav == null) return;   // 아직 생성 전 — 다음 프레임 재시도

        _bossWavePopupField ??= typeof(ScreenNavigator).GetField("_bossWavePopup", BindingFlags.Instance | BindingFlags.NonPublic);
        _bossWavePopup = _bossWavePopupField != null ? _bossWavePopupField.GetValue(nav) as GameObject : null;
        _bossWavePopupResolved = true;   // ScreenNavigator를 찾았으면 재탐색 종료
    }

    private static bool IsInGameStep(TutorialStep step)
        => step != null && step.scene == TutorialScene.InGame;

    private static bool IsCurrentStep(int stepId)
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        return step != null && step.stepId == stepId;
    }

    // 이번 인게임 런이 튜토리얼 런인지. 튜토 미완료 상태로 로비에서 일반 챕터를 선택해 들어오면
    // IsRunning만으로는 구분이 안 돼 디렉터가 일반 런을 하이재킹(보드 커스텀/정지, step3이면 강제 패배)하므로 게이트한다.
    // 판별은 진입 라우팅(ResolveStartChapterId)과 같은 신호를 쓴다: 로비 선택 런은 SelectedChapterId가 세팅되고,
    // 튜토 진입(부트스트랩 직행)과 그 재입장/Retry는 0이다. (StageLoopManager는 Start 순서상 아직 없을 수 있어 못 쓴다)
    private static bool IsTutorialChapterRun()
    {
        return GameManager.Instance == null || GameManager.Instance.SelectedChapterId == 0;
    }

    private void ResolveSystems()
    {
        _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_growth == null) _growth = FindFirstObjectByType<InGameGrowthSystem>();
        _sortSystem = FindFirstObjectByType<SortSystem>();
        _inputHandler = FindFirstObjectByType<SortInputHandler>();
        _player = FindFirstObjectByType<PlayerModel>();
        _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_firstEnchantScenarioDriver == null)
            _firstEnchantScenarioDriver = FindFirstObjectByType<ScenarioDataDriver>();
    }

    // 모든 Start 완료(부트스트랩의 StartChapter 포함) 뒤 현재 튜토리얼 시퀀스를 시작한다.
    private void TrySubscribeGrowthLevelUp()
    {
        if (_isGrowthLevelUpSubscribed) return;

        if (_growth == null)
            _growth = FindFirstObjectByType<InGameGrowthSystem>();
        if (_growth == null) return;

        _growth.OnLevelUp -= HandleLevelUp;
        _growth.OnLevelUp += HandleLevelUp;
        _isGrowthLevelUpSubscribed = true;
    }

    private void UnsubscribeGrowthLevelUp()
    {
        if (_growth == null) return;

        _growth.OnLevelUp -= HandleLevelUp;
        _isGrowthLevelUpSubscribed = false;
    }

    private IEnumerator SuppressThenBegin()
    {
        yield return null;

        // 튜토리얼 2-3 이후 단계는 실제 몬스터 스폰/공격/경험치 흐름을 전제로 한다.
        // 여기서 StageBootstrapper.StopStage()를 호출하면 Presenter가 해제되어 이후 스폰 요청이 끊긴다.
        BeginCurrentStep();
    }

    private void SuppressNormalWaves()
    {
        PauseGameplayForGuide();
    }

    private void PauseGameplayForGuide()
    {
        if (_isGameplayPausedForGuide) return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AllowsPausedSortInput = true;
        _isGameplayPausedForGuide = true;
    }

    private void ResumeGameplayAfterGuide()
    {
        if (!_isGameplayPausedForGuide) return;

        Time.timeScale = _previousTimeScale;
        AllowsPausedSortInput = false;
        _isGameplayPausedForGuide = false;
    }

    // 현재 단계에 맞는 시퀀스를 시작한다.
    private void BeginCurrentStep()
    {
        var step = TutorialManager.Instance.CurrentStep;
        if (step == null) return;
        Debug.Log($"[TutorialInGameDirector] 단계 {step.stepId} 시작");

        _runningStepId = step.stepId;
        switch (step.stepId)
        {
            case 0: RunStep0(); break;
            case 1: RunStep1(); break;
            case 2: RunStep2(); break;
            case 3: RunStep3(); break;
            case 14: RunStep14(); break;
            default:
                ResumeGameplayAfterGuide();
                ReleaseStageForTutorialPractice();
                ClearTutorialPracticeOverrides();
                break;
        }
    }

    // step0: 시나리오 → 멈춘 몬스터 3마리 등장·강조 → 퍼즐 정렬로 1방컷 3회.
    private void RunStep0()
    {
        _step0DragArrowHidden = false;
        _step0PuzzlePhase = false;
        _step0KillCount = 0;
        ClearTutorialPracticeOverrides();
        HoldStageForTutorialGuide();   // 일반 웨이브만 정지 (timeScale은 코루틴이 제어)

        // 로드 직후부터 지정 퍼즐 보드를 깔아둔다. 퍼즐 단계에서 세팅하면 인트로 동안 랜덤 보드가
        // 보이다가 갑자기 바뀌어 어색하므로, step0 진입 시점에 미리 고정 보드로 맞춘다.
        if (_sortSystem != null) _sortSystem.Initialize(_step0Seed);
        SetupStep0Board();

        if (_step0Routine == null)
            _step0Routine = StartCoroutine(RunStep0CombatIntro());
    }

    private IEnumerator RunStep0CombatIntro()
    {
        Debug.Log("[TutorialInGameDirector] step0 전투 연출 시작");

        // 도입 시나리오. 재생 중 timeScale=0로 대기.
        float prevScale = Time.timeScale;
        Time.timeScale = 0f;

        // 0챕터 진입 대사(몬스터 없는 화면) → 이후 몬스터 스폰 대사(100019)
        yield return PlayWorldDialogue(_step0EntryScenarioStartId, _step0EntryScenarioEndId);
        yield return PlayWorldDialogue(_step0IntroScenarioId, _step0IntroScenarioId);

        // 연출 구간: timeScale=0 유지 → 입력 차단 + 몬스터 자연 정지. 위치는 코드로 직접 제어.
        SpawnStep0FrozenMonsters();
        yield return null;   // 스폰(코루틴) 반영 대기

        // 카메라는 건드리지 않는다. 소환된 몬스터만 잠깐 크게 키워 강조 → 원래 크기 → 하강.
        yield return EmphasizeStep0Monsters();
        yield return DescendStep0Monsters();

        // 복귀 후 대사 → 퍼즐 조작 유도 대사(유저 터치로 넘김) → 퍼즐 가이드(딤+화살표)
        yield return PlayWorldDialogue(_step0PostEmphasisScenarioStartId, _step0PostEmphasisScenarioEndId);
        yield return PlayWorldDialogue(_step0PuzzleGuideScenarioId, _step0PuzzleGuideScenarioId);

        EnterStep0PuzzlePhase();
        _step0Routine = null;
    }

    // 연출 종료 후: timeScale 정상화(입력 허용), 퍼즐 보드/강조 표시.
    // 몬스터는 계속 멈춰 있고, 정렬 시 기존 CombatSystem이 자동 공격한다.
    private void EnterStep0PuzzlePhase()
    {
        Time.timeScale = 1f;
        _step0PuzzlePhase = true;

        // 보드는 step0 진입 시점(RunStep0)에서 이미 고정 보드로 세팅했으므로 여기서 다시 깔지 않는다.
        // (여기서 재세팅하면 timeScale=1 상태에서 클리어→재배치로 1프레임 깜빡임이 생길 수 있다.)

        var highlights = CollectStep0GuideHighlights();
        if (highlights.Count > 0)
        {
            _step0DimMask = transform.root.GetComponentInChildren<TutorialDimMask>(true);
            if (_step0DimMask != null) _step0DimMask.ShowWithHoles(highlights.ToArray());

            _step0Finger = transform.root.GetComponentInChildren<TutorialFingerGuide>(true);
            if (_step0Finger != null) _step0Finger.PointAt(highlights[0]);
        }

        RectTransform dragFrom = FindTargetByName(_step0DragFromName);
        RectTransform dragTo = FindTargetByName(_step0DragToName);
        if (dragFrom != null && dragTo != null)
        {
            _step0DragArrow = transform.root.GetComponentInChildren<TutorialDragArrow>(true);
            if (_step0DragArrow != null) _step0DragArrow.ShowDrag(dragFrom, dragTo);
        }
    }

    // 소환된 몬스터만 확대해 강조한 뒤 원래 크기로 복귀. 카메라/플레이어는 영향 없음.
    private IEnumerator EmphasizeStep0Monsters()
    {
        int count = _step0Monsters.Count;
        if (count == 0) yield break;

        var baseScales = new Vector3[count];
        for (int i = 0; i < count; i++)
            baseScales[i] = _step0Monsters[i] != null ? _step0Monsters[i].transform.localScale : Vector3.one;

        yield return ScaleStep0Monsters(baseScales, 1f, _step0MonsterEmphasisScale, _step0MonsterEmphasisDuration);
        yield return WaitUnscaled(_step0MonsterEmphasisHold);
        yield return ScaleStep0Monsters(baseScales, _step0MonsterEmphasisScale, 1f, _step0MonsterEmphasisDuration);
    }

    private IEnumerator ScaleStep0Monsters(Vector3[] baseScales, float fromMul, float toMul, float duration)
    {
        int count = _step0Monsters.Count;
        if (duration <= 0f)
        {
            for (int i = 0; i < count; i++)
                if (_step0Monsters[i] != null) _step0Monsters[i].transform.localScale = baseScales[i] * toMul;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            float mul = Mathf.Lerp(fromMul, toMul, k);
            for (int i = 0; i < count; i++)
                if (_step0Monsters[i] != null) _step0Monsters[i].transform.localScale = baseScales[i] * mul;
            yield return null;
        }

        for (int i = 0; i < count; i++)
            if (_step0Monsters[i] != null) _step0Monsters[i].transform.localScale = baseScales[i] * toMul;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    // 멈춘 몬스터를 목표 Y까지 unscaled로 부드럽게 내린 뒤 그 자리에서 다시 멈춤.
    private IEnumerator DescendStep0Monsters()
    {
        var starts = new Vector3[_step0Monsters.Count];
        for (int i = 0; i < _step0Monsters.Count; i++)
            starts[i] = _step0Monsters[i] != null ? _step0Monsters[i].transform.position : Vector3.zero;

        float duration = Mathf.Max(0.01f, _step0MonsterDescendDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            for (int i = 0; i < _step0Monsters.Count; i++)
            {
                MonsterAI m = _step0Monsters[i];
                if (m == null) continue;
                Vector3 p = starts[i];
                p.y = Mathf.Lerp(starts[i].y, _step0MonsterStopY, k);
                m.transform.position = p;
            }
            yield return null;
        }

        for (int i = 0; i < _step0Monsters.Count; i++)
        {
            MonsterAI m = _step0Monsters[i];
            if (m == null) continue;
            m.ApplyStun(9999f);   // 하강 종료 지점에서 다시 멈춤 고정
        }
    }

    private void SpawnStep0FrozenMonsters()
    {
        if (_spawner == null) _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_spawner == null || _step0MonsterIds == null || _step0MonsterIds.Length == 0) return;

        int before = _spawner.AliveMonsters.Count;

        var queue = new Queue<StageModel.SpawnCommand>();
        foreach (int id in _step0MonsterIds)
        {
            if (id <= 0) continue;
            queue.Enqueue(new StageModel.SpawnCommand
            {
                CharacterId = id,
                ScalingData = null,
                AccumulateCount = 0,
                Type = StageModel.SpawnType.Normal
            });
        }
        if (queue.Count == 0) return;

        _spawner.SpawnMonsterBatch(queue, 0f);

        // 스폰 직후 AliveMonsters에서 이번에 추가된 몬스터를 집어 고정 배치 + 멈춤 + HP 1.
        _step0Monsters.Clear();
        var alive = _spawner.AliveMonsters;
        for (int i = before; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m == null) continue;

            int idx = _step0Monsters.Count;
            if (idx < _step0MonsterSpawnPositions.Length)
                m.transform.position = _step0MonsterSpawnPositions[idx];

            m.ApplyStun(9999f);   // 연출/퍼즐 내내 멈춤 유지

            if (_step0GuaranteeOneShot && m.CurrentHP > 1)
                m.TakeDamage(m.CurrentHP - 1);   // 1방컷: 실제 공격이 자연 연출로 처치

            _step0Monsters.Add(m);
        }
    }

    private void HandleStep0MonsterDied(MonsterAI monster, bool isKamikaze)
    {
        if (!_step0PuzzlePhase) return;
        if (!_step0Monsters.Contains(monster)) return;

        _step0KillCount++;
        Debug.Log($"[TutorialInGameDirector] step0 처치 {_step0KillCount}/{_step0Monsters.Count}");
        // 다음 단계 진행은 기존 requiredSortCount(3) 훅이 담당. 여기선 카운트만.
    }

    private void RunStep1()
    {
        if (_step1Routine == null)
            _step1Routine = StartCoroutine(RunStep1Sequence());
    }

    // 3정렬 직후(step1 진입) 콤보 학습(2-3-3)을 먼저 재생하고, 이어서 자유 연습을 세팅한다.
    private IEnumerator RunStep1Sequence()
    {
        if (!_step0ComboLessonPlayed)
        {
            _step0ComboLessonPlayed = true;

            // 공격 직후 즉시 일시정지 + 콤보 카운트 UI만 남기고 딤 → 100027~29 → 딤 해제 + 재개
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            HighlightComboCountUI();
            yield return PlayWorldDialogue(_step0ComboScenarioStartId, _step0ComboScenarioEndId);
            ClearEnchantDim();
            Time.timeScale = prev <= 0f ? 1f : prev;
        }

        ResumeGameplayAfterGuide();
        HideStep0GuideVisuals();
        HideTutorialViewForFreePractice();
        ApplyTutorialPracticeOverrides();
        ReleaseStageForTutorialPractice();
        _step1Routine = null;
    }

    // 2-3-3: 콤보 카운트 UI(ComboPopupCanvas/Boundary/ComboText)만 남기고 딤 처리해 강조한다.
    private void HighlightComboCountUI()
    {
        GameObject canvas = GameObject.Find("ComboPopupCanvas");
        if (canvas == null) return;

        Transform found = FindDeepChild(canvas.transform, "ComboText");
        if (found is not RectTransform rt) return;

        TutorialDimMask dim = ResolveEnchantDimMask();
        if (dim != null) dim.ShowWithHole(rt);
    }

    private void RunStep2()
    {
        ResumeGameplayAfterGuide();
        HideStep0GuideVisuals();
        ReleaseStageForTutorialPractice();
        ClearTutorialPracticeOverrides();

        if (!_firstEnchantScenarioPlayed)
            StartCoroutine(PlayFirstEnchantScenarioAfterPopupOpened());
    }

    private void RunStep3()
    {
        ResumeGameplayAfterGuide();
        HideStep0GuideVisuals();
        ReleaseStageForTutorialPractice();
        ClearTutorialPracticeOverrides();

        if (_step3Routine == null)
            _step3Routine = StartCoroutine(RunStep3RushSequence());
    }

    private void RunStep14()
    {
        ResumeGameplayAfterGuide();
        HideStep0GuideVisuals();
        ReleaseStageForTutorialPractice();
        ClearTutorialPracticeOverrides();
        ApplyStep14SpawnThrottle();
        _wasStep14BossWavePopupVisible = false;

        if (!_step14EntryScenarioPlayed && _step14Routine == null)
            _step14Routine = StartCoroutine(RunStep14EntryScenario());
    }

    private IEnumerator RunStep14EntryScenario()
    {
        _step14EntryScenarioPlayed = true;
        PauseStep14Scenario();
        yield return PlayWorldDialogue(_step14EntryScenarioStartId, _step14EntryScenarioEndId);
        ResumeStep14ScenarioPause();
        _step14Routine = null;
    }

    private IEnumerator RunStep14BossScenario()
    {
        _step14BossScenarioPlayed = true;
        PauseStep14Scenario();
        HighlightJokerTable();
        yield return PlayWorldDialogue(_step14BossScenarioId, _step14BossScenarioId);
        ClearEnchantDim();
        ResumeStep14ScenarioPause();
        _step14BossScenarioRoutine = null;
    }

    private void HighlightJokerTable()
    {
        GameObject jokerTable = GameObject.Find("JokerTable");
        if (jokerTable == null) return;

        RectTransform target = jokerTable.GetComponent<RectTransform>();
        if (target == null) return;

        TutorialDimMask dim = ResolveEnchantDimMask();
        if (dim != null) dim.ShowWithHole(target);
    }

    private void StartStep14JokerProtection()
    {
        StunAliveMonstersForStep14Joker();

        if (_step14JokerProtectionRoutine == null)
            _step14JokerProtectionRoutine = StartCoroutine(ProtectStep14JokerUse());
    }

    private IEnumerator ProtectStep14JokerUse()
    {
        bool jokerStarted = false;
        float interval = Mathf.Max(0.02f, _step14JokerProtectionRefreshInterval);

        while (IsCurrentStep(14))
        {
            StunAliveMonstersForStep14Joker();

            JokerSystem joker = FindFirstObjectByType<JokerSystem>();
            if (joker != null && joker.IsActive)
                jokerStarted = true;
            else if (jokerStarted)
                break;

            yield return new WaitForSeconds(interval);
        }

        _step14JokerProtectionRoutine = null;
    }

    private void StunAliveMonstersForStep14Joker()
    {
        float duration = Mathf.Max(0.05f, _step14JokerProtectionStunDuration);
        foreach (MonsterAI monster in FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
        {
            if (monster != null)
                monster.ApplyStun(duration);
        }
    }

    private void HoldStageForTutorialGuide()
    {
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(true);
    }

    private void ReleaseStageForTutorialPractice()
    {
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(false);
    }

    private void PauseStep3Scenario()
    {
        if (_isStep3ScenarioPaused) return;

        _step3PreviousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(true);
        _isStep3ScenarioPaused = true;
    }

    private void ResumeStep3ScenarioPause()
    {
        if (!_isStep3ScenarioPaused) return;

        Time.timeScale = _step3PreviousTimeScale;
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(false);
        _isStep3ScenarioPaused = false;
    }

    private void PauseStep14Scenario()
    {
        if (_isStep14ScenarioPaused) return;

        _step14PreviousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(true);
        _isStep14ScenarioPaused = true;
    }

    private void ResumeStep14ScenarioPause()
    {
        if (!_isStep14ScenarioPaused) return;

        Time.timeScale = _step14PreviousTimeScale <= 0f ? 1f : _step14PreviousTimeScale;
        if (_stageBootstrapper == null) _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_stageBootstrapper != null) _stageBootstrapper.SetStageTickPaused(false);
        _isStep14ScenarioPaused = false;
    }

    private void ApplyTutorialPracticeOverrides()
    {
        _hasTutorialSpawnOverride = true;
        _tutorialSpawnInterval = Step1TutorialSpawnInterval;
        _tutorialSpawnAmount = Step1TutorialSpawnAmount;

        _hasTutorialMonsterExpOverride = true;
        _tutorialMonsterExp = ResolveTutorialPracticeExpPerKill();
    }

    private void ClearTutorialPracticeOverrides()
    {
        _hasTutorialSpawnOverride = false;
        _tutorialSpawnInterval = 0f;
        _tutorialSpawnAmount = 0;

        _hasTutorialMonsterExpOverride = false;
        _tutorialMonsterExp = 0;
    }

    // 보스 스테이지(step14)는 60초 단일 웨이브라 가속 스폰이 걸리면 몬스터가 100마리 넘게 쌓여
    // 데미지1이라도 누적으로 패배한다. 정규 스폰만 소량 고정해 누적을 막는다(경험치는 오버라이드하지 않음).
    private void ApplyStep14SpawnThrottle()
    {
        _hasTutorialSpawnOverride = true;
        _tutorialSpawnInterval = Step1TutorialSpawnInterval;
        _tutorialSpawnAmount = Step1TutorialSpawnAmount;
    }

    private int ResolveTutorialPracticeExpPerKill()
    {
        int requiredExp = 60;
        int currentExp = _growth != null ? _growth.CurrentEXP : 0;
        int currentLevel = _growth != null ? _growth.CurrentLevel : 1;

        if (DataManager.Instance != null && DataManager.Instance.ConfigRepo != null)
        {
            var levelData = DataManager.Instance.ConfigRepo.GetInLevel(currentLevel);
            if (levelData != null && levelData.RequiredEXP > 0)
            {
                requiredExp = levelData.RequiredEXP;
            }
        }

        int remainingExp = Mathf.Max(1, requiredExp - currentExp);
        return Mathf.Max(1, Mathf.CeilToInt(remainingExp / (float)Step1TutorialTargetKillCount));
    }

    private void HideStep0GuideVisuals()
    {
        if (_step0DimMask != null) _step0DimMask.Hide();
        if (_step0DragArrow != null) _step0DragArrow.Hide();
        if (_step0Finger != null) _step0Finger.Hide();
        _step0DragArrowHidden = true;
    }

    private void HideTutorialViewForFreePractice()
    {
        TutorialView view = FindFirstObjectByType<TutorialView>();
        if (view != null) view.Hide();
    }

    private void HandleLevelUp(int level)
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (tm == null || !tm.IsRunning || step == null || step.stepId != 1) return;

        TutorialFirstEnchantSelectionOverride.RequestFixedChoices();
        ClearTutorialPracticeOverrides();
        tm.AdvanceStep();
    }

    private IEnumerator PlayFirstEnchantScenarioAfterPopupOpened()
    {
        _firstEnchantScenarioPlayed = true;

        // InGameGrowthSystem은 OnLevelUp 이벤트 이후 같은 프레임에 인챈트 팝업을 연다.
        yield return null;

        // 대화 동안 카드 선택 잠금 (마지막에 해제)
        LockFirstEnchantSelection();
        SetEnchantDialogueBubblePosition();

        // 4-3-1-2 도입 (전 카드 노출)
        yield return PlayWorldDialogue(_firstEnchantScenarioGroupId, _firstEnchantScenarioEndId);

        // 4-3-1-3 자동공격 스킬만 남기고 딤
        DimEnchantExcept(EnchantCardAutoAttack);
        yield return PlayWorldDialogue(_enchantAutoAttackStartId, _enchantAutoAttackEndId);

        // 4-3-1-4 조합 스킬만 남기고 딤
        DimEnchantExcept(EnchantCardCombination);
        yield return PlayWorldDialogue(_enchantCombinationId, _enchantCombinationId);

        // 4-3-1-5 팝업을 잠시 숨기고 조합식 테이블을 강조 → 100038 → 복귀
        ClearEnchantDim();
        SetFirstEnchantPopupVisible(false);
        ShowTemporaryCombinationRecipe();
        HighlightCombinationTable();
        yield return PlayWorldDialogue(_enchantCombinationGuideId, _enchantCombinationGuideId);
        ClearEnchantDim();
        ClearTemporaryCombinationRecipe();
        SetFirstEnchantPopupVisible(true);

        // 4-3-1-6 콤보 스킬만 남기고 딤
        DimEnchantExcept(EnchantCardCombo);
        yield return PlayWorldDialogue(_enchantComboStartId, _enchantComboEndId);

        // 4-3-1-7 스킬 딤 해제 후 마무리
        ClearEnchantDim();
        yield return PlayWorldDialogue(_enchantWrapupId, _enchantWrapupId);
        ClearEnchantDialogueBubblePosition();

        // 4-3-1-8 유저가 3장 중 1장 선택 → 팝업 닫힘 → 다음 단계
        UnlockFirstEnchantSelection();
        StartCoroutine(WaitForFirstEnchantChoiceClosed());
    }

    // 인챈트 팝업의 런타임 생성 카드(_spawnedCards)를 리플렉션으로 읽어 RectTransform 목록을 만든다.
    private List<RectTransform> ResolveEnchantCardRects()
    {
        if (_firstEnchantSelectView == null)
            _firstEnchantSelectView = FindFirstObjectByType<EnchantSelectView>();
        if (_firstEnchantSelectView == null) return null;

        _enchantSpawnedCardsField ??= typeof(EnchantSelectView)
            .GetField("_spawnedCards", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_enchantSpawnedCardsField == null) return null;

        if (_enchantSpawnedCardsField.GetValue(_firstEnchantSelectView) is not List<GameObject> cards)
            return null;

        var rects = new List<RectTransform>(cards.Count);
        foreach (GameObject card in cards)
        {
            if (card != null) rects.Add(card.GetComponent<RectTransform>());
        }
        return rects;
    }

    // 지정 카드만 남기고 나머지를 딤 처리(딤마스크 구멍).
    private void DimEnchantExcept(int keepIndex)
    {
        List<RectTransform> rects = ResolveEnchantCardRects();
        if (rects == null || keepIndex < 0 || keepIndex >= rects.Count || rects[keepIndex] == null) return;

        TutorialDimMask dim = ResolveEnchantDimMask();
        if (dim != null)
        {
            PrepareEnchantDimLayer(dim);
            dim.ShowWithHoles(new[] { rects[keepIndex] });
        }
    }

    private void ClearEnchantDim()
    {
        TutorialDimMask dim = ResolveEnchantDimMask();
        if (dim != null) dim.Hide();
    }

    private void PrepareEnchantDimLayer(TutorialDimMask dim)
    {
        if (dim == null) return;

        if (_firstEnchantSelectView == null)
            _firstEnchantSelectView = FindFirstObjectByType<EnchantSelectView>();

        Canvas dimCanvas = dim.GetComponentInParent<Canvas>();
        Canvas popupCanvas = _firstEnchantSelectView != null
            ? _firstEnchantSelectView.GetComponentInParent<Canvas>()
            : null;
        if (dimCanvas == null || popupCanvas == null || dimCanvas == popupCanvas) return;

        dimCanvas.overrideSorting = true;
        dimCanvas.sortingLayerID = popupCanvas.sortingLayerID;
        dimCanvas.sortingOrder = Mathf.Max(dimCanvas.sortingOrder, popupCanvas.sortingOrder + 1);
    }

    // 4-3-1-5: 인게임 조합식 테이블의 첫 슬롯(CombinationCanvas/Slot_1)만 남기고 딤 처리해 강조한다.
    private void HighlightCombinationTable()
    {
        RectTransform target = ResolveCombinationSlot();
        if (target == null) return;

        TutorialDimMask dim = ResolveEnchantDimMask();
        if (dim == null) return;

        PrepareEnchantDimLayer(dim);
        dim.ShowWithHole(target);
    }

    private void ShowTemporaryCombinationRecipe()
    {
        CombinationView view = ResolveCombinationView();
        if (view == null) return;
        if (!TryGetTutorialCombinationRecipe(out int recipeKey, out int[] ingredients, out SkillTableData skillData))
            return;

        EnsureTemporaryFusionData(recipeKey, skillData);
        view.SetRecipe(0, recipeKey, ingredients);
        _temporaryCombinationRecipeShown = true;
    }

    private void ClearTemporaryCombinationRecipe()
    {
        if (!_temporaryCombinationRecipeShown) return;

        CombinationView view = ResolveCombinationView();
        if (view != null) view.ClearRecipe(0);

        RectTransform slot = ResolveCombinationSlot();
        if (slot != null) slot.gameObject.SetActive(false);

        if (_temporaryFusionDataAdded && _enchantCombinationModel != null && _enchantCombinationModel.FusionData != null)
            _enchantCombinationModel.FusionData.Remove(TutorialCombinationSkillNameId);

        _temporaryCombinationRecipeShown = false;
        _temporaryFusionDataAdded = false;
    }

    private CombinationView ResolveCombinationView()
    {
        if (_combinationView == null)
            _combinationView = FindFirstObjectByType<CombinationView>();
        return _combinationView;
    }

    private bool TryGetTutorialCombinationRecipe(out int recipeKey, out int[] ingredients, out SkillTableData skillData)
    {
        recipeKey = TutorialCombinationSkillNameId;
        ingredients = null;
        skillData = null;

        SpellRepo repo = DataManager.Instance != null ? DataManager.Instance.SpellRepo : null;
        SkillNameChainData chain = repo != null
            ? repo.GetSkillChainByName(EnchantModel.GROUP_COMBINATION_SKILL, recipeKey)
            : null;
        skillData = chain != null ? chain.GetNextLevelData(0) : null;
        if (skillData == null) return false;

        var ingredientList = new List<int>(3);
        AddIngredient(ingredientList, skillData.RequiredValue_1);
        AddIngredient(ingredientList, skillData.RequiredValue_2);
        AddIngredient(ingredientList, skillData.RequiredValue_3);
        ingredients = ingredientList.ToArray();
        return ingredients.Length > 0;
    }

    private static void AddIngredient(List<int> ingredients, float rawValue)
    {
        int unitType = ConvertRawIdToUnitType(rawValue);
        if (unitType != (int)UnitType.None) ingredients.Add(unitType);
    }

    private static int ConvertRawIdToUnitType(float rawValue)
    {
        int id = Mathf.RoundToInt(rawValue);
        return id switch
        {
            1001 => (int)UnitType.Red,
            1002 => (int)UnitType.Blue,
            1003 => (int)UnitType.Yellow,
            1004 => (int)UnitType.Green,
            1005 => (int)UnitType.Purple,
            _ => (int)UnitType.None
        };
    }

    private void EnsureTemporaryFusionData(int recipeKey, SkillTableData skillData)
    {
        if (_enchantCombinationModel == null)
            _enchantCombinationModel = FindFirstObjectByType<EnchantCombinationModel>();
        if (_enchantCombinationModel == null || _enchantCombinationModel.FusionData == null || skillData == null)
            return;
        if (_enchantCombinationModel.FusionData.ContainsKey(recipeKey)) return;

        _enchantCombinationModel.FusionData.Add(recipeKey, new FusionEnchantData(
            skillData.Skill_ID,
            ConvertRawIdToUnitType(skillData.RequiredValue_1),
            ConvertRawIdToUnitType(skillData.RequiredValue_2),
            ConvertRawIdToUnitType(skillData.RequiredValue_3),
            skillData.SkillIcon_ID));
        _temporaryFusionDataAdded = true;
    }

    private void SetFirstEnchantPopupVisible(bool visible)
    {
        if (_firstEnchantSelectView == null)
            _firstEnchantSelectView = FindFirstObjectByType<EnchantSelectView>();
        if (_firstEnchantSelectView == null) return;

        if (_firstEnchantCanvasGroup == null)
            _firstEnchantCanvasGroup = _firstEnchantSelectView.GetComponent<CanvasGroup>();
        if (_firstEnchantCanvasGroup == null)
            _firstEnchantCanvasGroup = _firstEnchantSelectView.gameObject.AddComponent<CanvasGroup>();

        _firstEnchantCanvasGroup.alpha = visible ? 1f : 0f;
        _firstEnchantCanvasGroup.interactable = true;
        if (_isFirstEnchantSelectionLocked)
            _firstEnchantCanvasGroup.blocksRaycasts = false;
    }

    private void SetEnchantDialogueBubblePosition()
    {
        if (_talkPresenter != null)
            _talkPresenter.SetBubbleViewportPosition(_enchantBubbleViewportPosition, _enchantBubbleScreenOffset);
    }

    private void ClearEnchantDialogueBubblePosition()
    {
        if (_talkPresenter != null)
            _talkPresenter.ClearBubblePositionOverride();
    }

    private RectTransform ResolveCombinationSlot()
    {
        GameObject canvas = GameObject.Find("CombinationCanvas");
        if (canvas == null) return null;

        Transform slot = FindDeepChild(canvas.transform, "Slot_1");
        return slot as RectTransform;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private TutorialDimMask ResolveEnchantDimMask()
    {
        if (_enchantDimMask == null)
            _enchantDimMask = transform.root.GetComponentInChildren<TutorialDimMask>(true);
        return _enchantDimMask;
    }

    private void LockFirstEnchantSelection()
    {
        if (_isFirstEnchantSelectionLocked) return;

        if (_firstEnchantSelectView == null)
            _firstEnchantSelectView = FindFirstObjectByType<EnchantSelectView>();
        if (_firstEnchantSelectView == null) return;

        _firstEnchantCanvasGroup = _firstEnchantSelectView.GetComponent<CanvasGroup>();
        if (_firstEnchantCanvasGroup == null)
            _firstEnchantCanvasGroup = _firstEnchantSelectView.gameObject.AddComponent<CanvasGroup>();

        _previousFirstEnchantBlocksRaycasts = _firstEnchantCanvasGroup.blocksRaycasts;

        _firstEnchantCanvasGroup.alpha = 1f;
        _firstEnchantCanvasGroup.interactable = true;
        _firstEnchantCanvasGroup.blocksRaycasts = false;
        _isFirstEnchantSelectionLocked = true;
    }

    private void UnlockFirstEnchantSelection()
    {
        if (!_isFirstEnchantSelectionLocked || _firstEnchantCanvasGroup == null) return;

        _firstEnchantCanvasGroup.alpha = 1f;
        _firstEnchantCanvasGroup.blocksRaycasts = _previousFirstEnchantBlocksRaycasts;
        _isFirstEnchantSelectionLocked = false;
    }

    private IEnumerator WaitForFirstEnchantChoiceClosed()
    {
        if (_isWaitingFirstEnchantChoice) yield break;
        _isWaitingFirstEnchantChoice = true;

        while (Time.timeScale <= 0f)
            yield return null;

        _isWaitingFirstEnchantChoice = false;

        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (tm != null && tm.IsRunning && step != null && step.stepId == 2)
            tm.AdvanceStep();
    }

    private IEnumerator RunStep3RushSequence()
    {
        _isStep3Running = true;
        _isStep3RushActive = false;
        _isStep3DefeatHandled = false;
        SubscribePlayerDeath();

        yield return new WaitForSeconds(_step3RushWarningDelay);

        if (!_isStep3Running || _isStep3DefeatHandled) yield break;

        PauseStep3Scenario();
        yield return PlayWorldDialogue(_step3RushWarningScenarioStartId, _step3RushWarningScenarioEndId);
        ResumeStep3ScenarioPause();

        StartStep3Rush();
    }

    private void StartStep3Rush()
    {
        if (_isStep3RushActive) return;

        _isStep3RushActive = true;
        if (_step3RushRoutine == null)
            _step3RushRoutine = StartCoroutine(SpawnStep3RushLoop());
        if (_step3ForcedDefeatDelay > 0f && _step3ForceDefeatRoutine == null)
            _step3ForceDefeatRoutine = StartCoroutine(ForceStep3DefeatAfterDelay());
    }

    private IEnumerator SpawnStep3RushLoop()
    {
        float interval = Mathf.Max(0.05f, _step3RushSpawnInterval);

        while (_isStep3RushActive && _player != null && !_player.IsDead)
        {
            SpawnStep3RushBatch();
            yield return new WaitForSeconds(interval);
        }

        _step3RushRoutine = null;
    }

    private void SpawnStep3RushBatch()
    {
        if (_spawner == null) _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_spawner == null || _step3RushMonsterIds == null || _step3RushMonsterIds.Length == 0) return;

        var queue = new Queue<StageModel.SpawnCommand>();
        int amount = Mathf.Max(1, _step3RushBatchAmount);
        for (int i = 0; i < amount; i++)
        {
            int characterId = _step3RushMonsterIds[UnityEngine.Random.Range(0, _step3RushMonsterIds.Length)];
            if (characterId <= 0) continue;

            queue.Enqueue(new StageModel.SpawnCommand
            {
                CharacterId = characterId,
                ScalingData = null,
                AccumulateCount = 0,
                Type = StageModel.SpawnType.Rush
            });
        }

        if (queue.Count > 0)
            _spawner.SpawnMonsterBatch(queue, 0.05f);
    }

    private IEnumerator ForceStep3DefeatAfterDelay()
    {
        yield return new WaitForSeconds(_step3ForcedDefeatDelay);

        _step3ForceDefeatRoutine = null;
        if (!_isStep3Running || _isStep3DefeatHandled) yield break;
        if (_player == null || _player.IsDead) yield break;

        _player.TakeDamage(Mathf.Max(1, _player.CurrentHP));
    }

    private void SubscribePlayerDeath()
    {
        if (_player == null) _player = FindFirstObjectByType<PlayerModel>();
        if (_player == null) return;

        _player.OnPlayerDeath -= HandlePlayerDeath;
        _player.OnPlayerDeath += HandlePlayerDeath;
    }

    private void UnsubscribePlayerDeath()
    {
        if (_player == null) return;

        _player.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (!_isStep3Running || _isStep3DefeatHandled || step == null || step.stepId != 3) return;

        StartCoroutine(HandleStep3Defeat());
    }

    private IEnumerator HandleStep3Defeat()
    {
        _isStep3DefeatHandled = true;
        _isStep3RushActive = false;

        if (_spawner == null) _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_spawner != null) _spawner.StopSpawning();

        PauseStep3Scenario();
        yield return PlayWorldDialogue(_step3DefeatScenarioStartId, _step3DefeatScenarioEndId);
        ResumeStep3ScenarioPause();

        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (tm != null && tm.IsRunning && step != null && step.stepId == 3)
            tm.AdvanceStep();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadLobby();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("_Lobby");
    }

    // 인게임 다이제틱 대화 버블로 라인들을 재생하고 종료까지 대기한다.
    // 라인 소스(데이터 어댑터)는 이 함수 밖에서 만들어 넘긴다.
    private IEnumerator PlayTalk(IReadOnlyList<TalkLine> lines)
    {
        if (_talkPresenter == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] 대화 버블 프리젠터가 연결되지 않았습니다.");
            yield break;
        }
        if (lines == null || lines.Count == 0) yield break;

        bool finished = false;
        Action handleFinished = () => finished = true;
        _talkPresenter.OnFinished += handleFinished;
        _talkPresenter.Play(lines);

        while (!finished)
            yield return null;

        _talkPresenter.OnFinished -= handleFinished;
    }

    // 시나리오 소스 그룹(3002)에서 ID 범위를 골라 버블용 TalkLine으로 변환한다.
    // 화자는 이름으로 매핑(에이프릴=플레이어, 그 외=래리). 데이터 교체 시 이 어댑터만 손보면 된다.
    private List<TalkLine> BuildTutorialTalkLines(int startId, int endId)
    {
        var result = new List<TalkLine>();

        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] StoryRepo를 찾지 못해 대화 라인을 만들지 못했습니다.");
            return result;
        }

        List<Story_TalkData> source = repo.GetTalkGroup(_tutorialScenarioSourceGroupId);
        foreach (Story_TalkData line in CollectTutorialScenarioLines(source, startId, endId))
        {
            if (line == null) continue;
            TalkSpeaker speaker = string.Equals(line.name_KR, _aprilSpeakerName)
                ? TalkSpeaker.April
                : TalkSpeaker.Rary;
            result.Add(new TalkLine(speaker, line.name_KR, line.Text_KR));
        }

        return result;
    }

    // 월드(필드) 인게임 대사: 버블 프리젠터가 연결돼 있으면 스토리박스(버블)로, 없으면 기존 ScenarioView로 폴백.
    private IEnumerator PlayWorldDialogue(int startId, int endId)
    {
        if (_talkPresenter != null)
            yield return PlayTalk(BuildTutorialTalkLines(startId, endId));
        else
            yield return PlayScenarioRange(startId, endId);
    }

    private IEnumerator PlayScenarioRange(int startId, int endId)
    {
        if (_firstEnchantScenarioDriver == null)
            _firstEnchantScenarioDriver = FindFirstObjectByType<ScenarioDataDriver>();

        if (_firstEnchantScenarioDriver == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] 시나리오 드라이버를 찾지 못했습니다.");
            yield break;
        }

        bool finished = false;
        Action handleFinished = () => finished = true;
        _firstEnchantScenarioDriver.OnFinished += handleFinished;

        if (!TryPlayTutorialScenarioRange(startId, endId))
        {
            _firstEnchantScenarioDriver.OnFinished -= handleFinished;
            yield break;
        }

        while (!finished)
            yield return null;

        _firstEnchantScenarioDriver.OnFinished -= handleFinished;
    }

    // 수정자: 홍정옥
    // 수정 내용: 담당자 스크립트(StoryRepo/ScenarioDataDriver)를 수정하지 않기 위해,
    // 튜토리얼 전용으로 GroupID 3002 대사 목록에서 Talk ID 범위만 골라 ScenarioDataDriver에 주입한다.
    private bool TryPlayTutorialScenarioRange(int startId, int endId)
    {
        if (_firstEnchantScenarioDriver == null)
            _firstEnchantScenarioDriver = FindFirstObjectByType<ScenarioDataDriver>();

        if (_firstEnchantScenarioDriver == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] 시나리오 드라이버를 찾지 못했습니다.");
            return false;
        }

        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] StoryRepo를 찾지 못했습니다.");
            return false;
        }

        List<Story_TalkData> sourceLines = repo.GetTalkGroup(_tutorialScenarioSourceGroupId);
        List<Story_TalkData> rangeLines = CollectTutorialScenarioLines(sourceLines, startId, endId);
        if (rangeLines.Count == 0)
        {
            Debug.LogWarning($"[TutorialInGameDirector] 튜토리얼 시나리오 ID {startId}~{endId} 대사를 찾지 못했습니다.");
            return false;
        }

        return TryInjectScenarioLines(_firstEnchantScenarioDriver, rangeLines);
    }

    private static List<Story_TalkData> CollectTutorialScenarioLines(List<Story_TalkData> sourceLines, int startId, int endId)
    {
        var result = new List<Story_TalkData>();
        if (sourceLines == null) return result;

        int min = Mathf.Min(startId, endId);
        int max = Mathf.Max(startId, endId);
        foreach (Story_TalkData line in sourceLines)
        {
            if (line != null && line.ID >= min && line.ID <= max)
                result.Add(line);
        }

        result.Sort((a, b) => a.ID.CompareTo(b.ID));
        return result;
    }

    private static bool TryInjectScenarioLines(ScenarioDataDriver driver, List<Story_TalkData> lines)
    {
        if (driver == null || lines == null || lines.Count == 0) return false;
        if (!EnsureScenarioDriverMembers()) return false;

        try
        {
            _scenarioSubscribeMethod.Invoke(driver, null);
            _scenarioLinesField.SetValue(driver, lines);
            _scenarioIndexField.SetValue(driver, 0);
            _scenarioFinishedField.SetValue(driver, false);
            _scenarioIsPlayingField.SetValue(driver, true);
            _scenarioShowMethod.Invoke(driver, null);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialInGameDirector] 튜토리얼 시나리오 범위 재생 준비 실패: {e.Message}");
            return false;
        }
    }

    private static bool EnsureScenarioDriverMembers()
    {
        Type type = typeof(ScenarioDataDriver);
        _scenarioLinesField ??= type.GetField("_lines", ScenarioDriverMemberFlags);
        _scenarioIndexField ??= type.GetField("_index", ScenarioDriverMemberFlags);
        _scenarioIsPlayingField ??= type.GetField("_isPlaying", ScenarioDriverMemberFlags);
        _scenarioFinishedField ??= type.GetField("_finished", ScenarioDriverMemberFlags);
        _scenarioSubscribeMethod ??= type.GetMethod("Subscribe", ScenarioDriverMemberFlags);
        _scenarioShowMethod ??= type.GetMethod("Show", ScenarioDriverMemberFlags);

        bool hasAllMembers = _scenarioLinesField != null
            && _scenarioIndexField != null
            && _scenarioIsPlayingField != null
            && _scenarioFinishedField != null
            && _scenarioSubscribeMethod != null
            && _scenarioShowMethod != null;

        if (!hasAllMembers)
            Debug.LogWarning("[TutorialInGameDirector] ScenarioDataDriver 내부 재생 멤버를 찾지 못했습니다.");

        return hasAllMembers;
    }

    private void HandleDragStarted(int tableIdx, int slotIdx)
    {
        if (!_active || _step0DragArrowHidden) return;

        var tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (step == null || step.stepId != 0) return;
        if (!MatchesConfiguredDragFrom(tableIdx, slotIdx)) return;

        ResumeGameplayAfterGuide();
        if (_step0DimMask != null) _step0DimMask.Hide();
        if (_step0DragArrow != null) _step0DragArrow.Hide();
        if (_step0Finger != null) _step0Finger.Hide();
        _step0DragArrowHidden = true;
    }

    private System.Collections.Generic.List<RectTransform> CollectStep0GuideHighlights()
    {
        var highlights = new System.Collections.Generic.List<RectTransform>();

        AddStep0GuideTableHighlight(highlights, _step0DragFromName);
        AddStep0GuideTableHighlight(highlights, _step0DragToName);

        if (highlights.Count > 0) return highlights;

        foreach (string n in _step0HighlightNames)
        {
            RectTransform rt = FindTargetByName(n);
            AddUniqueHighlight(highlights, rt);
        }

        return highlights;
    }

    private void AddStep0GuideTableHighlight(System.Collections.Generic.List<RectTransform> highlights, string targetName)
    {
        int tableIdx = ResolveTableIndexFromTargetName(targetName);
        if (tableIdx < 0) return;

        RectTransform table = FindTargetByName($"Table ({tableIdx})");
        AddUniqueHighlight(highlights, table);
    }

    private static void AddUniqueHighlight(System.Collections.Generic.List<RectTransform> highlights, RectTransform target)
    {
        if (target == null || highlights.Contains(target)) return;
        highlights.Add(target);
    }

    private static int ResolveTableIndexFromTargetName(string targetName)
    {
        int index = ParseSlotIndex(targetName);
        if (index < 0) return -1;

        return targetName.StartsWith("Table")
            ? index
            : index / SortModel.SLOTS_PER_TABLE;
    }

    private bool MatchesConfiguredDragFrom(int tableIdx, int slotIdx)
    {
        if (string.IsNullOrEmpty(_step0DragFromName)) return false;

        if (_step0DragFromName.StartsWith("Table"))
        {
            int configuredTable = ParseSlotIndex(_step0DragFromName);
            return configuredTable == tableIdx;
        }

        int configuredSlot = ParseSlotIndex(_step0DragFromName);
        return configuredSlot >= 0
            && configuredSlot / SortModel.SLOTS_PER_TABLE == tableIdx
            && configuredSlot % SortModel.SLOTS_PER_TABLE == slotIdx;
    }

    // step0 커스텀 보드: 보드를 비우고 강조 슬롯들에 같은 유닛을 깔아 한 수면 정렬되게 한다.
    private void SetupStep0Board()
    {
        var model = FindFirstObjectByType<SortModel>();
        if (model == null) return;

        // 모든 슬롯을 개별 ClearSlot으로 비운다(ResetBoard는 뷰가 빈 깡통이라 시각 반영 안 됨).
        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
            for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
                model.ClearSlot(t, s);

        Step0BoardSlot[] boardSlots = _step0BoardSlots != null && _step0BoardSlots.Length > 0
            ? _step0BoardSlots
            : CreateDefaultStep0BoardSlots();

        bool placedAny = false;
        foreach (Step0BoardSlot slot in boardSlots)
        {
            placedAny |= TryPlaceStep0BoardSlot(model, slot.SlotName, slot.UnitType);
        }

        placedAny |= PlaceRequiredStep0RedSlots(model);

        if (placedAny) return;

        foreach (string n in _step0HighlightNames)
        {
            TryPlaceStep0BoardSlot(model, n, _step0BoardUnitType);
        }

        PlaceRequiredStep0RedSlots(model);
    }

    // "sort (4)" → 4
    private static Step0BoardSlot[] CreateDefaultStep0BoardSlots()
    {
        return new[]
        {
            new Step0BoardSlot("sort (0)", 0),
            new Step0BoardSlot("sort (4)", 0),
            new Step0BoardSlot("sort (5)", 0),
            new Step0BoardSlot("sort (6)", 1),
            new Step0BoardSlot("sort (7)", 2),
            new Step0BoardSlot("sort (9)", 1),
            new Step0BoardSlot("sort (10)", 3),
            new Step0BoardSlot("sort (12)", 2),
            new Step0BoardSlot("sort (13)", 0),
            new Step0BoardSlot("sort (15)", 3),
            new Step0BoardSlot("sort (16)", 0),
        };
    }

    private static bool PlaceRequiredStep0RedSlots(SortModel model)
    {
        bool placedAny = false;
        foreach (string slotName in RequiredStep0RedSlotNames)
        {
            placedAny |= TryPlaceStep0BoardSlot(model, slotName, (int)UnitType.Red);
        }

        return placedAny;
    }

    private static bool TryPlaceStep0BoardSlot(SortModel model, string slotName, int unitType)
    {
        int idx = ParseSlotIndex(slotName);
        if (idx < 0) return false;

        int safeUnitType = Mathf.Clamp(unitType, 0, SortModel.UNIT_TYPE_COUNT - 1);
        model.PlaceUnit(idx / SortModel.SLOTS_PER_TABLE, idx % SortModel.SLOTS_PER_TABLE, safeUnitType);
        return true;
    }

    private static int ParseSlotIndex(string slotName)
    {
        if (string.IsNullOrEmpty(slotName)) return -1;
        int open = slotName.IndexOf('(');
        int close = slotName.IndexOf(')');
        if (open < 0 || close <= open) return -1;
        string num = slotName.Substring(open + 1, close - open - 1).Trim();
        return int.TryParse(num, out int v) ? v : -1;
    }

    // 씬에서 이름으로 RectTransform을 찾는다(활성 오브젝트 대상).
    private RectTransform FindTargetByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;
        GameObject go = GameObject.Find(targetName);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }
}

public static class TutorialFirstEnchantSelectionOverride
{
    private const int FixedNormalSkillNameId = 50;
    private const int FixedCombinationSkillNameId = 71;
    private const int FixedComboSkillNameId = 54;

    private static bool _hasPendingFixedChoices;

    public static bool IsShowingFixedChoices { get; private set; }

    public static void RequestFixedChoices()
    {
        _hasPendingFixedChoices = true;
        IsShowingFixedChoices = false;
    }

    public static bool TryConsumeFixedChoices(
        EnchantModel model,
        SpellRepo repo,
        out System.Collections.Generic.List<EnchantCandidate> choices)
    {
        choices = null;
        IsShowingFixedChoices = false;

        if (!_hasPendingFixedChoices && !ShouldForceFixedChoicesForTutorial())
            return false;

        _hasPendingFixedChoices = false;

        if (model == null || repo == null)
        {
            Debug.LogWarning("[TutorialFirstEnchantSelectionOverride] 고정 인챈트 후보 생성에 필요한 참조가 없어 기존 선택지를 사용합니다.");
            return false;
        }

        var fixedChoices = new System.Collections.Generic.List<EnchantCandidate>(3);
        TryAddSkillChoice(fixedChoices, model, repo, EnchantModel.GROUP_NORMAL_SKILL, FixedNormalSkillNameId);
        TryAddSkillChoice(fixedChoices, model, repo, EnchantModel.GROUP_COMBINATION_SKILL, FixedCombinationSkillNameId);
        TryAddSkillChoice(fixedChoices, model, repo, EnchantModel.GROUP_COMBO_SKILL, FixedComboSkillNameId);

        if (fixedChoices.Count != 3)
        {
            Debug.LogWarning("[TutorialFirstEnchantSelectionOverride] 튜토리얼 고정 인챈트 후보를 모두 찾지 못해 기존 선택지를 사용합니다.");
            return false;
        }

        choices = fixedChoices;
        IsShowingFixedChoices = true;
        return true;
    }

    public static void ClearFixedChoiceState()
    {
        IsShowingFixedChoices = false;
    }

    private static bool ShouldForceFixedChoicesForTutorial()
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        return tm != null
            && tm.IsRunning
            && step != null
            && (step.stepId == 1 || step.stepId == 2)
            && !IsShowingFixedChoices;
    }

    private static void TryAddSkillChoice(
        System.Collections.Generic.List<EnchantCandidate> choices,
        EnchantModel model,
        SpellRepo repo,
        int groupId,
        int nameId)
    {
        SkillNameChainData chain = repo.GetSkillChainByName(groupId, nameId);
        if (chain == null) return;

        int currentLevel = model.GetSkillLevel(nameId);
        SkillTableData nextData = chain.GetNextLevelData(currentLevel);
        if (nextData == null) return;

        choices.Add(new EnchantCandidate
        {
            Type = EnchantType.Skill,
            Name_ID = nameId,
            Specific_ID = nextData.Skill_ID,
            Level = nextData.Level,
            SkillData = nextData
        });
    }
}
