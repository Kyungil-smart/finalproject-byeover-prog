// 담당자: 조규민
// 자동 순찰 거리와 대기 시간을 Inspector에서 조정하고 잘못된 범위를 자동 보정하도록 변경

using UnityEngine;

/// <summary>
/// 하우징 이동 가능 영역 안에서 플레이어의 자동 순찰 제어
/// </summary>
public class HousingPlayerMoveController : MonoBehaviour
{
    private const string MoveAreaName = "Housing_MoveArea";
    private const float ArrivalDistance = 0.01f;
    private const float DirectionFlipEpsilon = 0.001f;

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
    [Tooltip("한 번의 자동 이동에서 선택할 최소 거리입니다.")]
    [SerializeField] private float _minPatrolDistance = 120f;
    [Tooltip("한 번의 자동 이동에서 선택할 최대 거리입니다.")]
    [SerializeField] private float _maxPatrolDistance = 360f;
    [Tooltip("다음 자동 이동 전 최소 대기 시간입니다.")]
    [SerializeField] private float _minIdleTime = 2f;
    [Tooltip("다음 자동 이동 전 최대 대기 시간입니다.")]
    [SerializeField] private float _maxIdleTime = 3f;
    [Tooltip("이동 가능한 임의 위치를 찾을 최대 시도 횟수입니다.")]
    [SerializeField] private int _autoPatrolMaxPickCount = 30;

    [Header("장애물 확장")]
    [Tooltip("추후 가구 장애물 판정에 사용할 Collider2D 목록입니다.")]
    [SerializeField] private Collider2D[] _obstacleColliders;

    private Vector3 _targetPosition;
    private bool _hasTarget;
    private float _idleTimer;
    private float _playerVisualBaseScaleX = 1f;
    private float[] _nonFlipBaseScaleXs;
    private Vector3 _playerStartLocalPosition;
    private bool _isMovementPaused;

    // 자동 순찰에 필요한 이동 영역과 방향 전환 기준값 초기화
    private void Awake()
    {
        ResolveMoveArea();
        ResolvePlayerVisual();
        CacheFlipBaseScales();
        CachePlayerStartPosition();
    }

    // Inspector 입력값의 음수 방지 및 최소·최대 범위 검증
    private void OnValidate()
    {
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _minPatrolDistance = Mathf.Max(0f, _minPatrolDistance);
        _maxPatrolDistance = Mathf.Max(_minPatrolDistance, _maxPatrolDistance);
        _minIdleTime = Mathf.Max(0f, _minIdleTime);
        _maxIdleTime = Mathf.Max(_minIdleTime, _maxIdleTime);
        _autoPatrolMaxPickCount = Mathf.Max(1, _autoPatrolMaxPickCount);
    }

    private void Update()
    {
        UpdateAutoPatrol();
        MovePlayer();
    }

    // 현재 목적지 해제 및 다음 자동 순찰까지의 대기 시간 초기화
    public void StopMove()
    {
        _hasTarget = false;
        ResetIdleTimer();
    }

    // 추가: 조규민 - 가구 상호작용 중 자동 순찰 일시 정지 및 종료 시 시작 위치 복원
    public void PauseMovement()
    {
        _isMovementPaused = true;
        StopMove();
    }

    // 필요 시 저장된 시작 위치로 복원한 뒤 자동 순찰 재개
    public void ResumeMovement(bool _restoreStartPosition)
    {
        if (_player != null && _restoreStartPosition)
        {
            _player.localPosition = _playerStartLocalPosition;
        }

        _isMovementPaused = false;
        StopMove();
    }

