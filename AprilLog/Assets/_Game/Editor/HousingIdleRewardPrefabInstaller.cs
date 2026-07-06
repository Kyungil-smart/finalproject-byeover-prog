//담당자: 조규민

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Page_Housing 프리팹에 시간 누적 보상 UI를 배치합니다.
/// </summary>
// 로비 Prefab에 방치 보상 버튼·팝업·Controller 생성 및 참조 연결
public static class HousingIdleRewardPrefabInstaller
{
    private const string _prefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/Housing/Page_Housing.prefab";
    private const string _popupRootName = "Panel_AutoCurrencyPopup";
    private const string _controllerRootName = "HousingIdleRewardRoot";
    private const string _targetFurnitureName = "Static_Furniture_02";
    private const string _circleSpritePath = "Assets/Imports/Cartoon Coffee/2D Deluxe VFX/Masks/Circle Mask 001.png";

    [MenuItem("Tools/Housing/Install Idle Reward UI")]
    // 대상 로비 Prefab 로드와 방치 보상 구성 설치 후 저장
    public static void Install()
    {
        GameObject _prefabRoot = PrefabUtility.LoadPrefabContents(_prefabPath);

        try
        {
            InstallToPrefab(_prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(_prefabRoot, _prefabPath);
            Debug.Log("[HousingIdleRewardPrefabInstaller] 시간 누적 보상 UI 설치 완료");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_prefabRoot);
        }
    }

    private static void InstallToPrefab(GameObject _prefabRoot)
    {
        Transform _pageRoot = _prefabRoot.transform;
        Transform _furnitureTransform = FindChildRecursive(_pageRoot, _targetFurnitureName);

        if (_furnitureTransform == null)
        {
            Debug.LogError($"[HousingIdleRewardPrefabInstaller] {_targetFurnitureName}를 찾지 못했습니다.");
            return;
        }

        HousingIdleRewardButtonView _rewardButtonView = EnsureComponent<HousingIdleRewardButtonView>(_furnitureTransform.gameObject);
        HousingIdleRewardPopupView _popupView = CreatePopup(_pageRoot, _furnitureTransform.GetComponent<Image>()?.sprite);
        HousingIdleRewardController _controller = CreateController(_pageRoot);

        SerializedObject _controllerObject = new SerializedObject(_controller);
        _controllerObject.FindProperty("_rewardButtonView").objectReferenceValue = _rewardButtonView;
        _controllerObject.FindProperty("_popupView").objectReferenceValue = _popupView;
        _controllerObject.FindProperty("_useHousingRewardTable").boolValue = true;
        _controllerObject.FindProperty("_defaultClearChapter").intValue = 1;
        _controllerObject.FindProperty("_maxChargeSeconds").intValue = 3600;
        _controllerObject.FindProperty("_refreshIntervalSeconds").floatValue = 1f;
        _controllerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    // 게이지·보상 수량·확인 버튼을 포함한 방치 보상 팝업 생성
    private static HousingIdleRewardPopupView CreatePopup(Transform _pageRoot, Sprite _furnitureSprite)
    {
        bool _isNewPopup = _pageRoot.Find(_popupRootName) == null;
        GameObject _popupRoot = CreateRectObject(_popupRootName, _pageRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(368f, 522f));

        if (_isNewPopup)
        {
            ClearChildren(_popupRoot.transform);
        }

        Image _backgroundImage = EnsureComponent<Image>(_popupRoot);
        _backgroundImage.color = new Color(0.76f, 0.67f, 0.53f, 1f);
        _backgroundImage.raycastTarget = true;

        HousingIdleRewardPopupView _popupView = EnsureComponent<HousingIdleRewardPopupView>(_popupRoot);

        TextMeshProUGUI _statusText = CreateText("Text_Status", _popupRoot.transform, "FULL!", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(180f, 30f), 18, TextAlignmentOptions.Center);
        _statusText.color = Color.black;

        Sprite _circleSprite = LoadCircleSprite();
        Image _circleImage = CreateImage("Image_FurnitureCircle", _popupRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(68f, 68f), Color.white);
        _circleImage.sprite = _circleSprite;
        _circleImage.raycastTarget = false;
        Mask _circleMask = EnsureComponent<Mask>(_circleImage.gameObject);
        _circleMask.showMaskGraphic = false;

        Image _emptyCircleImage = CreateImage("Image_EmptyCircle", _circleImage.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.82f, 0.82f, 0.82f, 1f));
        _emptyCircleImage.sprite = _circleSprite;
        _emptyCircleImage.raycastTarget = false;

        Image _circleFillImage = CreateImage("Image_CircleFill", _circleImage.transform, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, Vector2.zero, new Color(0f, 1f, 0.28f, 1f));
        _circleFillImage.sprite = _circleSprite;
        _circleFillImage.raycastTarget = false;
        RectTransform _circleFillRect = _circleFillImage.rectTransform;

        Image _furnitureIcon = CreateImage("Image_FurnitureIcon", _circleImage.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f), Color.white);
        _furnitureIcon.sprite = _furnitureSprite;
        _furnitureIcon.preserveAspect = true;
        _furnitureIcon.raycastTarget = false;

