//담당자: 조규민
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 하우징 카테고리 버튼 입력을 받아 색상 선택 팝업 표시와 가구 색상 적용을 중계한다.
/// </summary>
public class HousingCategoryPopupPresenter : MonoBehaviour
{
    private const int WallpaperCategoryIndex = 0;

    private enum SurfacePopupType
    {
        Floor,
        Wallpaper
    }

    [Serializable]
    private class CategoryPopupData
    {
        public string buttonName;
        public string title;
        public string targetObjectName;
        public Color[] colors;
    }

    [Serializable]
    private class SurfacePopupData
    {
        public SurfacePopupType surfaceType;
        public string title;
        public string targetObjectName;
        public Color[] colors;
    }

    [Header("팝업 View")]
    [SerializeField] private HousingCategoryPopupView _popupView;

    [Header("벽지 적용 대상")]
    [FormerlySerializedAs("_housingMoveAreaImage")]
    [SerializeField] private Image _wallpaperImage;

    [Header("바닥 적용 대상")]
    [SerializeField] private Image _floorImage;

    private readonly CategoryPopupData[] _categories =
    {
        new CategoryPopupData
        {
            buttonName = "Btn_Wallpaper",
            title = "벽지",
            targetObjectName = "Housing_TempWallpaper",
            colors = new[]
            {
                new Color(0.72f, 0.56f, 0.35f, 1f),
                new Color(0.28f, 0.30f, 0.29f, 1f),
                new Color(0.58f, 0.50f, 0.40f, 1f),
                new Color(0.14f, 0.16f, 0.15f, 1f),
                new Color(0.90f, 0.88f, 0.84f, 1f),
                new Color(0.74f, 0.65f, 0.56f, 1f),
                new Color(0.78f, 0.78f, 0.76f, 1f),
                new Color(0.19f, 0.20f, 0.20f, 1f),
                new Color(0.46f, 0.38f, 0.30f, 1f),
                new Color(0.34f, 0.40f, 0.44f, 1f),
                new Color(0.66f, 0.60f, 0.50f, 1f),
                new Color(0.12f, 0.13f, 0.14f, 1f)
            }
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Large",
            title = "대형 - 침대",
            targetObjectName = "Housing_TempBed",
            colors = new[]
            {
                new Color(0.96f, 0.28f, 0.34f, 1f),
                new Color(0.24f, 0.58f, 0.96f, 1f),
                new Color(0.98f, 0.74f, 0.18f, 1f),
                new Color(0.48f, 0.32f, 0.94f, 1f),
                new Color(0.20f, 0.78f, 0.58f, 1f),
                new Color(1.00f, 0.46f, 0.18f, 1f),
                new Color(0.94f, 0.32f, 0.78f, 1f),
                new Color(0.46f, 0.82f, 1.00f, 1f),
                new Color(0.62f, 0.42f, 0.30f, 1f),
                new Color(0.30f, 0.42f, 0.62f, 1f),
                new Color(0.56f, 0.58f, 0.48f, 1f),
                new Color(0.18f, 0.18f, 0.22f, 1f)
            }
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Medium",
            title = "중형 - 책장",
            targetObjectName = "Housing_TempBookcase",
            colors = new[]
            {
                new Color(0.96f, 0.45f, 0.20f, 1f),
                new Color(0.18f, 0.64f, 0.98f, 1f),
                new Color(0.30f, 0.82f, 0.32f, 1f),
                new Color(0.98f, 0.30f, 0.62f, 1f),
                new Color(0.72f, 0.44f, 1.00f, 1f),
                new Color(0.98f, 0.86f, 0.24f, 1f),
                new Color(0.20f, 0.86f, 0.80f, 1f),
                new Color(0.86f, 0.24f, 0.30f, 1f),
                new Color(0.45f, 0.30f, 0.18f, 1f),
                new Color(0.26f, 0.36f, 0.46f, 1f),
                new Color(0.64f, 0.56f, 0.42f, 1f),
                new Color(0.20f, 0.22f, 0.24f, 1f)
            }
        },
        new CategoryPopupData
        {
            buttonName = "Btn_Small",
            title = "소형 - 화분",
            targetObjectName = "Housing_TempPlant",
            colors = new[]
            {
                new Color(0.22f, 0.86f, 0.38f, 1f),
                new Color(0.98f, 0.36f, 0.28f, 1f),
                new Color(0.30f, 0.70f, 1.00f, 1f),
                new Color(1.00f, 0.82f, 0.20f, 1f),
                new Color(0.76f, 0.40f, 1.00f, 1f),
                new Color(0.18f, 0.88f, 0.82f, 1f),
                new Color(1.00f, 0.52f, 0.76f, 1f),
                new Color(0.62f, 0.92f, 0.22f, 1f),
                new Color(0.34f, 0.62f, 0.26f, 1f),
                new Color(0.58f, 0.40f, 0.24f, 1f),
                new Color(0.78f, 0.70f, 0.52f, 1f),
                new Color(0.24f, 0.30f, 0.26f, 1f)
            }
        }
    };

