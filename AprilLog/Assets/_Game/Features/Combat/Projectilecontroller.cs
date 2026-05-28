// 담당자 : 정승우
// 설명   : 투사체 이동 + 충돌 판정

using UnityEngine;

// 수정자 : Codex
// 수정내용 : 기본 공격용 직선 탄을 객체 생성 없이 이동하도록 최적화.

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
    public void Setup(IProjectileBehavior behavior, int damage, Vector2 origin, Vector2 target)
    {
        _behavior = behavior;
        _damage = damage;
        _isStraightActive = false;
        _isFinished = false;
        _lifeTimer = 0f;
        _behavior.Initialize(transform, origin, target, 10f);
    }

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
