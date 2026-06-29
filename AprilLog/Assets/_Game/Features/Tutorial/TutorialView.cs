// 담당자 : 정승우(골격/매니저 연결) + 홍정옥(겉모습 구현)
// 설명   : 각 씬(_Lobby/_InGame)에 1개씩 두는 튜토리얼 오버레이.
//          로드되면 TutorialManager에 자기를 등록하고, 매니저가 "이 단계 그려라"(ShowStep) 하면 화면에 표시한다.
//          ★실제 보이는 부분(어두운 막/강조/말풍선)은 아래 [홍정옥 작성] 구간을 채우면 됨. 매니저 연결은 이미 돼 있음.

using System;
using UnityEngine;

public class TutorialView : MonoBehaviour, ITutorialView
{
    public event Action OnStepActionCompleted;

    [Header("UI (홍정옥)")]
    [Tooltip("화면을 어둡게 덮는 마스크 루트. 단계 표시 중에만 켜짐.")]
    [SerializeField] private GameObject _overlayRoot;
    [Tooltip("말풍선 텍스트(TMP 등). 홍정옥이 연결.")]
    [SerializeField] private TMPro.TextMeshProUGUI _guideText;

    private TutorialStep _currentStep;

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
        // step.highlightTargetId 로 강조할 UI 요소를 찾아 그 영역만 밝게(마스크 구멍) 만들고,
        // step.guideText 말풍선을 그 근처에 배치. step.advanceMode 가 TapHighlight 면 그 영역 탭 받기.
        // 강조 대상 매핑(id -> RectTransform)은 씬별로 둘 수 있음.
        // -------------------------
    }

    public void Hide()
    {
        _currentStep = null;
        if (_overlayRoot != null) _overlayRoot.SetActive(false);

        // ----- [홍정옥 작성] -----
        // 강조/말풍선 정리.
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
}