    private readonly SurfacePopupData[] _surfacePopupData =
    {
        new SurfacePopupData
        {
            surfaceType = SurfacePopupType.Floor,
            title = "바닥",
            targetObjectName = "Housing_TempFloor",
            colors = new[]
            {
                new Color(0.52f, 0.43f, 0.34f, 1f),
                new Color(0.34f, 0.25f, 0.18f, 1f),
                new Color(0.62f, 0.48f, 0.32f, 1f),
                new Color(0.28f, 0.25f, 0.22f, 1f),
                new Color(0.72f, 0.64f, 0.52f, 1f),
                new Color(0.44f, 0.36f, 0.28f, 1f),
                new Color(0.78f, 0.72f, 0.64f, 1f),
                new Color(0.22f, 0.22f, 0.22f, 1f),
                new Color(0.58f, 0.42f, 0.26f, 1f),
                new Color(0.42f, 0.46f, 0.48f, 1f),
                new Color(0.66f, 0.56f, 0.44f, 1f),
                new Color(0.14f, 0.14f, 0.16f, 1f)
            }
        },
        new SurfacePopupData
        {
            surfaceType = SurfacePopupType.Wallpaper,
            title = "벽지",
            targetObjectName = "Housing_TempWallpaper",
            colors = new[]
            {
                new Color(0.72f, 0.56f, 0.35f, 1f),
                new Color(0.28f, 0.30f, 0.29f, 1f),
                new Color(0.58f, 0.50f, 0.40f, 1f),
                new Color(0.14f, 0.16f, 0.15f, 1f),
                new Color(0.90f, 0.88f, 0.84f, 1f),
                new Color(0.74f, 0.65f, 0.56f, 1f),
                new Color(0.78f, 0.78f, 0.76f, 1f),
                new Color(0.19f, 0.20f, 0.20f, 1f),
                new Color(0.46f, 0.38f, 0.30f, 1f),
                new Color(0.34f, 0.40f, 0.44f, 1f),
                new Color(0.66f, 0.60f, 0.50f, 1f),
                new Color(0.12f, 0.13f, 0.14f, 1f)
            }
        }
    };

    private readonly int[] _selectedColorIndices = new int[4];
    private readonly int[] _selectedSurfaceColorIndices = new int[2];

    private Button[] _categoryButtons;
    private UnityAction[] _categoryActions;
    private int _activeCategoryIndex = -1;
    private SurfacePopupType _activeSurfaceType = SurfacePopupType.Wallpaper;

    private void Awake()
    {
        // 기능: 팝업 View, 적용 대상 Image, 카테고리 버튼 이벤트 준비를 초기화한다.
        ResolveView();
        ResolveWallpaperImage();
        ResolveFloorImage();
        ResolveButtons();
        EnsureCategoryActions();
    }

    private void OnEnable()
    {
        // 기능: 카테고리 버튼과 팝업 선택 이벤트를 활성 상태에 맞춰 연결한다.
        BindButtons();
        BindPopupView();

        if (_popupView != null)
            _popupView.Hide();
    }

    private void OnDisable()
    {
        // 기능: 비활성화 시 버튼과 팝업 이벤트 연결을 해제하고 팝업을 닫는다.
        UnbindButtons();
        UnbindPopupView();

        if (_popupView != null)
            _popupView.Hide();
    }

    private void ResolveView()
    {
        // 기능: Inspector 미연결 시 같은 오브젝트에서 팝업 View를 찾는다.
        if (_popupView != null)
            return;

        _popupView = GetComponent<HousingCategoryPopupView>();

        if (_popupView == null)
            Debug.LogWarning("[HousingCategoryPopupPresenter] 팝업 View가 연결되지 않았습니다.", this);
    }

    private void ResolveWallpaperImage()
    {
        // 기능: 벽지 카테고리의 색상을 적용할 임시 배경 Image를 찾는다.
        if (_wallpaperImage != null)
            return;

        _wallpaperImage = FindTargetImage(_categories[WallpaperCategoryIndex].targetObjectName);

        if (_wallpaperImage == null)
            Debug.LogWarning("[HousingCategoryPopupPresenter] Housing_TempWallpaper Image를 찾지 못했습니다.", this);
    }

