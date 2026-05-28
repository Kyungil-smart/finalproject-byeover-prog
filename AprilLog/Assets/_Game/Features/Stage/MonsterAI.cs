// 담당자 : 정승우
// 설명   : 개별 몬스터 FSM + 이동 패턴

using System;
using UnityEngine;

// 수정자 : Codex
// 수정내용 : 몬스터 이동 속도를 MonsterStatusData 기준으로 적용.

// 수정자 : 김영찬
// 수정 내용 : 1. 공격방식에 따라 공격을 다르게 하도록 구현
//           2. 사거리에 따라 정지거리가 달라지도록 구현

/// <summary>
/// 몬스터 1마리의 상태(이동/공격/사망)와 이동을 처리한다.
/// 이동 방식은 IMovementPattern으로 교체 가능.
/// </summary>
public class MonsterAI : MonoBehaviour, IDamageable, IPoolable
{
    // ---------- 이벤트 ----------
    public event Action<MonsterAI> OnDeath;
    public event Action<int, int> OnHPChanged;

    // ---------- IDamageable ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int MonsterID { get; private set; }

    // ---------- SerializeField ----------
    [Header("설정")]
    [Tooltip("방어선 Y좌표. 이 아래로 내려가면 공격 상태")]
    [SerializeField] private float _defenseLineY = -3f;
    
    [Tooltip("공격 간격(초)")]
    [SerializeField] private float _attackInterval = 1.5f;

    [Tooltip("애니메이터 지정")] 
    [SerializeField] private Animator _animator;

    // ---------- 공격 방식 지정 ----------
    private enum AttackType { Melee, Range, Kamikaze} // 근거리, 원거리, 자폭
    private AttackType _attackType;
    
    // ---------- 상태 ----------
    private enum State { Moving, Attacking, Dead }
    private State _state;

    private IMovementPattern _movement;
    private int _attack;
    private float _attackTimer;
    private Rect _moveBounds;
    private int _range;

    // 플레이어 참조 (공격할 때 TakeDamage 호출용)
    private PlayerModel _playerModel;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData stats, MonsterStatusData monsterStats, int monsterId)
    {
        MonsterID = monsterId;
        MaxHP = stats != null ? stats.MaxHP : 1;
        CurrentHP = MaxHP;
        _attack = stats != null ? stats.Attack : 1;
        _state = State.Moving;
        _attackTimer = 0f;
        _range = monsterStats != null ? monsterStats.Range : 1;

        // 사거리에 따라 공격 타입 자동 지정
        if (monsterStats != null)
        {
            AttackTypeSelect(monsterStats.Range);
        }

        // 이동 패턴은 일단 직선으로. 몬스터 타입별 분기는 나중에 추가.
        float moveSpeed = monsterStats != null && monsterStats.MoveSpeed > 0f
            ? monsterStats.MoveSpeed
            : 3f;

        // 모바일 비용을 낮추기 위해 현재 기획 이동은 직선 이동으로 고정한다.
        _movement = new StraightDownMovement(moveSpeed);

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
        // 투사체를 소환해서 발사
        // ToDo : ProjectileController 정비 되면 착수
    }

    private void KamikazeAttack(int damage)
    {
        if (_playerModel != null)
            _playerModel.TakeDamage(damage);
        Die();
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
    public void TakeDamage(int amount)
    {
        if (_state == State.Dead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
            Die();
    }

    private void Die()
    {
        _state = State.Dead;
        OnDeath?.Invoke(this);
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
