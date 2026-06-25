// 담당자 : 정승우
// 설명   : 개별 몬스터 FSM + 이동 패턴

using System;
using UnityEngine;

// 수정자 : 정승우
// 수정내용 : 몬스터 이동 속도를 MonsterStatusData.MoveSpeed 기준으로 적용하고 현재 이동 정책을 직선 이동으로 명확화.

// 수정자 : 김영찬
// 수정 내용 : 1. 공격방식에 따라 공격을 다르게 하도록 구현
//           2. 사거리에 따라 정지거리가 달라지도록 구현

// 수정자 : 김영찬
// 수정 내용 : 1. 몬스터가 스테이터스의 모든 항목을 불러오도록 수정 (기존 : 필요한 값만 우선적으로 불러왔음)
//           2. 몬스터가 정해진 데미지 공식에 의거 방어력에 따라 데미지를 점감 받도록 구현
//           3. 몬스터가 자폭시에는 Exp를 얻을 수 없도록 수정

// 수정자 : 김영찬
// 수정 내용 : 몬스터 및 웨이브 관련 DB에 맞춰 소환 로직 최신화 및 책임 분산

/// <summary>
/// 몬스터 1마리의 상태(이동/공격/사망)와 이동을 처리한다.
/// 이동 방식은 IMovementPattern으로 교체 가능.
/// </summary>
public class MonsterAI : MonoBehaviour, IDamageable, IPoolable
{
    // ---------- 이벤트 ----------
    /// <summary>
    /// 죽은 몬스터와 자폭 여부를 전파<br/>
    /// 앞의 bool = true면 자폭한 몬스터<br/>
    /// 뒤의 bool = true면 보스 몬스터
    /// </summary>
    public event Action<MonsterAI, bool, bool> OnDeath;
    public event Action<int, int> OnHPChanged;

    // ---------- SerializeField ----------
    [Header("설정")]
    [Tooltip("방어선 Y좌표. 이 아래로 내려가면 공격 상태")]
    [SerializeField] private float _defenseLineY = -3f;

    [Tooltip("애니메이터 지정")] 
    [SerializeField] private Animator _animator;
    
    // ---------- IDamageable ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int MonsterID { get; private set; }
    
    // ---------- Other Status ----------
    private int _attack;
    private float _attackInterval;
    
    private int _defense;
    private int _range;
    public int Exp { get; private set; }
    private IMovementPattern _movement;
    private int _zigzagAmplitude;

    // ---------- 공격 방식 지정 ----------
    private enum AttackType { Melee, Range, Kamikaze} // 근거리, 원거리, 자폭
    private AttackType _attackType;
    
    // ---------- 상태 ----------
    private enum State { Moving, Attacking, Dead }
    private enum MoveType{ Straight, Zigzag }
    private State _state;
    
    private float _attackTimer;
    private Rect _moveBounds;
    private bool _isBoss;

    // ---------- CC 상태 (바람: 허리케인=슬로우 / 돌풍=넉백) ----------
    private float _slowFactor = 1f;                 // 1=정상, <1=이동 감속 배율
    private float _slowEndTime = 0f;
    private Vector2 _knockbackVel = Vector2.zero;   // 넉백 중 월드 속도(units/s)
    private float _knockbackEndTime = 0f;
    private float _stunEndTime = 0f;                // 스턴(번개 벼락): 이동+공격 완전 정지

    /// <summary>이 몬스터가 보스인지 (번개 벼락의 엘리트/보스 우선 타겟용). Elite도 현재 isBoss=true로 스폰됨.</summary>
    public bool IsBoss => _isBoss;

