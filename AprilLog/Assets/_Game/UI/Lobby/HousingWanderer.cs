//담당자: 조규민
//설명: 하우징 페이지에 배치된 임시 플레이어가 지정 영역 안에서 이동하고 터치 이벤트를 전달한다.

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 하우징 임시 플레이어의 자동 배회, 상호작용 이동, 터치 이벤트를 담당한다.
/// </summary>
public class HousingWanderer : MonoBehaviour, IPointerClickHandler
{
    [Header("이동 영역")]
    [Tooltip("임시 플레이어가 벗어나지 않아야 하는 RectTransform 영역")]
    [SerializeField] private RectTransform _moveArea;

    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 180f;
    [SerializeField] private float _arrivalDistance = 8f;
    [SerializeField] private float _minWaitTime = 0.4f;
    [SerializeField] private float _maxWaitTime = 1.2f;
    [SerializeField] private Vector2 _edgePadding = new Vector2(70f, 70f);

    public event Action Clicked;

    private Graphic _touchGraphic;
    private RectTransform _rectTransform;
    private Vector2 _targetPosition;
    private float _waitTimer;
    private Action _arrivalCallback;
    private bool _isInteractionMoving;

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
        // 기능: 활성화 시 자동 배회 상태를 초기화하고 첫 목표 위치를 선택한다.
        _waitTimer = 0f;
        _arrivalCallback = null;
        _isInteractionMoving = false;
        PickNextTarget();
    }

    public void MoveToInteractionTarget(Vector2 _targetPosition, Action _onArrived)
    {
        // 기능: 가구 상호작용 위치로 이동하도록 목표 좌표와 도착 콜백을 설정한다.
        if (_rectTransform == null || _moveArea == null)
            return;

        this._targetPosition = ClampToMoveArea(_targetPosition);
        _arrivalCallback = _onArrived;
        _isInteractionMoving = true;
        _waitTimer = 0f;
    }

    public void MoveImmediatelyToInteractionTarget(Vector2 _targetPosition, Action _onArrived)
    {
        // 기능: 프로토타입 연출용으로 캐릭터를 상호작용 위치에 즉시 배치하고 콜백을 실행한다.
        if (_rectTransform == null || _moveArea == null)
            return;

        this._targetPosition = ClampToMoveArea(_targetPosition);
        _rectTransform.anchoredPosition = this._targetPosition;
        _arrivalCallback = null;
        _isInteractionMoving = false;
        _waitTimer = UnityEngine.Random.Range(_minWaitTime, Mathf.Max(_minWaitTime, _maxWaitTime));
        _onArrived?.Invoke();
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        // 기능: 캐릭터 터치 입력을 외부 Controller 이벤트로 전달한다.
        Clicked?.Invoke();
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

    private void Update()
    {
        // 기능: 대기 시간이 끝나면 목표 지점까지 캐릭터를 계속 이동시킨다.
        if (_rectTransform == null || _moveArea == null)
            return;

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.deltaTime;
            return;
        }

        MoveToTarget();
    }

    private void MoveToTarget()
    {
        // 기능: 현재 위치에서 목표 위치로 이동하고 도착 시 배회 또는 상호작용 콜백을 처리한다.
        Vector2 _currentPosition = _rectTransform.anchoredPosition;
        Vector2 _nextPosition = Vector2.MoveTowards(
            _currentPosition,
            _targetPosition,
            _moveSpeed * Time.deltaTime);

        _rectTransform.anchoredPosition = ClampToMoveArea(_nextPosition);

        if (Vector2.Distance(_rectTransform.anchoredPosition, _targetPosition) > _arrivalDistance)
            return;

        if (_isInteractionMoving)
        {
            _isInteractionMoving = false;
            _arrivalCallback?.Invoke();
            _arrivalCallback = null;
            _waitTimer = UnityEngine.Random.Range(_minWaitTime, Mathf.Max(_minWaitTime, _maxWaitTime));
            return;
        }

        _waitTimer = UnityEngine.Random.Range(_minWaitTime, Mathf.Max(_minWaitTime, _maxWaitTime));
        PickNextTarget();
    }

    private void PickNextTarget()
    {
        // 기능: 이동 영역 안에서 다음 자동 배회 목표 좌표를 무작위로 선택한다.
        if (_moveArea == null)
            return;

        Rect _rect = _moveArea.rect;
        GetMoveBounds(_rect, out float _minX, out float _maxX, out float _minY, out float _maxY);

        _targetPosition = new Vector2(
            UnityEngine.Random.Range(_minX, _maxX),
            UnityEngine.Random.Range(_minY, _maxY));
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
