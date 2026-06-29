// 담당자 : 정승우(골격/매니저 연결) + 홍정옥(겉모습 구현)
// 설명   : 각 씬(_Lobby/_InGame)에 1개씩 두는 튜토리얼 오버레이.
//          로드되면 TutorialManager에 자기를 등록하고, 매니저가 "이 단계 그려라"(ShowStep) 하면 화면에 표시한다.
//          ★실제 보이는 부분(어두운 막/강조/말풍선)은 아래 [홍정옥 작성] 구간을 채우면 됨. 매니저 연결은 이미 돼 있음.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialView : MonoBehaviour, ITutorialView
{
    public event Action OnStepActionCompleted;

    [Header("UI (홍정옥)")]
    [Tooltip("화면을 어둡게 덮는 마스크 루트. 단계 표시 중에만 켜짐.")]
    [SerializeField] private GameObject _overlayRoot;
    [Tooltip("말풍선 텍스트(TMP 등). 홍정옥이 연결.")]
    [SerializeField] private TMPro.TextMeshProUGUI _guideText;

    [Tooltip("딤 마스크")]
    [SerializeField] private TutorialDimMask _dimMask;
    [Tooltip("손가락 가이드")]
    [SerializeField] private TutorialFingerGuide _fingerGuide;
    [Tooltip("로비 게이트(로비 씬만)")]
    [SerializeField] private TutorialLobbyGate _lobbyGate;

    [System.Serializable]
    private struct HighlightTarget
    {
        public string id;
        public RectTransform target;
    }

    [Tooltip("강조 대상 매핑 (id → RectTransform)")]
    [SerializeField] private List<HighlightTarget> _highlightTargets = new();

    private TutorialStep _currentStep;
    private Button _tappedButton;
    private string _pendingId;
    private bool _highlightApplied;

    private void Start()
    {
        // 씬 로드 시 매니저에 자기 등록(매니저가 현재 단계를 판단해 ShowStep/Hide 호출).
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.RegisterView(this);
        else
            Hide(); // 매니저 없으면(튜토리얼 미사용 빌드/테스트 씬) 그냥 숨김
    }

    private void OnDestroy()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.UnregisterView(this);
    }

    // ===== ITutorialView : 매니저 -> 뷰 =====

    public void ShowStep(TutorialStep step)
    {
        _currentStep = step;
        if (_overlayRoot != null) _overlayRoot.SetActive(true);
        if (_guideText != null) _guideText.text = step != null ? step.guideText : string.Empty;

        // ----- [홍정옥 작성] -----
        ApplyLobbyPhase(step);   // phase 먼저 — 버튼 해금이 대상 활성화로 이어질 수 있음
        UnbindTap();
        if (_dimMask != null) _dimMask.Hide();
        if (_fingerGuide != null) _fingerGuide.Hide();

        _pendingId = step != null ? step.highlightTargetId : null;
        _highlightApplied = false;
        TryApplyHighlight();
        // -------------------------
    }

    public void Hide()
    {
        _currentStep = null;
        if (_overlayRoot != null) _overlayRoot.SetActive(false);

        // ----- [홍정옥 작성] -----
        _pendingId = null;
        _highlightApplied = false;
        if (_dimMask != null) _dimMask.Hide();
        if (_fingerGuide != null) _fingerGuide.Hide();
        UnbindTap();
        // -------------------------
    }

    // ===== 뷰 -> 매니저 =====

    /// <summary>강조 영역을 탭했을 때 호출(버튼 OnClick 또는 홍정옥의 탭 처리에서). 다음 단계로 넘어간다.</summary>
    public void OnHighlightTapped()
    {
        // TapHighlight 단계만 탭으로 진행. GameAction 단계는 게임 이벤트 훅이 TutorialManager.AdvanceStep을 호출.
        if (_currentStep != null && _currentStep.advanceMode == TutorialAdvanceMode.TapHighlight)
            OnStepActionCompleted?.Invoke();
    }

    // ===== [홍정옥] 내부 =====

    private RectTransform ResolveTarget(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (HighlightTarget t in _highlightTargets)
            if (t.id == id && t.target != null && t.target.gameObject.activeInHierarchy)
                return t.target;
        return null;
    }

    // 대상이 활성될 때까지 매 프레임 재시도해 강조를 적용한다 (페이지 전환 타이밍 대응)
    private void Update()
    {
        if (_currentStep == null || _highlightApplied) return;
        TryApplyHighlight();
    }

    private void TryApplyHighlight()
    {
        if (string.IsNullOrEmpty(_pendingId)) { _highlightApplied = true; return; }   // 대상 없는 단계는 안내문구만

        RectTransform target = ResolveTarget(_pendingId);
        if (target == null) return;   // 아직 비활성 → 다음 프레임 재시도

        if (!_currentStep.noDim && _dimMask != null) _dimMask.ShowWithHole(target);
        if (_fingerGuide != null) _fingerGuide.PointAt(target);
        BindTap(_currentStep, target);
        _highlightApplied = true;
    }

    // TapHighlight 단계는 강조 대상 버튼을 누르면 진행한다
    private void BindTap(TutorialStep step, RectTransform target)
    {
        UnbindTap();
        if (step == null || step.advanceMode != TutorialAdvanceMode.TapHighlight || target == null) return;
        _tappedButton = target.GetComponent<Button>();
        if (_tappedButton != null) _tappedButton.onClick.AddListener(OnHighlightTapped);
    }

    private void UnbindTap()
    {
        if (_tappedButton != null) _tappedButton.onClick.RemoveListener(OnHighlightTapped);
        _tappedButton = null;
    }

    // 강조하는 버튼이 풀려 있도록 단계에 맞춰 로비 해금 phase를 적용한다
    private void ApplyLobbyPhase(TutorialStep step)
    {
        if (_lobbyGate == null || step == null || step.scene != TutorialScene.Lobby) return;
        int phase = step.stepId >= 6 ? 2 : 1;   // 4~5: 성장/전투, 6~9: 상점 해제
        _lobbyGate.SetPhase(phase);
    }
}
