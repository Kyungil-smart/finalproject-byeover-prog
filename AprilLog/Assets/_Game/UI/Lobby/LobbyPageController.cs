using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//작성자 : 홍정옥
//설명 : 아래 버튼 클릭시 페이지 전환 + 선택 인디케이터 및 아이콘 애니메이션

// 1차 수정자 : 조규민
// 수정 내용 : 시나리오 다시보기 종료 후 로비 복귀 시 하우징 페이지로 돌아오도록 초기 페이지 예약 처리 추가

public class LobbyPageController : MonoBehaviour
{
    [Serializable]
    public class LobbyPageEntry
    {
        [Header("페이지 설정")]
        public LobbyPageType pageType;
        public GameObject pageObject;

        [Header("버튼 설정")]
        public Button button;

        [Header("하단 탭 비주얼")]
        [Tooltip("선택 상태를 나타내는 인디케이터 오브젝트 (BottomBtn_Select)")]
        public GameObject selectIndicator;
        [Tooltip("이동시킬 아이콘 RectTransform (Icon_Main)")]
        public RectTransform icon;

        [Tooltip("선택 시 아이콘이 이동할 목표 위치(빈 RectTransform 마커). 비우면 기본 '위로 이동' 사용")]
        public RectTransform selectedIconTarget;

        [Tooltip("선택 시 켜질 라벨/텍스트 오브젝트 (원하는 위치에 미리 배치)")]
        public GameObject selectedLabel;
    }

    [Header("기본 페이지")]
    [SerializeField] private LobbyPageType defaultPage = LobbyPageType.Main;

    [Header("페이지 목록")]
    [SerializeField] private List<LobbyPageEntry> pages = new();

    [Header("아이콘 애니메이션")]
    [Tooltip("선택 시 아이콘이 올라가는 Y 거리 (px)")]
    [SerializeField] private float _iconUpOffset = 30f;
    [Tooltip("아이콘 이동 애니메이션 시간")]
    [SerializeField] private float _iconAnimDuration = 0.25f;
    [Tooltip("아이콘 이동 Ease")]
    [SerializeField] private Ease _iconEase = Ease.OutBack;

    private readonly Dictionary<LobbyPageType, GameObject> _pageMap = new();

    // 각 아이콘의 기본 anchoredPosition 저장 (선택 해제 시 복귀 위치)
    private readonly Dictionary<LobbyPageType, Vector2> _iconBasePos = new();

    private LobbyPageType _currentPage;

    private void Awake()
    {
        InitializePageMap();
        CacheIconBasePositions();
        BindButtons();
    }

    private void Start()
    {
        // 인디케이터·아이콘 초기 상태를 즉시 적용 (애니메이션 없이)
        LobbyPageType initialPage = ResolveInitialPage();
        ApplyTabVisual(initialPage, instant: true);
        ShowPage(initialPage);
    }

    private LobbyPageType ResolveInitialPage()
    {
        // 추가:조규민 기능 설명: 다른 씬에서 _Lobby로 복귀하며 예약한 페이지가 있으면 초기 페이지로 사용한다.
        if (LobbyReturnContext.TryConsumePage(out LobbyPageType pendingPage))
            return pendingPage;

        return defaultPage;
    }

    private void InitializePageMap()
    {
        _pageMap.Clear();

        foreach (var entry in pages)
        {
            if (entry == null)
                continue;

            if (entry.pageObject == null)
            {
                Debug.LogWarning($"[LobbyPageController] {entry.pageType} 페이지 오브젝트가 비어있습니다.", this);
                continue;
            }

            if (_pageMap.ContainsKey(entry.pageType))
            {
                Debug.LogWarning($"[LobbyPageController] {entry.pageType} 페이지가 중복 등록되었습니다.", this);
                continue;
            }

            _pageMap.Add(entry.pageType, entry.pageObject);
        }
    }

    private void CacheIconBasePositions()
    {
        foreach (var entry in pages)
        {
            if (entry == null || entry.icon == null)
                continue;

            _iconBasePos[entry.pageType] = entry.icon.anchoredPosition;
        }
    }

    private void BindButtons()
    {
        foreach (var entry in pages)
        {
            if (entry == null || entry.button == null)
                continue;

            LobbyPageType capturedPageType = entry.pageType;

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() =>
            {
                if (_currentPage == capturedPageType)
                    return;

                ApplyTabVisual(capturedPageType, instant: false);
                ShowPage(capturedPageType);
            });
        }
    }

    // ------------------------------------------------------------------
    // 탭 비주얼 (인디케이터 + 아이콘) 적용
    // ------------------------------------------------------------------
    private void ApplyTabVisual(LobbyPageType targetPage, bool instant)
    {
        _currentPage = targetPage;

        foreach (var entry in pages)
        {
            if (entry == null)
                continue;

            bool isSelected = entry.pageType == targetPage;

            // ① BottomBtn_Select 표시/숨김
            if (entry.selectIndicator != null)
                entry.selectIndicator.SetActive(isSelected);

            // ② 선택 라벨/텍스트 켜기/끄기
            if (entry.selectedLabel != null)
                entry.selectedLabel.SetActive(isSelected);

            // ③ 아이콘 이동 (선택 시 지정 위치, 해제 시 원위치)
            if (entry.icon != null && _iconBasePos.TryGetValue(entry.pageType, out Vector2 basePos))
            {
                Vector2 targetPos;
                if (isSelected)
                {
                    // 목표 마커가 있으면 그 위치, 없으면 기본(위로 이동)
                    targetPos = entry.selectedIconTarget != null
                        ? entry.selectedIconTarget.anchoredPosition
                        : basePos + new Vector2(0f, _iconUpOffset);
                }
                else
                {
                    targetPos = basePos;
                }

                entry.icon.DOKill();

                if (instant)
                    entry.icon.anchoredPosition = targetPos;
                else
                    entry.icon.DOAnchorPos(targetPos, _iconAnimDuration)
                        .SetEase(_iconEase)
                        .SetUpdate(true);
            }
        }
    }

    // ------------------------------------------------------------------
    // 페이지 표시
    // ------------------------------------------------------------------
    public void ShowPage(LobbyPageType pageType)
    {
        foreach (var pair in _pageMap)
        {
            bool isTargetPage = pair.Key == pageType;
            pair.Value.SetActive(isTargetPage);
        }

        // SFX 가이드 하우징 1: 하우징 화면 입장 시 BGM 교체, 나가면 로비 BGM 복귀. (같은 곡이면 무시되므로 매 호출 안전)
        AudioManager.Bgm(pageType == LobbyPageType.Housing ? SfxId.HousingBgm : SfxId.LobbyBgm);
    }

    public void ShowDefaultPage()
    {
        ShowPage(defaultPage);
    }
}
