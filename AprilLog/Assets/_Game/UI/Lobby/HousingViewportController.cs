//담당자: 조규민
// 포인터 Raycast 차단 판정 결과를 캐싱해 드래그/핀치 입력 중 반복 계층 탐색을 줄임

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// 하우징 방 UI의 이동과 확대·축소 입력을 관리합니다.
/// </summary>
// 단일 터치 드래그와 두 손가락 확대·축소 입력 처리
// 화면 경계에 맞춘 콘텐츠 위치 제한과 입력 종료 상태 초기화
// 버튼 등 상호작용 UI 위에서 시작된 제스처 차단
public class HousingViewportController : MonoBehaviour
{
    private const int MaxSupportedTouchCount = 2;
    private const float MinValidScale = 0.01f;
    private const float ScaleEpsilon = 0.0001f;

    [Header("화면 연결")]
    [Tooltip("하우징 방이 표시되는 고정 영역입니다.")]
    [SerializeField] private RectTransform _viewport;
    [Tooltip("배경, 가구, 플레이어를 함께 이동하고 확대할 방 루트입니다.")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private GraphicRaycaster _graphicRaycaster;

    [Header("이동 설정")]
    [Tooltip("손가락 또는 마우스 이동량에 적용할 배율입니다.")]
    [SerializeField] private float _dragSpeed = 1f;
    [Tooltip("탭과 드래그를 구분하는 최소 화면 이동 거리입니다.")]
    [SerializeField] private float _dragThreshold = 10f;

    [Header("확대 및 축소 설정")]
    [Tooltip("핀치 거리 변화에 적용할 확대·축소 속도입니다.")]
    [SerializeField] private float _pinchZoomSpeed = 0.005f;
    [Tooltip("마우스 휠 입력에 적용할 확대·축소 속도입니다.")]
    [SerializeField] private float _mouseWheelZoomSpeed = 0.001f;
    [SerializeField] private float _minZoom = 1f;
    [SerializeField] private float _maxZoom = 2f;

    [Header("초기화 설정")]
    [Tooltip("하우징 페이지를 다시 열 때 최초 위치와 배율로 복원합니다.")]
    [SerializeField] private bool _resetOnEnable = true;

    private readonly List<RaycastResult> _raycastResults = new();
    private readonly Dictionary<Transform, bool> _interactiveTransformCache = new();

    private Canvas _parentCanvas;
    private EventSystem _eventSystem;
    private PointerEventData _pointerEventData;
    private Vector2 _initialAnchoredPosition;
    private Vector3 _initialLocalScale;
    private Vector2 _dragStartScreenPosition;
    private Vector2 _previousPointerPosition;
    private int _activeTouchId = -1;
    private bool _isInitialized;
    private bool _isPointerTracking;
    private bool _isDragging;
    private bool _isPinching;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        _interactiveTransformCache.Clear();
        ResetInputState();

        if (_resetOnEnable)
        {
            RestoreInitialView();
            return;
        }

        ClampContentPosition();
    }

    private void OnDisable()
    {
        _interactiveTransformCache.Clear();
        ResetInputState();
    }

    private void OnValidate()
    {
        _dragSpeed = Mathf.Max(0f, _dragSpeed);
        _dragThreshold = Mathf.Max(0f, _dragThreshold);
        _pinchZoomSpeed = Mathf.Max(0f, _pinchZoomSpeed);
        _mouseWheelZoomSpeed = Mathf.Max(0f, _mouseWheelZoomSpeed);
        _minZoom = Mathf.Max(MinValidScale, _minZoom);
        _maxZoom = Mathf.Max(_minZoom, _maxZoom);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (_isInitialized)
        {
            ClampContentPosition();
        }
    }