    // 플레이어 참조 (공격할 때 TakeDamage 호출용)
    private PlayerModel _playerModel;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData stats, MonsterStatusData monsterStats, int monsterId, bool isBoss = false)
    {
        MonsterID = monsterId;
        MaxHP = stats != null ? stats.MaxHP : 1;
        CurrentHP = MaxHP;
        _attack = stats != null ? stats.Attack : 1;
        // BaseAttackSpeed는 게이지 충전율(값이 클수록 빠름, 범위 0.01~1).
        // 공격 간격(초) = 1 / 충전율. (0 이하/누락이면 기본 1.5초)
        float atkSpeed = stats != null ? stats.BaseAttackSpeed : 0f;
        _attackInterval = atkSpeed > 0f ? 1f / atkSpeed : 1.5f;
        
        _defense = monsterStats != null ? monsterStats.Defense : 0;
        _range = monsterStats != null ? monsterStats.Range : 1;
        Exp = monsterStats != null ? monsterStats.EXP : 0;
        _zigzagAmplitude = monsterStats != null ? monsterStats.ZigzagAmplitude : -1;
        
        _isBoss = isBoss;

        // 방벽 정지선: DefenseLine(방벽) 오브젝트의 Y를 단일 진실 소스로 사용한다.
        // 플레이어도 같은 DefenseLine에 정렬되므로 정지선과 플레이어 위치가 항상 일치한다.
        // (오브젝트가 없으면 serialized 기본값 _defenseLineY 유지)
        var defenseLine = GameObject.Find("DefenseLine");
        if (defenseLine != null)
            _defenseLineY = defenseLine.transform.position.y;

        _state = State.Moving;
        _attackTimer = 0f;

        // 사거리에 따라 공격 타입 자동 지정
        if (monsterStats != null)
        {
            AttackTypeSelect(monsterStats.Range);
        }

        // 몬스터 전용 스탯의 MoveSpeed를 우선 사용하고, 데이터가 없으면 임시 기본값을 사용한다.
        float moveSpeed = monsterStats != null && monsterStats.MoveSpeed > 0f
            ? monsterStats.MoveSpeed
            : 3f;

        // 이동 패턴 파싱. Enum.Parse는 빈 값/오타에 예외를 던지므로 TryParse로 안전 처리.
        // 잘못된/누락 값이면 기본값 Straight. (_movement는 항상 설정해 Update NRE 방지)
        MoveType moveType = MoveType.Straight;
        if (monsterStats != null && !string.IsNullOrEmpty(monsterStats.MovementPattern))
            Enum.TryParse(monsterStats.MovementPattern, out moveType);

        switch (moveType)
        {
            case MoveType.Zigzag:
                _movement = new ZigzagMovement(moveSpeed, _zigzagAmplitude);
                break;
            case MoveType.Straight:
            default:
                _movement = new StraightDownMovement(moveSpeed);
                break;
        }

        // 이동 범위 (화면 양 끝)
        _moveBounds = new Rect(-3f, -10f, 6f, 20f);
    }
    
    public void ApplyScaling(MonsterStageScalingData scalingData, int accumulateCount)
    {
        if (scalingData != null && accumulateCount > 0) // 💡 누적할 게 있을 때만 실행!
        {
            // 스케일링 데이터의 수식과 '누적 횟수'를 곱해서 최종 스탯 산출
            MaxHP = CalculateScaledStat(MaxHP, scalingData.MaxHPGrowthType, scalingData.MaxHPGrowthValue, accumulateCount);
            _attack = CalculateScaledStat(_attack, scalingData.AttackGrowthType, scalingData.AttackGrowthValue, accumulateCount);
            _defense = CalculateScaledStat(_defense, scalingData.DefenseGrowthType, scalingData.DefenseGrowthValue, accumulateCount);
        }

        // 4단계: 버프 배율 (미구현 상태이므로 1.0f)
        float hpBuffMultiplier = 1.0f;
        float atkBuffMultiplier = 1.0f;
        float defBuffMultiplier = 1.0f;

        // 최종 스탯 확정 (스케일링 스탯 * 버프)
        MaxHP = Mathf.RoundToInt(MaxHP * hpBuffMultiplier);
        _attack = Mathf.RoundToInt(_attack * atkBuffMultiplier);
        _defense = Mathf.RoundToInt(_defense * defBuffMultiplier);

        // 현재 체력 리셋
        CurrentHP = MaxHP; 
    }

    public void SetPlayerModel(PlayerModel player)
    {
        _playerModel = player;
    }

    // ---------- CC 적용 (스킬에서 호출) ----------
    /// <summary>이동 속도를 factor배(0~1)로 duration초 동안 감속. 재적용 시 갱신. (바람 허리케인 슬로우)</summary>
    public void ApplySlow(float factor, float duration)
    {
        if (_state == State.Dead) return;
        _slowFactor = Mathf.Clamp01(factor);
        _slowEndTime = Time.time + duration;
    }

    /// <summary>displacement(월드)만큼 duration초에 걸쳐 밀어낸다. (바람 돌풍 넉백)</summary>
    public void ApplyKnockback(Vector2 displacement, float duration)
    {
        if (_state == State.Dead || duration <= 0f) return;
        _knockbackVel = displacement / duration;
        _knockbackEndTime = Time.time + duration;
    }

    /// <summary>duration초 동안 이동+공격 완전 정지. (번개 벼락 Lv3 스턴)</summary>
    public void ApplyStun(float duration)
    {
        if (_state == State.Dead || duration <= 0f) return;
        _stunEndTime = Time.time + duration;
    }

    // ---------- FSM ----------
    private void Update()
    {
        // 스턴(번개 벼락) 중이면 이동·공격 모두 정지
        if (Time.time < _stunEndTime)
        {
            if (_animator != null) _animator.SetBool("Move", false);
            return;
        }

        switch (_state)
        {
            case State.Moving:
                UpdateMoving();
                break;
            case State.Attacking:
                UpdateAttacking();
                break;
        }
        
        // 애니메이션 제어
        if (_animator != null)
        {
            _animator.SetBool("Move", _state == State.Moving);
        }
    }

    private void UpdateMoving()
    {
        // 넉백(돌풍) 중이면 이동 패턴 무시하고 넉백 속도로 밀린다. 끝나면 일반 이동 복귀.
        if (Time.time < _knockbackEndTime)
        {
            Vector2 kb = (Vector2)transform.position + _knockbackVel * Time.deltaTime;
            kb.x = Mathf.Clamp(kb.x, _moveBounds.xMin, _moveBounds.xMax);
            transform.position = kb;
            return;
        }

        // 슬로우(허리케인): 지속시간 내면 이동 dt에 배율, 만료되면 1로 복귀.
        float slow = (Time.time < _slowEndTime) ? _slowFactor : 1f;
        Vector2 newPos = _movement.CalculateNextPosition(
            transform.position, Time.deltaTime * slow, _moveBounds);
        transform.position = newPos;

        // 방어선 도달
        if (transform.position.y + (_range - 1) <= _defenseLineY)
        {
            _state = State.Attacking;
        }
    }

    private void UpdateAttacking()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            _attackTimer = 0f;
            AttackSupport();
        }
    }

    // ---------- 공격 지원 ----------
    private void AttackSupport()
    {
        if (_animator != null)
            _animator.SetTrigger("Attack");

        switch (_attackType)
        {
            case AttackType.Melee:
                MeleeAttack(_attack);
                break;
            case AttackType.Range:
                RangeAttack(_attack);
                break;
            case AttackType.Kamikaze:
                KamikazeAttack(_attack);
                break;
        }
    }
    
    private void MeleeAttack(int damage)
    {
        if (_playerModel != null)
            _playerModel.TakeDamage(damage);
    }

    public void RangeAttack(int damage)
    {
        var obj = PoolManager.Instance.Spawn("Projectile_Basic", transform.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;
        
        controller.SetupMonsterProjectile(damage, _playerModel, _defenseLineY);
    }

    private void KamikazeAttack(int damage)
    {
        if (_playerModel != null)
            _playerModel.TakeDamage(damage);
        _state = State.Dead;
        OnDeath?.Invoke(this, true, _isBoss);
    }

    private void AttackTypeSelect(int range)
    {
        switch (range)
        {
            case <= 0:
                _attackType = AttackType.Kamikaze;
                break;
            case >= 2 :
                _attackType = AttackType.Range;
                break;
            default:
                _attackType = AttackType.Melee;
                break;
        }
    }

    // ---------- IDamageable ----------
    public void TakeDamage(int baseDamage) => TakeDamage(baseDamage, 0);

    /// <summary>skillId(StandardID)를 함께 받아 정산 '인챈트별 최고뎀' 기록까지 처리. 0이면 스킬별 기록 생략(기본공격 등).</summary>
    public void TakeDamage(int baseDamage, int skillId)
    {
        if (_state == State.Dead) return;

        // Final_Damage = Base_Damage x { 1 - Effective_Armor / (100 + Effective_Armor) }
        int penetration = 0; // Todo : 데모때는 관통 없음. CBT때 시트 수정 예정
        // 유효 방어력 하한 0 (관통이 방어력보다 커도 데미지가 증폭되지 않도록, 기획 2-4-4)
        int effectiveArmor = Mathf.Max(0, _defense - penetration);
        int finalDamage = Mathf.FloorToInt(baseDamage * (1 - effectiveArmor / (float)(100 + effectiveArmor)));

        RunStats.AddDamage(finalDamage, skillId); // 정산용: 총 데미지 + 스킬(인챈트)별 최고뎀 누적

        CurrentHP = Mathf.Max(0, CurrentHP - finalDamage);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        _state = State.Dead;
        OnDeath?.Invoke(this, false, _isBoss);
    }

    // ---------- IPoolable ----------
    public void OnSpawn()
    {
        _state = State.Moving;
        _slowFactor = 1f; _slowEndTime = 0f;
        _knockbackVel = Vector2.zero; _knockbackEndTime = 0f;
        _stunEndTime = 0f;
    }

    public void OnDespawn()
    {
        // 풀 반납 후 스테일 참조로 TakeDamage가 들어와도 무시되도록 사망 상태로 둔다.
        // (장판/광역 스킬이 스냅샷한 목록을 타격하는 동안 디스폰이 끼어드는 경우 방어)
        _state = State.Dead;
        OnDeath = null;
        OnHPChanged = null;
        _playerModel = null;
        _slowFactor = 1f; _slowEndTime = 0f;
        _knockbackVel = Vector2.zero; _knockbackEndTime = 0f;
        _stunEndTime = 0f;
    }
    
    // ---------- Stat Scaling ----------
    private int CalculateScaledStat(int baseStat, string growthType, float growthValue, int accumulateCount)
    {
        switch (growthType)
        {
            case "Add":
                // 고정 수치 누적 
                // 수식: 기본스탯 + (증가량 * 누적횟수)
                // 예: HP 100, 증가량 50, 누적 4회 -> 100 + (50 * 4) = 300
                return baseStat + Mathf.RoundToInt(growthValue * accumulateCount);
                
            case "Rate":
                // 비율 누적 
                // 수식: 기본스탯 * (1 + (비율 * 누적횟수)) 복리 대신 단리로 적용 (기획 정석)
                // 예: HP 100, 증가량 0.1(10%), 누적 4회 -> 100 * (1 + (0.1 * 4)) = 140
                return Mathf.RoundToInt(baseStat * (1f + (growthValue * accumulateCount)));
                
            case "None":
            default:
                // 스케일링 없음
                return baseStat;
        }
    }
}

