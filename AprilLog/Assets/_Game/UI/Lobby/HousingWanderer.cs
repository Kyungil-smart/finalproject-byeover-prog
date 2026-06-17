//담당자: 조규민
//설명: 하우징 페이지에 배치된 임시 플레이어가 지정 영역 안에서 이동하고 터치 이벤트를 전달한다.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 하우징 임시 플레이어의 자동 배회, 상호작용 이동, 터치 이벤트를 담당한다.
/// </summary>
public class HousingWanderer : MonoBehaviour, IPointerClickHandler
{
    private enum MoveState
    {
        Idle,
        Walking,
        Interacting,
        Waiting
    }

    [Header("이동 영역")]
    [Tooltip("임시 플레이어가 벗어나지 않아야 하는 RectTransform 영역")]
    [SerializeField] private RectTransform _moveArea;

    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 180f;
    [SerializeField] private float _arrivalDistance = 8f;
    [SerializeField] private float _defaultInteractionSeconds = 1.2f;
    [SerializeField] private float _defaultWaitAfterInteractionSeconds = 0.8f;
    [SerializeField] private Vector2 _edgePadding = new Vector2(70f, 70f);

    public event Action Clicked;

    private readonly List<HousingFurnitureView> _routeFurnitures = new List<HousingFurnitureView>();

    private Graphic _touchGraphic;
    private RectTransform _rectTransform;
    private HousingFurnitureView _currentFurniture;
    private HousingFurnitureView _targetFurniture;
    private Vector2 _targetPosition;
    private Action _arrivalCallback;
    private MoveState _state = MoveState.Idle;
    private float _stateTimer;
    private int _routeIndex = -1;

    private void Awake()
    {
        // 기능: 캐릭터 RectTransform과 이동 영역, 터치 대상 Graphic을 준비한다.
        _rectTransform = transform as RectTransform;

        if (_rectTransform == null)
            Debug.LogWarning("[HousingWanderer] RectTransform이 필요합니다.", this);

        if (_moveArea == null)
            Debug.LogWarning("[HousingWanderer] 이동 영역이 연결되지 않았습니다.", this);

        EnsureTouchTarget();
    }

    private void OnEnable()
    {
        // 기능: 하우징 재진입 시 현재 위치에서 자동 이동을 다시 시작한다.
        _arrivalCallback = null;
        _targetFurniture = null;
        _state = MoveState.Idle;
        _stateTimer = 0f;
        StartNextRouteMove();
    }

    private void Update()
    {
        // 기능: 현재 이동 상태에 따라 자동 이동과 상호작용 대기를 처리한다.
        if (_rectTransform == null || _moveArea == null)
            return;

        switch (_state)
        {
            case MoveState.Walking:
                UpdateWalking();
                break;
            case MoveState.Interacting:
                TickStateTimer(FinishInteraction);
                break;
            case MoveState.Waiting:
                TickStateTimer(StartNextRouteMove);
                break;
            case MoveState.Idle:
                StartNextRouteMove();
                break;
        }
    }

    public void SetRouteFurnitures(IReadOnlyList<HousingFurnitureView> _furnitures)
    {
        // 기능: 자동 이동 경로 후보를 시계 방향 순서로 정렬해 저장한다.
        _routeFurnitures.Clear();

        if (_furnitures == null)
        {
            _routeIndex = -1;
            return;
        }

        for (int _index = 0; _index < _furnitures.Count; _index++)
        {
            HousingFurnitureView _furniture = _furnitures[_index];
            if (_furniture == null || !_furniture.IsAutoMoveTarget)
                continue;

            if (_furniture.IsNonInteractive)
                continue;

            _routeFurnitures.Add(_furniture);
        }

        _routeFurnitures.Sort(CompareClockwise);
        _routeIndex = FindNearestRouteIndex();
    }

    public void MoveToFurniture(HousingFurnitureView _furniture, Action _onArrived)
    {
        MoveToFurniture(_furniture, _onArrived, true);
    }

