// 담당자 : 홍정옥
// 설명   : 설정 팝업 내 탭 버튼 클릭 시 페이지 전환 및 탭 선택 강조

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPageController : MonoBehaviour
{
    [Serializable]
    public class SettingTabEntry
    {
        [Header("탭 설정")]
        public string tabName;          // 에디터 식별용 이름
        public Button tabButton;        // 탭 버튼
        public GameObject page;         // 연결된 페이지 패널

        [Header("탭 비주얼")]
        [Tooltip("선택된 탭 이미지 (없으면 Button의 Image 사용)")]
        public Image tabImage;
    }

    [Header("탭 목록")]
    [SerializeField] private List<SettingTabEntry> _tabs = new();

    [Header("기본 탭 인덱스")]
    [SerializeField] private int _defaultTabIndex = 0;

    [Header("탭 색상")]
    [SerializeField] private Color _selectedColor   = new Color(1f,   0.85f, 0.45f, 1f);
    [SerializeField] private Color _deselectedColor = new Color(0.75f, 0.65f, 0.5f,  1f);

    private int _currentIndex = -1;

    // ------------------------------------------------------------------
    private void Awake()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            var entry = _tabs[i];
            if (entry == null || entry.tabButton == null) continue;

            // tabImage 미지정 시 Button의 Image로 대체
            if (entry.tabImage == null)
                entry.tabImage = entry.tabButton.GetComponent<Image>();

            int captured = i;
            entry.tabButton.onClick.RemoveAllListeners();
            entry.tabButton.onClick.AddListener(() => ShowTab(captured));
        }
    }

    private void OnEnable()
    {
        // 팝업이 열릴 때마다 기본 탭으로 초기화
        ShowTab(_defaultTabIndex);
    }

    // ------------------------------------------------------------------
    public void ShowTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        if (_currentIndex == index) return;

        _currentIndex = index;

        for (int i = 0; i < _tabs.Count; i++)
        {
            var entry = _tabs[i];
            if (entry == null) continue;

            bool isSelected = (i == index);

            // 페이지 표시/숨김
            if (entry.page != null)
                entry.page.SetActive(isSelected);

            // 탭 색 강조
            if (entry.tabImage != null)
                entry.tabImage.color = isSelected ? _selectedColor : _deselectedColor;
        }
    }
}