    private void ResolveFloorImage()
    {
        // 추가:조규민 기능 설명: 바닥 탭에서 색상을 적용할 임시 바닥 Image를 찾는다.
        if (_floorImage != null)
            return;

        _floorImage = FindTargetImage("Housing_TempFloor");

        if (_floorImage == null)
            Debug.LogWarning("[HousingCategoryPopupPresenter] Housing_TempFloor Image를 찾지 못했습니다.", this);
    }

    private void ResolveButtons()
    {
        // 기능: 카테고리 버튼 이름을 기준으로 Hierarchy에 배치된 버튼 참조를 수집한다.
        _categoryButtons = new Button[_categories.Length];

        for (int _index = 0; _index < _categories.Length; _index++)
        {
            _categoryButtons[_index] = FindChildButton(_categories[_index].buttonName);

            if (_categoryButtons[_index] == null)
                Debug.LogWarning("[HousingCategoryPopupPresenter] 카테고리 버튼을 찾지 못했습니다. " + _categories[_index].buttonName, this);
        }
    }

    private void EnsureCategoryActions()
    {
        // 기능: 각 카테고리 버튼이 자기 인덱스의 팝업을 열도록 UnityAction을 만든다.
        _categoryActions = new UnityAction[_categories.Length];

        for (int _index = 0; _index < _categories.Length; _index++)
        {
            int _capturedIndex = _index;
            _categoryActions[_index] = () => ShowCategoryPopup(_capturedIndex);
        }
    }

    private void BindButtons()
    {
        // 기능: 카테고리 버튼 클릭 이벤트를 중복 없이 연결한다.
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
        // 기능: 카테고리 버튼 클릭 이벤트 연결을 해제한다.
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
        // 기능: 팝업에서 선택된 색상 이벤트를 Presenter 처리 함수에 연결한다.
        if (_popupView == null)
            return;

        _popupView.ColorSelected -= HandleColorSelected;
        _popupView.ColorSelected += HandleColorSelected;
        _popupView.FloorTabClicked -= HandleFloorTabClicked;
        _popupView.FloorTabClicked += HandleFloorTabClicked;
        _popupView.WallpaperTabClicked -= HandleWallpaperTabClicked;
        _popupView.WallpaperTabClicked += HandleWallpaperTabClicked;
    }

    private void UnbindPopupView()
    {
        // 기능: 팝업 색상 선택 이벤트 연결을 해제한다.
        if (_popupView == null)
            return;

        _popupView.ColorSelected -= HandleColorSelected;
        _popupView.FloorTabClicked -= HandleFloorTabClicked;
        _popupView.WallpaperTabClicked -= HandleWallpaperTabClicked;
    }

    private Button FindChildButton(string _buttonName)
    {
        // 기능: 하위 오브젝트에서 이름이 일치하는 Button 컴포넌트를 찾는다.
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
        // 기능: 선택한 카테고리의 색상 목록을 팝업 View에 전달한다.
        if (_popupView == null)
            return;

        if (!IsValidCategoryIndex(_categoryIndex))
            return;

        CategoryPopupData _category = _categories[_categoryIndex];
        _activeCategoryIndex = _categoryIndex;

        if (_categoryIndex == WallpaperCategoryIndex)
        {
            ShowSurfacePopup(_activeSurfaceType);
            return;
        }

        _popupView.ShowColorOptions(_category.title, _category.colors, _selectedColorIndices[_categoryIndex]);
    }

    private void HandleColorSelected(int _colorIndex, Color _color)
    {
        // 기능: 팝업에서 선택한 색상을 현재 카테고리 대상 Image에 적용한다.
        if (!IsValidCategoryIndex(_activeCategoryIndex))
            return;

        if (_activeCategoryIndex == WallpaperCategoryIndex)
        {
            HandleSurfaceColorSelected(_colorIndex, _color);
            return;
        }

        CategoryPopupData _category = _categories[_activeCategoryIndex];
        if (_category.colors == null || _colorIndex < 0 || _colorIndex >= _category.colors.Length)
            return;

        _selectedColorIndices[_activeCategoryIndex] = _colorIndex;

        Image _targetImage = GetTargetImage(_activeCategoryIndex);
        if (_targetImage != null)
            _targetImage.color = _color;
    }

