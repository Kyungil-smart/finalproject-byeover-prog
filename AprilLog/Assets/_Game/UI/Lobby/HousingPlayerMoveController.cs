// 담당자: 조규민

using UnityEngine;

/// <summary>
/// 하우징 이동 가능 영역 안에서 플레이어를 자동 순회시킵니다.
/// </summary>
public class HousingPlayerMoveController : MonoBehaviour
{
    private const float ArrivalDistance = 0.01f;
    private const float DirectionFlipEpsilon = 0.001f;
    private const float MinPatrolDistance = 120f;
    private const float MaxPatrolDistance = 360f;
    private const float MinIdleTime = 2f;
    private const float MaxIdleTime = 3f;
    private const int AutoPatrolMaxPickCount = 30;

    [Header("이동 영역")]
    [Tooltip("플레이어가 이동할 수 있는 바닥 영역의 PolygonCollider2D입니다.")]
    [SerializeField] private PolygonCollider2D _moveArea;

    [Header("플레이어")]
    [Tooltip("실제로 이동시킬 플레이어 Transform입니다.")]
    [SerializeField] private Transform _player;

    [Header("방향 전환")]
    [Tooltip("이동 방향에 따라 좌우 반전할 플레이어 시각 Transform입니다. 비우면 Player를 사용합니다.")]
    [SerializeField] private Transform _playerVisual;
    [Tooltip("플레이어 반전 시 글자처럼 같이 뒤집히면 안 되는 자식 Transform 목록입니다.")]
    [SerializeField] private Transform[] _nonFlipTargets;
    [Tooltip("원본 스프라이트가 오른쪽을 보고 있으면 켭니다.")]
    [SerializeField] private bool _defaultFacesRight = true;

    [Header("이동 설정")]
    [Tooltip("초당 이동 거리입니다.")]
    [SerializeField] private float _moveSpeed = 3f;

    [Header("장애물 확장")]
    [Tooltip("추후 가구 장애물 판정에 사용할 Collider2D 목록입니다.")]
    [SerializeField] private Collider2D[] _obstacleColliders;

    private Vector3 _targetPosition;
    private bool _hasTarget;
    private float _idleTimer;
    private float _playerVisualBaseScaleX = 1f;
    private float[] _nonFlipBaseScaleXs;
    private Vector2[] _nonFlipBaseAnchoredPositions;

    private void Awake()
    {
        ResolvePlayerVisual();
        CacheFlipBaseScales();
    }

    private void Update()
    {
        UpdateAutoPatrol();
        MovePlayer();
    }

    public void StopMove()
    {
        _hasTarget = false;
        ResetIdleTimer();
    }

    private void MovePlayer()
    {
        if (_hasTarget == false)
        {
            return;
        }

        if (_player == null)
        {
            StopMove();
            return;
        }

        Vector3 _nextPosition = Vector3.MoveTowards(
            _player.position,
            _targetPosition,
            _moveSpeed * Time.deltaTime
        );

        UpdateFacingDirection(_nextPosition.x - _player.position.x);

        if (CanMoveTo(_nextPosition) == false)
        {
            StopMove();
            return;
        }

        _player.position = _nextPosition;

        if (Vector3.Distance(_player.position, _targetPosition) > ArrivalDistance)
        {
            return;
        }

        _player.position = _targetPosition;
        StopMove();
    }

    private void UpdateAutoPatrol()
    {
        if (_hasTarget)
        {
            return;
        }

        if (_player == null)
        {
            return;
        }

        if (_moveArea == null)
        {
            return;
        }

        _idleTimer -= Time.deltaTime;

        if (_idleTimer > 0f)
        {
            return;
        }

        if (TryGetRandomMovePoint(out Vector3 _randomPoint) == false)
        {
            ResetIdleTimer();
            return;
        }

        _targetPosition = _randomPoint;
        _hasTarget = true;
    }

    private bool TryGetRandomMovePoint(out Vector3 _worldPosition)
    {
        for (int _index = 0; _index < AutoPatrolMaxPickCount; _index++)
        {
            Vector2 _direction = Random.insideUnitCircle;

            if (_direction.sqrMagnitude < 0.01f)
            {
                continue;
            }

            _direction.Normalize();

            float _distance = Random.Range(MinPatrolDistance, MaxPatrolDistance);
            Vector3 _candidate = _player.position + new Vector3(_direction.x, _direction.y, 0f) * _distance;
            _candidate.z = _player.position.z;

            if (CanMoveTo(_candidate) == false)
            {
                continue;
            }

            UpdateFacingDirection(_candidate.x - _player.position.x);
            _worldPosition = _candidate;
            return true;
        }

        _worldPosition = default;
        return false;
    }

