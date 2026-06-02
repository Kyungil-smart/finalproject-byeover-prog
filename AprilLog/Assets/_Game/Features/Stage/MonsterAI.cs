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

/// <summary>
/// 몬스터 1마리의 상태(이동/공격/사망)와 이동을 처리한다.
/// 이동 방식은 IMovementPattern으로 교체 가능.
/// </summary>
public class MonsterAI : MonoBehaviour, IDamageable, IPoolable
{
    // ---------- 이벤트 ----------
    /// <summary>
    /// 죽은 몬스터와 자폭 여부를 전파<br/>
    /// true면 자폭한 몬스터
    /// </summary>
    public event Action<MonsterAI, bool> OnDeath;
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

    // 플레이어 참조 (공격할 때 TakeDamage 호출용)
    private PlayerModel _playerModel;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData stats, MonsterStatusData monsterStats, int monsterId)
    {
        MonsterID = monsterId;
        MaxHP = stats != null ? stats.MaxHP : 1;
        CurrentHP = MaxHP;
        _attack = stats != null ? stats.Attack : 1;
        _attackInterval = stats != null ? stats.BaseAttackSpeed : 1.5f;
        
        _defense = monsterStats != null ? monsterStats.Defense : 0;
        _range = monsterStats != null ? monsterStats.Range : 1;
        Exp = monsterStats != null ? monsterStats.EXP : 0;
        
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

        if (monsterStats != null)
        {
            MoveType moveType = (MoveType)Enum.Parse(typeof(MoveType), monsterStats.MovementPattern);
            switch (moveType)
            {
                case MoveType.Straight:
                    _movement = new StraightDownMovement(moveSpeed);
                    break;
                case MoveType.Zigzag:
                    _movement = new ZigzagMovement(moveSpeed);
                    break;
            }
        }

        // 이동 범위 (화면 양 끝)
        _moveBounds = new Rect(-3f, -10f, 6f, 20f);
    }

    public void SetPlayerModel(PlayerModel player)
    {
        _playerModel = player;
    }

    // ---------- FSM ----------
    private void Update()
    {
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
        Vector2 newPos = _movement.CalculateNextPosition(
            transform.position, Time.deltaTime, _moveBounds);
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
        // 기획상 투사체를 발사해야 하나, 몬스터 투사체 연출은 후속 작업.
        // 현재는 사거리에서 멈춘 뒤 방어선에 데미지를 적용해 원거리 몬스터가
        // 실제로 플레이어를 위협하도록 한다. (투사체 ToDo: ProjectileController 연동)
        if (_playerModel != null)
            _playerModel.TakeDamage(damage);
    }

    private void KamikazeAttack(int damage)
    {
        if (_playerModel != null)
            _playerModel.TakeDamage(damage);
        _state = State.Dead;
        OnDeath?.Invoke(this, true);
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
    public void TakeDamage(int baseDamage)
    {
        if (_state == State.Dead) return;
        
        // Final_Damage = Base_Damage x { 1 - Effective_Armor / (100 + Effective_Armor) }
        int penetration = 0; // Todo : 데모때는 관통 없음. CBT때 시트 수정 예정
        // 유효 방어력 하한 0 (관통이 방어력보다 커도 데미지가 증폭되지 않도록, 기획 2-4-4)
        int effectiveArmor = Mathf.Max(0, _defense - penetration);
        int finalDamage = Mathf.FloorToInt(baseDamage * (1 - effectiveArmor / (float)(100 + effectiveArmor)));
        
        CurrentHP = Mathf.Max(0, CurrentHP - finalDamage);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        _state = State.Dead;
        OnDeath?.Invoke(this, false);
    }

    // ---------- IPoolable ----------
    public void OnSpawn()
    {
        _state = State.Moving;
    }

    public void OnDespawn()
    {
        OnDeath = null;
        OnHPChanged = null;
        _playerModel = null;
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

    public ZigzagMovement(float speed)
    {
        _speed = speed;
        _horizontalDir = -1f;
    }

    public Vector2 CalculateNextPosition(Vector2 current, float dt, Rect bounds)
    {
        float newY = current.y - _speed * dt;
        float newX = current.x + _horizontalDir * _speed * 0.5f * dt;

        // 영역 벗어나면 방향 반전
        if (newX <= bounds.xMin || newX >= bounds.xMax)
        {
            _horizontalDir *= -1f;
            newX = Mathf.Clamp(newX, bounds.xMin, bounds.xMax);
        }

        return new Vector2(newX, newY);
    }
}
