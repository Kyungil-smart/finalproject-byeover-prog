//담당자: 조규민
//설명: 하우징 가구 선택, 교체 팝업, 캐릭터 이동, 임시 저장, 핀치 줌 흐름을 연결한다.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 1차 기능의 입력과 화면 갱신 흐름을 조율한다.
/// </summary>
public class HousingInteractionController : MonoBehaviour
{
    [Serializable]
    private class FurnitureSkinData
    {
        public int furnitureId;
        public string displayName;
        public string interactionMessage;
        public Color color = Color.white;
    }

    [Header("하우징 구성")]
    [SerializeField] private HousingWanderer _playerMover;
    [SerializeField] private HousingInteractionView _interactionView;
    [SerializeField] private List<HousingFurnitureView> _furnitures = new List<HousingFurnitureView>();

    [Header("가구 스킨")]
    [SerializeField] private List<FurnitureSkinData> _ownedFurnitureSkins = new List<FurnitureSkinData>();

    [Header("줌 설정")]
    [Tooltip("핀치 줌을 적용할 하우징 콘텐츠 루트입니다. 비어 있으면 현재 오브젝트를 사용합니다.")]
    [SerializeField] private RectTransform _zoomRoot;
    [SerializeField] private float _minZoom = 0.85f;
    [SerializeField] private float _maxZoom = 1.45f;
    [SerializeField] private float _zoomSpeed = 0.006f;

    private const string SAVE_KEY_PREFIX = "Housing_FurnitureSlot_";
    private static readonly string[] EMOTES = { "!", "?", "...", "*" };

    private readonly Dictionary<int, FurnitureSkinData> _skinMap = new Dictionary<int, FurnitureSkinData>();
    private readonly Dictionary<int, int> _slotFurnitureIds = new Dictionary<int, int>();

    private GameObject _popupRoot;
    private Transform _popupContent;
    private HousingFurnitureView _selectedFurniture;
    private bool _previousMultiTouchEnabled;
    private bool _hasPreviousMultiTouchValue;
    private float _lastPinchDistance;

    private void Awake()
    {
        if (_playerMover == null)
            Debug.LogWarning("[HousingInteractionController] 플레이어 이동 컴포넌트가 연결되지 않았습니다.", this);

        if (_interactionView == null)
            Debug.LogWarning("[HousingInteractionController] 상호작용 View가 연결되지 않았습니다.", this);

        if (_zoomRoot == null)
            _zoomRoot = transform as RectTransform;

        EnsureDefaultFurnitureSkins();
        BuildSkinMap();
        InitializeFurnitureSlots();
        BuildPopup();
        ApplySavedFurnitureSkins();
    }

    private void OnEnable()
    {
        EnableMultiTouch();

        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            _furniture.ShortClicked += OnFurnitureShortClicked;
            _furniture.LongPressed += OnFurnitureLongPressed;
        }

        if (_playerMover != null)
            _playerMover.Clicked += OnPlayerClicked;

