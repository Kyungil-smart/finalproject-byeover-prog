//담당자: 조규민

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Page_Housing 프리팹에 광고 보기 팝업 UI를 배치합니다.
/// </summary>
// 로비 Prefab에 광고 보상 팝업 UI와 Controller 오브젝트 생성 및 직렬화 참조 연결
public static class HousingAdRewardPopupPrefabInstaller
{
    private const string _prefabPath = "Assets/_Game/Prefabs/UI/Lobby/Page/Housing/Page_Housing.prefab";
    private const string _targetFurnitureName = "Location2_Coffee";
    private const string _popupRootName = "Panel_HousingAdPopup";
    private const string _controllerRootName = "HousingAdRewardRoot";

    [MenuItem("Tools/Housing/Install Ad Reward Popup UI")]
    // 대상 로비 Prefab 로드와 광고 보상 UI 설치 후 저장
    public static void Install()
    {
        GameObject _prefabRoot = PrefabUtility.LoadPrefabContents(_prefabPath);

        try
        {
            InstallToPrefab(_prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(_prefabRoot, _prefabPath);
            Debug.Log("[HousingAdRewardPopupPrefabInstaller] 광고 보기 팝업 UI 설치 완료");
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
            Debug.LogError($"[HousingAdRewardPopupPrefabInstaller] {_targetFurnitureName}를 찾지 못했습니다.");
            return;
        }

        HousingAdRewardButtonView _buttonView = EnsureComponent<HousingAdRewardButtonView>(_furnitureTransform.gameObject);
        Image _furnitureImage = _furnitureTransform.GetComponent<Image>();

        if (_furnitureImage != null)
        {
            _furnitureImage.raycastTarget = true;
        }

        HousingAdRewardPopupView _popupView = CreatePopup(_pageRoot, _furnitureImage != null ? _furnitureImage.sprite : null);
        HousingAdRewardController _controller = CreateController(_pageRoot);
        RewardedAdService _adService = EnsureComponent<RewardedAdService>(_controller.gameObject);

        SerializedObject _controllerObject = new SerializedObject(_controller);
        _controllerObject.FindProperty("_buttonView").objectReferenceValue = _buttonView;
        _controllerObject.FindProperty("_popupView").objectReferenceValue = _popupView;
        _controllerObject.FindProperty("_message").stringValue = "광고를 보고 보상을 획득하시겠습니까?";
        _controllerObject.FindProperty("_rewardTitle").stringValue = "Reward";
        _controllerObject.FindProperty("_confirmText").stringValue = "광고 보기";
        _controllerObject.FindProperty("_cancelText").stringValue = "취소";
        _controllerObject.FindProperty("_applyFurnitureIconFromButton").boolValue = false;
        _controllerObject.FindProperty("_rewardStamina").intValue = 20;
        _controllerObject.FindProperty("_rewardDiamond").intValue = 200;
        _controllerObject.FindProperty("_loadAdOnEnable").boolValue = true;
        _controllerObject.FindProperty("_adService").objectReferenceValue = _adService;
        _controllerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    // 광고 상태·보상 아이콘·확인 버튼을 포함한 팝업 계층 생성
    private static HousingAdRewardPopupView CreatePopup(Transform _pageRoot, Sprite _furnitureSprite)
    {
        GameObject _popupRoot = CreateRectObject(
            _popupRootName,
            _pageRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(420f, 620f));

        _popupRoot.transform.SetAsLastSibling();
        Image _backgroundImage = EnsureComponent<Image>(_popupRoot);
        _backgroundImage.color = new Color(0.77f, 0.65f, 0.50f, 0.96f);
        _backgroundImage.raycastTarget = true;

        HousingAdRewardPopupView _popupView = EnsureComponent<HousingAdRewardPopupView>(_popupRoot);

        Image _furnitureIcon = CreateImage(
            "Image_RewardFurnitureIcon",
            _popupRoot.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -78f),
            new Vector2(76f, 76f),
            Color.white);
        _furnitureIcon.sprite = _furnitureSprite;
        _furnitureIcon.preserveAspect = true;
        _furnitureIcon.raycastTarget = false;

        TextMeshProUGUI _messageText = CreateText(
            "Text_Message",
            _popupRoot.transform,
            "광고를 보고 보상을 획득하시겠습니까?",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -180f),
            new Vector2(340f, 110f),
            25,
            TextAlignmentOptions.Center);
        _messageText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI _rewardTitleText = CreateText(
            "Text_RewardTitle",
            _popupRoot.transform,
            "Reward",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -315f),
            new Vector2(220f, 40f),
            27,
            TextAlignmentOptions.Center);
        _rewardTitleText.fontStyle = FontStyles.Bold;

