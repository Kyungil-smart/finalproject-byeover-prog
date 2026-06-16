//담당자: 조규민
//설명: 하우징 가구 선택, 교체 팝업, 캐릭터 이동, 임시 저장, 핀치 줌 흐름을 연결한다.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 1차 기능의 입력과 화면 갱신 흐름을 조율한다.
/// </summary>
public class HousingInteractionController : MonoBehaviour
{
    private struct LayerSortEntry
    {
        public Transform Transform;
        public int LayerOrder;
        public int SlotId;
    }

    [Header("하우징 구성")]
    [SerializeField] private HousingModel _housingModel;
    [SerializeField] private HousingWanderer _playerMover;
    [SerializeField] private HousingInteractionView _interactionView;
    [SerializeField] private List<HousingFurnitureView> _furnitures = new List<HousingFurnitureView>();

    [Header("줌 설정")]
    [Tooltip("핀치 줌을 적용할 하우징 콘텐츠 루트입니다. 비어 있으면 현재 오브젝트를 사용합니다.")]
    [SerializeField] private RectTransform _zoomRoot;
    [SerializeField] private float _minZoom = 0.85f;
    [SerializeField] private float _maxZoom = 1.45f;
    [SerializeField] private float _zoomSpeed = 0.006f;

    [Header("스토리 다시보기")]
    [Tooltip("책장 기능형 가구 클릭 시 열 시나리오 다시보기 팝업입니다.")]
    [SerializeField] private ReplayStoryPopup _replayStoryPopup;

    private const int AUTO_SLOT_ID_BASE = 1000;
    private static readonly string[] EMOTES = { "!", "?", "...", "*" };

    private GameObject _popupRoot;
    private Transform _popupContent;
    private TMP_Text _popupTitleText;
    private HousingFurnitureView _selectedFurniture;
    private float _lastPinchDistance;

    private void Awake()
    {
        // 기능: 하우징 구성 요소를 검증하고 모델, 슬롯, 레이어, 팝업, 착용 표시를 초기화한다.
        if (_playerMover == null)
            Debug.LogWarning("[HousingInteractionController] 플레이어 이동 컴포넌트가 연결되지 않았습니다.", this);

        if (_interactionView == null)
            Debug.LogWarning("[HousingInteractionController] 상호작용 View가 연결되지 않았습니다.", this);

        if (_zoomRoot == null)
            _zoomRoot = transform as RectTransform;

        ResolveReplayStoryPopup();
        ResolveHousingModel();
        _housingModel.Initialize();
        InitializeFurnitureSlots();
        ApplyHousingLayerOrder();
        BuildPopup();
        ApplyEquippedFurnitureViews();
    }

    private void OnEnable()
    {
        // 기능: 멀티터치와 가구/캐릭터/모델 이벤트를 활성 상태에 맞춰 연결한다.
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

        if (_housingModel != null)
            _housingModel.SlotEquippedChanged += OnSlotEquippedChanged;

        HidePopup();
    }

    private void OnDisable()
    {
        // 기능: 가구/캐릭터/모델 이벤트 연결을 해제하고 팝업과 핀치 상태를 초기화한다.
        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            _furniture.ShortClicked -= OnFurnitureShortClicked;
            _furniture.LongPressed -= OnFurnitureLongPressed;
        }

        if (_playerMover != null)
            _playerMover.Clicked -= OnPlayerClicked;

        if (_housingModel != null)
            _housingModel.SlotEquippedChanged -= OnSlotEquippedChanged;

