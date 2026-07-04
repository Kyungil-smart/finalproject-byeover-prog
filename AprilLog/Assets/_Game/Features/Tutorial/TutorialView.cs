// 담당자 : 정승우(골격/매니저 연결) + 홍정옥(겉모습 구현)
// 설명   : 각 씬(_Lobby/_InGame)에 1개씩 두는 튜토리얼 오버레이.
//          로드되면 TutorialManager에 자신을 등록하고, 매니저가 ShowStep을 호출하면 현재 튜토리얼 단계를 화면에 표시한다.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialView : MonoBehaviour, ITutorialView
{
    public event Action OnStepActionCompleted;

    [Header("튜토리얼 UI")]
    [Tooltip("화면을 어둡게 덮는 오버레이 루트입니다. 튜토리얼 단계 표시 중에만 활성화됩니다.")]
    [SerializeField]
    private GameObject _overlayRoot;

    [Tooltip("현재 튜토리얼 안내 문구를 표시하는 TMP 텍스트입니다.")]
    [SerializeField]
    private TextMeshProUGUI _guideText;

    [Tooltip("강조 대상 이외 영역을 어둡게 가리는 딤 마스크입니다.")]
    [SerializeField]
    private TutorialDimMask _dimMask;

    [Tooltip("강조 대상을 가리키는 손가락 가이드입니다.")]
    [SerializeField]
    private TutorialFingerGuide _fingerGuide;

    [Tooltip("로비 튜토리얼 단계에 따라 버튼 잠금을 제어하는 로비 게이트입니다.")]
    [SerializeField]
    private TutorialLobbyGate _lobbyGate;

    [Header("강조 대상 매핑")]
    [Tooltip("TutorialStep의 highlightTargetId와 실제 RectTransform을 연결합니다.")]
    [SerializeField]
    private List<HighlightTarget> _highlightTargets = new();

    [Header("디버그")]
    [Tooltip("활성화하면 튜토리얼 대상 탐색과 버튼 바인딩 상태를 콘솔에 출력합니다.")]
    [SerializeField]
    private bool _showDebugLog = true;

    [Serializable]
    private struct HighlightTarget
    {
        [Tooltip("TutorialStepData에 입력한 highlightTargetId입니다.")]
        public string id;

        [Tooltip("화면에서 강조할 UI 대상입니다. 가능하면 실제 Button 루트를 연결하세요.")]
        public RectTransform target;
    }

    private TutorialStep _currentStep;
    private Button _tappedButton;

    private string _pendingId;
    private bool _highlightApplied;

    // 동일한 실패 로그가 매 프레임 출력되는 것을 방지하기 위한 값
    private bool _targetMissingWarningPrinted;
    private bool _buttonBindingWarningPrinted;

    private void Start()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.RegisterView(this);

            LogDebug(
                $"튜토리얼 뷰 등록 완료. Scene={gameObject.scene.name}");

            // 튜토리얼을 이미 끝냈으면(정상 완료/스킵) 로비 버튼을 모두 해금한다.
            if (_lobbyGate != null && TutorialManager.Instance.IsCompleted)
            {
                _lobbyGate.UnlockAll();
            }
        }
        else
        {
            Debug.LogWarning(
                $"[TutorialView] TutorialManager가 없어 뷰를 숨깁니다. " +
                $"Scene={gameObject.scene.name}",
                this);

            Hide();
        }
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화될 때 기존 버튼 리스너가 남지 않도록 정리합니다.
        UnbindTap();
    }

    private void OnDestroy()
    {
        UnbindTap();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.UnregisterView(this);
        }
    }

    private void Update()
    {
        if (_currentStep == null)
        {
            return;
        }

        if (_highlightApplied)
        {
            return;
        }

        // 팝업 또는 버튼이 한 프레임 이상 늦게 활성화되는 상황에 대응합니다.
        TryApplyHighlight();
    }

    private static bool ShouldHideGuideTextForLobbyScenario(TutorialStep step)
    {
        return step != null
            && step.scene == TutorialScene.Lobby
            && TutorialLobbyDirector.HasScenarioForStep(step.stepId);
    }

    // =========================================================
    // ITutorialView : TutorialManager → TutorialView
    // =========================================================

    public void ShowStep(TutorialStep step)
    {
        _currentStep = step;

        ResetRuntimeState();

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[TutorialView] Overlay Root가 연결되지 않았습니다.",
                this);
        }

        if (_guideText != null)
        {
            _guideText.text = ShouldHideGuideTextForLobbyScenario(step)
                ? string.Empty
                : step != null
                    ? step.guideText
                    : string.Empty;
        }

        if (step == null)
        {
            Debug.LogWarning(
                "[TutorialView] ShowStep에 null 단계가 전달되었습니다.",
                this);

            HideVisualElements();
            return;
        }

        LogDebug(
            $"단계 표시 시작. " +
            $"StepId={step.stepId}, " +
            $"Scene={step.scene}, " +
            $"AdvanceMode={step.advanceMode}, " +
            $"TargetId={step.highlightTargetId}, " +
            $"NoDim={step.noDim}");

        // 버튼이 잠겨 있으면 ResolveTarget에서 비활성 대상으로 판단될 수 있으므로
        // 강조 대상 탐색보다 먼저 로비 해금 단계를 적용합니다.
        ApplyLobbyPhase(step);

        HideVisualElements();

        _pendingId = step.highlightTargetId;
        _highlightApplied = false;

        TryApplyHighlight();
    }

    public void Hide()
    {
        LogDebug("튜토리얼 뷰 숨김.");

        _currentStep = null;

        ResetRuntimeState();
        HideVisualElements();

        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    // =========================================================
    // TutorialView → TutorialManager
    // =========================================================

    /// <summary>
    /// 강조 대상 버튼이 눌렸을 때 호출됩니다.
    /// TapHighlight 단계인 경우에만 다음 단계 진행 이벤트를 발생시킵니다.
    /// </summary>
    public void OnHighlightTapped()
    {
        if (_currentStep == null)
        {
            Debug.LogWarning(
                "[TutorialView] 현재 단계가 없는 상태에서 강조 대상 클릭이 호출되었습니다.",
                this);

            return;
        }

        if (_currentStep.advanceMode != TutorialAdvanceMode.TapHighlight)
        {
            LogDebug(
                $"강조 대상 클릭 무시. " +
                $"StepId={_currentStep.stepId}, " +
                $"AdvanceMode={_currentStep.advanceMode}");

            return;
        }

        LogDebug(
            $"강조 대상 클릭 완료. " +
            $"StepId={_currentStep.stepId}, " +
            $"TargetId={_currentStep.highlightTargetId}, " +
            $"Button={(_tappedButton != null ? _tappedButton.name : "NULL")}");

        OnStepActionCompleted?.Invoke();
    }

    // =========================================================
    // 강조 대상 처리
    // =========================================================

    private void TryApplyHighlight()
    {
        if (_currentStep == null)
        {
            return;
        }

        // 강조 대상이 없는 단계는 안내 문구만 표시하고 완료 처리합니다.
        if (string.IsNullOrWhiteSpace(_pendingId))
        {
            _highlightApplied = true;

            LogDebug(
                $"강조 대상이 없는 단계입니다. " +
                $"StepId={_currentStep.stepId}");

            return;
        }

        RectTransform target = ResolveTarget(_pendingId);

        if (target == null)
        {
            PrintTargetMissingWarningOnce();
            return;
        }

        // 대상이 활성화된 순간부터 이전 실패 경고 상태를 초기화합니다.
        _targetMissingWarningPrinted = false;

        if (!_currentStep.noDim)
        {
            if (_dimMask != null)
            {
                _dimMask.ShowWithHole(target);
            }
            else
            {
                Debug.LogWarning(
                    $"[TutorialView] DimMask가 연결되지 않았습니다. " +
                    $"StepId={_currentStep.stepId}, " +
                    $"TargetId={_pendingId}",
                    this);
            }
        }
        else if (_dimMask != null)
        {
            _dimMask.Hide();
        }

        if (_fingerGuide != null)
        {
            _fingerGuide.PointAt(target);
        }

        bool bindSucceeded = BindTap(_currentStep, target);

        /*
         * TapHighlight 단계에서 버튼 바인딩에 실패했는데
         * _highlightApplied를 true로 만들면 이후 재시도하지 않습니다.
         *
         * 따라서 TapHighlight 단계는 바인딩 성공 시에만 적용 완료로 처리합니다.
         * GameAction 단계는 버튼 바인딩이 필요 없으므로 즉시 완료 처리합니다.
         */
        if (_currentStep.advanceMode == TutorialAdvanceMode.TapHighlight)
        {
            _highlightApplied = bindSucceeded;
        }
        else
        {
            _highlightApplied = true;
        }

        if (_highlightApplied)
        {
            LogDebug(
                $"강조 적용 완료. " +
                $"StepId={_currentStep.stepId}, " +
                $"TargetId={_pendingId}, " +
                $"Target={target.name}");
        }
    }

    /// <summary>
    /// 등록된 highlightTargetId에 해당하는 활성화된 RectTransform을 반환합니다.
    /// </summary>
    private RectTransform ResolveTarget(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        for (int i = 0; i < _highlightTargets.Count; i++)
        {
            HighlightTarget item = _highlightTargets[i];

            if (!string.Equals(item.id, id, StringComparison.Ordinal))
            {
                continue;
            }

            if (item.target == null)
            {
                return null;
            }

            if (!item.target.gameObject.activeInHierarchy)
            {
                return null;
            }

            return item.target;
        }

        return null;
    }

    // =========================================================
    // 버튼 바인딩
    // =========================================================

    /// <summary>
    /// TapHighlight 단계의 강조 대상에서 실제 Button을 찾아
    /// OnHighlightTapped를 연결합니다.
    /// </summary>
    private bool BindTap(TutorialStep step, RectTransform target)
    {
        UnbindTap();

        if (step == null)
        {
            return false;
        }

        if (step.advanceMode != TutorialAdvanceMode.TapHighlight)
        {
            // GameAction 단계는 버튼 탭으로 진행하지 않습니다.
            return true;
        }

        if (target == null)
        {
            PrintButtonBindingWarningOnce(
                step,
                null,
                "강조 대상 RectTransform이 null입니다.");

            return false;
        }

        _tappedButton = FindButton(target);

        if (_tappedButton == null)
        {
            PrintButtonBindingWarningOnce(
                step,
                target,
                "강조 대상과 부모·자식에서 Button 컴포넌트를 찾지 못했습니다.");

            return false;
        }

        if (!_tappedButton.gameObject.activeInHierarchy)
        {
            PrintButtonBindingWarningOnce(
                step,
                target,
                $"찾은 버튼이 비활성 상태입니다. Button={_tappedButton.name}");

            _tappedButton = null;
            return false;
        }

        if (!_tappedButton.interactable)
        {
            /*
             * interactable=false여도 나중에 TutorialLobbyGate나 팝업 초기화가
             * 버튼을 활성화할 가능성이 있으므로 현재 프레임에서는 실패 처리합니다.
             * Update에서 다시 바인딩을 시도하게 됩니다.
             */
            PrintButtonBindingWarningOnce(
                step,
                target,
                $"찾은 버튼의 interactable이 false입니다. Button={_tappedButton.name}");

            _tappedButton = null;
            return false;
        }

        // 혹시 같은 리스너가 남아 있어도 중복 호출되지 않도록 제거 후 추가합니다.
        _tappedButton.onClick.RemoveListener(OnHighlightTapped);
        _tappedButton.onClick.AddListener(OnHighlightTapped);

        _buttonBindingWarningPrinted = false;

        LogDebug(
            $"버튼 바인딩 성공. " +
            $"StepId={step.stepId}, " +
            $"TargetId={step.highlightTargetId}, " +
            $"Target={target.name}, " +
            $"Button={_tappedButton.name}");

        return true;
    }

    /// <summary>
    /// 강조 RectTransform이 버튼 루트가 아닌 자식 이미지나 텍스트여도
    /// 실제 Button을 찾을 수 있도록 여러 방향으로 탐색합니다.
    /// </summary>
    private Button FindButton(RectTransform target)
    {
        if (target == null)
        {
            return null;
        }

        // 1. 강조 대상으로 연결된 오브젝트 자체
        Button button = target.GetComponent<Button>();

        if (button != null)
        {
            return button;
        }

        // 2. 강조 대상의 부모
        button = target.GetComponentInParent<Button>(true);

        if (button != null)
        {
            return button;
        }

        // 3. 강조 대상의 자식
        button = target.GetComponentInChildren<Button>(true);

        return button;
    }

    private void UnbindTap()
    {
        if (_tappedButton == null)
        {
            return;
        }

        _tappedButton.onClick.RemoveListener(OnHighlightTapped);

        LogDebug(
            $"버튼 바인딩 해제. Button={_tappedButton.name}");

        _tappedButton = null;
    }

    // =========================================================
    // 로비 버튼 해금
    // =========================================================

    /// <summary>
    /// 강조할 버튼이 잠겨 있지 않도록 튜토리얼 단계에 맞는 로비 phase를 적용합니다.
    /// </summary>
    private void ApplyLobbyPhase(TutorialStep step)
    {
        if (_lobbyGate == null)
        {
            return;
        }

        if (step == null)
        {
            return;
        }

        if (step.scene != TutorialScene.Lobby)
        {
            return;
        }

        // stepId 4~5 : 성장/전투 기능
        // stepId 6 이상 : 상점 등 추가 기능
        int phase = step.stepId >= 6 ? 2 : 1;

        _lobbyGate.SetPhase(phase);

        LogDebug(
            $"로비 게이트 적용. " +
            $"StepId={step.stepId}, " +
            $"Phase={phase}");
    }

    // =========================================================
    // 내부 상태 및 UI 정리
    // =========================================================

    private void ResetRuntimeState()
    {
        UnbindTap();

        _pendingId = null;
        _highlightApplied = false;

        _targetMissingWarningPrinted = false;
        _buttonBindingWarningPrinted = false;
    }

    private void HideVisualElements()
    {
        if (_dimMask != null)
        {
            _dimMask.Hide();
        }

        if (_fingerGuide != null)
        {
            _fingerGuide.Hide();
        }
    }

    // =========================================================
    // 진단 로그
    // =========================================================

    private void PrintTargetMissingWarningOnce()
    {
        if (_targetMissingWarningPrinted)
        {
            return;
        }

        _targetMissingWarningPrinted = true;

        bool idRegistered = false;
        bool targetAssigned = false;
        bool targetActive = false;
        string registeredTargetName = "NULL";

        for (int i = 0; i < _highlightTargets.Count; i++)
        {
            HighlightTarget item = _highlightTargets[i];

            if (!string.Equals(item.id, _pendingId, StringComparison.Ordinal))
            {
                continue;
            }

            idRegistered = true;
            targetAssigned = item.target != null;

            if (item.target != null)
            {
                targetActive = item.target.gameObject.activeInHierarchy;
                registeredTargetName = item.target.name;
            }

            break;
        }

        Debug.LogWarning(
            $"[TutorialView] 강조 대상을 아직 찾지 못했습니다.\n" +
            $"StepId={(_currentStep != null ? _currentStep.stepId : -1)}\n" +
            $"TargetId={_pendingId}\n" +
            $"ID 등록 여부={idRegistered}\n" +
            $"Target 연결 여부={targetAssigned}\n" +
            $"Target 활성 여부={targetActive}\n" +
            $"연결된 Target={registeredTargetName}\n" +
            $"TutorialView의 Highlight Targets 인스펙터 연결과 " +
            $"대상 GameObject의 활성 상태를 확인하세요.",
            this);
    }

    private void PrintButtonBindingWarningOnce(
        TutorialStep step,
        RectTransform target,
        string reason)
    {
        if (_buttonBindingWarningPrinted)
        {
            return;
        }

        _buttonBindingWarningPrinted = true;

        Debug.LogWarning(
            $"[TutorialView] 강조 대상 버튼 바인딩 실패.\n" +
            $"원인={reason}\n" +
            $"StepId={(step != null ? step.stepId : -1)}\n" +
            $"TargetId={(step != null ? step.highlightTargetId : "NULL")}\n" +
            $"Target={(target != null ? target.name : "NULL")}\n" +
            $"강조 대상에는 실제 Button 루트 또는 " +
            $"Button의 부모·자식 RectTransform을 연결해야 합니다.",
            target != null ? target : this);
    }

    private void LogDebug(string message)
    {
        if (!_showDebugLog)
        {
            return;
        }

        Debug.Log(
            $"[TutorialView] {message}",
            this);
    }
}
