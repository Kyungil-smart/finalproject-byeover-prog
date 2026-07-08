using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 시각적 연출(공격 배속, 다중 파츠 피격 효과)을 전담하는 컨트롤러.
/// AI 로직의 이벤트를 'Handle' 메서드로 받아 처리합니다.
/// </summary>
public class MonsterVisualController : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("Animation Settings")]
    [SerializeField] private float _targetAttackDuration = 1.0f;
    
    [Header("State FeedBack Color Settings")]
    [SerializeField] private StateFeedBackColorSO _feedBackColor;
    
    // ---------- basic Private ----------
    private float _calculatedAttackSpeed = 1f;
    
    private MonsterAI _ai;
    private Animator _animator;
    private Renderer[] _renderers; // 여러 개로 쪼개진 파츠들을 모두 담음
    
    private MaterialPropertyBlock _mpb; 
    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    
    private bool _isOriginColor;
    
    // ---------- OnHit ----------
    private float _onHitRootTime;
    
    // ---------- OnCrowdControl ----------
    private Dictionary<CrowdControlType, float> _activeCCTimers;

    // ---------- Initialize ----------
    private void Init()
    {
        _ai = GetComponent<MonsterAI>();
        _animator = GetComponentInChildren<Animator>();
        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _feedBackColor ??= Resources.Load<StateFeedBackColorSO>("StateFeedBackColorSO");
        _activeCCTimers = new Dictionary<CrowdControlType, float>();

        _onHitRootTime = 0f;
        foreach (var ccType in _feedBackColor.CCPriorityList)
        {
            _activeCCTimers[ccType] = 0f;
        }
        
        CalculateAttackSpeed();
    }
    
    private void CalculateAttackSpeed()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.ToLower().Contains("akt"))
            {
                _calculatedAttackSpeed = clip.length / _targetAttackDuration;
                return;
            }
        }
        _calculatedAttackSpeed = 1f;
    }
    
    // ---------- Life Cycle ----------
    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        if (_ai == null) return;
        
        _ai.OnAttack += HandleOnAttack;
        _ai.OnHit += HandleOnHit;
        _ai.OnCrowdControl += HandleOnCrowdControl;
        _ai.OnDeath += HandleOnDeath;

        ResetVisuals(); 
    }

    private void OnDisable()
    {
        if (_ai == null) return;
        
        _ai.OnAttack -= HandleOnAttack;
        _ai.OnHit -= HandleOnHit;
        _ai.OnCrowdControl -= HandleOnCrowdControl;
        _ai.OnDeath -= HandleOnDeath;
    }

    private void Update()
    {
        if (_ai == null) return;
        PlayEffect();
    }

    // ---------- Event Handler ----------
    private void HandleOnAttack()
    {
        PlayAttackAnim();
    }

    private void HandleOnHit()
    {
        SetHitEffect();
    }

    private void HandleOnCrowdControl(CrowdControlType ccType, float duration)
    {
        SetCCEffect(ccType, duration);
    }

    private void HandleOnDeath(MonsterAI monsterAI, bool isKamikaze, bool isBoss)
    {
        ResetVisuals();
    }

    // ---------- 보조 함수 ----------
    private void PlayAttackAnim()
    {
        if (_animator == null) return;
        _animator.SetFloat("AttackSpeed", _calculatedAttackSpeed);
        _animator.SetTrigger("att"); 
    }

    private void SetHitEffect()
    {
        if (_renderers == null || _renderers.Length == 0) return;

        _onHitRootTime = Time.time + _ai.OnHitRootTime;
    }

    private void SetCCEffect(CrowdControlType ccType, float duration)
    {
        if (_renderers == null || _renderers.Length == 0) return;
        
        _activeCCTimers[ccType] = duration; // 이미 이벤트 전달 받을때 시간을 계산해서 받음으로 단순 대입함
    }

    private void PlayEffect()
    {
        if (Time.time < _onHitRootTime)
        {
            ApplyEffectColor(_feedBackColor.GetOnHitColor());
            _isOriginColor = false;
            return;
        }
        
        var priorityList = _feedBackColor.CCPriorityList;
        for (int i = 0; i < priorityList.Count; i++)
        {
            var ccType = priorityList[i];
            
            if (_activeCCTimers.TryGetValue(ccType, out float endTime) && Time.time < endTime)
            {
                if (_feedBackColor.TryGetEffectColor(ccType, out Color ccColor))
                {
                    ApplyEffectColor(ccColor);
                    _isOriginColor = false;
                    return; // 색을 적용했으므로 하위 CC 무시하고 종료
                }
            }
        }
        
        if (!_isOriginColor)
        {
            ResetVisuals();
        }
    }

    private void ResetVisuals()
    {
        ApplyEffectColor(Color.white);
        _isOriginColor = true;
    }
    
    private void ApplyEffectColor(Color color)
    {
        if (_renderers == null || _mpb == null) return;
        
        _mpb.SetColor(ColorProp, color);
        
        foreach (var data in _renderers)
        {
            data.SetPropertyBlock(_mpb);
        }
    }
}