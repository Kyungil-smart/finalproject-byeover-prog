// 담당자 : 정승우
// 설명   : 시스템 공용 인터페이스 모음

using System;
using UnityEngine;

/// <summary>
/// Sort 정렬 완성 이벤트를 발행하는 시스템.
/// CombatSystem은 이것만 알면 됨. Sort 내부 로직은 모름.
/// </summary>
public interface ISortNotifier
{
    event Action<UnitType> OnSortCompleted;
    event Action OnDeadlockDetected;
}

/// <summary>
/// 데미지를 받을 수 있는 대상. 몬스터랑 플레이어 둘 다 구현.
/// </summary>
public interface IDamageable
{
    int CurrentHP { get; }
    int MaxHP { get; }
    void TakeDamage(int amount);
    event Action<int, int> OnHPChanged;     // current, max
}

/// <summary>
/// 오브젝트 풀에서 재사용되는 오브젝트.
/// Spawn 될 때, Despawn 될 때 초기화/정리 로직을 넣는 용도.
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

/// <summary>
/// 투사체 이동 행동. 타입별로 다르게 구현. (OCP)
/// 새 투사체 타입 추가 = 이 인터페이스 구현하는 클래스 1개 추가.
/// </summary>
public interface IProjectileBehavior
{
    void Initialize(Transform self, Vector2 origin, Vector2 target, float speed);
    void UpdateMovement(float deltaTime);
    void OnHit(Transform hitTarget);
    bool IsFinished { get; }
}

/// <summary>
/// 몬스터 이동 패턴. 일자형/좌우반복형 등. (OCP)
/// 새 이동 패턴 = 이 인터페이스 구현하는 클래스 1개 추가.
/// </summary>
public interface IMovementPattern
{
    Vector2 CalculateNextPosition(Vector2 current, float deltaTime, Rect bounds);
}
