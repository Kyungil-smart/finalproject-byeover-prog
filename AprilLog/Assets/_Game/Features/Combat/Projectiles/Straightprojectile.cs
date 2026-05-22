// 담당자 : 정승우
// 설명   : 직선 투사체 -- 앞으로 가다가 화면 밖이면 소멸

using UnityEngine;

public class StraightProjectile : IProjectileBehavior
{
    private Transform _self;
    private Vector2 _direction;
    private float _speed;

    public bool IsFinished { get; private set; }

    public void Initialize(Transform self, Vector2 origin, Vector2 target, float speed)
    {
        _self = self;
        _direction = (target - origin).normalized;
        _speed = speed;
    }

    public void UpdateMovement(float dt)
    {
        _self.Translate(_direction * _speed * dt);

        // 화면 밖이면 소멸
        if (_self.position.y > 15f || _self.position.y < -15f)
            IsFinished = true;
    }

    public void OnHit(Transform target)
    {
        IsFinished = true;
    }
}