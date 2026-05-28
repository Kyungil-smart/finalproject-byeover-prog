// 담당자 : 정승우
// 설명   : 개별 몬스터 FSM + 이동 패턴

using System;
using UnityEngine;

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

    // ---------- 상태 ----------
    private enum State { Moving, Attacking, Dead }
    private State _state;

    private IMovementPattern _movement;
    private int _attack;
    private float _attackTimer;
    private Rect _moveBounds;

    // 플레이어 참조 (공격할 때 TakeDamage 호출용)
    private PlayerModel _playerModel;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData stats, int monsterId)
    {
        MonsterID = monsterId;
        MaxHP = stats.MaxHP;
        CurrentHP = stats.MaxHP;
        _attack = stats.Attack;
        _state = State.Moving;
        _attackTimer = 0f;

        // 이동 패턴은 일단 직선으로. 몬스터 타입별 분기는 나중에 추가.
        _movement = new StraightDownMovement(3f);

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
        if (transform.position.y <= _defenseLineY)
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
            _animator.SetTrigger("Attack");
            if (_playerModel != null)
                _playerModel.TakeDamage(_attack);
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