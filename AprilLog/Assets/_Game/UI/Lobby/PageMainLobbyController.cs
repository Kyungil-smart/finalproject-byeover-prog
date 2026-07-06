// 담당자 : 홍정옥
// 설명   : Page_MainLobby — 이전/다음 버튼으로 챕터 슬롯 슬라이드 전환
//          - ChapterTestDataSO에서 데이터 읽기
//          - DOTween으로 카드 슬라이드 인/아웃 연출
//          - 첫 번째/마지막 챕터면 이동 불가 버튼 숨김

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PageMainLobbyController : MonoBehaviour
{
    // ------------------------------------------------------------------
    [Header("데이터")]
    [SerializeField] private ChapterDataSO _data;

    [Header("슬롯")]
    [Tooltip("슬라이드 애니메이션할 챕터 카드의 RectTransform")]
    [SerializeField] private RectTransform _slotRect;
    [Tooltip("슬롯 카드에 붙어있는 ChapterSlotUI")]
    [SerializeField] private ChapterSlotUI _slotUI;

    [Header("버튼")]
    [SerializeField] private Button _btnPrev;
    [SerializeField] private Button _btnNext;
    [SerializeField] private Button _btnStart;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideWidth    = 1440f;
    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private Ease  _slideEase     = Ease.OutCubic;

    // ------------------------------------------------------------------
    private int  _currentIndex;
    private bool _isAnimating;
    public event Action<int> OnGameStart; 

    // ------------------------------------------------------------------
    private void Awake()
    {
        if (_btnPrev != null) _btnPrev.onClick.AddListener(OnPrevClicked);
        else Debug.LogWarning("[PageMainLobbyController] Btn_Prev 미연결", this);

        if (_btnNext != null) _btnNext.onClick.AddListener(OnNextClicked);
        else Debug.LogWarning("[PageMainLobbyController] Btn_Next 미연결", this);
        
        if (_btnStart != null) _btnStart.onClick.AddListener(OnStartClicked);
        else Debug.LogWarning("[PageMainLobbyController] Btn_Start 미연결", this);
        
        if (_data == null) _data = ScriptableObject.CreateInstance<ChapterDataSO>();
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

    private void Start()
    {
        _data.InitChapters();
        ShowChapter(0, instant: true);
    }

    // ------------------------------------------------------------------
    private void OnPrevClicked() => Navigate(-1);
    private void OnNextClicked() => Navigate(+1);
    private void OnStartClicked() => GameStart(_currentIndex);

    private void GameStart(int index)
    {
        OnGameStart?.Invoke(index);
    }

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
        bool hasPrev = _currentIndex > 0;
        bool hasNext = _data != null && _currentIndex < _data.ChapterCount - 1;

        // 첫 챕터면 이전 숨김, 마지막 챕터면 다음 숨김 (그 외엔 보이고 활성)
        SetButtonVisible(_btnPrev, hasPrev);
        SetButtonVisible(_btnNext, hasNext);
        
        // 클라우드 세이브의 맵 해금 상태를 확인하여 현재 스타트 버튼의 활성화 여부를 제어한다.
        SwitchStartButtonInteractable();
    }

    private void SetButtonsInteractable(bool value)
    {
        if (!value)
        {
            if (_btnPrev != null) _btnPrev.interactable = false;
            if (_btnNext != null) _btnNext.interactable = false;
        }
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button == null) return;
        button.gameObject.SetActive(visible);
        button.interactable = visible;
    }
    
    private void SetStartButtonInteractable(bool value)
    {
        if(_btnStart != null) _btnStart.interactable = value;
    }

    private bool GetChapterUnLock(int index)
    {
        if(GameManager.Instance == null)
        {
            Debug.LogWarning("[PageMainLobbyController] 게임 매니저 미 감지. 임시로 전 스테이지 해금.");
            return true;
        }
        
        if (GameManager.Instance.CloudData == null)
        {
            Debug.LogError("[PageMainLobbyController]클라우드 데이터를 찾을 수 없습니다. 클라우드 데이터를 확인해주세요");
            return false;
        }

        var repo = DataManager.Instance.StageRepo;
        int chapterId = repo.GetChapterIdByIndex(index);
        if (chapterId == -1)
        {
            Debug.LogError($"[PageMainLobbyController]잘못된 인덱스로 접근 {index}. 인덱스를 확인해주세요.");
            return false;
        }
        int stageId = repo.GetStageId(chapterId, 1);
        
        if (stageId != -1) return GameManager.Instance.CloudData.unlockedStages.Contains(stageId);
        
        Debug.LogError($"[PageMainLobbyController]잘못된 인덱스로 접근 {index}. 인덱스를 확인해주세요.");
        return false;
    }

    private void SwitchStartButtonInteractable()
    {
       SetStartButtonInteractable(GetChapterUnLock(_currentIndex));
    }
}
