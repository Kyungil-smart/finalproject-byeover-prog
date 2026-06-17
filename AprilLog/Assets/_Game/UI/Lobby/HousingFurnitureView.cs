//담당자: 조규민
//설명: 하우징 페이지에 배치된 가구 슬롯의 짧은 터치/롱터치 이벤트와 임시 스킨 표시 데이터를 전달한다.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 하우징 가구 슬롯의 선택 이벤트와 표시 데이터를 담당한다.
/// </summary>
public class HousingFurnitureView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("가구 정보")]
    [SerializeField] private int _slotId = -1;
    [SerializeField] private string _furnitureName = "임시 가구";
    [SerializeField] private HousingFurnitureType _furnitureType = HousingFurnitureType.Interaction;
    [SerializeField] private HousingFurnitureCategory _category = HousingFurnitureCategory.Medium;
    [SerializeField] private HousingUiFunctionType _uiFunctionType = HousingUiFunctionType.None;

    [Header("배치 레이어")]
    [Tooltip("카테고리 기준 레이어를 우선 사용합니다. 끄면 아래 수동 레이어 값을 사용합니다.")]
    [SerializeField] private bool _useCategoryLayer = true;
    [Tooltip("수동으로 지정할 하우징 배치 레이어입니다.")]
    [SerializeField] private HousingLayerType _manualLayer = HousingLayerType.MediumFurniture;

    [Header("상호작용 설정")]
    [Tooltip("플레이어가 가구와 상호작용하기 위해 이동할 위치")]
    [SerializeField] private RectTransform _interactionPoint;
    [Tooltip("가구 상호작용 시 Housing_InteractionTexture 위치에 표시할 문구")]
    [SerializeField] private string _interactionMessage = "가구와 상호작용했습니다.";
    [Tooltip("자동 배회 경로에서 우선 방문할 가구인지 여부입니다.")]
    [SerializeField] private bool _isAutoMoveTarget = true;
    [Tooltip("애니메이션이나 연출이 있는 상호작용 가구인지 여부입니다.")]
    [SerializeField] private bool _hasMotionInteraction;
    [Tooltip("가구 상호작용 연출이 지속되는 시간입니다.")]
    [SerializeField] private float _interactionSeconds = 1.2f;
    [Tooltip("상호작용 종료 후 해당 자리에서 머무르는 시간입니다.")]
    [SerializeField] private float _waitAfterInteractionSeconds = 0.8f;

    [Header("임시 스킨 표시")]
    [Tooltip("임시 가구 색상을 표시할 Image입니다. 비어 있으면 같은 오브젝트의 Image를 사용합니다.")]
    [SerializeField] private Image _skinImage;
    [SerializeField] private TMP_Text _nameText;

    [Header("터치 설정")]
    [Tooltip("롱터치로 판정할 시간입니다.")]
    [SerializeField] private float _longPressSeconds = 0.45f;

    public event Action<HousingFurnitureView> Clicked;
    public event Action<HousingFurnitureView> ShortClicked;
    public event Action<HousingFurnitureView> LongPressed;

    private Coroutine _longPressCoroutine;
    private bool _isPointerDown;
    private bool _isLongPressed;

    public int SlotId => _slotId;
    public string FurnitureName => _furnitureName;
    public HousingFurnitureType FurnitureType => _furnitureType;
    public HousingFurnitureCategory Category => _category;
    public HousingUiFunctionType UiFunctionType => _uiFunctionType;
    public HousingLayerType LayerType => GetLayerType();
    public int LayerOrder => (int)GetLayerType();
    public string InteractionMessage => _interactionMessage;
    public bool IsAutoMoveTarget => _isAutoMoveTarget;
    public bool HasMotionInteraction => _hasMotionInteraction;
    public float InteractionSeconds => Mathf.Max(0f, _interactionSeconds);
    public float WaitAfterInteractionSeconds => Mathf.Max(0f, _waitAfterInteractionSeconds);
    public bool IsCharacterInteraction => _furnitureType == HousingFurnitureType.Interaction;
    public bool IsUiFunction => _furnitureType == HousingFurnitureType.UiFunction;
    public bool IsNonInteractive => _furnitureType == HousingFurnitureType.Background
        || _furnitureType == HousingFurnitureType.Decoration
        || _furnitureType == HousingFurnitureType.None;

    private void Awake()
    {
        // 기능: 가구 표시 Image와 이름 TMP 참조를 준비한다.
        EnsureVisualTargets();

        if (_interactionPoint == null && transform as RectTransform == null)
            Debug.LogWarning("[HousingFurnitureView] 상호작용 위치를 찾지 못했습니다.", this);
    }

    private void OnDisable()
    {
        // 기능: 비활성화 시 진행 중인 롱터치 판정을 취소한다.
        CancelLongPress();
    }

    public void InitializeSlotId(int _newSlotId)
    {
        // 기능: 슬롯 ID가 비어 있는 프로토타입 가구에 임시 슬롯 ID를 부여한다.
        if (_slotId >= 0)
            return;

        _slotId = _newSlotId;
    }

    public void ApplyFurnitureDefinition(HousingFurnitureDefinition _definition)
    {
        // 기능: Model의 가구 정의를 실제 배치된 프로토타입 View 표시 정보에 반영한다.
        if (_definition == null)
            return;

        _furnitureName = _definition.DisplayName;
        _furnitureType = _definition.FurnitureType;
        _category = _definition.Category;
        _uiFunctionType = _definition.UiFunctionType;
        _manualLayer = _definition.LayerType;
        _interactionMessage = _definition.InteractionMessage;

        EnsureVisualTargets();

        if (_skinImage != null)
            _skinImage.color = _definition.PrototypeColor;
    }

    public Vector2 GetInteractionPosition()
    {
        // 기능: 캐릭터가 가구 상호작용을 위해 이동할 UI 좌표를 반환한다.
        RectTransform _rectTransform = _interactionPoint != null
            ? _interactionPoint
            : transform as RectTransform;

        if (_rectTransform == null)
        {
            Debug.LogWarning("[HousingFurnitureView] 상호작용 위치를 찾지 못했습니다.", this);
            return Vector2.zero;
        }

        return _rectTransform.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData _eventData)
    {
        // 기능: 터치 시작 시 롱터치 판정을 시작한다.
        CancelLongPress();
        _isPointerDown = true;
        _isLongPressed = false;
        _longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        // 기능: 롱터치가 아니었던 터치 종료만 짧은 클릭 이벤트로 전달한다.
        bool _shouldInvokeShortClick = _isPointerDown && !_isLongPressed;
        CancelLongPress();

        if (!_shouldInvokeShortClick)
            return;

        ShortClicked?.Invoke(this);
        Clicked?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        // 기능: 터치 포인터가 가구 영역을 벗어나면 롱터치 판정을 취소한다.
        CancelLongPress();
    }

    private IEnumerator LongPressRoutine()
    {
        // 기능: 지정 시간 동안 터치가 유지되면 롱터치 이벤트를 발생시킨다.
        yield return new WaitForSecondsRealtime(_longPressSeconds);

        if (!_isPointerDown)
            yield break;

        _isLongPressed = true;
        LongPressed?.Invoke(this);
    }

    private void CancelLongPress()
    {
        // 기능: 터치 상태와 롱터치 코루틴을 초기화한다.
        _isPointerDown = false;

        if (_longPressCoroutine == null)
            return;

        StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = null;
    }

    private void EnsureVisualTargets()
    {
        // 기능: Inspector 참조가 비어 있을 때 현재 오브젝트와 하위 TMP에서 표시 대상을 찾는다.
        if (_skinImage == null)
            _skinImage = GetComponent<Image>();

        if (_nameText != null)
            return;

        _nameText = GetComponentInChildren<TMP_Text>(true);
    }

    private HousingLayerType GetLayerType()
    {
        // 기능: 가구 카테고리를 하우징 표시 레이어로 변환한다.
        if (!_useCategoryLayer)
            return _manualLayer;

        if (_furnitureType == HousingFurnitureType.Background)
            return HousingLayerType.Background;

        switch (_category)
        {
            case HousingFurnitureCategory.Background:
                return HousingLayerType.Background;
            case HousingFurnitureCategory.Large:
                return HousingLayerType.LargeFurniture;
            case HousingFurnitureCategory.Medium:
                return HousingLayerType.MediumFurniture;
            case HousingFurnitureCategory.Small:
                return HousingLayerType.SmallFurniture;
            default:
                return _manualLayer;
        }
    }
}