    private void Update()
    {
        if (_isInitialized == false)
        {
            return;
        }

#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_viewport == null || _content == null)
        {
            Debug.LogWarning("[HousingViewportController] Viewport와 Content 연결이 필요합니다.", this);
            return;
        }

        _parentCanvas = GetComponentInParent<Canvas>();

        if (_graphicRaycaster == null && _parentCanvas != null)
        {
            _graphicRaycaster = _parentCanvas.GetComponent<GraphicRaycaster>();
        }

        ResolveEventSystem();
        NormalizeZoomRange();

        _initialAnchoredPosition = _content.anchoredPosition;
        _initialLocalScale = _content.localScale;
        _isInitialized = true;
    }

    private void NormalizeZoomRange()
    {
        _minZoom = Mathf.Max(MinValidScale, _minZoom);
        _maxZoom = Mathf.Max(_minZoom, _maxZoom);

        float _currentZoom = GetCurrentZoom();
        SetContentScale(Mathf.Clamp(_currentZoom, _minZoom, _maxZoom));
    }

    // 활성 터치 개수에 따른 드래그 또는 핀치 입력 분기
    private void HandleTouchInput()
    {
        Touchscreen _touchscreen = Touchscreen.current;

        if (_touchscreen == null)
        {
            ResetInputState();
            return;
        }

        TouchControl _firstTouch = null;
        TouchControl _secondTouch = null;
        int _touchCount = CollectPressedTouches(_touchscreen, ref _firstTouch, ref _secondTouch);

        if (_touchCount == 1)
        {
            HandleSingleTouch(_firstTouch);
            return;
        }

        if (_touchCount == MaxSupportedTouchCount)
        {
            HandlePinchTouch(_firstTouch, _secondTouch);
            return;
        }

        ResetInputState();
    }

    private int CollectPressedTouches(
        Touchscreen _touchscreen,
        ref TouchControl _firstTouch,
        ref TouchControl _secondTouch)
    {
        int _touchCount = 0;

        foreach (TouchControl _touch in _touchscreen.touches)
        {
            if (_touch.press.isPressed == false)
            {
                continue;
            }

            _touchCount++;

            if (_firstTouch == null)
            {
                _firstTouch = _touch;
            }
            else if (_secondTouch == null)
            {
                _secondTouch = _touch;
            }

            if (_touchCount > MaxSupportedTouchCount)
            {
                break;
            }
        }

        return _touchCount;
    }

    // 단일 터치 시작·이동·종료 단계별 화면 드래그 처리
    private void HandleSingleTouch(TouchControl _touch)
    {
        if (_touch == null)
        {
            ResetInputState();
            return;
        }

        int _touchId = _touch.touchId.ReadValue();
        Vector2 _screenPosition = _touch.position.ReadValue();

        if (_isPinching || _activeTouchId != _touchId)
        {
            _isPinching = false;
            BeginPointerTracking(_screenPosition, _touchId);
            return;
        }

        if (_isPointerTracking)
        {
            UpdateDrag(_screenPosition);
        }
    }

    // 두 터치 간 거리 변화 기반 확대·축소 처리
    private void HandlePinchTouch(TouchControl _firstTouch, TouchControl _secondTouch)
    {
        ResetDragState();

        if (_firstTouch == null || _secondTouch == null)
        {
            _isPinching = false;
            return;
        }

        Vector2 _firstPosition = _firstTouch.position.ReadValue();
        Vector2 _secondPosition = _secondTouch.position.ReadValue();

        if (_isPinching == false)
        {
            _isPinching = CanStartGesture(_firstPosition) && CanStartGesture(_secondPosition);
            return;
        }

        Vector2 _firstPreviousPosition = _firstPosition - _firstTouch.delta.ReadValue();
        Vector2 _secondPreviousPosition = _secondPosition - _secondTouch.delta.ReadValue();
        float _previousDistance = Vector2.Distance(_firstPreviousPosition, _secondPreviousPosition);
        float _currentDistance = Vector2.Distance(_firstPosition, _secondPosition);
        float _zoomDelta = (_currentDistance - _previousDistance) * _pinchZoomSpeed;
        Vector2 _pinchCenter = (_firstPosition + _secondPosition) * 0.5f;

        ApplyZoom(_zoomDelta, _pinchCenter);
    }