        TextMeshProUGUI _messageText = CreateText("Text_Message", _popupRoot.transform, "누적된 재화를 수령하시겠습니까?", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(320f, 32f), 18, TextAlignmentOptions.Center);
        _messageText.fontStyle = FontStyles.Bold;

        RewardPreviewUi _goldPreview = CreateProductionPreview(_popupRoot.transform, "Gold", new Vector2(-105f, -225f), new Color(0.94f, 0.65f, 0.31f, 1f), new Color(1f, 0.86f, 0.38f, 1f));
        RewardPreviewUi _parchmentPreview = CreateProductionPreview(_popupRoot.transform, "Parchment", new Vector2(0f, -225f), new Color(0.94f, 0.65f, 0.31f, 1f), new Color(0.85f, 0.93f, 1f, 1f));
        RewardPreviewUi _diamondPreview = CreateProductionPreview(_popupRoot.transform, "Diamond", new Vector2(105f, -225f), new Color(0.94f, 0.65f, 0.31f, 1f), new Color(0.62f, 0.92f, 1f, 1f));

        TextMeshProUGUI _rewardTitleText = CreateText("Text_RewardTitle", _popupRoot.transform, "획득 보상", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -282f), new Vector2(220f, 34f), 24, TextAlignmentOptions.Center);
        _rewardTitleText.fontStyle = FontStyles.Bold;

        GameObject _rewardArea = CreateRectObject("Panel_RewardArea", _popupRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -355f), new Vector2(356f, 140f));
        Image _rewardAreaImage = EnsureComponent<Image>(_rewardArea);
        _rewardAreaImage.color = new Color(0.93f, 0.62f, 0.26f, 1f);
        _rewardAreaImage.raycastTarget = false;

        RewardSlotUi _goldReward = CreateRewardSlot(_rewardArea.transform, "Gold", new Vector2(-84f, 6f), _goldPreview.Icon.color);
        RewardSlotUi _parchmentReward = CreateRewardSlot(_rewardArea.transform, "Parchment", new Vector2(0f, 6f), _parchmentPreview.Icon.color);
        RewardSlotUi _diamondReward = CreateRewardSlot(_rewardArea.transform, "Diamond", new Vector2(84f, 6f), _diamondPreview.Icon.color);

        Button _confirmButton = CreateButton("Button_Confirm", _popupRoot.transform, "예", new Vector2(-60f, -482f), new Vector2(108f, 44f), new Color(0.58f, 1f, 0.74f, 1f));
        Button _cancelButton = CreateButton("Button_Cancel", _popupRoot.transform, "아니요", new Vector2(60f, -482f), new Vector2(108f, 44f), new Color(0.94f, 0.65f, 0.31f, 1f));

        SerializedObject _popupObject = new SerializedObject(_popupView);
        _popupObject.FindProperty("_popupRoot").objectReferenceValue = _popupRoot;
        _popupObject.FindProperty("_gaugeFillImage").objectReferenceValue = null;
        _popupObject.FindProperty("_gaugeSlider").objectReferenceValue = null;
        _popupObject.FindProperty("_progressText").objectReferenceValue = _statusText;
        _popupObject.FindProperty("_circleFillRect").objectReferenceValue = _circleFillRect;
        _popupObject.FindProperty("_furnitureIconImage").objectReferenceValue = _furnitureIcon;
        _popupObject.FindProperty("_messageText").objectReferenceValue = _messageText;
        _popupObject.FindProperty("_goldIconImage").objectReferenceValue = _goldPreview.Icon;
        _popupObject.FindProperty("_parchmentIconImage").objectReferenceValue = _parchmentPreview.Icon;
        _popupObject.FindProperty("_diamondIconImage").objectReferenceValue = _diamondPreview.Icon;
        _popupObject.FindProperty("_goldAmountText").objectReferenceValue = _goldPreview.AmountText;
        _popupObject.FindProperty("_parchmentAmountText").objectReferenceValue = _parchmentPreview.AmountText;
        _popupObject.FindProperty("_diamondAmountText").objectReferenceValue = _diamondPreview.AmountText;
        _popupObject.FindProperty("_goldRewardText").objectReferenceValue = _goldReward.AmountText;
        _popupObject.FindProperty("_parchmentRewardText").objectReferenceValue = _parchmentReward.AmountText;
        _popupObject.FindProperty("_diamondRewardText").objectReferenceValue = _diamondReward.AmountText;
        _popupObject.FindProperty("_confirmButton").objectReferenceValue = _confirmButton;
        _popupObject.FindProperty("_cancelButton").objectReferenceValue = _cancelButton;
        _popupObject.ApplyModifiedPropertiesWithoutUndo();

        _popupRoot.SetActive(false);
        return _popupView;
    }

    // 방치 보상 Controller 생성과 버튼·팝업 참조 연결
    private static HousingIdleRewardController CreateController(Transform _pageRoot)
    {
        GameObject _controllerRoot = CreateRectObject(_controllerRootName, _pageRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        return EnsureComponent<HousingIdleRewardController>(_controllerRoot);
    }

    private static RewardPreviewUi CreateProductionPreview(Transform _parent, string _name, Vector2 _anchoredPosition, Color _backgroundColor, Color _iconColor)
    {
        GameObject _slotRoot = CreateRectObject($"Slot_{_name}", _parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), _anchoredPosition, new Vector2(92f, 68f));
        Image _slotImage = EnsureComponent<Image>(_slotRoot);
        _slotImage.color = _backgroundColor;
        _slotImage.raycastTarget = false;

        Image _icon = CreateImage($"Image_{_name}Icon", _slotRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -19f), new Vector2(34f, 34f), _iconColor);
        _icon.raycastTarget = false;

        TextMeshProUGUI _amountText = CreateText($"Text_{_name}Rate", _slotRoot.transform, "0/h", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(82f, 20f), 13, TextAlignmentOptions.Center);
        _amountText.color = Color.black;
        _amountText.fontStyle = FontStyles.Bold;

        return new RewardPreviewUi(_icon, _amountText);
    }

    private static RewardSlotUi CreateRewardSlot(Transform _parent, string _name, Vector2 _anchoredPosition, Color _iconColor)
    {
        GameObject _slotRoot = CreateRectObject($"RewardSlot_{_name}", _parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), _anchoredPosition, new Vector2(74f, 112f));
        Image _frameImage = CreateImage("Image_Frame", _slotRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(58f, 58f), new Color(0.96f, 0.94f, 0.88f, 1f));
        _frameImage.raycastTarget = false;

        Image _icon = CreateImage($"Image_{_name}Icon", _frameImage.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f), _iconColor);
        _icon.raycastTarget = false;

        Image _amountBackground = CreateImage("Image_AmountBackground", _slotRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(62f, 24f), new Color(0.96f, 0.94f, 0.88f, 1f));
        _amountBackground.raycastTarget = false;

        TextMeshProUGUI _amountText = CreateText("Text_Amount", _amountBackground.transform, "0", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 13, TextAlignmentOptions.Center);
        _amountText.color = Color.black;
        _amountText.fontStyle = FontStyles.Bold;

        return new RewardSlotUi(_amountText);
    }

    private static Button CreateButton(string _name, Transform _parent, string _label, Vector2 _anchoredPosition, Vector2 _size, Color _color)
    {
        GameObject _buttonObject = CreateRectObject(_name, _parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), _anchoredPosition, _size);
        Image _image = EnsureComponent<Image>(_buttonObject);
        _image.color = _color;
        Button _button = EnsureComponent<Button>(_buttonObject);
        _button.targetGraphic = _image;

        TextMeshProUGUI _labelText = CreateText("Text_Label", _buttonObject.transform, _label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAlignmentOptions.Center);
        _labelText.color = Color.black;
        _labelText.fontStyle = FontStyles.Bold;
        return _button;
    }

    private static Sprite LoadCircleSprite()
    {
        Sprite _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(_circleSpritePath);

        if (_circleSprite == null)
        {
            Debug.LogWarning($"[HousingIdleRewardPrefabInstaller] 원형 게이지 Sprite를 찾지 못했습니다. Path: {_circleSpritePath}");
        }

        return _circleSprite;
    }

    private static Image CreateImage(string _name, Transform _parent, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _anchoredPosition, Vector2 _sizeDelta, Color _color)
    {
        GameObject _object = CreateRectObject(_name, _parent, _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
        Image _image = EnsureComponent<Image>(_object);
        _image.color = _color;
        return _image;
    }

    private static TextMeshProUGUI CreateText(string _name, Transform _parent, string _text, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _anchoredPosition, Vector2 _sizeDelta, int _fontSize, TextAlignmentOptions _alignment)
    {
        GameObject _object = CreateRectObject(_name, _parent, _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
        TextMeshProUGUI _textComponent = EnsureComponent<TextMeshProUGUI>(_object);
        _textComponent.text = _text;
        _textComponent.fontSize = _fontSize;
        _textComponent.alignment = _alignment;
        _textComponent.color = Color.white;
        return _textComponent;
    }

    private static GameObject CreateRectObject(string _name, Transform _parent, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _anchoredPosition, Vector2 _sizeDelta)
    {
        Transform _existing = _parent.Find(_name);

        if (_existing != null)
        {
            return _existing.gameObject;
        }

        GameObject _object = new GameObject(_name, typeof(RectTransform));
        _object.layer = _parent.gameObject.layer;
        _object.transform.SetParent(_parent, false);
        ApplyRect(_object.GetComponent<RectTransform>(), _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
        return _object;
    }

    private static void ApplyRect(RectTransform _rectTransform, Vector2 _anchorMin, Vector2 _anchorMax, Vector2 _anchoredPosition, Vector2 _sizeDelta)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.anchorMin = _anchorMin;
        _rectTransform.anchorMax = _anchorMax;
        _rectTransform.anchoredPosition = _anchoredPosition;
        _rectTransform.sizeDelta = _sizeDelta;
        _rectTransform.localScale = Vector3.one;
        _rectTransform.localRotation = Quaternion.identity;
    }

    private static void ClearChildren(Transform _parent)
    {
        for (int _index = _parent.childCount - 1; _index >= 0; _index--)
        {
            Object.DestroyImmediate(_parent.GetChild(_index).gameObject);
        }
    }

    private static T EnsureComponent<T>(GameObject _gameObject)
        where T : Component
    {
        T _component = _gameObject.GetComponent<T>();

        if (_component != null)
        {
            return _component;
        }

        return _gameObject.AddComponent<T>();
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

    private readonly struct RewardPreviewUi
    {
        public Image Icon { get; }
        public TextMeshProUGUI AmountText { get; }

        public RewardPreviewUi(Image _icon, TextMeshProUGUI _amountText)
        {
            Icon = _icon;
            AmountText = _amountText;
        }
    }

    private readonly struct RewardSlotUi
    {
        public TextMeshProUGUI AmountText { get; }

        public RewardSlotUi(TextMeshProUGUI _amountText)
        {
            AmountText = _amountText;
        }
    }
}
