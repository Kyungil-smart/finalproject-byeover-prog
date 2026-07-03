//담당자: 조규민

/// 하우징 배치 팝업의 탭과 아이템 목록을 표시
//  가구 배치 팝업에 카테고리 제목과 섹션별 아이템 그리드를 표시

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HousingPlacementPopupView : MonoBehaviour
{
    [Serializable]
    public class CategoryButtonBinding
    {
        [Header("카테고리")]
        [SerializeField] private HousingPlacementCategory _category;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _labelText;

        public HousingPlacementCategory Category => _category;
        public Button Button => _button;
        public TextMeshProUGUI LabelText => _labelText;
    }

    [Header("카테고리 버튼")]
    [SerializeField] private CategoryButtonBinding[] _categoryButtons;

    [Header("아이템 목록")]
    [SerializeField] private Transform _itemContent;
    [SerializeField] private HousingPlacementItemSlotView _itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI _emptyText;

    [Header("섹션 레이아웃")]
    [SerializeField] private int _sectionColumnCount = 4;
    [SerializeField] private Vector2 _sectionGridSpacing = new(18f, 18f);
    [SerializeField] private float _sectionSpacing = 24f;
    [SerializeField] private int _sectionTopPadding = 50;
    [SerializeField] private float _sectionTitleHeight = 46f;
    [SerializeField] private Color _sectionTitleBackgroundColor = new(0.86f, 0.76f, 0.62f, 1f);
    [SerializeField] private Color _sectionTitleTextColor = new(0.10f, 0.08f, 0.05f, 1f);

    [Header("섹션 템플릿")]
    [Tooltip("Content 아래에 비활성으로 둔 섹션 템플릿입니다.")]
    [SerializeField] private RectTransform _sectionTemplate;
    [Tooltip("섹션 제목 영역 템플릿입니다. 비워두면 Template_Section 안에서 Template_Title을 찾습니다.")]
    [SerializeField] private RectTransform _sectionTitleTemplate;
    [Tooltip("섹션 제목 텍스트입니다. 비워두면 생성된 Title 안에서 Text_Title을 찾습니다.")]
    [SerializeField] private TextMeshProUGUI _sectionTitleTextTemplate;
    [Tooltip("아이템 슬롯이 배치될 그리드 템플릿입니다. 비워두면 Template_Section 안에서 Template_ItemGrid를 찾습니다.")]
    [SerializeField] private RectTransform _sectionGridTemplate;

    [Header("적용")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private TextMeshProUGUI _selectedItemText;

    [Header("선택 표시")]
    [Tooltip("켜면 선택된 카테고리 텍스트 색상을 View에서 변경합니다.")]
    [SerializeField] private bool _useCategorySelectionTextColor;

    private readonly List<HousingPlacementItemSlotView> _spawnedSlots = new();
    private readonly List<GameObject> _spawnedSectionObjects = new();

    public event Action<HousingPlacementCategory> OnCategoryClicked;
    public event Action<HousingPlacementItemData> OnItemClicked;
    public event Action OnApplyClicked;

    private void Awake()
    {
        BindCategories();
        BindApplyButton();
    }

    private void OnDestroy()
    {
        UnbindCategories();
        UnbindApplyButton();
        ClearItems();
    }

    public void RefreshItems(IReadOnlyList<HousingPlacementItemData> _items, HousingPlacementCategory _selectedCategory)
    {
        List<HousingPlacementSectionData> _sections = new();

        if (_items != null && _items.Count > 0)
        {
            _sections.Add(new HousingPlacementSectionData(GetFallbackSectionTitle(_selectedCategory), _items));
        }

        RefreshSections(_sections, _selectedCategory);
    }

    public void RefreshSections(
        IReadOnlyList<HousingPlacementSectionData> _sections,
        HousingPlacementCategory _selectedCategory,
        Func<HousingPlacementItemData, HousingPlacementItemState> _stateResolver = null)
    {
        ClearItems();
        RefreshCategoryLabels(_selectedCategory);

        int _itemCount = CountSectionItems(_sections);

        if (_emptyText != null)
        {
            _emptyText.gameObject.SetActive(_itemCount <= 0);
        }

        if (_itemContent == null || _itemSlotPrefab == null)
        {
            return;
        }

        ConfigureContentLayout();

        if (_sections == null)
        {
            return;
        }

        for (int _index = 0; _index < _sections.Count; _index++)
        {
            HousingPlacementSectionData _section = _sections[_index];

            if (_section == null || _section.Items.Count <= 0)
            {
                continue;
            }

            CreateSection(_section, _stateResolver);
        }
    }

    public void SetSelectedItem(HousingPlacementItemData _itemData)
    {
        bool _hasSelection = _itemData != null;

        if (_applyButton != null)
        {
            _applyButton.interactable = _hasSelection;
        }

        if (_selectedItemText == null)
        {
            return;
        }

        _selectedItemText.text = _hasSelection ? _itemData.DisplayName : string.Empty;
        _selectedItemText.gameObject.SetActive(_hasSelection);
    }

    private void BindCategories()
    {
        if (_categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.Button == null)
            {
                continue;
            }

            HousingPlacementCategory _category = _binding.Category;
            _binding.Button.transition = Selectable.Transition.None;
            _binding.Button.onClick.RemoveAllListeners();
            _binding.Button.onClick.AddListener(() => OnCategoryClicked?.Invoke(_category));
        }
    }

    private void UnbindCategories()
    {
        if (_categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.Button == null)
            {
                continue;
            }

            _binding.Button.onClick.RemoveAllListeners();
        }
    }

    private void BindApplyButton()
    {
        if (_applyButton == null)
        {
            return;
        }

        _applyButton.onClick.RemoveListener(HandleApplyClicked);
        _applyButton.onClick.AddListener(HandleApplyClicked);
        _applyButton.interactable = false;
    }

    private void UnbindApplyButton()
    {
        if (_applyButton == null)
        {
            return;
        }

        _applyButton.onClick.RemoveListener(HandleApplyClicked);
    }

    private void RefreshCategoryLabels(HousingPlacementCategory _selectedCategory)
    {
        if (_useCategorySelectionTextColor == false || _categoryButtons == null)
        {
            return;
        }

        for (int _index = 0; _index < _categoryButtons.Length; _index++)
        {
            CategoryButtonBinding _binding = _categoryButtons[_index];

            if (_binding?.LabelText == null)
            {
                continue;
            }

            bool _isSelected = _binding.Category == _selectedCategory;
            _binding.LabelText.color = _isSelected ? Color.white : new Color(0.72f, 0.72f, 0.72f);
        }
    }

    private void ClearItems()
    {
        for (int _index = 0; _index < _spawnedSlots.Count; _index++)
        {
            HousingPlacementItemSlotView _slot = _spawnedSlots[_index];

            if (_slot == null)
            {
                continue;
            }

            _slot.OnClicked -= HandleItemClicked;
        }

        _spawnedSlots.Clear();

        for (int _index = 0; _index < _spawnedSectionObjects.Count; _index++)
        {
            GameObject _sectionObject = _spawnedSectionObjects[_index];

            if (_sectionObject == null)
            {
                continue;
            }

            Destroy(_sectionObject);
        }

        _spawnedSectionObjects.Clear();
    }

    private void ConfigureContentLayout()
    {
        GridLayoutGroup _legacyGridLayout = _itemContent.GetComponent<GridLayoutGroup>();

        if (_legacyGridLayout != null)
        {
            DestroyImmediate(_legacyGridLayout);
        }

        VerticalLayoutGroup _verticalLayout = _itemContent.GetComponent<VerticalLayoutGroup>();

        if (_verticalLayout == null)
        {
            _verticalLayout = _itemContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        _verticalLayout.padding = new RectOffset(0, 0, _sectionTopPadding, 20);
        _verticalLayout.spacing = _sectionSpacing;
        _verticalLayout.childAlignment = TextAnchor.UpperLeft;
        _verticalLayout.childControlWidth = true;
        _verticalLayout.childControlHeight = true;
        _verticalLayout.childForceExpandWidth = true;
        _verticalLayout.childForceExpandHeight = false;

        ContentSizeFitter _fitter = _itemContent.GetComponent<ContentSizeFitter>();

        if (_fitter == null)
        {
            _fitter = _itemContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void CreateSection(
        HousingPlacementSectionData _section,
        Func<HousingPlacementItemData, HousingPlacementItemState> _stateResolver)
    {
        GameObject _sectionRoot = CreateSectionRoot(_section.Title);
        RectTransform _gridRoot = CreateSectionContent(_sectionRoot.transform, _section.Title, _section.Items.Count);

        for (int _index = 0; _index < _section.Items.Count; _index++)
        {
            HousingPlacementItemSlotView _slot = Instantiate(_itemSlotPrefab, _gridRoot);
            _slot.gameObject.SetActive(true);
            HousingPlacementItemData _itemData = _section.Items[_index];
            HousingPlacementItemState _state = _stateResolver != null
                ? _stateResolver(_itemData)
                : HousingPlacementItemState.Owned;
            _slot.SetData(_itemData, _state);
            _slot.OnClicked += HandleItemClicked;
            _spawnedSlots.Add(_slot);
        }
    }

    private GameObject CreateSectionRoot(string _title)
    {
        if (_sectionTemplate != null)
        {
            RectTransform _templateSectionRoot = Instantiate(_sectionTemplate, _itemContent);
            _templateSectionRoot.gameObject.name = $"Section_{_title}";
            _templateSectionRoot.gameObject.SetActive(true);
            _spawnedSectionObjects.Add(_templateSectionRoot.gameObject);
            return _templateSectionRoot.gameObject;
        }

        GameObject _sectionRoot = new($"Section_{_title}", typeof(RectTransform));
        _sectionRoot.transform.SetParent(_itemContent, false);
        _spawnedSectionObjects.Add(_sectionRoot);

        RectTransform _rectTransform = _sectionRoot.GetComponent<RectTransform>();
        _rectTransform.anchorMin = new Vector2(0f, 1f);
        _rectTransform.anchorMax = new Vector2(1f, 1f);
        _rectTransform.pivot = new Vector2(0.5f, 1f);
        _rectTransform.sizeDelta = Vector2.zero;

        VerticalLayoutGroup _layout = _sectionRoot.AddComponent<VerticalLayoutGroup>();
        _layout.spacing = 14f;
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlWidth = true;
        _layout.childControlHeight = true;
        _layout.childForceExpandWidth = true;
        _layout.childForceExpandHeight = false;

        ContentSizeFitter _fitter = _sectionRoot.AddComponent<ContentSizeFitter>();
        _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return _sectionRoot;
    }

    private RectTransform CreateSectionContent(Transform _sectionRoot, string _title, int _itemCount)
    {
        if (_sectionTemplate != null)
        {
            RectTransform _templateGrid = ResolveTemplateChild(_sectionRoot, _sectionGridTemplate, "Template_ItemGrid");

            if (_templateGrid != null)
            {
                ConfigureTemplateTitle(_sectionRoot, _title);
                _templateGrid.gameObject.name = "ItemGrid";
                _templateGrid.gameObject.SetActive(true);
                ConfigureGrid(_templateGrid, _itemCount);
                return _templateGrid;
            }

            Debug.LogWarning("[HousingPlacementPopupView] 섹션 템플릿 안에서 Template_ItemGrid를 찾지 못해 기본 섹션 UI를 생성합니다.", this);
        }

        CreateSectionTitle(_sectionRoot, _title);
        return CreateSectionGrid(_sectionRoot, _itemCount);
    }

    private void ConfigureTemplateTitle(Transform _sectionRoot, string _title)
    {
        RectTransform _titleRoot = ResolveTemplateChild(_sectionRoot, _sectionTitleTemplate, "Template_Title");

        if (_titleRoot == null)
        {
            return;
        }

        _titleRoot.gameObject.name = $"Title_{_title}";
        _titleRoot.gameObject.SetActive(true);

        TextMeshProUGUI _titleText = ResolveTitleText(_titleRoot);

        if (_titleText == null)
        {
            return;
        }

        _titleText.text = _title;
    }

    private void CreateSectionTitle(Transform _parent, string _title)
    {
        GameObject _titleObject = new($"Title_{_title}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _titleObject.transform.SetParent(_parent, false);

        Image _background = _titleObject.GetComponent<Image>();
        _background.color = _sectionTitleBackgroundColor;
        _background.raycastTarget = false;

        GameObject _textObject = new("Text_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _textObject.transform.SetParent(_titleObject.transform, false);

        RectTransform _textRect = _textObject.GetComponent<RectTransform>();
        _textRect.anchorMin = Vector2.zero;
        _textRect.anchorMax = Vector2.one;
        _textRect.offsetMin = new Vector2(24f, 0f);
        _textRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI _titleText = _textObject.GetComponent<TextMeshProUGUI>();
        _titleText.text = _title;
        _titleText.fontSize = 24f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.MidlineLeft;
        _titleText.color = _sectionTitleTextColor;
        _titleText.raycastTarget = false;

        LayoutElement _layout = _titleObject.AddComponent<LayoutElement>();
        _layout.preferredHeight = _sectionTitleHeight;
        _layout.flexibleHeight = 0f;
    }

    private RectTransform CreateSectionGrid(Transform _parent, int _itemCount)
    {
        GameObject _gridObject = new("ItemGrid", typeof(RectTransform));
        _gridObject.transform.SetParent(_parent, false);

        GridLayoutGroup _grid = _gridObject.AddComponent<GridLayoutGroup>();
        _grid.childAlignment = TextAnchor.UpperLeft;
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        ConfigureGrid(_gridObject.GetComponent<RectTransform>(), _itemCount);

        return _gridObject.GetComponent<RectTransform>();
    }

    private void ConfigureGrid(RectTransform _gridRoot, int _itemCount)
    {
        if (_gridRoot == null)
        {
            return;
        }

        GridLayoutGroup _grid = _gridRoot.GetComponent<GridLayoutGroup>();

        if (_grid == null)
        {
            _grid = _gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            _grid.childAlignment = TextAnchor.UpperLeft;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        if (_grid.cellSize.x <= 0f || _grid.cellSize.y <= 0f)
        {
            _grid.cellSize = ResolveSlotSize();
        }

        if (_grid.spacing == Vector2.zero)
        {
            _grid.spacing = _sectionGridSpacing;
        }

        if (_grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount)
        {
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        if (_grid.constraintCount <= 0)
        {
            _grid.constraintCount = Mathf.Max(1, _sectionColumnCount);
        }

        int _columnCount = Mathf.Max(1, _grid.constraintCount);
        int _rowCount = Mathf.CeilToInt(_itemCount / (float)_columnCount);
        float _height = (_rowCount * _grid.cellSize.y) + (Mathf.Max(0, _rowCount - 1) * _grid.spacing.y);

        LayoutElement _layout = _gridRoot.GetComponent<LayoutElement>();

        if (_layout == null)
        {
            _layout = _gridRoot.gameObject.AddComponent<LayoutElement>();
        }

        _layout.preferredHeight = _height;
        _layout.flexibleHeight = 0f;
    }

    private Vector2 ResolveSlotSize()
    {
        RectTransform _slotRect = _itemSlotPrefab.GetComponent<RectTransform>();

        if (_slotRect == null)
        {
            return new Vector2(200f, 300f);
        }

        if (_slotRect.rect.width > 0f && _slotRect.rect.height > 0f)
        {
            return _slotRect.rect.size;
        }

        return _slotRect.sizeDelta;
    }

    private static int CountSectionItems(IReadOnlyList<HousingPlacementSectionData> _sections)
    {
        if (_sections == null)
        {
            return 0;
        }

        int _itemCount = 0;

        for (int _index = 0; _index < _sections.Count; _index++)
        {
            HousingPlacementSectionData _section = _sections[_index];

            if (_section == null)
            {
                continue;
            }

            _itemCount += _section.Items.Count;
        }

        return _itemCount;
    }

    private static string GetFallbackSectionTitle(HousingPlacementCategory _category)
    {
        switch (_category)
        {
            case HousingPlacementCategory.Background:
                return "\uBC30\uACBD";
            case HousingPlacementCategory.Function:
                return "\uAE30\uB2A5";
            default:
                return "\uC7A5\uC2DD";
        }
    }

    private RectTransform ResolveTemplateChild(Transform _sectionRoot, RectTransform _originalTemplate, string _childName)
    {
        if (_sectionRoot == null)
        {
            return null;
        }

        if (_originalTemplate != null)
        {
            Transform _foundByName = FindChildRecursive(_sectionRoot, _originalTemplate.name);

            if (_foundByName is RectTransform _matchedTemplate)
            {
                return _matchedTemplate;
            }
        }

        Transform _found = FindChildRecursive(_sectionRoot, _childName);
        return _found as RectTransform;
    }

    private TextMeshProUGUI ResolveTitleText(RectTransform _titleRoot)
    {
        if (_titleRoot == null)
        {
            return null;
        }

        if (_sectionTitleTextTemplate != null)
        {
            Transform _foundByName = FindChildRecursive(_titleRoot, _sectionTitleTextTemplate.name);

            if (_foundByName != null && _foundByName.TryGetComponent(out TextMeshProUGUI _matchedText))
            {
                return _matchedText;
            }
        }

        Transform _textTransform = FindChildRecursive(_titleRoot, "Text_Title");

        if (_textTransform == null)
        {
            return _titleRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        return _textTransform.GetComponent<TextMeshProUGUI>();
    }

    private static Transform FindChildRecursive(Transform _parent, string _name)
    {
        if (_parent == null)
        {
            return null;
        }

        if (_parent.name == _name)
        {
            return _parent;
        }

        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _found = FindChildRecursive(_parent.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private void HandleItemClicked(HousingPlacementItemData _itemData)
    {
        OnItemClicked?.Invoke(_itemData);
    }

    private void HandleApplyClicked()
    {
        OnApplyClicked?.Invoke();
    }
}
