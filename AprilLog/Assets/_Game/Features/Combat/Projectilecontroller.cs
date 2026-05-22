// 담당자 : 정승우
// 설명   : 투사체 이동 + 충돌 판정

using UnityEngine;

/// <summary>
/// 투사체 1개의 이동과 충돌을 처리한다.
/// IProjectileBehavior로 이동 방식을 교체할 수 있음.
/// </summary>
public class ProjectileController : MonoBehaviour, IPoolable
{
    // ---------- Private ----------
    private IProjectileBehavior _behavior;
    private int _damage;

    // ---------- 초기화 ----------
    public void Setup(IProjectileBehavior behavior, int damage, Vector2 origin, Vector2 target)
    {
        _behavior = behavior;
        _damage = damage;
        _behavior.Initialize(transform, origin, target, 10f);
    }

    // ---------- Update ----------
    private void Update()
    {
        if (_behavior == null) return;

        _behavior.UpdateMovement(Time.deltaTime);

        if (_behavior.IsFinished)
            PoolManager.Instance.Despawn("Projectile_Basic", gameObject);
    }

    // ---------- 충돌 ----------
    private void OnTriggerEnter2D(Collider2D other)
    {
        var target = other.GetComponent<IDamageable>();
        if (target == null) return;

        target.TakeDamage(_damage);
        _behavior.OnHit(other.transform);
    }

    // ---------- IPoolable ----------
    public void OnSpawn() { }

    public void OnDespawn()
    {
        _behavior = null;
        _damage = 0;
    }
}