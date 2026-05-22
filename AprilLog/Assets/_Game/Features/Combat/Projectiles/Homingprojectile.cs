// 담당자 : 정승우
// 설명   : 추적 투사체 -- 타겟 따라감, 맞으면 소멸

using UnityEngine;

public class HomingProjectile : IProjectileBehavior
{
    private Transform _self;
    private Transform _target;
    private float _speed;

    public bool IsFinished { get; private set; }

    public void Initialize(Transform self, Vector2 origin, Vector2 target, float speed)
    {
        _self = self;
        _speed = speed;
        // 초기 방향만 잡아두고, OnHit에서 실제 타겟 Transform을 받음
    }

    // 타겟 Transform 직접 설정 (SkillSystem에서 호출)
    public void SetTarget(Transform target)
    {
        _target = target;
    }

    public void UpdateMovement(float dt)
    {
        if (_target == null || !_target.gameObject.activeSelf)
        {
            IsFinished = true;
            return;
        }

        Vector2 dir = ((Vector2)_target.position - (Vector2)_self.position).normalized;
        _self.Translate(dir * _speed * dt);
    }

    public void OnHit(Transform target)
    {
        IsFinished = true;
    }
}