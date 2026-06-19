// 담당자 : 정승우
// 설명   : 투사체 이동 + 충돌 판정

using System.Collections.Generic;
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

    // 관통 직선 탄(바람 칼날·템페스트): 적을 뚫고 진행. _piercedTargets=같은 적 중복 방지,
    // _maxPierceCount=관통 횟수 캡(소진 시 종료), _hitMultiplier=피격당 N회 대미지(템페스트 8회).
    private bool _pierce;
    private int _maxPierceCount = int.MaxValue;
    private int _piercedCount;
    private int _hitMultiplier = 1;
    private int _skillId;   // 발사한 스킬의 StandardID — 정산 '인챈트별 최고뎀' 기록용(TakeDamage에 전달)
    private readonly HashSet<IDamageable> _piercedTargets = new HashSet<IDamageable>();

    // VFX 스킨 (풀 공유 오브젝트라 인스턴스 단위로 입히고 디스폰 시 원복)
    private GameObject _skin;
    private SpriteRenderer _placeholderSprite;

    private void Awake()
    {
        _placeholderSprite = GetComponent<SpriteRenderer>();
    }

    /// <summary>이 투사체에 VFX 스킨(예: 화염 작렬 Fireball_2_normal)을 입히고 기본 사각형 스프라이트를 숨긴다.
    /// 풀로 돌아갈 때 OnDespawn에서 반드시 원복되므로 기본공격 등 다른 투사체엔 영향이 없다.</summary>
    public void SetSkin(GameObject skinInstance)
    {
        if (_skin != null) Destroy(_skin);   // 이중 부착 방어: 직전 스킨이 남아있으면 정리 후 교체
        _skin = skinInstance;
        if (_placeholderSprite != null) _placeholderSprite.enabled = false;
    }

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
    public void SetupStraight(int damage, Vector2 origin, Vector2 target, float speed, bool pierce = false, int maxPierceCount = int.MaxValue, int hitMultiplier = 1, int skillId = 0)
    {
        _behavior = null;
        _damage = damage;
        _lifeTimer = 0f;
        _isFinished = false;
        _isStraightActive = true;
        _pierce = pierce;
        _maxPierceCount = maxPierceCount > 0 ? maxPierceCount : int.MaxValue;
        _hitMultiplier = hitMultiplier > 0 ? hitMultiplier : 1;
        _skillId = skillId;
        _piercedCount = 0;
        _piercedTargets.Clear();

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

        // 관통 직선 탄(바람 칼날·템페스트): 같은 적 1회만, 피격당 _hitMultiplier회 대미지, 관통 횟수 소진 시 종료.
        if (_isStraightActive && _pierce)
        {
            if (!_piercedTargets.Add(target)) return;       // 이미 맞힌 적이면 무시(중복 방지)
            for (int h = 0; h < _hitMultiplier; h++)        // 템페스트: 피격 시 8회 대미지
                target.TakeDamage(_damage, _skillId);
            _piercedCount++;
            if (_piercedCount >= _maxPierceCount)           // 관통 횟수 소진 → 종료
            {
                _isFinished = true;
                DespawnSelf();
            }
            return;
        }

        target.TakeDamage(_damage, _skillId);
        if (_isStraightActive)
        {
            _isFinished = true;
            DespawnSelf();
            return;
        }

        _behavior?.OnHit(other.transform);
    }

    // ---------- IPoolable ----------
    public void OnSpawn()
    {
        // 풀에서 꺼낼 때 기본 상태 보장 — 어떤 경로로 반납됐든 스킨 잔류 없이 깨끗하게 시작.
        if (_skin != null) { Destroy(_skin); _skin = null; }
        if (_placeholderSprite != null) _placeholderSprite.enabled = true;
        _pierce = false;
        _maxPierceCount = int.MaxValue;
        _piercedCount = 0;
        _hitMultiplier = 1;
        _skillId = 0;
        _piercedTargets.Clear();
    }

    public void OnDespawn()
    {
        _behavior = null;
        _damage = 0;
        _velocity = Vector3.zero;
        _lifeTimer = 0f;
        _isStraightActive = false;
        _isFinished = false;
        _pierce = false;
        _maxPierceCount = int.MaxValue;
        _piercedCount = 0;
        _hitMultiplier = 1;
        _skillId = 0;
        _piercedTargets.Clear();

        // VFX 스킨 정리 — 풀 재사용 시 다음 투사체(기본공격 등)가 화염탄으로 보이지 않도록 반드시 원복.
        // Destroy는 프레임 끝에 처리되는데 풀은 같은 프레임에 이 오브젝트를 즉시 재사용할 수 있으므로,
        // 루프 파티클을 멈추고 → 부모에서 떼고(SetParent null) → 끈(SetActive false) 뒤 파괴해 '유령 화염탄' 1프레임 잔상을 막는다.
        if (_skin != null)
        {
            foreach (var ps in _skin.GetComponentsInChildren<ParticleSystem>(true))
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _skin.transform.SetParent(null, false);
            _skin.SetActive(false);
            Destroy(_skin);
            _skin = null;
        }
        if (_placeholderSprite != null) _placeholderSprite.enabled = true;
    }

    private void DespawnSelf()
    {
        PoolManager.Instance.Despawn(ProjectilePoolKey, gameObject);
    }
}
