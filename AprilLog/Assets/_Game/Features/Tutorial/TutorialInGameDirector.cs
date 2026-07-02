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
    [SerializeField] private int _firstEnchantScenarioGroupId = 100030;
    [SerializeField] private int _firstEnchantScenarioEndId = 100034;

    [Header("step3 한계 체감 러시")]
    [SerializeField] private float _step3RushWarningDelay = 38f;
    [SerializeField] private int _step3RushWarningScenarioStartId = 100035;
    [SerializeField] private int _step3RushWarningScenarioEndId = 100035;
    [SerializeField] private int _step3DefeatScenarioStartId = 100036;
    [SerializeField] private int _step3DefeatScenarioEndId = 100039;
    [SerializeField] private int[] _step3RushMonsterIds = { 11, 12, 13 };
    [SerializeField] private int _step3RushBatchAmount = 6;
    [SerializeField] private float _step3RushSpawnInterval = 0.35f;
    [SerializeField] private float _step3ForcedDefeatDelay = 12f;

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
    private CanvasGroup _firstEnchantCanvasGroup;

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
    private bool _previousFirstEnchantInteractable;
    private bool _previousFirstEnchantBlocksRaycasts;
    private int _runningStepId = -1;
    private float _previousTimeScale = 1f;
    private float _step3PreviousTimeScale = 1f;
    private Coroutine _step3Routine;
    private Coroutine _step3RushRoutine;
    private Coroutine _step3ForceDefeatRoutine;

    private static readonly BindingFlags ScenarioDriverMemberFlags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo _scenarioLinesField;
    private static FieldInfo _scenarioIndexField;
    private static FieldInfo _scenarioIsPlayingField;
    private static FieldInfo _scenarioFinishedField;
    private static MethodInfo _scenarioSubscribeMethod;
    private static MethodInfo _scenarioShowMethod;

    private void Start()
    {
        var tm = TutorialManager.Instance;
        _active = tm != null && tm.IsRunning && IsInGameStep(tm.CurrentStep) && IsTutorialChapterRun();
        if (!_active) return;

        ResolveSystems();
        if (_inputHandler != null) _inputHandler.OnDragStarted += HandleDragStarted;
        TrySubscribeGrowthLevelUp();
        if (tm.CurrentStep != null && tm.CurrentStep.stepId == 0) HoldStageForTutorialGuide();
        StartCoroutine(SuppressThenBegin());
    }

    private void OnDestroy()
    {
        if (_inputHandler != null) _inputHandler.OnDragStarted -= HandleDragStarted;
        UnsubscribeGrowthLevelUp();
        UnsubscribePlayerDeath();
        ResumeGameplayAfterGuide();
        ResumeStep3ScenarioPause();
        UnlockFirstEnchantSelection();
        ReleaseStageForTutorialPractice();
        ClearTutorialPracticeOverrides();
        TutorialFirstEnchantSelectionOverride.ClearFixedChoiceState();
        if (_firstEnchantScenarioDriver != null)
            _firstEnchantScenarioDriver.OnFinished -= HandleFirstEnchantScenarioFinished;
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

    private static bool IsInGameStep(TutorialStep step)
        => step != null && step.scene == TutorialScene.InGame;

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
            default:
                ResumeGameplayAfterGuide();
                ReleaseStageForTutorialPractice();
                ClearTutorialPracticeOverrides();
                break;
        }
    }

    // step0: 지정한 PuzzleCanvas 오브젝트(Table 등)만 강조(딤 구멍 + 손가락). 대상은 이름으로 지정.
    private void RunStep0()
    {
        _step0DragArrowHidden = false;
        ClearTutorialPracticeOverrides();
        HoldStageForTutorialGuide();
        SuppressNormalWaves();

        // 대기열 준비 후 커스텀 보드로 덮어 "한 수면 정렬 완성" 보드를 만든다.
        if (_sortSystem != null) _sortSystem.Initialize(_step0Seed);
        SetupStep0Board();

        // 강조할 슬롯들을 이름 목록으로 수집해 하나의 묶음 영역으로 강조
        RectTransform dragFrom = FindTargetByName(_step0DragFromName);
        RectTransform dragTo = FindTargetByName(_step0DragToName);
        var highlights = CollectStep0GuideHighlights();
        if (highlights.Count == 0)
        {
            Debug.LogWarning("[TutorialInGameDirector] step0 강조 슬롯을 못 찾음 — 이름 목록 확인");
            return;
        }

        _step0DimMask = transform.root.GetComponentInChildren<TutorialDimMask>(true);
        if (_step0DimMask != null) _step0DimMask.ShowWithHoles(highlights.ToArray());

        _step0Finger = transform.root.GetComponentInChildren<TutorialFingerGuide>(true);
        if (_step0Finger != null)
        {
            RectTransform fingerTarget = dragFrom != null ? dragFrom : FindTargetByName("sort (0)");
            _step0Finger.PointAt(fingerTarget != null ? fingerTarget : highlights[0]);
        }

        // 드래그 화살표: 출발/도착 슬롯이 둘 다 지정됐을 때만
        if (dragFrom != null && dragTo != null)
        {
            _step0DragArrow = transform.root.GetComponentInChildren<TutorialDragArrow>(true);
            if (_step0DragArrow != null) _step0DragArrow.ShowDrag(dragFrom, dragTo);
        }
    }

    private void RunStep1()
    {
        ResumeGameplayAfterGuide();
        HideStep0GuideVisuals();
        HideTutorialViewForFreePractice();
        ApplyTutorialPracticeOverrides();
        ReleaseStageForTutorialPractice();
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

        if (_firstEnchantScenarioDriver == null)
            _firstEnchantScenarioDriver = FindFirstObjectByType<ScenarioDataDriver>();

        if (_firstEnchantScenarioDriver == null)
        {
            Debug.LogWarning("[TutorialInGameDirector] 최초 인챈트 안내 시나리오 드라이버를 찾지 못했습니다.");
            StartCoroutine(WaitForFirstEnchantChoiceClosed());
            yield break;
        }

        _firstEnchantScenarioDriver.OnFinished -= HandleFirstEnchantScenarioFinished;
        _firstEnchantScenarioDriver.OnFinished += HandleFirstEnchantScenarioFinished;
        LockFirstEnchantSelection();
        if (!TryPlayTutorialScenarioRange(_firstEnchantScenarioGroupId, _firstEnchantScenarioEndId))
            HandleFirstEnchantScenarioFinished();
    }

    private void HandleFirstEnchantScenarioFinished()
    {
        if (_firstEnchantScenarioDriver != null)
            _firstEnchantScenarioDriver.OnFinished -= HandleFirstEnchantScenarioFinished;

        UnlockFirstEnchantSelection();
        StartCoroutine(WaitForFirstEnchantChoiceClosed());
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

        _previousFirstEnchantInteractable = _firstEnchantCanvasGroup.interactable;
        _previousFirstEnchantBlocksRaycasts = _firstEnchantCanvasGroup.blocksRaycasts;

        _firstEnchantCanvasGroup.interactable = false;
        _firstEnchantCanvasGroup.blocksRaycasts = false;
        _isFirstEnchantSelectionLocked = true;
    }

    private void UnlockFirstEnchantSelection()
    {
        if (!_isFirstEnchantSelectionLocked || _firstEnchantCanvasGroup == null) return;

        _firstEnchantCanvasGroup.interactable = _previousFirstEnchantInteractable;
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
        yield return PlayScenarioRange(_step3RushWarningScenarioStartId, _step3RushWarningScenarioEndId);
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
        yield return PlayScenarioRange(_step3DefeatScenarioStartId, _step3DefeatScenarioEndId);
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
    private const int FixedCombinationSkillNameId = 51;
    private const int FixedComboSkillNameId = 53;

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

        if (!_hasPendingFixedChoices) return false;

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
