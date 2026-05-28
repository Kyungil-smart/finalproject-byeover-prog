using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//작성자 : 홍정옥
//설명 : 아래 버튼 클릭시 페이지 전환

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
    }

    [Header("기본 페이지")]
    [SerializeField] private LobbyPageType defaultPage = LobbyPageType.Main;

    [Header("페이지 목록")]
    [SerializeField] private List<LobbyPageEntry> pages = new();

    private readonly Dictionary<LobbyPageType, GameObject> _pageMap = new();

    private void Awake()
    {
        InitializePageMap();
        BindButtons();
    }

    private void Start()
    {
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

    private void BindButtons()
    {
        foreach (var entry in pages)
        {
            if (entry == null || entry.button == null)
                continue;

            LobbyPageType capturedPageType = entry.pageType;

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => ShowPage(capturedPageType));
        }
    }

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