    private void HandleSurfaceColorSelected(int _colorIndex, Color _color)
    {
        // 추가:조규민 기능 설명: 바닥/벽지 하위 탭의 선택 색상을 각각 저장하고 해당 대상 Image에 적용한다.
        SurfacePopupData _surfaceData = GetSurfaceData(_activeSurfaceType);
        if (_surfaceData == null || _surfaceData.colors == null || _colorIndex < 0 || _colorIndex >= _surfaceData.colors.Length)
            return;

        _selectedSurfaceColorIndices[(int)_activeSurfaceType] = _colorIndex;

        Image _targetImage = GetSurfaceTargetImage(_activeSurfaceType);
        if (_targetImage != null)
            _targetImage.color = _color;
    }

    private void HandleFloorTabClicked()
    {
        // 추가:조규민 기능 설명: 왼쪽 바닥 탭 선택 시 바닥 색상 목록으로 팝업 슬롯을 갱신한다.
        ShowSurfacePopup(SurfacePopupType.Floor);
    }

    private void HandleWallpaperTabClicked()
    {
        // 추가:조규민 기능 설명: 왼쪽 벽지 탭 선택 시 벽지 색상 목록으로 팝업 슬롯을 갱신한다.
        ShowSurfacePopup(SurfacePopupType.Wallpaper);
    }

    private void ShowSurfacePopup(SurfacePopupType _surfaceType)
    {
        // 추가:조규민 기능 설명: 벽지 카테고리 안에서 선택한 하위 배경 타입의 색상 슬롯을 표시한다.
        if (_popupView == null)
            return;

        SurfacePopupData _surfaceData = GetSurfaceData(_surfaceType);
        if (_surfaceData == null)
            return;

        _activeSurfaceType = _surfaceType;
        _activeCategoryIndex = WallpaperCategoryIndex;
        int _selectedIndex = _selectedSurfaceColorIndices[(int)_surfaceType];
        _popupView.ShowSurfaceOptions(_surfaceData.title, _surfaceData.colors, _selectedIndex, _surfaceType == SurfacePopupType.Floor);
    }

    private Image GetTargetImage(int _categoryIndex)
    {
        // 기능: 현재 카테고리 색상을 적용할 대상 Image를 반환한다.
        if (_categoryIndex == WallpaperCategoryIndex)
        {
            if (_wallpaperImage == null)
                ResolveWallpaperImage();

            return _wallpaperImage;
        }

        return FindTargetImage(_categories[_categoryIndex].targetObjectName);
    }

    private Image GetSurfaceTargetImage(SurfacePopupType _surfaceType)
    {
        // 추가:조규민 기능 설명: 바닥/벽지 탭별 실제 적용 대상 Image를 반환한다.
        if (_surfaceType == SurfacePopupType.Floor)
        {
            if (_floorImage == null)
                ResolveFloorImage();

            return _floorImage;
        }

        if (_wallpaperImage == null)
            ResolveWallpaperImage();

        return _wallpaperImage;
    }

    private SurfacePopupData GetSurfaceData(SurfacePopupType _surfaceType)
    {
        // 추가:조규민 기능 설명: 하위 배경 타입에 맞는 색상 데이터 묶음을 찾는다.
        for (int _index = 0; _index < _surfacePopupData.Length; _index++)
        {
            SurfacePopupData _data = _surfacePopupData[_index];
            if (_data != null && _data.surfaceType == _surfaceType)
                return _data;
        }

        return null;
    }

    private Image FindTargetImage(string _targetObjectName)
    {
        // 기능: 대상 오브젝트 이름으로 하위 Image 컴포넌트를 찾는다.
        if (string.IsNullOrWhiteSpace(_targetObjectName))
            return null;

        Transform _target = FindChildTransform(_targetObjectName);
        if (_target == null)
            return null;

        return _target.GetComponent<Image>();
    }

    private Transform FindChildTransform(string _targetName)
    {
        // 기능: 비활성 오브젝트까지 포함해 이름이 일치하는 하위 Transform을 찾는다.
        Transform[] _children = GetComponentsInChildren<Transform>(true);
        for (int _index = 0; _index < _children.Length; _index++)
        {
            Transform _child = _children[_index];
            if (_child != null && _child.name == _targetName)
                return _child;
        }

        return null;
    }

    private bool IsValidCategoryIndex(int _categoryIndex)
    {
        // 기능: 카테고리 배열 범위 안의 인덱스인지 확인한다.
        return _categoryIndex >= 0 && _categoryIndex < _categories.Length;
    }
}