// 이동 패턴 (OCP: 새 패턴 = 새 클래스)
/// <summary>
/// 일자형 이동. x값 유지하면서 내려옴.
/// </summary>
public class StraightDownMovement : IMovementPattern
{
    private float _speed;

    public StraightDownMovement(float speed)
    {
        _speed = speed;
    }

    public Vector2 CalculateNextPosition(Vector2 current, float dt, Rect bounds)
    {
        return new Vector2(current.x, current.y - _speed * dt);
    }
}

/// <summary>
/// 좌우반복형 이동. 좌우로 왔다갔다하면서 내려옴.
/// 영역 밖으로 나가면 반대로 반사.
/// </summary>
public class ZigzagMovement : IMovementPattern
{
    private float _speed;
    private float _horizontalDir;
    private float _amplitude;   // 스폰 X 기준 좌우 반복 거리 (데이터 ZigzagAmplitude). 0 이하면 기존 기본값
    private float _originX;
    private bool _originSet;

    public ZigzagMovement(float speed, float amplitude = 0f)
    {
        _speed = speed;
        _amplitude = amplitude > 0f ? amplitude : speed * 0.5f; // 데이터 없으면 기존 동작 유지
        _horizontalDir = -1f;
    }

    public Vector2 CalculateNextPosition(Vector2 current, float dt, Rect bounds)
    {
        // 스폰 X를 기준점으로 ± amplitude 만큼 좌우 반복 (기획 6-1-2/6-1-4)
        if (!_originSet) { _originX = current.x; _originSet = true; }

        float newY = current.y - _speed * dt;
        float newX = current.x + _horizontalDir * _speed * dt;

        // 좌우 한계 = 스폰X ± amplitude, 단 이동 영역(bounds) 안으로 제한
        float leftLimit = Mathf.Max(bounds.xMin, _originX - _amplitude);
        float rightLimit = Mathf.Min(bounds.xMax, _originX + _amplitude);

        if (newX <= leftLimit) { newX = leftLimit; _horizontalDir = 1f; }
        else if (newX >= rightLimit) { newX = rightLimit; _horizontalDir = -1f; }

        return new Vector2(newX, newY);
    }
}