    // 목적지 방향으로 이동하면서 영역 이탈과 장애물 충돌을 검증하고 도착 상태 처리
    private void MovePlayer()
    {
        if (_isMovementPaused)
        {
            return;
        }

        if (_hasTarget == false)
        {
            return;
        }

        if (_player == null)
        {
            StopMove();
            return;
        }

        // SFX 가이드 하우징 3: 에이프릴 이동 발소리. 매 프레임 호출돼도 걸음 리듬/볼륨은
        // SoundLibrary(id 61)의 minInterval/volume이 결정한다 — 튜닝은 코드가 아니라 라이브러리에서.
        AudioManager.Play(SfxId.HousingFootstep);

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

    // 대기 시간이 끝난 플레이어의 다음 자동 순찰 목적지 설정
    private void UpdateAutoPatrol()
    {
        if (_isMovementPaused)
        {
            return;
        }

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

    // 제한된 시도 횟수 안에서 이동 조건을 만족하는 임의 목적지 탐색
    private bool TryGetRandomMovePoint(out Vector3 _worldPosition)
    {
        for (int _index = 0; _index < _autoPatrolMaxPickCount; _index++)
        {
            Vector2 _direction = Random.insideUnitCircle;

            if (_direction.sqrMagnitude < 0.01f)
            {
                continue;
            }

            _direction.Normalize();

            float _distance = Random.Range(_minPatrolDistance, _maxPatrolDistance);
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
        _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
    }

    // 이동 영역 내부 여부와 등록된 장애물 중첩 여부 검증
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

    private void CachePlayerStartPosition()
    {
        if (_player == null)
        {
            return;
        }

        _playerStartLocalPosition = _player.localPosition;
    }

    // Inspector 참조가 없을 때 하우징 페이지 하위의 이동 영역 자동 탐색
    private void ResolveMoveArea()
    {
        if (_moveArea != null)
        {
            return;
        }

        Transform _moveAreaTransform = FindChildRecursive(GetPageRoot(), MoveAreaName);

        if (_moveAreaTransform == null)
        {
            Debug.LogWarning("[HousingPlayerMoveController] Housing_MoveArea를 찾지 못했습니다.", this);
            return;
        }

        _moveArea = _moveAreaTransform.GetComponent<PolygonCollider2D>();

        if (_moveArea == null)
        {
            Debug.LogWarning("[HousingPlayerMoveController] Housing_MoveArea에 PolygonCollider2D가 없습니다.", this);
        }
    }

    private Transform GetPageRoot()
    {
        Transform _current = transform;

        while (_current.parent != null)
        {
            if (_current.name == "Page_Housing")
            {
                return _current;
            }

            _current = _current.parent;
        }

        return _current;
    }

    private static Transform FindChildRecursive(Transform _parent, string _name)
    {
        if (_parent == null)
        {
            return null;
        }

        if (_parent.name == _name)
        {
            return _parent;
        }

        for (int _index = 0; _index < _parent.childCount; _index++)
        {
            Transform _found = FindChildRecursive(_parent.GetChild(_index), _name);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    // 좌우 반전 시 원본 크기를 유지하기 위한 시각 오브젝트의 X축 스케일 저장
    private void CacheFlipBaseScales()
    {
        if (_playerVisual != null)
        {
            _playerVisualBaseScaleX = Mathf.Abs(_playerVisual.localScale.x);
        }

        if (_nonFlipTargets == null)
        {
            _nonFlipBaseScaleXs = null;
            return;
        }

        _nonFlipBaseScaleXs = new float[_nonFlipTargets.Length];

        for (int _index = 0; _index < _nonFlipTargets.Length; _index++)
        {
            Transform _target = _nonFlipTargets[_index];
            _nonFlipBaseScaleXs[_index] = _target != null ? Mathf.Abs(_target.localScale.x) : 1f;
        }
    }

    // 수평 이동 방향에 따른 플레이어 시각 오브젝트의 좌우 반전 적용
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

    // 글자 등 반전 제외 자식의 화면 방향 유지를 위한 X축 스케일 보정
    private void UpdateNonFlipTargets(float _directionScale)
    {
        if (_nonFlipTargets == null || _nonFlipBaseScaleXs == null)
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
        }
    }

    private void SetLocalScaleX(Transform _target, float _scaleX)
    {
        Vector3 _localScale = _target.localScale;
        _localScale.x = _scaleX;
        _target.localScale = _localScale;
    }

}
