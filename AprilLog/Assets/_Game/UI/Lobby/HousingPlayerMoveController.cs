// 담당자: 조규민

using UnityEngine;

/// <summary>
/// 하우징 이동 가능 영역 안에서 플레이어를 자동 순회시킵니다.
/// </summary>
public class HousingPlayerMoveController : MonoBehaviour
{
    private const float ArrivalDistance = 0.01f;
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

    [Header("이동 설정")]
    [Tooltip("초당 이동 거리입니다.")]
    [SerializeField] private float _moveSpeed = 3f;

    [Header("장애물 확장")]
    [Tooltip("추후 가구 장애물 판정에 사용할 Collider2D 목록입니다.")]
    [SerializeField] private Collider2D[] _obstacleColliders;

    private Vector3 _targetPosition;
    private bool _hasTarget;
    private float _idleTimer;

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
}
