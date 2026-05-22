// 담당자 : 정승우
// 설명   : 튕기기 투사체 -- 적 치고 가장 가까운 다음 적으로 튕김

using UnityEngine;

public class BouncingProjectile : IProjectileBehavior
{
    private Transform _self;
    private Vector2 _direction;
    private float _speed;
    private int _remainBounces = 3;

    public bool IsFinished { get; private set; }

    public void Initialize(Transform self, Vector2 origin, Vector2 target, float speed)
    {
        _self = self;
        _direction = (target - origin).normalized;
        _speed = speed;
        _remainBounces = 3;
    }

    public void UpdateMovement(float dt)
    {
        _self.Translate(_direction * _speed * dt);

        if (_self.position.y > 15f || _self.position.y < -15f)
            IsFinished = true;
    }

    public void OnHit(Transform hitTarget)
    {
        _remainBounces--;

        if (_remainBounces <= 0)
        {
            IsFinished = true;
            return;
        }
        
        _direction = -_direction;
    }
}