        HidePopup();
        _lastPinchDistance = 0f;
    }

    private void Update()
    {
        // 기능: 매 프레임 모바일 핀치 줌 입력만 처리한다.
        UpdatePinchZoom();
    }

    private void OnFurnitureShortClicked(HousingFurnitureView _furniture)
    {
        // 기능: 짧은 터치는 가구 타입에 따라 이동 연출, 기능형 UI, 메시지 숨김으로 분기한다.
        if (_furniture == null)
            return;

        if (!TryGetEquippedDefinition(_furniture, out HousingFurnitureDefinition _definition))
            return;

        if (_definition.FurnitureType == HousingFurnitureType.Interaction)
        {
            MoveToFurniture(_furniture, _definition);
            return;
        }

        if (_definition.FurnitureType == HousingFurnitureType.UiFunction)
        {
            OpenFurnitureFunction(_definition);
            return;
        }

        if (_interactionView != null)
            _interactionView.Hide();
    }

    private void OnFurnitureLongPressed(HousingFurnitureView _furniture)
    {
        // 기능: 롱터치는 해당 슬롯의 교체 가능한 가구 목록 팝업을 연다.
        if (_furniture == null)
            return;

        _selectedFurniture = _furniture;
        RefreshPopupButtons(_furniture.SlotId);
        ShowPopup();
    }

    private void OnPlayerClicked()
    {
        // 기능: 캐릭터 터치 시 임시 이모티콘 문구를 표시한다.
        if (_interactionView == null)
            return;

        _interactionView.ShowEmote(EMOTES[UnityEngine.Random.Range(0, EMOTES.Length)]);
    }

    private void MoveToFurniture(HousingFurnitureView _furniture, HousingFurnitureDefinition _definition)
    {
        // 기능: 캐릭터를 가구 상호작용 위치로 이동시키고 도착 문구를 표시한다.
        if (_playerMover == null || _interactionView == null || _furniture == null || _definition == null)
            return;

        _interactionView.Hide();
        _playerMover.MoveImmediatelyToInteractionTarget(
            _furniture.GetInteractionPosition(),
            () => _interactionView.ShowMessage(_definition.InteractionMessage));
    }

    private void SelectFurniture(HousingFurnitureDefinition _definition)
    {
        // 기능: 팝업에서 선택한 가구를 구매/착용 처리하고 View에 적용한다.
        if (_selectedFurniture == null || _definition == null || _housingModel == null)
            return;

        int _slotId = _selectedFurniture.SlotId;
        HousingPurchaseResult _result = _housingModel.TryPurchaseAndEquip(
            _slotId,
            _definition.FurnitureId,
            0,
            0);

        if (_result != HousingPurchaseResult.Success)
        {
            ShowPurchaseResult(_result);
            return;
        }

        HidePopup();
        ApplyFurnitureDefinitionToView(_slotId, _definition);

        if (_definition.FurnitureType == HousingFurnitureType.Interaction)
            MoveToFurniture(_selectedFurniture, _definition);
    }

    private void OpenFurnitureFunction(HousingFurnitureDefinition _definition)
    {
        // 기능: 기능형 가구 타입에 따라 연결된 UI 기능을 실행한다.
        if (_definition == null)
            return;

        switch (_definition.UiFunctionType)
        {
            case HousingUiFunctionType.StoryReplay:
                OpenReplayStoryPopup();
                break;
            case HousingUiFunctionType.CoffeeMachine:
                _interactionView?.ShowMessage("커피머신은 광고 시청과 피로도 회복 기능으로 연결됩니다.");
                break;
            case HousingUiFunctionType.ProfileArchive:
                _interactionView?.ShowMessage("프로필 보관함 기능은 추후 업적 UI와 연결됩니다.");
                break;
            case HousingUiFunctionType.GoldGenerator:
                _interactionView?.ShowMessage("골드 자동 생산 기능은 추후 연결됩니다.");
                break;
            case HousingUiFunctionType.Closet:
                _interactionView?.ShowMessage("옷장 기능은 추후 캐릭터 스킨 UI와 연결됩니다.");
                break;
            default:
                _interactionView?.ShowMessage(_definition.InteractionMessage);
                break;
        }
    }

    private void OpenReplayStoryPopup()
    {
        // 기능: 책장 기능형 가구에서 시나리오 다시보기 팝업을 연다.
        ResolveReplayStoryPopup();

        if (_replayStoryPopup == null)
        {
            Debug.LogWarning("[HousingInteractionController] 시나리오 다시보기 팝업이 연결되지 않았습니다.", this);
            _interactionView?.ShowMessage("시나리오 다시보기 팝업이 연결되지 않았습니다.");
            return;
        }

        _interactionView?.Hide();
        _replayStoryPopup.OpenForHousingBookcase();
    }

    private void InitializeFurnitureSlots()
    {
        // 기능: 씬에 배치된 가구 슬롯 ID 누락과 중복을 검사한다.
        HashSet<int> _usedSlotIds = new HashSet<int>();

        for (int _index = 0; _index < _furnitures.Count; _index++)
        {
            HousingFurnitureView _furniture = _furnitures[_index];
            if (_furniture == null)
                continue;

            if (_furniture.SlotId < 0)
            {
                int _fallbackSlotId = AUTO_SLOT_ID_BASE + _index;
                _furniture.InitializeSlotId(_fallbackSlotId);
                Debug.LogWarning(
                    "[HousingInteractionController] 슬롯 ID가 비어 있어 임시 ID를 부여했습니다. "
                    + _furniture.name + " / " + _fallbackSlotId,
                    _furniture);
            }

            if (_usedSlotIds.Add(_furniture.SlotId))
                continue;

            Debug.LogWarning(
                "[HousingInteractionController] 중복된 하우징 슬롯 ID가 있습니다. "
                + _furniture.name + " / " + _furniture.SlotId,
                _furniture);
        }
    }

    public void ApplyHousingLayerOrder()
    {
        // 기능: 배경, 대형, 중형, 소형, 캐릭터 레이어 순서가 Hierarchy 표시 순서에 반영되도록 정렬한다.
        Dictionary<Transform, List<LayerSortEntry>> _entriesByParent = new Dictionary<Transform, List<LayerSortEntry>>();

        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            AddLayerSortEntry(
                _entriesByParent,
                _furniture.transform,
                _furniture.LayerOrder,
                _furniture.SlotId);
        }

        if (_playerMover != null)
        {
            AddLayerSortEntry(
                _entriesByParent,
                _playerMover.transform,
                (int)HousingLayerType.Character,
                int.MaxValue);
        }

        foreach (KeyValuePair<Transform, List<LayerSortEntry>> _pair in _entriesByParent)
        {
            ApplyLayerOrderInParent(_pair.Key, _pair.Value);
        }
    }

    private void AddLayerSortEntry(
        Dictionary<Transform, List<LayerSortEntry>> _entriesByParent,
        Transform _target,
        int _layerOrder,
        int _slotId)
    {
        // 기능: 같은 부모 아래에서 정렬할 대상 Transform과 레이어 정보를 모은다.
        if (_target == null || _target.parent == null)
            return;

        Transform _parent = _target.parent;
        if (!_entriesByParent.TryGetValue(_parent, out List<LayerSortEntry> _entries))
        {
            _entries = new List<LayerSortEntry>();
            _entriesByParent.Add(_parent, _entries);
        }

        _entries.Add(new LayerSortEntry
        {
            Transform = _target,
            LayerOrder = _layerOrder,
            SlotId = _slotId
        });
    }

    private void ApplyLayerOrderInParent(Transform _parent, List<LayerSortEntry> _entries)
    {
        // 기능: 기존 형제 위치 범위 안에서 하우징 대상들만 레이어 순서대로 재배치한다.
        if (_parent == null || _entries == null || _entries.Count == 0)
            return;

        _entries.Sort(CompareLayerSortEntry);

        List<Transform> _children = new List<Transform>();
        List<int> _targetIndices = new List<int>();

        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _child = _parent.GetChild(_index);
            _children.Add(_child);

            if (ContainsLayerEntry(_entries, _child))
                _targetIndices.Add(_index);
        }

        for (int _index = 0; _index < _targetIndices.Count && _index < _entries.Count; _index++)
        {
            _children[_targetIndices[_index]] = _entries[_index].Transform;
        }

        for (int _index = 0; _index < _children.Count; _index++)
        {
            _children[_index].SetSiblingIndex(_index);
        }
    }

    private bool ContainsLayerEntry(List<LayerSortEntry> _entries, Transform _target)
    {
        // 기능: 특정 Transform이 레이어 정렬 대상 목록에 포함되어 있는지 확인한다.
        foreach (LayerSortEntry _entry in _entries)
        {
            if (_entry.Transform == _target)
                return true;
        }

        return false;
    }

    private int CompareLayerSortEntry(LayerSortEntry _left, LayerSortEntry _right)
    {
        // 기능: 레이어 값이 낮은 항목을 뒤쪽, 같은 레이어는 슬롯 ID 순서로 정렬한다.
        int _layerCompare = _left.LayerOrder.CompareTo(_right.LayerOrder);
        if (_layerCompare != 0)
            return _layerCompare;

        return _left.SlotId.CompareTo(_right.SlotId);
    }

    private void ResolveHousingModel()
    {
        // 기능: Inspector 미연결 시 현재 오브젝트에서 HousingModel을 찾고 없으면 추가한다.
        if (_housingModel != null)
            return;

        _housingModel = GetComponent<HousingModel>();

        if (_housingModel != null)
            return;

        _housingModel = gameObject.AddComponent<HousingModel>();
    }

    private void ResolveReplayStoryPopup()
    {
        // 기능: Inspector 미연결 시 씬에 배치된 시나리오 다시보기 팝업을 이름 기준으로 찾는다.
        if (_replayStoryPopup != null)
            return;

        _replayStoryPopup = FindSceneComponentByName<ReplayStoryPopup>("POPUp_RePlayStory");
        if (_replayStoryPopup == null)
            _replayStoryPopup = FindSceneComponentByName<ReplayStoryPopup>("POPUP_RePlayStory");
    }

    private void ApplyEquippedFurnitureViews()
    {
        // 기능: 저장된 착용 가구 정의를 씬에 배치된 프로토타입 가구 View에 적용한다.
        if (_housingModel == null)
            return;

        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null)
                continue;

            if (!_housingModel.TryGetEquippedDefinition(_furniture.SlotId, out HousingFurnitureDefinition _definition))
                continue;

            _furniture.ApplyFurnitureDefinition(_definition);
        }

        ApplyHousingLayerOrder();
    }

    private bool TryGetEquippedDefinition(HousingFurnitureView _furniture, out HousingFurnitureDefinition _definition)
    {
        // 기능: 가구 View 슬롯에 착용된 정의를 찾고 없으면 경고를 남긴다.
        _definition = null;

        if (_furniture == null || _housingModel == null)
            return false;

        if (_housingModel.TryGetEquippedDefinition(_furniture.SlotId, out _definition))
            return true;

        Debug.LogWarning(
            "[HousingInteractionController] 착용된 하우징 가구 정의를 찾지 못했습니다. SlotId: "
            + _furniture.SlotId,
            _furniture);
        return false;
    }

    private void OnSlotEquippedChanged(int _slotId, int _furnitureId)
    {
        // 기능: Model의 착용 변경 이벤트를 받아 해당 슬롯 View와 레이어를 갱신한다.
        if (_housingModel == null)
            return;

        if (!_housingModel.TryGetDefinition(_furnitureId, out HousingFurnitureDefinition _definition))
            return;

        ApplyFurnitureDefinitionToView(_slotId, _definition);
        ApplyHousingLayerOrder();
    }

    private void ApplyFurnitureDefinitionToView(int _slotId, HousingFurnitureDefinition _definition)
    {
        // 기능: 슬롯 ID에 맞는 View를 찾아 가구 정의 표시 데이터를 적용한다.
        HousingFurnitureView _furniture = FindFurnitureView(_slotId);
        if (_furniture == null)
            return;

        _furniture.ApplyFurnitureDefinition(_definition);
    }

    private HousingFurnitureView FindFurnitureView(int _slotId)
    {
        // 기능: 등록된 가구 View 목록에서 슬롯 ID가 일치하는 View를 찾는다.
        foreach (HousingFurnitureView _furniture in _furnitures)
        {
            if (_furniture == null || _furniture.SlotId != _slotId)
                continue;

            return _furniture;
        }

        return null;
    }

    private void BuildPopup()
    {
        // 기능: 프로토타입용 가구 교체 팝업을 런타임 UI로 구성한다.
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
        _popupTitleText = _titleObject.GetComponent<TMP_Text>();
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

        HidePopup();
    }

    private void RefreshPopupButtons(int _slotId)
    {
        // 기능: 선택 슬롯에 착용 가능한 가구 버튼을 해금/구매/착용 상태와 함께 다시 만든다.
        if (_housingModel == null || _popupContent == null)
            return;

        ClearPopupButtons();

        if (_popupTitleText != null)
            _popupTitleText.text = "가구 교체";

        List<HousingFurnitureDefinition> _definitions = _housingModel.GetDefinitionsBySlot(_slotId);
        if (_definitions.Count == 0)
        {
            CreateEmptyPopupMessage("교체 가능한 가구가 없습니다.");
            return;
        }

        foreach (HousingFurnitureDefinition _definition in _definitions)
        {
            CreateFurnitureButton(_definition);
        }
    }

    private void ClearPopupButtons()
    {
        // 기능: 가구 교체 팝업 Content 하위의 기존 버튼들을 제거한다.
        if (_popupContent == null)
            return;

        for (int _index = _popupContent.childCount - 1; _index >= 0; _index--)
        {
            Destroy(_popupContent.GetChild(_index).gameObject);
        }
    }

    private void CreateFurnitureButton(HousingFurnitureDefinition _definition)
    {
        // 기능: 가구 하나의 상태 문구, 해금 조건, 선택 가능 여부를 버튼 UI로 표시한다.
        if (_definition == null || _popupContent == null || _housingModel == null)
            return;

        GameObject _buttonObject = new GameObject("Button_" + _definition.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button));
        _buttonObject.transform.SetParent(_popupContent, false);

        Image _buttonImage = _buttonObject.GetComponent<Image>();
        _buttonImage.color = _definition.PrototypeColor;

        Button _button = _buttonObject.GetComponent<Button>();
        bool _isLocked = !_housingModel.IsUnlocked(_definition);
        bool _isEquipped = _housingModel.IsEquipped(_definition.SlotId, _definition.FurnitureId);
        _button.interactable = !_isLocked && !_isEquipped;
        _button.onClick.AddListener(() => SelectFurniture(_definition));

        string _stateLabel = _housingModel.GetStateLabel(_definition);
        string _unlockCondition = _housingModel.GetUnlockConditionLabel(_definition);
        string _buttonText = _definition.DisplayName + "\n" + _stateLabel;
        if (!string.IsNullOrWhiteSpace(_unlockCondition))
            _buttonText += "\n" + _unlockCondition;

        GameObject _labelObject = CreateTextObject("Text_Name", _buttonObject.transform, _buttonText, 30f);
        RectTransform _labelRect = _labelObject.GetComponent<RectTransform>();
        _labelRect.anchorMin = Vector2.zero;
        _labelRect.anchorMax = Vector2.one;
        _labelRect.offsetMin = new Vector2(8f, 8f);
        _labelRect.offsetMax = new Vector2(-8f, -8f);

        TMP_Text _labelText = _labelObject.GetComponent<TMP_Text>();
        if (_labelText != null && !string.IsNullOrWhiteSpace(_unlockCondition))
            _labelText.fontSizeMin = 14f;
    }

    private void CreateEmptyPopupMessage(string _message)
    {
        // 기능: 교체 가능한 가구가 없을 때 팝업 Content에 안내 문구를 표시한다.
        GameObject _labelObject = CreateTextObject("Text_Empty", _popupContent, _message, 32f);
        RectTransform _labelRect = _labelObject.GetComponent<RectTransform>();
        _labelRect.anchorMin = Vector2.zero;
        _labelRect.anchorMax = Vector2.one;
        _labelRect.offsetMin = Vector2.zero;
        _labelRect.offsetMax = Vector2.zero;
    }

    private void ShowPurchaseResult(HousingPurchaseResult _result)
    {
        // 기능: 구매/착용 실패 결과를 사용자에게 보이는 임시 메시지로 변환한다.
        if (_interactionView == null)
            return;

        switch (_result)
        {
            case HousingPurchaseResult.Locked:
                _interactionView.ShowMessage("아직 해금되지 않은 가구입니다.");
                break;
            case HousingPurchaseResult.NotEnoughCurrency:
                _interactionView.ShowMessage("재화가 부족합니다.");
                break;
            case HousingPurchaseResult.NotOwned:
                _interactionView.ShowMessage("먼저 구매해야 하는 가구입니다.");
                break;
            case HousingPurchaseResult.SlotMismatch:
                _interactionView.ShowMessage("이 슬롯에 배치할 수 없는 가구입니다.");
                break;
            default:
                _interactionView.ShowMessage("가구를 적용할 수 없습니다.");
                break;
        }
    }

    private GameObject CreateTextObject(string _name, Transform _parent, string _text, float _fontSize)
    {
        // 기능: 런타임 팝업에 사용할 TMP 텍스트 오브젝트를 생성한다.
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
        // 기능: 가구 교체 팝업을 표시한다.
        if (_popupRoot != null)
            _popupRoot.SetActive(true);
    }

    private void HidePopup()
    {
        // 기능: 가구 교체 팝업을 숨긴다.
        if (_popupRoot != null)
            _popupRoot.SetActive(false);
    }

    private void UpdatePinchZoom()
    {
        // 기능: 모바일 두 손가락 입력 거리 변화로 하우징 영역 확대/축소를 적용한다.
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
        // 기능: New Input System의 활성 터치 두 개를 사용해 핀치 거리를 계산한다.
        _distance = 0f;

#if ENABLE_INPUT_SYSTEM
        var _activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
        if (_activeTouches.Count >= 2)
        {
            _distance = Vector2.Distance(_activeTouches[0].screenPosition, _activeTouches[1].screenPosition);
            return true;
        }
#endif

        return false;
    }

    private void EnableMultiTouch()
    {
        // 기능: New Input System EnhancedTouch를 켜서 모바일 멀티터치 입력을 받을 수 있게 한다.
        // 구 Input(multiTouchEnabled)은 New Input System 전용 빌드에서 throw하므로 레거시 빌드에서만 사용.
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
    }

    private static T FindSceneComponentByName<T>(string _objectName) where T : Component
    {
        // 기능: 비활성 오브젝트까지 포함해 현재 씬에 있는 특정 이름의 컴포넌트를 찾는다.
        Transform[] _transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int _index = 0; _index < _transforms.Length; _index++)
        {
            Transform _target = _transforms[_index];
            if (_target == null || _target.name != _objectName)
                continue;

            GameObject _gameObject = _target.gameObject;
            if (!_gameObject.scene.IsValid())
                continue;

            return _gameObject.GetComponent<T>();
        }

        return null;
    }

}
