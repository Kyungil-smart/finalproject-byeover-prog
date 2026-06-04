//담당자: 조규민
//설명: 하우징 페이지에 배치된 임시 플레이어가 지정 영역 안에서 자동으로 돌아다니도록 제어한다.

using UnityEngine;

/// <summary>
/// 하우징 임시 플레이어의 자동 배회 이동을 담당한다.
/// </summary>
public class HousingWanderer : MonoBehaviour
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

    private RectTransform _rectTransform;
    private Vector2 _targetPosition;
    private float _waitTimer;
    private System.Action _arrivalCallback;
    private bool _isInteractionMoving;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_rectTransform == null)
            Debug.LogWarning("[HousingWanderer] RectTransform이 필요합니다.", this);

        if (_moveArea == null)
            Debug.LogWarning("[HousingWanderer] 이동 영역이 연결되지 않았습니다.", this);
    }

    private void OnEnable()
    {
        _waitTimer = 0f;
        _arrivalCallback = null;
        _isInteractionMoving = false;
        PickNextTarget();
    }

    public void MoveToInteractionTarget(Vector2 _targetPosition, System.Action _onArrived)
    {
        if (_rectTransform == null || _moveArea == null)
            return;

        this._targetPosition = ClampToMoveArea(_targetPosition);
        _arrivalCallback = _onArrived;
        _isInteractionMoving = true;
        _waitTimer = 0f;
    }

    private void Update()
    {
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
            _waitTimer = Random.Range(_minWaitTime, Mathf.Max(_minWaitTime, _maxWaitTime));
            return;
        }

        _waitTimer = Random.Range(_minWaitTime, Mathf.Max(_minWaitTime, _maxWaitTime));
        PickNextTarget();
    }

    private void PickNextTarget()
    {
        if (_moveArea == null)
            return;

        Rect _rect = _moveArea.rect;
        float _minX = _rect.xMin + _edgePadding.x;
        float _maxX = _rect.xMax - _edgePadding.x;
        float _minY = _rect.yMin + _edgePadding.y;
        float _maxY = _rect.yMax - _edgePadding.y;

        if (_minX > _maxX)
        {
            _minX = _rect.xMin;
            _maxX = _rect.xMax;
        }

        if (_minY > _maxY)
        {
            _minY = _rect.yMin;
            _maxY = _rect.yMax;
        }

        _targetPosition = new Vector2(
            Random.Range(_minX, _maxX),
            Random.Range(_minY, _maxY));
    }

    private Vector2 ClampToMoveArea(Vector2 _position)
    {
        Rect _rect = _moveArea.rect;

        float _minX = _rect.xMin + _edgePadding.x;
        float _maxX = _rect.xMax - _edgePadding.x;
        float _minY = _rect.yMin + _edgePadding.y;
        float _maxY = _rect.yMax - _edgePadding.y;

        if (_minX > _maxX)
        {
            _minX = _rect.xMin;
            _maxX = _rect.xMax;
        }

        if (_minY > _maxY)
        {
            _minY = _rect.yMin;
            _maxY = _rect.yMax;
        }

        _position.x = Mathf.Clamp(_position.x, _minX, _maxX);
        _position.y = Mathf.Clamp(_position.y, _minY, _maxY);
        return _position;
    }
}
