// 담당자 : 정승우
// 설명   : 투사체 이동 + 충돌 판정

using UnityEngine;

// 수정자 : 정승우
// 수정내용 : Setup과 SetupStraight의 사용 목적을 명확히 구분.

/// <summary>
/// 투사체 1개의 이동과 충돌을 처리한다.
/// IProjectileBehavior로 이동 방식을 교체할 수 있음.
/// </summary>
public class ProjectileController : MonoBehaviour, IPoolable
{
    private const string ProjectilePoolKey = "Projectile_Basic";
    private const float DefaultSpeed = 10f;
    private const float MinDirectionSqr = 0.0001f;

    [Header("직선 탄 설정")]
    [Tooltip("화면 밖 판정 X 범위")]
    [SerializeField] private float _despawnAbsX = 12f;

    [Tooltip("화면 밖 판정 Y 범위")]
    [SerializeField] private float _despawnAbsY = 15f;

    [Tooltip("화면 밖 판정 실패 시에도 회수되는 최대 생존 시간")]
    [SerializeField] private float _maxLifetime = 4f;

    // ---------- Private ----------
    private IProjectileBehavior _behavior;
    private int _damage;
    private Vector3 _velocity;
    private float _lifeTimer;
    private bool _isStraightActive;
    private bool _isFinished;

    // ---------- 초기화 ----------
    /// <summary>
    /// 특수 탄 전용 초기화.
    /// Homing, Piercing, Bouncing처럼 별도 이동 로직이 필요한 투사체에서만 사용한다.
    /// 기본 공격과 현재 플레이어 스킬은 비용 절감을 위해 SetupStraight를 사용한다.
    /// </summary>
    public void Setup(IProjectileBehavior behavior, int damage, Vector2 origin, Vector2 target)
    {
        if (behavior == null)
        {
            Debug.LogWarning("[ProjectileController] 특수 탄 Setup에 behavior가 없어 투사체를 회수합니다.", this);
            DespawnSelf();
            return;
        }

        _behavior = behavior;
        _damage = damage;
        _isStraightActive = false;
        _isFinished = false;
        _lifeTimer = 0f;
        _behavior.Initialize(transform, origin, target, 10f);
    }

    /// <summary>
    /// 직선 탄 전용 초기화.
    /// 타겟 위치는 발사 순간 한 번만 보고 방향을 고정한다.
    /// 현재 기본 공격과 플레이어 스킬은 모두 이 메서드를 사용한다.
    /// </summary>
    public void SetupStraight(int damage, Vector2 origin, Vector2 target, float speed)
    {
        _behavior = null;
        _damage = damage;
        _lifeTimer = 0f;
        _isFinished = false;
        _isStraightActive = true;

        Vector2 delta = target - origin;
        Vector2 direction = delta.sqrMagnitude > MinDirectionSqr
            ? delta.normalized
            : Vector2.up;

        float finalSpeed = speed > 0f ? speed : DefaultSpeed;
        _velocity = direction * finalSpeed;
    }

    // ---------- Update ----------
    private void Update()
    {
        if (_isStraightActive)
        {
            UpdateStraight(Time.deltaTime);
            return;
        }

        if (_behavior == null) return;

        _behavior.UpdateMovement(Time.deltaTime);

        if (_behavior.IsFinished)
            DespawnSelf();
    }

    private void UpdateStraight(float deltaTime)
    {
        _lifeTimer += deltaTime;
        transform.position += _velocity * deltaTime;

        Vector3 pos = transform.position;
        if (_lifeTimer >= _maxLifetime
            || Mathf.Abs(pos.x) > _despawnAbsX
            || Mathf.Abs(pos.y) > _despawnAbsY
            || _isFinished)
        {
            DespawnSelf();
        }
    }

    // ---------- 충돌 ----------
    private void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponent<IDamageable>();
        if (target == null) return;

        target.TakeDamage(_damage);
        if (_isStraightActive)
        {
            _isFinished = true;
            DespawnSelf();
            return;
        }

        _behavior?.OnHit(other.transform);
    }

    // ---------- IPoolable ----------
    public void OnSpawn() { }

    public void OnDespawn()
    {
        _behavior = null;
        _damage = 0;
        _velocity = Vector3.zero;
        _lifeTimer = 0f;
        _isStraightActive = false;
        _isFinished = false;
    }

    private void DespawnSelf()
    {
        PoolManager.Instance.Despawn(ProjectilePoolKey, gameObject);
    }
}