    public void MoveToFurniture(HousingFurnitureView _furniture, Action _onArrived, bool _canImmediateMove)
    {
        // 기능: 유저 클릭 또는 가구 변경 요청에 따라 지정 가구로 이동하고 이후 자동 경로를 이어간다.
        if (_furniture == null)
            return;

        _arrivalCallback = _onArrived;

        if (_canImmediateMove && _furniture.HasMotionInteraction)
        {
            MoveImmediatelyToFurniture(_furniture);
            return;
        }

        MoveToFurnitureByWalking(_furniture);
    }

    public void MoveToInteractionTarget(Vector2 _targetPosition, Action _onArrived)
    {
        // 기능: 기존 호출부 호환용으로 좌표 기반 상호작용 이동을 지원한다.
        if (_rectTransform == null || _moveArea == null)
            return;

        this._targetPosition = ClampToMoveArea(_targetPosition);
        _targetFurniture = null;
        _arrivalCallback = _onArrived;
        _state = MoveState.Walking;
        _stateTimer = 0f;
    }

    public void MoveImmediatelyToInteractionTarget(Vector2 _targetPosition, Action _onArrived)
    {
        // 기능: 기존 호출부 호환용으로 좌표 기반 즉시 이동을 지원한다.
        if (_rectTransform == null || _moveArea == null)
            return;

        this._targetPosition = ClampToMoveArea(_targetPosition);
        _rectTransform.anchoredPosition = this._targetPosition;
        _targetFurniture = null;
        _arrivalCallback = _onArrived;
        BeginInteraction();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        // 기능: 캐릭터 터치 입력을 외부 Controller 이벤트로 전달한다.
        Clicked?.Invoke();
    }

    private void MoveImmediatelyToFurniture(HousingFurnitureView _furniture)
    {
        if (_rectTransform == null || _moveArea == null)
            return;

        _targetFurniture = _furniture;
        _targetPosition = ClampToMoveArea(_furniture.GetInteractionPosition());
        _rectTransform.anchoredPosition = _targetPosition;
        BeginInteraction();
    }

    private void MoveToFurnitureByWalking(HousingFurnitureView _furniture)
    {
        if (_rectTransform == null || _moveArea == null)
            return;

        _targetFurniture = _furniture;
        _targetPosition = ClampToMoveArea(_furniture.GetInteractionPosition());
        _state = MoveState.Walking;
        _stateTimer = 0f;
    }

    private void UpdateWalking()
    {
        Vector2 _currentPosition = _rectTransform.anchoredPosition;
        Vector2 _nextPosition = Vector2.MoveTowards(
            _currentPosition,
            _targetPosition,
            _moveSpeed * Time.deltaTime);

        _rectTransform.anchoredPosition = ClampToMoveArea(_nextPosition);

        if (Vector2.Distance(_rectTransform.anchoredPosition, _targetPosition) > _arrivalDistance)
            return;

        BeginInteraction();
    }

    private void BeginInteraction()
    {
        _currentFurniture = _targetFurniture;
        _routeIndex = FindRouteIndex(_currentFurniture);
        _arrivalCallback?.Invoke();
        _arrivalCallback = null;

        float _interactionSeconds = _currentFurniture != null
            ? _currentFurniture.InteractionSeconds
            : _defaultInteractionSeconds;

        _stateTimer = Mathf.Max(0f, _interactionSeconds);
        _state = _stateTimer > 0f ? MoveState.Interacting : MoveState.Waiting;

        if (_state == MoveState.Waiting)
            FinishInteraction();
    }

    private void FinishInteraction()
    {
        float _waitSeconds = _currentFurniture != null
            ? _currentFurniture.WaitAfterInteractionSeconds
            : _defaultWaitAfterInteractionSeconds;

        _stateTimer = Mathf.Max(0f, _waitSeconds);
        _state = _stateTimer > 0f ? MoveState.Waiting : MoveState.Idle;
    }