    private void BeginPointerTracking(Vector2 _screenPosition, int _pointerId)
    {
        ResetDragState();
        _activeTouchId = _pointerId;

        if (CanStartGesture(_screenPosition) == false)
        {
            return;
        }

        _dragStartScreenPosition = _screenPosition;
        _previousPointerPosition = _screenPosition;
        _isPointerTracking = true;
    }

    private void UpdateDrag(Vector2 _screenPosition)
    {
        if (_isDragging == false)
        {
            float _distance = Vector2.Distance(_dragStartScreenPosition, _screenPosition);

            if (_distance < _dragThreshold)
            {
                _previousPointerPosition = _screenPosition;
                return;
            }

            _isDragging = true;
        }

        Vector2 _screenDelta = _screenPosition - _previousPointerPosition;
        _previousPointerPosition = _screenPosition;
        MoveContent(_screenDelta);
    }

    private void MoveContent(Vector2 _screenDelta)
    {
        float _canvasScaleFactor = GetCanvasScaleFactor();
        _content.anchoredPosition += _screenDelta * (_dragSpeed / _canvasScaleFactor);
        ClampContentPosition();
    }

    private void ApplyZoom(float _zoomDelta, Vector2 _screenFocusPosition)
    {
        float _currentZoom = GetCurrentZoom();
        float _targetZoom = Mathf.Clamp(_currentZoom + _zoomDelta, _minZoom, _maxZoom);

        if (Mathf.Abs(_targetZoom - _currentZoom) <= ScaleEpsilon)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport,
                _screenFocusPosition,
                GetEventCamera(),
                out Vector2 _focusLocalPosition) == false)
        {
            return;
        }

        float _scaleRatio = _targetZoom / _currentZoom;
        Vector2 _focusOffset = _focusLocalPosition - _content.anchoredPosition;

        SetContentScale(_targetZoom);
        _content.anchoredPosition = _focusLocalPosition - (_focusOffset * _scaleRatio);
        ClampContentPosition();
    }

    private void SetContentScale(float _zoom)
    {
        Vector3 _localScale = _content.localScale;
        _localScale.x = _zoom;
        _localScale.y = _zoom;
        _content.localScale = _localScale;
    }

    private float GetCurrentZoom()
    {
        return Mathf.Max(MinValidScale, _content.localScale.x);
    }

    // 확대 배율과 Viewport 크기 기준 콘텐츠 이동 범위 제한
    private void ClampContentPosition()
    {
        if (_viewport == null || _content == null)
        {
            return;
        }

        float _zoom = GetCurrentZoom();
        Vector2 _scaledContentSize = _content.rect.size * _zoom;
        Vector2 _viewportSize = _viewport.rect.size;
        Vector2 _allowedOffset = new Vector2(
            Mathf.Max(0f, (_scaledContentSize.x - _viewportSize.x) * 0.5f),
            Mathf.Max(0f, (_scaledContentSize.y - _viewportSize.y) * 0.5f)
        );

        Vector2 _position = _content.anchoredPosition;
        _position.x = Mathf.Clamp(
            _position.x,
            _initialAnchoredPosition.x - _allowedOffset.x,
            _initialAnchoredPosition.x + _allowedOffset.x
        );
        _position.y = Mathf.Clamp(
            _position.y,
            _initialAnchoredPosition.y - _allowedOffset.y,
            _initialAnchoredPosition.y + _allowedOffset.y
        );
        _content.anchoredPosition = _position;
    }

    private bool CanStartGesture(Vector2 _screenPosition)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(
                _viewport,
                _screenPosition,
                GetEventCamera()) == false)
        {
            return false;
        }

        return IsBlockedByInteractiveUi(_screenPosition) == false;
    }

    // 포인터 위치의 버튼·스크롤 등 상호작용 UI 검사
    private bool IsBlockedByInteractiveUi(Vector2 _screenPosition)
    {
        ResolveEventSystem();

        if (_graphicRaycaster == null || _pointerEventData == null)
        {
            return false;
        }

        _pointerEventData.Reset();
        _pointerEventData.position = _screenPosition;
        _raycastResults.Clear();
        _graphicRaycaster.Raycast(_pointerEventData, _raycastResults);

        for (int _index = 0; _index < _raycastResults.Count; _index++)
        {
            Transform _hitTransform = _raycastResults[_index].gameObject.transform;

            if (IsInteractiveTransform(_hitTransform))
            {
                return true;
            }

            if (_hitTransform == _viewport || _hitTransform.IsChildOf(_content))
            {
                continue;
            }

            // 방 콘텐츠가 아닌 별도 UI가 포인터를 받으면 팝업 배경을 포함해 조작을 차단합니다.
            return true;
        }

        return false;
    }

    private bool IsInteractiveTransform(Transform _target)
    {
        if (_target == null)
        {
            return false;
        }

        if (_interactiveTransformCache.TryGetValue(_target, out bool _isInteractive))
        {
            return _isInteractive;
        }

        _isInteractive = ResolveInteractiveTransform(_target);
        _interactiveTransformCache[_target] = _isInteractive;
        return _isInteractive;
    }

    private static bool ResolveInteractiveTransform(Transform _target)
    {
        if (_target.GetComponentInParent<Selectable>() != null)
        {
            return true;
        }

        if (_target.GetComponentInParent<ScrollRect>() != null)
        {
            return true;
        }

        if (_target.GetComponentInParent<HousingPlayerTouchReaction>() != null)
        {
            return true;
        }

        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(_target.gameObject) != null;
    }

    private void ResolveEventSystem()
    {
        if (_eventSystem == EventSystem.current && _pointerEventData != null)
        {
            return;
        }

        _eventSystem = EventSystem.current;
        _pointerEventData = _eventSystem != null ? new PointerEventData(_eventSystem) : null;
    }

    private Camera GetEventCamera()
    {
        if (_parentCanvas == null || _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return _parentCanvas.worldCamera;
    }

    private float GetCanvasScaleFactor()
    {
        return _parentCanvas == null
            ? 1f
            : Mathf.Max(MinValidScale, _parentCanvas.scaleFactor);
    }

    private void RestoreInitialView()
    {
        _content.anchoredPosition = _initialAnchoredPosition;
        _content.localScale = _initialLocalScale;
        ClampContentPosition();
    }

    private void ResetInputState()
    {
        ResetDragState();
        _isPinching = false;
    }

    private void ResetDragState()
    {
        _activeTouchId = -1;
        _isPointerTracking = false;
        _isDragging = false;
    }

#if UNITY_EDITOR
    private void HandleMouseInput()
    {
        Mouse _mouse = Mouse.current;

        if (_mouse == null)
        {
            ResetInputState();
            return;
        }

        Vector2 _screenPosition = _mouse.position.ReadValue();

        if (_mouse.leftButton.wasPressedThisFrame)
        {
            BeginPointerTracking(_screenPosition, 0);
        }

        if (_mouse.leftButton.isPressed && _isPointerTracking)
        {
            UpdateDrag(_screenPosition);
        }

        if (_mouse.leftButton.wasReleasedThisFrame)
        {
            ResetDragState();
        }

        float _scrollDelta = _mouse.scroll.ReadValue().y;

        if (Mathf.Approximately(_scrollDelta, 0f) || CanStartGesture(_screenPosition) == false)
        {
            return;
        }

        ApplyZoom(_scrollDelta * _mouseWheelZoomSpeed, _screenPosition);
    }
#endif
}