        GameObject _rewardArea = CreateRectObject(
            "Panel_RewardArea",
            _popupRoot.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -410f),
            new Vector2(300f, 120f));
        Image _rewardAreaImage = EnsureComponent<Image>(_rewardArea);
        _rewardAreaImage.color = new Color(1f, 1f, 1f, 0f);
        _rewardAreaImage.raycastTarget = false;

        CreateRewardSlot(_rewardArea.transform, "RewardSlot_Heart", new Vector2(-70f, 0f), new Color(1f, 0.22f, 0.18f, 1f), "20");
        CreateRewardSlot(_rewardArea.transform, "RewardSlot_Diamond", new Vector2(70f, 0f), new Color(0.15f, 0.86f, 1f, 1f), "200");

        Button _confirmButton = CreateButton(
            "Button_Confirm",
            _popupRoot.transform,
            "광고 보기",
            new Vector2(-84f, -552f),
            new Vector2(150f, 56f),
            new Color(0.33f, 1f, 0.55f, 1f),
            out TextMeshProUGUI _confirmText);

        Button _cancelButton = CreateButton(
            "Button_Cancel",
            _popupRoot.transform,
            "취소",
            new Vector2(84f, -552f),
            new Vector2(150f, 56f),
            new Color(0.73f, 0.46f, 0.27f, 1f),
            out TextMeshProUGUI _cancelText);

        SerializedObject _popupObject = new SerializedObject(_popupView);
        _popupObject.FindProperty("_popupRoot").objectReferenceValue = _popupRoot;
        _popupObject.FindProperty("_rewardFurnitureIconImage").objectReferenceValue = _furnitureIcon;
        _popupObject.FindProperty("_messageText").objectReferenceValue = _messageText;
        _popupObject.FindProperty("_rewardTitleText").objectReferenceValue = _rewardTitleText;
        _popupObject.FindProperty("_rewardArea").objectReferenceValue = _rewardArea;
        _popupObject.FindProperty("_confirmButton").objectReferenceValue = _confirmButton;
        _popupObject.FindProperty("_confirmButtonText").objectReferenceValue = _confirmText;
        _popupObject.FindProperty("_cancelButton").objectReferenceValue = _cancelButton;
        _popupObject.FindProperty("_cancelButtonText").objectReferenceValue = _cancelText;
        _popupObject.ApplyModifiedPropertiesWithoutUndo();

        _popupRoot.SetActive(false);
        return _popupView;
    }

    // 광고 보상 Controller 생성과 직렬화 참조 연결
    private static HousingAdRewardController CreateController(Transform _pageRoot)
    {
        GameObject _controllerRoot = CreateRectObject(
            _controllerRootName,
            _pageRoot,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        return EnsureComponent<HousingAdRewardController>(_controllerRoot);
    }

    private static void CreateRewardSlot(Transform _parent, string _name, Vector2 _anchoredPosition, Color _iconColor, string _amountText)
    {
        GameObject _slotRoot = CreateRectObject(
            _name,
            _parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            _anchoredPosition,
            new Vector2(90f, 110f));

        Image _frameImage = CreateImage(
            "Image_Frame",
            _slotRoot.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -36f),
            new Vector2(64f, 64f),
            new Color(0.31f, 0.17f, 0.10f, 1f));
        _frameImage.raycastTarget = false;

        Image _iconImage = CreateImage(
            "Image_RewardIcon",
            _frameImage.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(44f, 44f),
            _iconColor);
        _iconImage.raycastTarget = false;

        TextMeshProUGUI _amount = CreateText(
            "Text_RewardAmount",
            _slotRoot.transform,
            _amountText,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 12f),
            new Vector2(80f, 30f),
            20,
            TextAlignmentOptions.Center);
        _amount.fontStyle = FontStyles.Bold;
    }

    private static Button CreateButton(
        string _name,
        Transform _parent,
        string _label,
        Vector2 _anchoredPosition,
        Vector2 _size,
        Color _color,
        out TextMeshProUGUI _labelText)
    {
        GameObject _buttonObject = CreateRectObject(
            _name,
            _parent,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            _anchoredPosition,
            _size);

        Image _image = EnsureComponent<Image>(_buttonObject);
        _image.color = _color;
        _image.raycastTarget = true;

        Button _button = EnsureComponent<Button>(_buttonObject);
        _button.targetGraphic = _image;

        _labelText = CreateText(
            "Text_Label",
            _buttonObject.transform,
            _label,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            19,
            TextAlignmentOptions.Center);
        _labelText.color = Color.white;
        _labelText.fontStyle = FontStyles.Bold;

        return _button;
    }

    private static Image CreateImage(
        string _name,
        Transform _parent,
        Vector2 _anchorMin,
        Vector2 _anchorMax,
        Vector2 _anchoredPosition,
        Vector2 _sizeDelta,
        Color _color)
    {
        GameObject _object = CreateRectObject(_name, _parent, _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
        Image _image = EnsureComponent<Image>(_object);
        _image.color = _color;
        return _image;
    }

    private static TextMeshProUGUI CreateText(
        string _name,
        Transform _parent,
        string _text,
        Vector2 _anchorMin,
        Vector2 _anchorMax,
        Vector2 _anchoredPosition,
        Vector2 _sizeDelta,
        int _fontSize,
        TextAlignmentOptions _alignment)
    {
        GameObject _object = CreateRectObject(_name, _parent, _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
        TextMeshProUGUI _textComponent = EnsureComponent<TextMeshProUGUI>(_object);
        _textComponent.text = _text;
        _textComponent.fontSize = _fontSize;
        _textComponent.alignment = _alignment;
        _textComponent.color = Color.white;
        _textComponent.raycastTarget = false;
        return _textComponent;
    }

    private static GameObject CreateRectObject(
        string _name,
        Transform _parent,
        Vector2 _anchorMin,
        Vector2 _anchorMax,
        Vector2 _anchoredPosition,
        Vector2 _sizeDelta)
    {
        Transform _existing = _parent.Find(_name);

        if (_existing != null)
        {
            ApplyRect(_existing.GetComponent<RectTransform>(), _anchorMin, _anchorMax, _anchoredPosition, _sizeDelta);
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
}
