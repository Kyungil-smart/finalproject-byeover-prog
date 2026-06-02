using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//작성자 : 홍정옥
//설명 : 아래 버튼 클릭시 페이지 전환 + 선택 인디케이터 및 아이콘 애니메이션

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
        [Tooltip("위아래로 이동할 아이콘 RectTransform (Icon_Main)")]
        public RectTransform icon;
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

    // 각 아이콘의 기본 anchoredPosition.y 저장
    private readonly Dictionary<LobbyPageType, float> _iconBaseY = new();

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
        ApplyTabVisual(defaultPage, instant: true);
        ShowPage(defaultPage);
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

            _iconBaseY[entry.pageType] = entry.icon.anchoredPosition.y;
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

            // ② 아이콘 Y 이동
            if (entry.icon != null && _iconBaseY.TryGetValue(entry.pageType, out float baseY))
            {
                float targetY = isSelected ? baseY + _iconUpOffset : baseY;

                entry.icon.DOKill();

                if (instant)
                {
                    entry.icon.anchoredPosition = new Vector2(
                        entry.icon.anchoredPosition.x, targetY);
                }
                else
                {
                    entry.icon.DOAnchorPosY(targetY, _iconAnimDuration)
                        .SetEase(_iconEase)
                        .SetUpdate(true);
                }
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
    }

    public void ShowDefaultPage()
    {
        ShowPage(defaultPage);
    }
}