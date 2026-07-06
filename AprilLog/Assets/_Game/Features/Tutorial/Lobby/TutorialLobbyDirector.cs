using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로비 아티팩트 튜토리얼(7-3-3)을 구동한다. 레벨5 강화→돌파→장착 흐름을 감지·연출한다.
public class TutorialLobbyDirector : MonoBehaviour
{
    [Header("튜토리얼 아티팩트(인장) Gear_ID")]
    [SerializeField] private int _sealGearId = 50001;

    [Header("시나리오 ID")]
    [SerializeField] private int _breakthroughScenarioStartId = 100068;
    [SerializeField] private int _breakthroughScenarioEndId = 100071;
    [SerializeField] private int _equipDoneScenarioStartId = 100072;
    [SerializeField] private int _equipDoneScenarioEndId = 100074;
    [SerializeField] private int _scenarioSourceGroupId = 3002;

    [Header("팝업/버튼 참조")]
    [SerializeField] private ArtifactDetailPopupPresenter _detailPopup;
    [SerializeField] private RectTransform _ascendButton;   // 돌파(=레벨업) 버튼
    [SerializeField] private RectTransform _equipButton;    // 장착 버튼
    [SerializeField] private string _ascendButtonLabel = "돌파";

    [Header("입력 잠금")]
    [Tooltip("시나리오 중 전체 입력을 막을 CanvasGroup(전체화면 블로커)")]
    [SerializeField] private CanvasGroup _inputBlocker;

    [Header("버블")]
    [SerializeField] private InGameTalkBubble _talkBubble;
    [SerializeField] private Vector2 _bubbleViewportPosition = new Vector2(0.5f, 0.72f);
    [SerializeField] private Vector2 _bubbleScreenOffset;

    private ArtifactManager _artifacts;
    private ScenarioDataDriver _scenarioDriver;
    private TutorialFingerGuide _finger;
    private TutorialDimMask _dimMask;
    private Button _ascendButtonComponent;
    private TMP_Text _ascendButtonText;

    private bool _active;
    private bool _level5Handled;
    private bool _ascendHandled;
    private bool _equipHandled;
    private bool _inventorySubscribed;
    private bool _isPlayingBubble;
    private int _lastKnownAscensionCount;
    private int _playedLobbyScenarioStepId = -1;
    private Coroutine _level5Routine;
    private Coroutine _equipRoutine;
    private Coroutine _stepScenarioRoutine;

    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo _linesField, _indexField, _isPlayingField, _finishedField;
    private static MethodInfo _subscribeMethod, _showMethod;

    private void Start()
    {
        TryActivate();
    }

    private void OnDestroy()
    {
        if (_artifacts != null && _inventorySubscribed)
            _artifacts.OnInventoryUpdated -= HandleInventoryUpdated;
        SetInputBlocked(false);
        HideGuide();
    }

    private void Update()
    {
        if (!TryActivate()) return;

        TryPlayLobbyStepScenario();

        if (!IsArtifactUpgradeStepActive())
            return;

        HandleInventoryUpdated();

        if (!_ascendHandled || _equipHandled) return;

        ArtifactInstance seal = FindSealArtifact();
        if (seal == null || !seal.IsEquipped) return;

        _equipHandled = true;
        HideGuide();
        if (_equipRoutine == null)
            _equipRoutine = StartCoroutine(HandleEquipCompleted());
    }

    private void ResolveSystems()
    {
        _artifacts = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (_detailPopup == null) _detailPopup = FindFirstObjectByType<ArtifactDetailPopupPresenter>(FindObjectsInactive.Include);
        if (_scenarioDriver == null) _scenarioDriver = FindFirstObjectByType<ScenarioDataDriver>(FindObjectsInactive.Include);
        if (_talkBubble == null) _talkBubble = FindFirstObjectByType<InGameTalkBubble>(FindObjectsInactive.Include);
        if (_talkBubble != null) _talkBubble.Hide();
        _finger = transform.root.GetComponentInChildren<TutorialFingerGuide>(true);
        _dimMask = transform.root.GetComponentInChildren<TutorialDimMask>(true);
        ResolveAscendButtonComponents();
    }

    private bool TryActivate()
    {
        var tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning)
            return false;

        if (!_active)
        {
            _active = true;
            ResolveSystems();
            SetInputBlocked(false);
        }