    private void StartNextRouteMove()
    {
        if (_routeFurnitures.Count == 0)
        {
            _state = MoveState.Idle;
            return;
        }

        _routeIndex = GetNextRouteIndex();
        MoveToFurnitureByWalking(_routeFurnitures[_routeIndex]);
    }

    private void TickStateTimer(Action _onFinished)
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f)
            return;

        _onFinished?.Invoke();
    }

    private int GetNextRouteIndex()
    {
        if (_routeFurnitures.Count == 0)
            return -1;

        if (_routeIndex < 0)
            return 0;

        return (_routeIndex + 1) % _routeFurnitures.Count;
    }

    private int FindNearestRouteIndex()
    {
        if (_rectTransform == null || _routeFurnitures.Count == 0)
            return -1;

        int _nearestIndex = 0;
        float _nearestDistance = float.MaxValue;
        Vector2 _currentPosition = _rectTransform.anchoredPosition;

        for (int _index = 0; _index < _routeFurnitures.Count; _index++)
        {
            float _distance = Vector2.SqrMagnitude(_routeFurnitures[_index].GetInteractionPosition() - _currentPosition);
            if (_distance >= _nearestDistance)
                continue;

            _nearestDistance = _distance;
            _nearestIndex = _index;
        }

        return _nearestIndex;
    }

    private int FindRouteIndex(HousingFurnitureView _furniture)
    {
        if (_furniture == null)
            return _routeIndex;

        for (int _index = 0; _index < _routeFurnitures.Count; _index++)
        {
            if (_routeFurnitures[_index] == _furniture)
                return _index;
        }

        return _routeIndex;
    }

    private int CompareClockwise(HousingFurnitureView _left, HousingFurnitureView _right)
    {
        if (_left == null && _right == null)
            return 0;

        if (_left == null)
            return 1;

        if (_right == null)
            return -1;

        float _leftAngle = GetClockwiseAngle(_left.GetInteractionPosition());
        float _rightAngle = GetClockwiseAngle(_right.GetInteractionPosition());
        return _leftAngle.CompareTo(_rightAngle);
    }

    private float GetClockwiseAngle(Vector2 _position)
    {
        float _angle = Mathf.Atan2(_position.y, _position.x) * Mathf.Rad2Deg;
        _angle = 90f - _angle;

        if (_angle < 0f)
            _angle += 360f;

        return _angle;
    }

    private void EnsureTouchTarget()
    {
        // 기능: 캐릭터 UI가 터치 이벤트를 받을 수 있도록 Graphic Raycast를 켠다.
        _touchGraphic = GetComponent<Graphic>();
        if (_touchGraphic == null)
        {
            Debug.LogWarning("[HousingWanderer] 캐릭터 터치를 받으려면 Image 같은 Graphic 컴포넌트가 필요합니다.", this);
            return;
        }

        _touchGraphic.raycastTarget = true;
    }

    private Vector2 ClampToMoveArea(Vector2 _position)
    {
        // 기능: 캐릭터 위치가 이동 영역과 여백 밖으로 나가지 않도록 제한한다.
        Rect _rect = _moveArea.rect;
        GetMoveBounds(_rect, out float _minX, out float _maxX, out float _minY, out float _maxY);

        _position.x = Mathf.Clamp(_position.x, _minX, _maxX);
        _position.y = Mathf.Clamp(_position.y, _minY, _maxY);
        return _position;
    }

    private void GetMoveBounds(Rect _rect, out float _minX, out float _maxX, out float _minY, out float _maxY)
    {
        // 기능: 이동 영역 Rect와 가장자리 여백을 기준으로 실제 이동 가능 범위를 계산한다.
        _minX = _rect.xMin + _edgePadding.x;
        _maxX = _rect.xMax - _edgePadding.x;
        _minY = _rect.yMin + _edgePadding.y;
        _maxY = _rect.yMax - _edgePadding.y;

        if (_minX > _maxX)
        {
            _minX = _rect.xMin;
            _maxX = _rect.xMax;
        }

        if (_minY <= _maxY)
            return;

        _minY = _rect.yMin;
        _maxY = _rect.yMax;
    }
}
