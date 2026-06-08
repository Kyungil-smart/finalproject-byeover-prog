//담당자: 조규민
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 카테고리 팝업 UI 표시와 벽지 슬롯 선택 이벤트 전달을 담당한다.
/// </summary>
public class HousingCategoryPopupView : MonoBehaviour
{
    [Header("팝업 UI")]
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Button _closeButton;

    [Header("벽지 슬롯 UI")]
    [SerializeField] private GameObject _wallpaperContentRoot;
    [SerializeField] private HousingWallpaperSlotView[] _wallpaperSlots;

    public event Action<int, Color> WallpaperSelected;

    private Color[] _currentWallpaperColors;
    private int _selectedWallpaperIndex;

    private void Awake()
    {
        ResolveWallpaperSlots();
        ValidateReferences();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        BindWallpaperSlots();
        Hide();
    }

    private void OnDisable()
    {
        UnbindWallpaperSlots();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(Hide);
    }

    public void ShowWallpaper(string _title, Color[] _wallpaperColors, int _selectedIndex)
    {
        SetTitle(_title);
        SetWallpaperContentActive(true);

        _currentWallpaperColors = _wallpaperColors;
        _selectedWallpaperIndex = Mathf.Clamp(_selectedIndex, 0, GetWallpaperCount() - 1);

        RefreshWallpaperSlots();
        ShowRoot();
    }

    public void ShowEmpty(string _title)
    {
        SetTitle(_title);
        SetWallpaperContentActive(false);
        ShowRoot();
    }

    public void Hide()
    {
        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }

    private void ResolveWallpaperSlots()
    {
        if (_wallpaperSlots != null && _wallpaperSlots.Length > 0)
            return;

        if (_wallpaperContentRoot == null)
            return;

        _wallpaperSlots = _wallpaperContentRoot.GetComponentsInChildren<HousingWallpaperSlotView>(true);
    }

    private void BindWallpaperSlots()
    {
        ResolveWallpaperSlots();

        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            _slot.Clicked -= HandleWallpaperSlotClicked;
            _slot.Clicked += HandleWallpaperSlotClicked;
        }
    }

    private void UnbindWallpaperSlots()
    {
        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            _slot.Clicked -= HandleWallpaperSlotClicked;
        }
    }

    private void RefreshWallpaperSlots()
    {
        ResolveWallpaperSlots();

        if (_wallpaperSlots == null)
            return;

        for (int _index = 0; _index < _wallpaperSlots.Length; _index++)
        {
            HousingWallpaperSlotView _slot = _wallpaperSlots[_index];
            if (_slot == null)
                continue;

            bool _hasData = _currentWallpaperColors != null && _index < _currentWallpaperColors.Length;
            _slot.gameObject.SetActive(_hasData);

            if (!_hasData)
                continue;

            _slot.SetData(_index, _currentWallpaperColors[_index], _index == _selectedWallpaperIndex);
        }
    }

    private void HandleWallpaperSlotClicked(int _slotIndex)
    {
        if (_currentWallpaperColors == null)
            return;

        if (_slotIndex < 0 || _slotIndex >= _currentWallpaperColors.Length)
            return;

        _selectedWallpaperIndex = _slotIndex;
        RefreshWallpaperSlots();
        WallpaperSelected?.Invoke(_slotIndex, _currentWallpaperColors[_slotIndex]);
    }

    private int GetWallpaperCount()
    {
        if (_currentWallpaperColors == null)
            return 0;

        return _currentWallpaperColors.Length;
    }

    private void SetTitle(string _title)
    {
        if (_titleText != null)
            _titleText.text = _title;
    }

    private void SetWallpaperContentActive(bool _isActive)
    {
        if (_wallpaperContentRoot != null)
            _wallpaperContentRoot.SetActive(_isActive);
    }

    private void ShowRoot()
    {
        if (_popupRoot != null)
            _popupRoot.SetActive(true);
    }

    private void ValidateReferences()
    {
        if (_popupRoot == null)
            Debug.LogWarning("[HousingCategoryPopupView] 팝업 루트가 연결되지 않았습니다.", this);

        if (_titleText == null)
            Debug.LogWarning("[HousingCategoryPopupView] 제목 텍스트가 연결되지 않았습니다.", this);

        if (_closeButton == null)
            Debug.LogWarning("[HousingCategoryPopupView] 닫기 버튼이 연결되지 않았습니다.", this);

        if (_wallpaperContentRoot == null)
            Debug.LogWarning("[HousingCategoryPopupView] 벽지 슬롯 Content가 연결되지 않았습니다.", this);

        if (_wallpaperSlots == null || _wallpaperSlots.Length == 0)
            Debug.LogWarning("[HousingCategoryPopupView] 벽지 슬롯이 연결되지 않았습니다.", this);
    }
}
