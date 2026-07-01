//작성자 : 홍정옥
// 인게임 튜토리얼 0챕터 시퀀스를 직접 구동한다
// 튜토리얼 진행 중일 때만 동작한다.
// 씬에 두면 Start에서 시스템을 자가 탐색하고, 현재 단계 시퀀스를 시작한다.

using System.Collections;
using UnityEngine;

public class TutorialInGameDirector : MonoBehaviour
{
    private static readonly string[] RequiredStep0RedSlotNames = { "sort (0)", "sort (12)", "sort (13)" };

    public static bool AllowsPausedSortInput { get; private set; }

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

    private MonsterSpawner _spawner;
    private InGameGrowthSystem _growth;
    private SortSystem _sortSystem;
    private SortInputHandler _inputHandler;
    private PlayerModel _player;
    private StageBootstrapper _stageBootstrapper;
    private TutorialDragArrow _step0DragArrow;
    private TutorialFingerGuide _step0Finger;

    private bool _active;
    private bool _step0DragArrowHidden;
    private bool _isGameplayPausedForGuide;
    private float _previousTimeScale = 1f;

    private void Start()
    {
        var tm = TutorialManager.Instance;
        _active = tm != null && tm.IsRunning && IsInGameStep(tm.CurrentStep);
        if (!_active) return;

        ResolveSystems();
        if (_inputHandler != null) _inputHandler.OnDragStarted += HandleDragStarted;
        StartCoroutine(SuppressThenBegin());
    }

    private void OnDestroy()
    {
        if (_inputHandler != null) _inputHandler.OnDragStarted -= HandleDragStarted;
        ResumeGameplayAfterGuide();
    }

    private static bool IsInGameStep(TutorialStep step)
        => step != null && step.scene == TutorialScene.InGame;

    private void ResolveSystems()
    {
        _spawner = FindFirstObjectByType<MonsterSpawner>();
        _growth = FindFirstObjectByType<InGameGrowthSystem>();
        _sortSystem = FindFirstObjectByType<SortSystem>();
        _inputHandler = FindFirstObjectByType<SortInputHandler>();
        _player = FindFirstObjectByType<PlayerModel>();
        _stageBootstrapper = FindFirstObjectByType<StageBootstrapper>();
    }

    // 모든 Start 완료(부트스트랩의 StartChapter 포함) 뒤 현재 튜토리얼 시퀀스를 시작한다.
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

        switch (step.stepId)
        {
            case 0: RunStep0(); break;
        }
    }

    // step0: 지정한 PuzzleCanvas 오브젝트(Table 등)만 강조(딤 구멍 + 손가락). 대상은 이름으로 지정.
    private void RunStep0()
    {
        _step0DragArrowHidden = false;
        SuppressNormalWaves();

        // 대기열 준비 후 커스텀 보드로 덮어 "한 수면 정렬 완성" 보드를 만든다.
        if (_sortSystem != null) _sortSystem.Initialize(_step0Seed);
        SetupStep0Board();

        // 강조할 슬롯들을 이름 목록으로 수집해 하나의 묶음 영역으로 강조
        var highlights = new System.Collections.Generic.List<RectTransform>();
        foreach (string n in _step0HighlightNames)
        {
            RectTransform rt = FindTargetByName(n);
            if (rt != null) highlights.Add(rt);
        }
        if (highlights.Count == 0)
        {
            Debug.LogWarning("[TutorialInGameDirector] step0 강조 슬롯을 못 찾음 — 이름 목록 확인");
            return;
        }

        TutorialDimMask dim = transform.root.GetComponentInChildren<TutorialDimMask>(true);
        if (dim != null) dim.ShowWithHoles(highlights.ToArray());

        _step0Finger = transform.root.GetComponentInChildren<TutorialFingerGuide>(true);
        if (_step0Finger != null)
        {
            RectTransform fingerTarget = FindTargetByName("sort (0)");
            _step0Finger.PointAt(fingerTarget != null ? fingerTarget : highlights[0]);
        }

        // 드래그 화살표: 출발/도착 슬롯이 둘 다 지정됐을 때만
        RectTransform dragFrom = FindTargetByName(_step0DragFromName);
        RectTransform dragTo = FindTargetByName(_step0DragToName);
        if (dragFrom != null && dragTo != null)
        {
            _step0DragArrow = transform.root.GetComponentInChildren<TutorialDragArrow>(true);
            if (_step0DragArrow != null) _step0DragArrow.ShowDrag(dragFrom, dragTo);
        }
    }

    // step0 커스텀 보드: 보드를 비우고 강조 슬롯들에 같은 유닛을 깔아 한 수면 정렬되게 한다.
    private void HandleDragStarted(int tableIdx, int slotIdx)
    {
        if (!_active || _step0DragArrowHidden) return;

        var tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (step == null || step.stepId != 0) return;
        if (!MatchesConfiguredDragFrom(tableIdx, slotIdx)) return;

        ResumeGameplayAfterGuide();
        if (_step0DragArrow != null) _step0DragArrow.Hide();
        if (_step0Finger != null) _step0Finger.Hide();
        _step0DragArrowHidden = true;
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
