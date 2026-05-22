// 담당자 : 정승우
// 설명   : 관통 투사체 -- 적 맞아도 안 사라지고 화면 끝까지 감

using UnityEngine;

public class PiercingProjectile : IProjectileBehavior
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

        if (_self.position.y > 15f || _self.position.y < -15f)
            IsFinished = true;
    }

    public void OnHit(Transform target)
    {
        // 관통이라 안 사라짐
    }
}