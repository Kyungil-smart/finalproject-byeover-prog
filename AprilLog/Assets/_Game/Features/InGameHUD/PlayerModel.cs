// 담당자 : 정승우
// 설명   : 플레이어 Model -- HP, Attack, 스탯 데이터 + 이벤트

// 1차 수정자 : 홍정옥
// 수정내용 : 아웃게임 성장 보너스 적용 로직 추가

// 2차 수정자 : 정승우
// 수정내용 : 기획서 v1.03 반영. Shield 삭제, Attack/StunPower/SlowPower 추가.

// 3차 수정자 : 김영찬
// 수정내용 : 초기화 시 CharacterStatus반영 하도록 수정

// 4차 수정자 : 김영찬
// 수정 내용 : 26.06.12 DB 컬럼 변경 사항 반영하여 기절강화 둔화강화를 효과 강화로 연결 및 FlatPierce 변수 추가

using System;
using UnityEngine;

/// <summary>
/// 플레이어 HP, Attack, CC강화력을 관리한다. 값 바뀌면 이벤트 발행.
/// </summary>
public class PlayerModel : MonoBehaviour, IDamageable
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnHPChanged;
    public event Action OnHit;
    public event Action OnPlayerDeath;

    // ---------- 데이터 ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int Attack { get; private set; }
    public float CriticalRate { get; private set; }
    public float CriticalDamage { get; private set; }
    public int FlatPierce { get; private set; }
    public float PercentagePierce { get; private set; }
    public int EffectPower { get; private set; }
    public int HitCount { get; private set; }
    public int AoE { get; private set; }
    public int MaxTargets { get; private set; }
    public float AttackSpeed { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private int _baseAttack;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData data, CharacterStatusData characterData)
    {
        MaxHP = data.MaxHP;
        CurrentHP = data.MaxHP;
        _baseAttack = data.Attack;
        Attack = data.Attack;
        CriticalRate = characterData.CriticalRate;
        CriticalDamage = characterData.CriticalDamage;
        FlatPierce = characterData.FlatPierce;
        PercentagePierce = characterData.PercentagePierce;
        EffectPower = characterData.EffectPower;
        HitCount = characterData.HitCount;
        AoE = characterData.AoE;
        MaxTargets = characterData.MaxTargets;
        AttackSpeed = data.BaseAttackSpeed;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void RestoreFromSave(InGameSaveData save)
    {
        // 저장된 현재 HP로 복원. 인게임 부트스트랩에서 인챈트 HP 재적용 '뒤'에 호출해야
        //  인챈트 HP가 CurrentHP에 이중 가산되는 과회복을 막는다. MaxHP는 재적용으로 이미 보정됨.
        CurrentHP = Mathf.Clamp(save.playerHP, 1, MaxHP);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // ---------- 데미지 ----------
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        
        if(CurrentHP > 0)
            HitFeedBack();

        if (CurrentHP <= 0)
            OnPlayerDeath?.Invoke();
    }

    // IDamageable 오버로드 — 플레이어 피해는 인챈트별 기록 대상 아님(몬스터→플레이어). skillId 무시하고 기존 처리.
    public void TakeDamage(int amount, int skillId) => TakeDamage(amount);

    private void HitFeedBack()
    {
        OnHit?.Invoke();
    }

    // ---------- 회복 ----------
    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // 아웃게임 성장 보너스 적용 (홍정옥 로직 기반)
    // OutLevelData 기준: MaxHP, Attack, effectPower, flatPierce
    public void ApplyStatBonus_OutGameBonus(int hpBonus, int attackBonus, int effectPower, int flatPierce)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;
        Attack = _baseAttack + attackBonus;
        EffectPower += effectPower;
        FlatPierce += flatPierce;
        
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // ---------- 스텟 증가 ----------
    
    /// <summary>
    /// 스테이터스의 증감을 구현하기 위한 키 함수
    /// </summary>
    /// <param name="status">증감하기 위한 스텟의 종류</param>
    /// <param name="formula">스텟의 증감식 - Add : 합연산 / Rate : 곱연산</param>
    /// <param name="amount">스텟의 증감량</param>
    /// <param name="isRemoved">스텟 효과를 제거해야 될 때 True</param>
    public void StatusEnhance(PlayerStatus status, CalFormula formula, float amount, bool isRemoved)
    {
        switch (status)
        {
            case PlayerStatus.Hp:
                ApplyHp(formula, amount, isRemoved);
                break;
            case PlayerStatus.Attack:
                ApplyAttack(formula, amount, isRemoved);
                break;
            case PlayerStatus.AttackSpeed:
                ApplyAttackSpeed(formula, amount, isRemoved);
                break;
        }
    }

    private void ApplyHp(CalFormula formula, float amount, bool isRemoved)
    {
        switch (formula)
        {
            case CalFormula.Add:
                if (isRemoved)
                {
                    MaxHP -= Mathf.RoundToInt(amount);
                    if(MaxHP < 1) MaxHP = 1;
        
                    CurrentHP -= Mathf.RoundToInt(amount);
                    if(CurrentHP < 1) CurrentHP = 1;
        
                    OnHPChanged?.Invoke(CurrentHP, MaxHP);
                    break;
                }
                MaxHP += Mathf.RoundToInt(amount);
                CurrentHP += Mathf.RoundToInt(amount);
                OnHPChanged?.Invoke(CurrentHP, MaxHP);
                break;
            case CalFormula.Rate:
                if (isRemoved)
                {
                    amount = 1 - amount;
                    MaxHP = Mathf.FloorToInt(MaxHP * amount);
                    if(MaxHP < 1) MaxHP = 1;
        
                    CurrentHP = Mathf.FloorToInt(CurrentHP * amount);
                    if(CurrentHP < 1) CurrentHP = 1;
        
                    OnHPChanged?.Invoke(CurrentHP, MaxHP);
                    break;
                }
                amount = 1 + amount;
                MaxHP = Mathf.FloorToInt(MaxHP * amount);
                CurrentHP = Mathf.FloorToInt(CurrentHP * amount);
                OnHPChanged?.Invoke(CurrentHP, MaxHP);
                break;
        }
    }

    private void ApplyAttack(CalFormula formula, float amount, bool isRemoved)
    {
        switch (formula)
        {
            case CalFormula.Add:
                if (isRemoved)
                {
                    Attack -= Mathf.RoundToInt(amount);
                    if(Attack < _baseAttack) Attack = _baseAttack;
                    break;
                }
                Attack += Mathf.RoundToInt(amount);
                break;
            case CalFormula.Rate:
                if (isRemoved)
                {
                    amount = 1 - amount;
                    Attack = Mathf.FloorToInt(Attack * amount);
                    if(Attack < _baseAttack) Attack = _baseAttack;
                    break;
                }
                amount = 1 + amount;
                Attack = Mathf.FloorToInt(Attack * amount);
                break;
        }
    }

    private void ApplyAttackSpeed(CalFormula formula, float amount, bool isRemoved)
    {
        // 공격속도는 공격간 간격임으로, 공격속도가 증가한다 > 공격 간격이 감소함
        switch (formula)
        {
            case CalFormula.Add:
                if (isRemoved)
                {
                    AttackSpeed += amount;
                    break;
                }
                AttackSpeed -= amount;
                if(AttackSpeed < 0) AttackSpeed = 0;
                break;
            case CalFormula.Rate:
                if (isRemoved)
                {
                    amount = 1 + amount;
                    AttackSpeed *= amount;
                    
                    break;
                }
                amount = 1 - amount;
                AttackSpeed *= amount;
                if(AttackSpeed < 0) AttackSpeed = 0;
                break;
        }
    }
    
    
    /// <summary>
    /// 레거시 계산 함수 : StatusEnhance 사용할 것. 차후 삭제함
    /// </summary>
    /// <param name="bonus"></param>
    public void Legacy_ApplyHpBonus_Add(int bonus)
    {
        MaxHP += bonus;
        CurrentHP += bonus;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }
    
    /// <summary>
    /// 레거시 계산 함수 : StatusEnhance 사용할 것. 차후 삭제함
    /// </summary>
    /// <param name="bonus"></param>
    public void Legacy_ApplyAttackBonus_Rate(float bonus)
    {
        if (bonus < 1) bonus = 1 + bonus;
        Attack = Mathf.FloorToInt(Attack * bonus);
    }

    /// <summary>
    /// 레거시 계산 함수 : StatusEnhance 사용할 것. 차후 삭제함
    /// </summary>
    /// <param name="bonus"></param>
    // 관통(PercentagePierce) 가산. 인챈트 효과 적용용.
    public void Legacy_ApplyPierceBonus_Add(float bonus)
    {
        PercentagePierce += bonus;
    }

    /// <summary>
    /// 레거시 계산 함수 : StatusEnhance 사용할 것. 차후 삭제함
    /// </summary>
    /// <param name="bonus"></param>
    // 인챈트 효과 적용용 (float 가산). 기존 _Add는 int라 소수(예: +0.05)가 0으로 묻힘.
    public void Legacy_ApplyCriRateBonus_AddF(float bonus)
    {
        CriticalRate += bonus;
    }

    /// <summary>
    /// 레거시 계산 함수 : StatusEnhance 사용할 것. 차후 삭제함
    /// </summary>
    /// <param name="bonus"></param>
    public void Legacy_ApplyCriDmgBonus_AddF(float bonus)
    {
        CriticalDamage += bonus;
    }
}