        HidePopup();
    }

    private void OnDisable()
    {
        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            _furniture.ShortClicked -= OnFurnitureShortClicked;
            _furniture.LongPressed -= OnFurnitureLongPressed;
        }

        if (_playerMover != null)
            _playerMover.Clicked -= OnPlayerClicked;

        RestoreMultiTouch();
        HidePopup();
        _lastPinchDistance = 0f;
    }

    private void Update()
    {
        UpdatePinchZoom();
    }

    private void OnFurnitureShortClicked(HousingFurnitureView _furniture)
    {
        MoveToFurniture(_furniture);
    }

    private void OnFurnitureLongPressed(HousingFurnitureView _furniture)
    {
        if (_furniture == null)
            return;

        _selectedFurniture = _furniture;
        ShowPopup();
    }

    private void OnPlayerClicked()
    {
        if (_interactionView == null)
            return;

        _interactionView.ShowEmote(EMOTES[UnityEngine.Random.Range(0, EMOTES.Length)]);
    }

    private void MoveToFurniture(HousingFurnitureView _furniture)
    {
        if (_playerMover == null || _interactionView == null || _furniture == null)
            return;

        _interactionView.Hide();
        _playerMover.MoveImmediatelyToInteractionTarget(
            _furniture.GetInteractionPosition(),
            () => _interactionView.Show(_furniture));
    }

    private void SelectFurnitureSkin(FurnitureSkinData _skinData)
    {
        if (_selectedFurniture == null || _skinData == null)
            return;

        int _slotId = _selectedFurniture.SlotId;
        _slotFurnitureIds[_slotId] = _skinData.furnitureId;
        PlayerPrefs.SetInt(GetSlotSaveKey(_slotId), _skinData.furnitureId);
        PlayerPrefs.Save();

        _selectedFurniture.ApplyFurnitureSkin(
            _skinData.furnitureId,
            _skinData.displayName,
            _skinData.color,
            _skinData.interactionMessage);

        HidePopup();
        MoveToFurniture(_selectedFurniture);
    }

    private void InitializeFurnitureSlots()
    {
        for (int _index = 0; _index < _furnitures.Count; _index++)
        {
            HousingFurnitureView _furniture = _furnitures[_index];
            if (_furniture == null)
                continue;

            _furniture.InitializeSlotId(_index);
        }
    }

    private void ApplySavedFurnitureSkins()
    {
        if (_ownedFurnitureSkins.Count == 0)
            return;

        for (int _index = 0; _index < _furnitures.Count; _index++)
        {
            HousingFurnitureView _furniture = _furnitures[_index];
            if (_furniture == null)
                continue;

            int _defaultFurnitureId = _ownedFurnitureSkins[Mathf.Min(_index, _ownedFurnitureSkins.Count - 1)].furnitureId;
            int _savedFurnitureId = PlayerPrefs.GetInt(GetSlotSaveKey(_furniture.SlotId), _defaultFurnitureId);

            if (!_skinMap.TryGetValue(_savedFurnitureId, out FurnitureSkinData _skinData))
                _skinData = _ownedFurnitureSkins[0];

            _slotFurnitureIds[_furniture.SlotId] = _skinData.furnitureId;
            _furniture.ApplyFurnitureSkin(
                _skinData.furnitureId,
                _skinData.displayName,
                _skinData.color,
                _skinData.interactionMessage);
        }
    }

    private void EnsureDefaultFurnitureSkins()
    {
        if (_ownedFurnitureSkins.Count > 0)
            return;

        _ownedFurnitureSkins.Add(new FurnitureSkinData
        {
            furnitureId = 1001,
            displayName = "소파",
            interactionMessage = "소파에서 잠시 쉽니다.",
            color = new Color(0.35f, 0.62f, 0.95f, 1f)
        });

        _ownedFurnitureSkins.Add(new FurnitureSkinData
        {
            furnitureId = 1002,
            displayName = "테이블",
            interactionMessage = "테이블에서 작업을 준비합니다.",
            color = new Color(0.86f, 0.58f, 0.28f, 1f)
        });

        _ownedFurnitureSkins.Add(new FurnitureSkinData
        {
            furnitureId = 1003,
            displayName = "선반",
            interactionMessage = "선반에서 물건을 확인합니다.",
            color = new Color(0.45f, 0.78f, 0.52f, 1f)
        });

        _ownedFurnitureSkins.Add(new FurnitureSkinData
        {
            furnitureId = 1004,
            displayName = "침대",
            interactionMessage = "침대에서 휴식합니다.",
            color = new Color(0.78f, 0.52f, 0.86f, 1f)
        });
    }

    private void BuildSkinMap()
    {
        _skinMap.Clear();

        foreach (FurnitureSkinData _skinData in _ownedFurnitureSkins)
        {
            if (_skinData == null || _skinData.furnitureId == 0)
                continue;

            _skinMap[_skinData.furnitureId] = _skinData;
        }
    }

    private void BuildPopup()
    {
        if (_popupRoot != null)
            return;

        _popupRoot = new GameObject("Housing_FurnitureChangePopup", typeof(RectTransform), typeof(Image));
        _popupRoot.transform.SetParent(transform, false);

        RectTransform _popupRect = _popupRoot.GetComponent<RectTransform>();
        _popupRect.anchorMin = new Vector2(0.08f, 0.12f);
        _popupRect.anchorMax = new Vector2(0.92f, 0.42f);
        _popupRect.offsetMin = Vector2.zero;
        _popupRect.offsetMax = Vector2.zero;

        Image _popupImage = _popupRoot.GetComponent<Image>();
        _popupImage.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

        GameObject _titleObject = CreateTextObject("Text_Title", _popupRoot.transform, "가구 교체", 42f);
        RectTransform _titleRect = _titleObject.GetComponent<RectTransform>();
        _titleRect.anchorMin = new Vector2(0f, 0.72f);
        _titleRect.anchorMax = new Vector2(1f, 1f);
        _titleRect.offsetMin = new Vector2(24f, 0f);
        _titleRect.offsetMax = new Vector2(-24f, -8f);

        GameObject _contentObject = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _contentObject.transform.SetParent(_popupRoot.transform, false);
        _popupContent = _contentObject.transform;

        RectTransform _contentRect = _contentObject.GetComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 0.08f);
        _contentRect.anchorMax = new Vector2(1f, 0.7f);
        _contentRect.offsetMin = new Vector2(24f, 12f);
        _contentRect.offsetMax = new Vector2(-24f, -8f);

        HorizontalLayoutGroup _layoutGroup = _contentObject.GetComponent<HorizontalLayoutGroup>();
        _layoutGroup.spacing = 16f;
        _layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        _layoutGroup.childControlWidth = true;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.childForceExpandWidth = true;
        _layoutGroup.childForceExpandHeight = true;

        foreach (FurnitureSkinData _skinData in _ownedFurnitureSkins)
        {
            CreateFurnitureButton(_skinData);
        }

        HidePopup();
    }

    private void CreateFurnitureButton(FurnitureSkinData _skinData)
    {
        if (_skinData == null || _popupContent == null)
            return;

        GameObject _buttonObject = new GameObject("Button_" + _skinData.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
        _buttonObject.transform.SetParent(_popupContent, false);

        Image _buttonImage = _buttonObject.GetComponent<Image>();
        _buttonImage.color = _skinData.color;

        Button _button = _buttonObject.GetComponent<Button>();
        _button.onClick.AddListener(() => SelectFurnitureSkin(_skinData));

        GameObject _labelObject = CreateTextObject("Text_Name", _buttonObject.transform, _skinData.displayName, 32f);
        RectTransform _labelRect = _labelObject.GetComponent<RectTransform>();
        _labelRect.anchorMin = Vector2.zero;
        _labelRect.anchorMax = Vector2.one;
        _labelRect.offsetMin = new Vector2(8f, 8f);
        _labelRect.offsetMax = new Vector2(-8f, -8f);
    }

    private GameObject CreateTextObject(string _name, Transform _parent, string _text, float _fontSize)
    {
        GameObject _textObject = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI));
        _textObject.transform.SetParent(_parent, false);

        TextMeshProUGUI _textComponent = _textObject.GetComponent<TextMeshProUGUI>();
        _textComponent.text = _text;
        _textComponent.fontSize = _fontSize;
        _textComponent.enableAutoSizing = true;
        _textComponent.fontSizeMin = 18f;
        _textComponent.fontSizeMax = _fontSize;
        _textComponent.alignment = TextAlignmentOptions.Center;
        _textComponent.color = Color.white;
        _textComponent.raycastTarget = false;

        return _textObject;
    }

    private void ShowPopup()
    {
        if (_popupRoot != null)
            _popupRoot.SetActive(true);
    }

    private void HidePopup()
    {
        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }

    private void UpdatePinchZoom()
    {
        if (_zoomRoot == null)
            return;

        if (!TryGetPinchDistance(out float _pinchDistance))
        {
            _lastPinchDistance = 0f;
            return;
        }

        if (_lastPinchDistance <= 0f)
        {
            _lastPinchDistance = _pinchDistance;
            return;
        }

        float _delta = _pinchDistance - _lastPinchDistance;
        _lastPinchDistance = _pinchDistance;

        float _targetScale = Mathf.Clamp(
            _zoomRoot.localScale.x + _delta * _zoomSpeed,
            _minZoom,
            _maxZoom);

        _zoomRoot.localScale = new Vector3(_targetScale, _targetScale, 1f);
    }

    private bool TryGetPinchDistance(out float _distance)
    {
        _distance = 0f;

#if ENABLE_INPUT_SYSTEM
        var _activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (_activeTouches.Count >= 2)
        {
            _distance = Vector2.Distance(_activeTouches[0].screenPosition, _activeTouches[1].screenPosition);
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount >= 2)
        {
            _distance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
            return true;
        }
#endif

        return false;
    }

    private void EnableMultiTouch()
    {
        if (!_hasPreviousMultiTouchValue)
        {
            _previousMultiTouchEnabled = Input.multiTouchEnabled;
            _hasPreviousMultiTouchValue = true;
        }

        Input.multiTouchEnabled = true;

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
    }

    private void RestoreMultiTouch()
    {
        if (!_hasPreviousMultiTouchValue)
            return;

        Input.multiTouchEnabled = _previousMultiTouchEnabled;
        _hasPreviousMultiTouchValue = false;
    }

    private string GetSlotSaveKey(int _slotId)
    {
        return SAVE_KEY_PREFIX + _slotId;
    }
}
