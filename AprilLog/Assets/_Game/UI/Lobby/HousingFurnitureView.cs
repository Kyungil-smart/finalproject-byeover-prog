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

    [Header("상호작용 설정")]
    [Tooltip("플레이어가 가구와 상호작용하기 위해 이동할 위치")]
    [SerializeField] private RectTransform _interactionPoint;
    [Tooltip("가구 상호작용 시 Housing_InteractionTexture 위치에 표시할 문구")]
    [SerializeField] private string _interactionMessage = "가구와 상호작용했습니다.";

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
    public string InteractionMessage => _interactionMessage;

    private void Awake()
    {
        EnsureVisualTargets();

        if (_interactionPoint == null && transform as RectTransform == null)
            Debug.LogWarning("[HousingFurnitureView] 상호작용 위치를 찾지 못했습니다.", this);
    }

    private void OnDisable()
    {
        CancelLongPress();
    }

    public void InitializeSlotId(int _newSlotId)
    {
        if (_slotId >= 0)
            return;

        _slotId = _newSlotId;
    }

    public void ApplyFurnitureSkin(int _furnitureId, string _displayName, Color _displayColor, string _message)
    {
        _furnitureName = _displayName;
        _interactionMessage = _message;

        EnsureVisualTargets();

        if (_skinImage != null)
            _skinImage.color = _displayColor;

        if (_nameText != null)
            _nameText.text = _displayName;
    }

    public Vector2 GetInteractionPosition()
    {
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
        CancelLongPress();
        _isPointerDown = true;
        _isLongPressed = false;
        _longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        bool _shouldInvokeShortClick = _isPointerDown && !_isLongPressed;
        CancelLongPress();

        if (!_shouldInvokeShortClick)
            return;

        ShortClicked?.Invoke(this);
        Clicked?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData _eventData)
    {
        CancelLongPress();
    }

    private IEnumerator LongPressRoutine()
    {
        yield return new WaitForSecondsRealtime(_longPressSeconds);

        if (!_isPointerDown)
            yield break;

        _isLongPressed = true;
        LongPressed?.Invoke(this);
    }

    private void CancelLongPress()
    {
        _isPointerDown = false;

        if (_longPressCoroutine == null)
            return;

        StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = null;
    }

    private void EnsureVisualTargets()
    {
        if (_skinImage == null)
            _skinImage = GetComponent<Image>();

        if (_nameText != null)
            return;

        _nameText = GetComponentInChildren<TMP_Text>(true);
    }
}