        if (_artifacts == null)
            _artifacts = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;

        if (_artifacts != null && !_inventorySubscribed)
        {
            _artifacts.OnInventoryUpdated += HandleInventoryUpdated;
            _inventorySubscribed = true;
            HandleInventoryUpdated();
        }

        return true;
    }

    private ArtifactInstance FindSealArtifact()
        => _artifacts != null ? _artifacts.MyArtifacts.Find(a => a != null && a.MasterId == _sealGearId) : null;

    private void SetInputBlocked(bool blocked)
    {
        if (_inputBlocker == null) return;
        _inputBlocker.blocksRaycasts = blocked;
        _inputBlocker.interactable = blocked;
    }

    private void HandleInventoryUpdated()
    {
        if (!_active) return;
        if (!IsArtifactUpgradeStepActive()) return;

        if (_artifacts == null)
            ResolveSystems();

        ArtifactInstance seal = FindSealArtifact();
        if (seal == null) return;

        if (!_level5Handled && seal.CurrentLevel >= 5)
        {
            _level5Handled = true;
            _lastKnownAscensionCount = seal.AscensionCount;
            if (_level5Routine == null)
                _level5Routine = StartCoroutine(HandleLevel5Reached());
            return;
        }

        if (_level5Handled && !_ascendHandled && seal.AscensionCount > _lastKnownAscensionCount)
        {
            _ascendHandled = true;
            _lastKnownAscensionCount = seal.AscensionCount;
            ShowEquipGuide();
        }
        else
        {
            _lastKnownAscensionCount = seal.AscensionCount;
        }
    }

    private static bool IsArtifactUpgradeStepActive()
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (step == null || step.scene != TutorialScene.Lobby)
            return false;

        return step.stepId == 12
            || string.Equals(step.highlightTargetId, "ArtifactLevelUpButton", StringComparison.Ordinal)
            || step.gameAction == TutorialGameAction.ArtifactEquip;
    }

    private IEnumerator HandleLevel5Reached()
    {
        HideGuide();
        SetAscendButtonState(false);
        SetInputBlocked(true);
        yield return PlayScenarioRange(_breakthroughScenarioStartId, _breakthroughScenarioEndId);
        GrantAscensionMaterial();
        SetInputBlocked(false);
        SetAscendButtonState(true);
        ShowAscendGuide();
        _level5Routine = null;
    }

    private IEnumerator HandleEquipCompleted()
    {
        if (_detailPopup != null)
            _detailPopup.Close();

        SetInputBlocked(true);
        yield return PlayScenarioRange(_equipDoneScenarioStartId, _equipDoneScenarioEndId);
        SetInputBlocked(false);

        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (tm != null && tm.IsRunning && step != null
            && step.advanceMode == TutorialAdvanceMode.GameAction
            && step.gameAction == TutorialGameAction.ArtifactEquip)
        {
            tm.AdvanceStep();
        }

        _equipRoutine = null;
    }

    private void GrantAscensionMaterial()
    {
        if (_artifacts == null)
            ResolveSystems();
        if (_artifacts == null || _sealGearId <= 0) return;

        ArtifactInstance seal = FindSealArtifact();
        if (seal == null) return;

        // 돌파는 같은 장비를 소모하는데, AddArtifact로 중복을 지급하면 소유 상한(MaxOwned-1)을
        // 넘겨 곧바로 자동 분해된다. 돌파에 필요한 여유분을 직접 확보한다.
        int required = ResolveAscensionCostAmount(seal);
        seal.CurrentCount = Mathf.Max(seal.CurrentCount, required + 1);

        if (_detailPopup != null)
            _detailPopup.RefreshCurrentArtifact();
    }

    private int ResolveAscensionCostAmount(ArtifactInstance seal)
    {
        GearMasterData master = seal != null ? seal.MasterData : null;
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        GearAscensionCostData cost = master != null && repo != null
            ? repo.GetAscensionCosts(master.GearGrade, "SameGear")
            : null;
        return cost != null ? Mathf.Max(1, cost.CostAmount) : 1;
    }

    private void ShowAscendGuide()
    {
        ResolveGuideIfNeeded();
        SetAscendButtonState(true);
        if (_dimMask != null) _dimMask.Hide();
        if (_finger != null) _finger.PointAt(_ascendButton);
    }

    private void ShowEquipGuide()
    {
        ResolveGuideIfNeeded();
        if (_dimMask != null) _dimMask.ShowWithHole(_equipButton);
        if (_finger != null) _finger.PointAt(_equipButton);
    }

    private void HideGuide()
    {
        if (_dimMask != null) _dimMask.Hide();
        if (_finger != null) _finger.Hide();
    }

    private void ResolveGuideIfNeeded()
    {
        if (_finger == null)
            _finger = transform.root.GetComponentInChildren<TutorialFingerGuide>(true);
        if (_dimMask == null)
            _dimMask = transform.root.GetComponentInChildren<TutorialDimMask>(true);
    }

    private void ResolveAscendButtonComponents()
    {
        if (_ascendButton == null) return;

        if (_ascendButtonComponent == null)
            _ascendButtonComponent = _ascendButton.GetComponent<Button>()
                ?? _ascendButton.GetComponentInParent<Button>(true)
                ?? _ascendButton.GetComponentInChildren<Button>(true);

        if (_ascendButtonText == null)
        {
            _ascendButtonText = _ascendButton.GetComponentInChildren<TMP_Text>(true);
            if (_ascendButtonText == null && _ascendButtonComponent != null)
                _ascendButtonText = _ascendButtonComponent.GetComponentInChildren<TMP_Text>(true);
            if (_ascendButtonText == null)
                _ascendButtonText = _ascendButton.GetComponentInParent<TMP_Text>(true);
            if (_ascendButtonText == null && _ascendButton.parent != null)
                _ascendButtonText = _ascendButton.parent.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void SetAscendButtonState(bool interactable)
    {
        ResolveAscendButtonComponents();

        if (_ascendButtonText != null)
            _ascendButtonText.text = _ascendButtonLabel;
        if (_ascendButtonComponent != null)
            _ascendButtonComponent.interactable = interactable;
    }

    private IEnumerator PlayScenarioRange(int startId, int endId)
    {
        List<Story_TalkData> lines = CollectTutorialScenarioLines(startId, endId);
        if (lines.Count == 0)
        {
            Debug.LogWarning($"[TutorialLobbyDirector] 튜토리얼 시나리오 ID {startId}~{endId} 대사를 찾지 못했습니다.");
            yield break;
        }

        yield return PlayBubbleLines(lines);
    }

    private void TryPlayLobbyStepScenario()
    {
        TutorialManager tm = TutorialManager.Instance;
        TutorialStep step = tm != null ? tm.CurrentStep : null;
        if (step == null || step.scene != TutorialScene.Lobby)
            return;
        if (_playedLobbyScenarioStepId == step.stepId)
            return;
        if (_isPlayingBubble || _stepScenarioRoutine != null || _level5Routine != null || _equipRoutine != null)
            return;

        if (TryGetLobbyStepScenarioRange(step.stepId, out int startId, out int endId))
        {
            _playedLobbyScenarioStepId = step.stepId;
            _stepScenarioRoutine = StartCoroutine(PlayLobbyStepScenario(startId, endId));
            return;
        }

        _playedLobbyScenarioStepId = step.stepId;
    }

    private IEnumerator PlayLobbyStepScenario(int startId, int endId)
    {
        yield return PlayScenarioRange(startId, endId);
        _stepScenarioRoutine = null;
    }

    private IEnumerator PlayLobbyStepGuideText(string text)
    {
        var line = new Story_TalkData { name_KR = string.Empty, Text_KR = text };
        yield return PlayBubbleLines(new List<Story_TalkData> { line });
        _stepScenarioRoutine = null;
    }

    public static bool HasScenarioForStep(int stepId)
        => TryGetLobbyStepScenarioRange(stepId, out _, out _);

    private static bool TryGetLobbyStepScenarioRange(int stepId, out int startId, out int endId)
    {
        switch (stepId)
        {
            case 6:
                startId = 100054;
                endId = 100058;
                return true;
            case 7:
                startId = 100059;
                endId = 100062;
                return true;
            case 8:
                startId = 100063;
                endId = 100064;
                return true;
            case 10:
                startId = 100065;
                endId = 100067;
                return true;
            default:
                startId = 0;
                endId = 0;
                return false;
        }
    }

    private IEnumerator PlayBubbleLines(List<Story_TalkData> sourceLines)
    {
        if (_talkBubble == null)
            _talkBubble = FindFirstObjectByType<InGameTalkBubble>(FindObjectsInactive.Include);
        if (_talkBubble == null)
        {
            Debug.LogWarning("[TutorialLobbyDirector] 로비 튜토리얼 버블을 찾지 못했습니다.");
            yield break;
        }

        _talkBubble.UseViewportPosition(_bubbleViewportPosition, _bubbleScreenOffset);
        _talkBubble.Bind(null, Camera.main);
        SetInputBlocked(false);
        _isPlayingBubble = true;

        foreach (Story_TalkData line in sourceLines)
        {
            if (line == null) continue;

            bool advanced = false;
            Action handleAdvance = () => advanced = true;
            _talkBubble.OnAdvanceRequested += handleAdvance;
            _talkBubble.PlayLine(line.name_KR, line.Text_KR);

            while (!advanced)
                yield return null;

            _talkBubble.OnAdvanceRequested -= handleAdvance;
        }

        _talkBubble.Hide();
        _talkBubble.UseAnchorPosition();
        _isPlayingBubble = false;
    }

    // 담당자 스크립트(StoryRepo/ScenarioDataDriver)를 수정하지 않기 위해,
    // 튜토리얼 전용으로 GroupID 3002 대사 목록에서 Talk ID 범위만 골라 ScenarioDataDriver에 주입한다.
    private bool TryPlayTutorialScenarioRange(int startId, int endId)
    {
        if (_scenarioDriver == null)
            _scenarioDriver = FindFirstObjectByType<ScenarioDataDriver>();

        if (_scenarioDriver == null)
        {
            Debug.LogWarning("[TutorialLobbyDirector] 시나리오 드라이버를 찾지 못했습니다.");
            return false;
        }

        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[TutorialLobbyDirector] StoryRepo를 찾지 못했습니다.");
            return false;
        }

        List<Story_TalkData> sourceLines = repo.GetTalkGroup(_scenarioSourceGroupId);
        List<Story_TalkData> rangeLines = CollectTutorialScenarioLines(sourceLines, startId, endId);
        if (rangeLines.Count == 0)
        {
            Debug.LogWarning($"[TutorialLobbyDirector] 튜토리얼 시나리오 ID {startId}~{endId} 대사를 찾지 못했습니다.");
            return false;
        }

        return TryInjectScenarioLines(_scenarioDriver, rangeLines);
    }

    private List<Story_TalkData> CollectTutorialScenarioLines(int startId, int endId)
    {
        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null)
        {
            Debug.LogWarning("[TutorialLobbyDirector] StoryRepo를 찾지 못했습니다.");
            return new List<Story_TalkData>();
        }

        List<Story_TalkData> sourceLines = repo.GetTalkGroup(_scenarioSourceGroupId);
        return CollectTutorialScenarioLines(sourceLines, startId, endId);
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
            _subscribeMethod.Invoke(driver, null);
            _linesField.SetValue(driver, lines);
            _indexField.SetValue(driver, 0);
            _finishedField.SetValue(driver, false);
            _isPlayingField.SetValue(driver, true);
            _showMethod.Invoke(driver, null);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialLobbyDirector] 튜토리얼 시나리오 범위 재생 준비 실패: {e.Message}");
            return false;
        }
    }

    private static bool EnsureScenarioDriverMembers()
    {
        Type type = typeof(ScenarioDataDriver);
        _linesField ??= type.GetField("_lines", Flags);
        _indexField ??= type.GetField("_index", Flags);
        _isPlayingField ??= type.GetField("_isPlaying", Flags);
        _finishedField ??= type.GetField("_finished", Flags);
        _subscribeMethod ??= type.GetMethod("Subscribe", Flags);
        _showMethod ??= type.GetMethod("Show", Flags);

        bool hasAllMembers = _linesField != null
            && _indexField != null
            && _isPlayingField != null
            && _finishedField != null
            && _subscribeMethod != null
            && _showMethod != null;

        if (!hasAllMembers)
            Debug.LogWarning("[TutorialLobbyDirector] ScenarioDataDriver 내부 재생 멤버를 찾지 못했습니다.");

        return hasAllMembers;
    }
}
