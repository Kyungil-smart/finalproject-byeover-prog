//담당자: 조규민

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로비 씬에 배치 팝업·카테고리·슬롯 템플릿 UI 생성
// 배치 Controller와 View의 직렬화 참조 및 테스트 아이템 설정
public static class HousingPlacementSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/JGM/JGM_Lobby.unity";
    private const string RootName = "HousingPlacementRoot";
    private const string DefaultPreviewFramePath = "Assets/Imports/NewResource/Frame_Type2_01.Png";
    private const string EquippedPreviewFramePath = "Assets/Imports/NewResource/Frame_Type2_02.Png";
    private const string GreenStateButtonPath = "Assets/Imports/NewResource/Button_01_Mian_s_Bg_Green.Png";
    private const string RedStateButtonPath = "Assets/Imports/NewResource/Button_01_Mian_s_Bg_Red.Png";
    private const string GoldPriceIconPath = "Assets/Imports/NewResource/Icon_128_Coin_01.png";
    private const string DiamondPriceIconPath = "Assets/Imports/NewResource/Icon_128_Gem_03.png";

    [MenuItem("Tools/Housing/Install Placement UI To JGM Lobby")]
    // 열린 로비 씬에 배치 기능 UI와 Controller 일괄 생성
    public static void InstallJgmLobby()
    {
        Scene _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform _pageHousing = FindSceneTransform(_scene, "Page_Housing");

        if (_pageHousing == null)
        {
            Debug.LogError("[HousingPlacementSceneInstaller] Page_Housing을 찾지 못했습니다.");
            return;
        }

        Transform _oldRoot = _pageHousing.Find(RootName);

        if (_oldRoot != null)
        {
            Object.DestroyImmediate(_oldRoot.gameObject);
        }

        GameObject _root = CreateRectObject(RootName, _pageHousing, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        HousingPlacementButtonView _buttonView = _root.AddComponent<HousingPlacementButtonView>();
        HousingPlacementController _controller = _root.AddComponent<HousingPlacementController>();

        Button _placementButton = CreateTextButton("Button_Placement", _root.transform, "가구 배치");
        RectTransform _placementButtonRect = _placementButton.GetComponent<RectTransform>();
        SetTopRight(_placementButtonRect, new Vector2(170f, 56f), new Vector2(-36f, -36f));

        TextMeshProUGUI _modeText = CreateText("Text_PlacementMode", _root.transform, "가구 배치 중...", 36, TextAlignmentOptions.Center);
        RectTransform _modeTextRect = _modeText.GetComponent<RectTransform>();
        SetTopCenter(_modeTextRect, new Vector2(420f, 70f), new Vector2(0f, -132f));

        GameObject _popupRoot = CreatePopup(_root.transform, out HousingPlacementPopupView _popupView, out Button _closeButton);
        HousingPlacementItemSlotView _slotPrefab = CreateSlotTemplate(_popupRoot.transform);
        Transform _content = _popupRoot.transform.Find("Panel_ItemList/Scroll View/Viewport/Content");
        TextMeshProUGUI _emptyText = _popupRoot.transform.Find("Panel_ItemList/Text_Empty").GetComponent<TextMeshProUGUI>();
        ConfigureButtonView(_buttonView, _placementButton, _closeButton, _modeText, _popupRoot);
        ConfigurePopupView(_popupView, _popupRoot.transform, _content, _slotPrefab, _emptyText);
        ConfigureController(_controller, _buttonView, _popupView);

        _modeText.gameObject.SetActive(false);
        _popupRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(_scene);
        EditorSceneManager.SaveScene(_scene);
        Debug.Log("[HousingPlacementSceneInstaller] JGM_Lobby 하우징 배치 UI 설치 완료");
    }

    // 카테고리·목록·닫기 버튼을 포함한 배치 팝업 계층 생성
    private static GameObject CreatePopup(Transform _parent, out HousingPlacementPopupView _popupView, out Button _closeButton)
    {
        GameObject _popupRoot = CreateRectObject("Panel_PlacementPopup", _parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f));
        RectTransform _popupRect = _popupRoot.GetComponent<RectTransform>();
        _popupRect.sizeDelta = new Vector2(0f, 350f);
        _popupView = _popupRoot.AddComponent<HousingPlacementPopupView>();

        _closeButton = CreateTextButton("Button_ClosePlacement", _popupRoot.transform, "닫기");
        SetTopRight(_closeButton.GetComponent<RectTransform>(), new Vector2(96f, 44f), new Vector2(-24f, -20f));

        GameObject _categoryPanel = CreatePanel("Panel_CategoryTabs", _popupRoot.transform, new Color(0.10f, 0.12f, 0.15f, 0.96f));
        RectTransform _categoryPanelRect = _categoryPanel.GetComponent<RectTransform>();
        _categoryPanelRect.anchorMin = new Vector2(0f, 0f);
        _categoryPanelRect.anchorMax = new Vector2(0f, 1f);
        _categoryPanelRect.pivot = new Vector2(0f, 0.5f);
        _categoryPanelRect.anchoredPosition = new Vector2(24f, 0f);
        _categoryPanelRect.sizeDelta = new Vector2(172f, -48f);

        GameObject _itemPanel = CreatePanel("Panel_ItemList", _popupRoot.transform, new Color(0.08f, 0.09f, 0.11f, 0.96f));
        RectTransform _itemPanelRect = _itemPanel.GetComponent<RectTransform>();
        _itemPanelRect.anchorMin = new Vector2(0f, 0f);
        _itemPanelRect.anchorMax = new Vector2(1f, 1f);
        _itemPanelRect.pivot = new Vector2(0.5f, 0.5f);
        _itemPanelRect.offsetMin = new Vector2(214f, 24f);
        _itemPanelRect.offsetMax = new Vector2(-24f, -68f);

        GameObject _categoryRoot = CreateRectObject("CategoryButtons", _categoryPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform _categoryRect = _categoryRoot.GetComponent<RectTransform>();
        _categoryRect.offsetMin = new Vector2(16f, 18f);
        _categoryRect.offsetMax = new Vector2(-16f, -18f);
        VerticalLayoutGroup _categoryLayout = _categoryRoot.AddComponent<VerticalLayoutGroup>();
        _categoryLayout.spacing = 12f;
        _categoryLayout.padding = new RectOffset(0, 0, 0, 0);
        _categoryLayout.childControlHeight = true;
        _categoryLayout.childControlWidth = true;
        _categoryLayout.childForceExpandHeight = false;

        CreateCategoryButton("Button_Decoration", _categoryRoot.transform, "장식");
        CreateCategoryButton("Button_Background", _categoryRoot.transform, "배경");
        CreateCategoryButton("Button_Function", _categoryRoot.transform, "기능");

        CreateScrollView(_itemPanel.transform);

        TextMeshProUGUI _emptyText = CreateText("Text_Empty", _itemPanel.transform, "표시할 가구가 없습니다.", 24, TextAlignmentOptions.Center);
        RectTransform _emptyRect = _emptyText.GetComponent<RectTransform>();
        _emptyRect.anchorMin = new Vector2(0f, 0f);
        _emptyRect.anchorMax = new Vector2(1f, 1f);
        _emptyRect.offsetMin = new Vector2(20f, 20f);
        _emptyRect.offsetMax = new Vector2(-20f, -20f);
        _emptyText.gameObject.SetActive(false);

        return _popupRoot;
    }

    private static GameObject CreatePanel(string _name, Transform _parent, Color _color)
    {
        GameObject _panel = CreateRectObject(_name, _parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
        Image _image = _panel.AddComponent<Image>();
        _image.color = _color;
        return _panel;
    }

    private static void CreateScrollView(Transform _parent)
    {
        GameObject _scrollObject = CreateRectObject("Scroll View", _parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform _scrollRect = _scrollObject.GetComponent<RectTransform>();
        _scrollRect.offsetMin = new Vector2(18f, 18f);
        _scrollRect.offsetMax = new Vector2(-18f, -18f);

        ScrollRect _scroll = _scrollObject.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject _viewport = CreateRectObject("Viewport", _scrollObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
        Image _viewportImage = _viewport.AddComponent<Image>();
        _viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask _mask = _viewport.AddComponent<Mask>();
        _mask.showMaskGraphic = false;

        GameObject _content = CreateRectObject("Content", _viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero);
        RectTransform _contentRect = _content.GetComponent<RectTransform>();
        _contentRect.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup _grid = _content.AddComponent<GridLayoutGroup>();
        _grid.cellSize = new Vector2(200f, 260f);
        _grid.spacing = new Vector2(18f, 18f);
        _grid.padding = new RectOffset(0, 0, 0, 16);
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = 4;

        ContentSizeFitter _fitter = _content.AddComponent<ContentSizeFitter>();
        _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.viewport = _viewport.GetComponent<RectTransform>();
        _scroll.content = _contentRect;
    }

    // 아이콘·상태·가격 표시를 포함한 재사용 슬롯 템플릿 생성
    private static HousingPlacementItemSlotView CreateSlotTemplate(Transform _parent)
    {
        // 추가: 조규민 - 배치 슬롯을 실제 프리팹과 동일한 프레임, 이름, 상태 버튼 계층으로 생성한다.
        GameObject _slot = CreateRectObject("Template_ItemSlot", _parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero);
        RectTransform _slotRect = _slot.GetComponent<RectTransform>();
        _slotRect.sizeDelta = new Vector2(200f, 260f);
        Image _slotImage = _slot.AddComponent<Image>();
        _slotImage.color = new Color(1f, 1f, 1f, 0f);
        Button _slotButton = _slot.AddComponent<Button>();
        HousingPlacementItemSlotView _slotView = _slot.AddComponent<HousingPlacementItemSlotView>();
        LayoutElement _slotLayout = _slot.AddComponent<LayoutElement>();
        _slotLayout.preferredWidth = 200f;
        _slotLayout.preferredHeight = 260f;
        _slotLayout.flexibleWidth = 0f;
        _slotLayout.flexibleHeight = 0f;

        GameObject _previewFrame = CreateRectObject("PreviewFrame_Image", _slot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f));
        RectTransform _previewFrameRect = _previewFrame.GetComponent<RectTransform>();
        _previewFrameRect.sizeDelta = new Vector2(170f, 160f);
        Image _previewFrameImage = _previewFrame.AddComponent<Image>();
        Sprite _defaultPreviewFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultPreviewFramePath);
        Sprite _equippedPreviewFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EquippedPreviewFramePath);
        Sprite _greenStateButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreenStateButtonPath);
        Sprite _redStateButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RedStateButtonPath);
        Sprite _goldPriceIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldPriceIconPath);
        Sprite _diamondPriceIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondPriceIconPath);
        _previewFrameImage.sprite = _defaultPreviewFrameSprite;
        _previewFrameImage.color = Color.white;
        _previewFrameImage.raycastTarget = false;

        GameObject _icon = CreateRectObject("PreviewIcon_Image", _previewFrame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        RectTransform _iconRect = _icon.GetComponent<RectTransform>();
        _iconRect.sizeDelta = new Vector2(136f, 126f);
        Image _iconImage = _icon.AddComponent<Image>();
        _iconImage.color = new Color(0.85f, 0.87f, 0.89f, 1f);
        _iconImage.preserveAspect = true;

        TextMeshProUGUI _nameText = CreateText("ItemName_Text", _slot.transform, "가구", 18, TextAlignmentOptions.Center);
        RectTransform _nameRect = _nameText.GetComponent<RectTransform>();
        _nameRect.anchorMin = new Vector2(0.5f, 1f);
        _nameRect.anchorMax = new Vector2(0.5f, 1f);
        _nameRect.pivot = new Vector2(0.5f, 0.5f);
        _nameRect.anchoredPosition = new Vector2(0f, -184f);
        _nameRect.sizeDelta = new Vector2(180f, 36f);
        _nameText.textWrappingMode = TextWrappingModes.NoWrap;
        _nameText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject _stateButton = CreateRectObject("StateButton_Image", _slot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f));
        RectTransform _stateButtonRect = _stateButton.GetComponent<RectTransform>();
        _stateButtonRect.sizeDelta = new Vector2(170f, 44f);
        Image _stateButtonImage = _stateButton.AddComponent<Image>();
        _stateButtonImage.sprite = _greenStateButtonSprite;
        _stateButtonImage.type = Image.Type.Sliced;
        _stateButtonImage.color = Color.white;
        _stateButtonImage.raycastTarget = false;

        GameObject _currencyIcon = CreateRectObject("CurrencyIcon_Image", _stateButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-62f, 0f));
        RectTransform _currencyIconRect = _currencyIcon.GetComponent<RectTransform>();
        _currencyIconRect.sizeDelta = new Vector2(24f, 24f);
        Image _currencyIconImage = _currencyIcon.AddComponent<Image>();
        _currencyIconImage.sprite = _goldPriceIconSprite;
        _currencyIconImage.color = Color.white;
        _currencyIconImage.preserveAspect = true;
        _currencyIconImage.raycastTarget = false;
        _currencyIcon.SetActive(false);

        TextMeshProUGUI _stateText = CreateText("OwnershipOrPrice_Text", _stateButton.transform, "보유중", 17, TextAlignmentOptions.Center);
        RectTransform _stateRect = _stateText.GetComponent<RectTransform>();
        _stateRect.anchorMin = Vector2.zero;
        _stateRect.anchorMax = Vector2.one;
        _stateRect.anchoredPosition = new Vector2(12f, 0f);
        _stateRect.sizeDelta = new Vector2(-42f, 0f);
        _stateText.textWrappingMode = TextWrappingModes.NoWrap;
        _stateText.overflowMode = TextOverflowModes.Ellipsis;

        SerializedObject _serializedSlot = new SerializedObject(_slotView);
        _serializedSlot.FindProperty("_previewFrameImage").objectReferenceValue = _previewFrameImage;
        _serializedSlot.FindProperty("_iconImage").objectReferenceValue = _iconImage;
        _serializedSlot.FindProperty("_nameText").objectReferenceValue = _nameText;
        _serializedSlot.FindProperty("_stateButtonImage").objectReferenceValue = _stateButtonImage;
        _serializedSlot.FindProperty("_currencyIconImage").objectReferenceValue = _currencyIconImage;
        _serializedSlot.FindProperty("_stateText").objectReferenceValue = _stateText;
        _serializedSlot.FindProperty("_equippedPreviewFrameSprite").objectReferenceValue = _equippedPreviewFrameSprite;
        _serializedSlot.FindProperty("_defaultPreviewFrameSprite").objectReferenceValue = _defaultPreviewFrameSprite;
        _serializedSlot.FindProperty("_equippedStateSprite").objectReferenceValue = _greenStateButtonSprite;
        _serializedSlot.FindProperty("_ownedStateSprite").objectReferenceValue = _greenStateButtonSprite;
        _serializedSlot.FindProperty("_priceStateSprite").objectReferenceValue = _redStateButtonSprite;
        _serializedSlot.FindProperty("_goldPriceIconSprite").objectReferenceValue = _goldPriceIconSprite;
        _serializedSlot.FindProperty("_diamondPriceIconSprite").objectReferenceValue = _diamondPriceIconSprite;
        _serializedSlot.FindProperty("_slotButton").objectReferenceValue = _slotButton;
        _serializedSlot.ApplyModifiedPropertiesWithoutUndo();

        _slot.SetActive(false);
        return _slotView;
    }

    private static Button CreateCategoryButton(string _name, Transform _parent, string _label)
    {
        Button _button = CreateTextButton(_name, _parent, _label);
        LayoutElement _layout = _button.gameObject.AddComponent<LayoutElement>();
        _layout.preferredHeight = 58f;
        return _button;
    }

    private static Button CreateTextButton(string _name, Transform _parent, string _label)
    {
        GameObject _buttonObject = CreateRectObject(_name, _parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        Image _image = _buttonObject.AddComponent<Image>();
        _image.color = new Color(0.23f, 0.27f, 0.31f, 1f);
        Button _button = _buttonObject.AddComponent<Button>();

        TextMeshProUGUI _text = CreateText("Text_Label", _buttonObject.transform, _label, 22, TextAlignmentOptions.Center);
        RectTransform _textRect = _text.GetComponent<RectTransform>();
        _textRect.anchorMin = Vector2.zero;
        _textRect.anchorMax = Vector2.one;
        _textRect.offsetMin = new Vector2(8f, 4f);
        _textRect.offsetMax = new Vector2(-8f, -4f);

        return _button;
    }

    private static TextMeshProUGUI CreateText(string _name, Transform _parent, string _text, int _fontSize, TextAlignmentOptions _alignment)
    {
        GameObject _textObject = CreateRectObject(_name, _parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        TextMeshProUGUI _textComponent = _textObject.AddComponent<TextMeshProUGUI>();
        _textComponent.text = _text;
        _textComponent.fontSize = _fontSize;
        _textComponent.alignment = _alignment;
        _textComponent.color = Color.white;
        _textComponent.raycastTarget = false;
        return _textComponent;
    }

    private static GameObject CreateRectObject(string _name, Transform _parent, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _pivot, Vector2 _anchoredPosition)
    {
        GameObject _gameObject = new GameObject(_name, typeof(RectTransform));
        _gameObject.transform.SetParent(_parent, false);

        RectTransform _rectTransform = _gameObject.GetComponent<RectTransform>();
        _rectTransform.anchorMin = _anchorMin;
        _rectTransform.anchorMax = _anchorMax;
        _rectTransform.pivot = _pivot;
        _rectTransform.anchoredPosition = _anchoredPosition;
        _rectTransform.sizeDelta = Vector2.zero;

        return _gameObject;
    }

    private static void ConfigureButtonView(HousingPlacementButtonView _buttonView, Button _placementButton, Button _closeButton, TextMeshProUGUI _modeText, GameObject _popupRoot)
    {
        SerializedObject _serializedView = new SerializedObject(_buttonView);
        _serializedView.FindProperty("_placementButton").objectReferenceValue = _placementButton;
        _serializedView.FindProperty("_closeButton").objectReferenceValue = _closeButton;
        _serializedView.FindProperty("_placementModeText").objectReferenceValue = _modeText;
        _serializedView.FindProperty("_popupRoot").objectReferenceValue = _popupRoot;
        _serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePopupView(HousingPlacementPopupView _popupView, Transform _popupRoot, Transform _content, HousingPlacementItemSlotView _slotPrefab, TextMeshProUGUI _emptyText)
    {
        SerializedObject _serializedView = new SerializedObject(_popupView);
        SerializedProperty _categoryButtons = _serializedView.FindProperty("_categoryButtons");
        _categoryButtons.arraySize = 3;
        ConfigureCategory(_categoryButtons.GetArrayElementAtIndex(0), HousingPlacementCategory.Decoration, _popupRoot.Find("Panel_CategoryTabs/CategoryButtons/Button_Decoration"));
        ConfigureCategory(_categoryButtons.GetArrayElementAtIndex(1), HousingPlacementCategory.Background, _popupRoot.Find("Panel_CategoryTabs/CategoryButtons/Button_Background"));
        ConfigureCategory(_categoryButtons.GetArrayElementAtIndex(2), HousingPlacementCategory.Function, _popupRoot.Find("Panel_CategoryTabs/CategoryButtons/Button_Function"));
        _serializedView.FindProperty("_itemContent").objectReferenceValue = _content;
        _serializedView.FindProperty("_itemSlotPrefab").objectReferenceValue = _slotPrefab;
        _serializedView.FindProperty("_emptyText").objectReferenceValue = _emptyText;
        _serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCategory(SerializedProperty _property, HousingPlacementCategory _category, Transform _buttonTransform)
    {
        _property.FindPropertyRelative("_category").enumValueIndex = (int)_category;
        _property.FindPropertyRelative("_button").objectReferenceValue = _buttonTransform.GetComponent<Button>();
        _property.FindPropertyRelative("_labelText").objectReferenceValue = _buttonTransform.Find("Text_Label").GetComponent<TextMeshProUGUI>();
    }

    // 배치 Controller에 View와 초기 아이템 데이터 직렬화 연결
    private static void ConfigureController(HousingPlacementController _controller, HousingPlacementButtonView _buttonView, HousingPlacementPopupView _popupView)
    {
        SerializedObject _serializedController = new SerializedObject(_controller);
        _serializedController.FindProperty("_buttonView").objectReferenceValue = _buttonView;
        _serializedController.FindProperty("_popupView").objectReferenceValue = _popupView;

        SerializedProperty _items = _serializedController.FindProperty("_items");
        _items.arraySize = 6;
        ConfigureItem(_items.GetArrayElementAtIndex(0), "bed_basic", "침대", HousingPlacementCategory.Decoration, true, true, 0);
        ConfigureItem(_items.GetArrayElementAtIndex(1), "desk_basic", "책상", HousingPlacementCategory.Decoration, true, true, 0);
        ConfigureItem(_items.GetArrayElementAtIndex(2), "room_basic", "기본 방", HousingPlacementCategory.Background, true, true, 0);
        ConfigureItem(_items.GetArrayElementAtIndex(3), "wallpaper_blue", "벽지", HousingPlacementCategory.Background, false, true, 1200);
        ConfigureItem(_items.GetArrayElementAtIndex(4), "coin_generator", "코인 생성기", HousingPlacementCategory.Function, false, true, 3000);
        ConfigureItem(_items.GetArrayElementAtIndex(5), "story_bookshelf", "스토리 책장", HousingPlacementCategory.Function, true, true, 0);
        _serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureItem(SerializedProperty _property, string _itemId, string _displayName, HousingPlacementCategory _category, bool _isOwned, bool _isUnlocked, int _price)
    {
        _property.FindPropertyRelative("_itemId").stringValue = _itemId;
        _property.FindPropertyRelative("_displayName").stringValue = _displayName;
        _property.FindPropertyRelative("_category").enumValueIndex = (int)_category;
        _property.FindPropertyRelative("_icon").objectReferenceValue = null;
        _property.FindPropertyRelative("_isOwned").boolValue = _isOwned;
        _property.FindPropertyRelative("_isUnlocked").boolValue = _isUnlocked;
        _property.FindPropertyRelative("_price").intValue = _price;
    }

    private static Transform FindSceneTransform(Scene _scene, string _name)
    {
        GameObject[] _roots = _scene.GetRootGameObjects();

        for (int _index = 0; _index < _roots.Length; _index++)
        {
            Transform _found = FindChildRecursive(_roots[_index].transform, _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform _parent, string _name)
    {
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

    private static void SetTopRight(RectTransform _rectTransform, Vector2 _size, Vector2 _offset)
    {
        _rectTransform.anchorMin = Vector2.one;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = Vector2.one;
        _rectTransform.sizeDelta = _size;
        _rectTransform.anchoredPosition = _offset;
    }

    private static void SetTopCenter(RectTransform _rectTransform, Vector2 _size, Vector2 _offset)
    {
        _rectTransform.anchorMin = new Vector2(0.5f, 1f);
        _rectTransform.anchorMax = new Vector2(0.5f, 1f);
        _rectTransform.pivot = new Vector2(0.5f, 1f);
        _rectTransform.sizeDelta = _size;
        _rectTransform.anchoredPosition = _offset;
    }

    private static void SetBottomStretch(RectTransform _rectTransform, Vector2 _offsetMin, Vector2 _offsetMax)
    {
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.offsetMin = _offsetMin;
        _rectTransform.offsetMax = _offsetMax;
    }
}
#endif