    private void ResetIdleTimer()
    {
        _idleTimer = Random.Range(MinIdleTime, MaxIdleTime);
    }

    private bool CanMoveTo(Vector3 _worldPosition)
    {
        if (_moveArea == null)
        {
            return false;
        }

        Vector2 _point = _worldPosition;

        if (_moveArea.OverlapPoint(_point) == false)
        {
            return false;
        }

        if (IsBlockedByObstacle(_point))
        {
            return false;
        }

        return true;
    }

    private bool IsBlockedByObstacle(Vector2 _point)
    {
        if (_obstacleColliders == null)
        {
            return false;
        }

        for (int _index = 0; _index < _obstacleColliders.Length; _index++)
        {
            Collider2D _obstacleCollider = _obstacleColliders[_index];

            if (_obstacleCollider == null)
            {
                continue;
            }

            if (_obstacleCollider.OverlapPoint(_point))
            {
                return true;
            }
        }

        return false;
    }

    private void ResolvePlayerVisual()
    {
        if (_playerVisual != null)
        {
            return;
        }

        _playerVisual = _player;
    }

    private void CacheFlipBaseScales()
    {
        if (_playerVisual != null)
        {
            _playerVisualBaseScaleX = Mathf.Abs(_playerVisual.localScale.x);
        }

        if (_nonFlipTargets == null)
        {
            _nonFlipBaseScaleXs = null;
            _nonFlipBaseAnchoredPositions = null;
            return;
        }

        _nonFlipBaseScaleXs = new float[_nonFlipTargets.Length];
        _nonFlipBaseAnchoredPositions = new Vector2[_nonFlipTargets.Length];

        for (int _index = 0; _index < _nonFlipTargets.Length; _index++)
        {
            Transform _target = _nonFlipTargets[_index];
            _nonFlipBaseScaleXs[_index] = _target != null ? Mathf.Abs(_target.localScale.x) : 1f;
            _nonFlipBaseAnchoredPositions[_index] = GetAnchoredPosition(_target);
        }
    }

    private void UpdateFacingDirection(float _deltaX)
    {
        if (_playerVisual == null)
        {
            return;
        }

        if (Mathf.Abs(_deltaX) <= DirectionFlipEpsilon)
        {
            return;
        }

        bool _isMovingRight = _deltaX > 0f;
        float _directionScale = _isMovingRight == _defaultFacesRight ? 1f : -1f;

        SetLocalScaleX(_playerVisual, _playerVisualBaseScaleX * _directionScale);
        UpdateNonFlipTargets(_directionScale);
    }

    private void UpdateNonFlipTargets(float _directionScale)
    {
        if (_nonFlipTargets == null || _nonFlipBaseScaleXs == null || _nonFlipBaseAnchoredPositions == null)
        {
            return;
        }

        for (int _index = 0; _index < _nonFlipTargets.Length; _index++)
        {
            Transform _target = _nonFlipTargets[_index];

            if (_target == null)
            {
                continue;
            }

            if (_target.IsChildOf(_playerVisual) == false)
            {
                continue;
            }

            SetLocalScaleX(_target, _nonFlipBaseScaleXs[_index] * _directionScale);
            SetAnchoredPositionX(_target, _nonFlipBaseAnchoredPositions[_index].x * _directionScale);
        }
    }

    private void SetLocalScaleX(Transform _target, float _scaleX)
    {
        Vector3 _localScale = _target.localScale;
        _localScale.x = _scaleX;
        _target.localScale = _localScale;
    }

    private Vector2 GetAnchoredPosition(Transform _target)
    {
        if (_target is RectTransform _rectTransform)
        {
            return _rectTransform.anchoredPosition;
        }

        return Vector2.zero;
    }

    private void SetAnchoredPositionX(Transform _target, float _anchoredPositionX)
    {
        if (_target is not RectTransform _rectTransform)
        {
            return;
        }

        Vector2 _anchoredPosition = _rectTransform.anchoredPosition;
        _anchoredPosition.x = _anchoredPositionX;
        _rectTransform.anchoredPosition = _anchoredPosition;
    }
}
