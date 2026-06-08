//담당자: 조규민
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 하우징 카테고리 버튼 입력을 받아 팝업 View 표시와 벽지 선택을 중계한다.
/// </summary>
public class HousingCategoryPopupPresenter : MonoBehaviour
{
    private const int WallpaperCategoryIndex = 0;

    [Serializable]
    private class CategoryPopupData
    {
        public string buttonName;
        public string title;
    }

    [Serializable]
    private class WallpaperColorData
    {
        public Color color;
    }

    [Header("팝업 View")]
    [SerializeField] private HousingCategoryPopupView _popupView;

    [Header("벽지 적용 대상")]
    [SerializeField] private Image _housingMoveAreaImage;

    private readonly CategoryPopupData[] _categories =
    {
        new CategoryPopupData
        {
            buttonName = "Btn_Wallpaper",
            title = "벽지"
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Large",
            title = "대형"
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Medium",
            title = "중형"
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Small",
            title = "소형"
        }
    };

    private readonly WallpaperColorData[] _wallpaperColors =
    {
        new WallpaperColorData { color = new Color(0.72f, 0.56f, 0.35f, 1f) },
        new WallpaperColorData { color = new Color(0.28f, 0.30f, 0.29f, 1f) },
        new WallpaperColorData { color = new Color(0.58f, 0.50f, 0.40f, 1f) },
        new WallpaperColorData { color = new Color(0.14f, 0.16f, 0.15f, 1f) },
        new WallpaperColorData { color = new Color(0.90f, 0.88f, 0.84f, 1f) },
        new WallpaperColorData { color = new Color(0.74f, 0.65f, 0.56f, 1f) },
        new WallpaperColorData { color = new Color(0.78f, 0.78f, 0.76f, 1f) },
        new WallpaperColorData { color = new Color(0.19f, 0.20f, 0.20f, 1f) }
    };

    private Button[] _categoryButtons;
    private UnityAction[] _categoryActions;
    private int _selectedWallpaperIndex;

    private void Awake()
    {
        ResolveView();
        ResolveHousingMoveArea();
        ResolveButtons();
        EnsureCategoryActions();
    }

    private void OnEnable()
    {
        BindButtons();
        BindPopupView();

        if (_popupView != null)
            _popupView.Hide();
    }

    private void OnDisable()
    {
        UnbindButtons();
        UnbindPopupView();

        if (_popupView != null)
            _popupView.Hide();
    }

    private void ResolveView()
    {
        if (_popupView != null)
            return;

        _popupView = GetComponent<HousingCategoryPopupView>();

        if (_popupView == null)
            Debug.LogWarning("[HousingCategoryPopupPresenter] 팝업 View가 연결되지 않았습니다.", this);
    }

    private void ResolveHousingMoveArea()
    {
        if (_housingMoveAreaImage != null)
            return;

        Transform _moveArea = transform.Find("Housing_MoveArea");
        if (_moveArea != null)
            _housingMoveAreaImage = _moveArea.GetComponent<Image>();

        if (_housingMoveAreaImage == null)
            Debug.LogWarning("[HousingCategoryPopupPresenter] Housing_MoveArea Image를 찾지 못했습니다.", this);
    }

    private void ResolveButtons()
    {
        _categoryButtons = new Button[_categories.Length];

        for (int _index = 0; _index < _categories.Length; _index++)
        {
            _categoryButtons[_index] = FindChildButton(_categories[_index].buttonName);

            if (_categoryButtons[_index] == null)
                Debug.LogWarning("[HousingCategoryPopupPresenter] 카테고리 버튼을 찾지 못했습니다: " + _categories[_index].buttonName, this);
        }
    }

    private void EnsureCategoryActions()
    {
        _categoryActions = new UnityAction[_categories.Length];

        for (int _index = 0; _index < _categories.Length; _index++)
        {
            int _capturedIndex = _index;
            _categoryActions[_index] = () => ShowCategoryPopup(_capturedIndex);
        }
    }

    private void BindButtons()
    {
        if (_categoryButtons == null || _categoryButtons.Length != _categories.Length)
            ResolveButtons();

        if (_categoryActions == null || _categoryActions.Length != _categories.Length)
            EnsureCategoryActions();

        for (int _index = 0; _index < _categories.Length; _index++)
        {
            Button _button = _categoryButtons[_index];
            if (_button == null)
                continue;

            _button.onClick.RemoveListener(_categoryActions[_index]);
            _button.onClick.AddListener(_categoryActions[_index]);
        }
    }

    private void UnbindButtons()
    {
        if (_categoryButtons == null || _categoryActions == null)
            return;

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            Button _button = _categoryButtons[_index];
            if (_button == null || _index >= _categoryActions.Length)
                continue;

            _button.onClick.RemoveListener(_categoryActions[_index]);
        }
    }

    private void BindPopupView()
    {
        if (_popupView == null)
            return;

        _popupView.WallpaperSelected -= HandleWallpaperSelected;
        _popupView.WallpaperSelected += HandleWallpaperSelected;
    }

    private void UnbindPopupView()
    {
        if (_popupView == null)
            return;

        _popupView.WallpaperSelected -= HandleWallpaperSelected;
    }

    private Button FindChildButton(string _buttonName)
    {
        if (string.IsNullOrWhiteSpace(_buttonName))
            return null;

        Button[] _buttons = GetComponentsInChildren<Button>(true);
        for (int _index = 0; _index < _buttons.Length; _index++)
        {
            Button _button = _buttons[_index];
            if (_button != null && _button.name == _buttonName)
                return _button;
        }

        return null;
    }

    private void ShowCategoryPopup(int _categoryIndex)
    {
        if (_popupView == null)
            return;

        if (_categoryIndex < 0 || _categoryIndex >= _categories.Length)
            return;

        CategoryPopupData _category = _categories[_categoryIndex];
        if (_categoryIndex == WallpaperCategoryIndex)
        {
            _popupView.ShowWallpaper(_category.title, GetWallpaperColors(), _selectedWallpaperIndex);
            return;
        }

        _popupView.ShowEmpty(_category.title);
    }

    private Color[] GetWallpaperColors()
    {
        Color[] _colors = new Color[_wallpaperColors.Length];
        for (int _index = 0; _index < _wallpaperColors.Length; _index++)
        {
            _colors[_index] = _wallpaperColors[_index].color;
        }

        return _colors;
    }

    private void HandleWallpaperSelected(int _wallpaperIndex, Color _wallpaperColor)
    {
        if (_wallpaperIndex < 0 || _wallpaperIndex >= _wallpaperColors.Length)
            return;

        _selectedWallpaperIndex = _wallpaperIndex;

        if (_housingMoveAreaImage == null)
            ResolveHousingMoveArea();

        if (_housingMoveAreaImage != null)
            _housingMoveAreaImage.color = _wallpaperColor;
    }
}
