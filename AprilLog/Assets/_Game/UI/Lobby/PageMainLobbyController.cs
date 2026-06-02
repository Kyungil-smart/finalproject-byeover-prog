// 담당자 : 홍정옥
// 설명   : Page_MainLobby — 이전/다음 버튼으로 챕터 슬롯 슬라이드 전환
//          - ChapterTestDataSO에서 데이터 읽기
//          - DOTween으로 카드 슬라이드 인/아웃 연출
//          - 첫 번째 챕터면 이전 버튼 비활성화

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PageMainLobbyController : MonoBehaviour
{
    // ------------------------------------------------------------------
    [Header("데이터")]
    [SerializeField] private ChapterTestDataSO _data;

    [Header("슬롯")]
    [Tooltip("슬라이드 애니메이션할 챕터 카드의 RectTransform")]
    [SerializeField] private RectTransform _slotRect;
    [Tooltip("슬롯 카드에 붙어있는 ChapterSlotUI")]
    [SerializeField] private ChapterSlotUI _slotUI;

    [Header("버튼")]
    [SerializeField] private Button _btnPrev;
    [SerializeField] private Button _btnNext;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideWidth    = 1440f;
    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private Ease  _slideEase     = Ease.OutCubic;

    // ------------------------------------------------------------------
    private int  _currentIndex;
    private bool _isAnimating;

    // ------------------------------------------------------------------
    private void Awake()
    {
        if (_btnPrev != null) _btnPrev.onClick.AddListener(OnPrevClicked);
        else Debug.LogWarning("[PageMainLobbyController] Btn_Prev 미연결", this);

        if (_btnNext != null) _btnNext.onClick.AddListener(OnNextClicked);
        else Debug.LogWarning("[PageMainLobbyController] Btn_Next 미연결", this);
    }

    private void OnEnable()
    {
        if (_data == null)
        {
            Debug.LogWarning("[PageMainLobbyController] ChapterTestDataSO 미연결", this);
            return;
        }
        ShowChapter(0, instant: true);
    }

    // ------------------------------------------------------------------
    private void OnPrevClicked() => Navigate(-1);
    private void OnNextClicked() => Navigate(+1);

    private void Navigate(int direction)
    {
        if (_isAnimating) return;
        if (_data == null) return;

        int newIndex = _currentIndex + direction;
        if (newIndex < 0 || newIndex >= _data.ChapterCount) return;

        _isAnimating = true;

        float exitX  = direction > 0 ? -_slideWidth :  _slideWidth;
        float enterX = direction > 0 ?  _slideWidth : -_slideWidth;

        _slotRect.DOKill();
        SetButtonsInteractable(false);

        // 슬라이드 아웃
        _slotRect.DOAnchorPosX(exitX, _slideDuration)
            .SetEase(_slideEase)
            .OnComplete(() =>
            {
                _currentIndex = newIndex;
                UpdateSlotData();

                Vector2 pos = _slotRect.anchoredPosition;
                pos.x = enterX;
                _slotRect.anchoredPosition = pos;

                // 슬라이드 인
                _slotRect.DOAnchorPosX(0f, _slideDuration)
                    .SetEase(_slideEase)
                    .OnComplete(() =>
                    {
                        _isAnimating = false;
                        RefreshButtons();
                    });
            });
    }

    private void ShowChapter(int index, bool instant)
    {
        _currentIndex = Mathf.Clamp(index, 0, _data != null ? _data.ChapterCount - 1 : 0);
        UpdateSlotData();

        if (instant && _slotRect != null)
        {
            _slotRect.DOKill();
            Vector2 pos = _slotRect.anchoredPosition;
            pos.x = 0f;
            _slotRect.anchoredPosition = pos;
        }
        RefreshButtons();
    }

    private void UpdateSlotData()
    {
        if (_data == null || _slotUI == null) return;
        _slotUI.SetData(_data.GetChapter(_currentIndex), _currentIndex);
    }

    private void RefreshButtons()
    {
        SetButtonsInteractable(true);
        if (_btnPrev != null) _btnPrev.interactable = _currentIndex > 0;
        if (_btnNext != null) _btnNext.interactable = _data != null && _currentIndex < _data.ChapterCount - 1;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (!value)
        {
            if (_btnPrev != null) _btnPrev.interactable = false;
            if (_btnNext != null) _btnNext.interactable = false;
        }
    }
}
