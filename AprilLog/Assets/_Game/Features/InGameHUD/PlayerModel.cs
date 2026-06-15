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
    public event Action OnPlayerDeath;

    // ---------- 데이터 ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public float Attack { get; private set; }
    public float CriticalRate { get; private set; }
    public float CriticalDamage { get; private set; }
    public int FlatPierce { get; private set; }
    public float PercentagePierce { get; private set; }
    public int StunPower { get; private set; }
    public int SlowPower { get; private set; }
    public int HitCount { get; private set; }
    public int AoE { get; private set; }
    public int MaxTargets { get; private set; }
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
        StunPower = characterData.EffectPower;
        SlowPower = characterData.EffectPower;
        HitCount = characterData.HitCount;
        AoE = characterData.AoE;
        MaxTargets = characterData.MaxTargets;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void RestoreFromSave(InGameSaveData save)
    {
        CurrentHP = save.playerHP;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // ---------- 데미지 ----------
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
            OnPlayerDeath?.Invoke();
    }

    // ---------- 회복 ----------
    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // 아웃게임 성장 보너스 적용 (홍정옥 로직 기반)
    // OutLevelData 기준: MaxHP, Attack, StunPower, SlowPower
    // ToDo : 아웃게임 성장 DB 변경사항 반영 할 것
    public void ApplyStatBonus_OutGameBonus(int hpBonus, int attackBonus, int stunBonus, int slowBonus)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;
        Attack = _baseAttack + attackBonus;
        StunPower += stunBonus;
        SlowPower += slowBonus;
        
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // ---------- 스텟 증가 ----------
    public void ApplyHpBonus_Add(int bonus)
    {
        MaxHP += bonus;
        CurrentHP += bonus;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }
    
    public void ApplyHpBonus_Rate(float bonus)
    {
        if (bonus < 1) bonus = 1 + bonus;
        MaxHP += Mathf.FloorToInt(MaxHP * bonus);
        CurrentHP += Mathf.FloorToInt(CurrentHP * bonus);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    public void ApplyAttackBonus_Add(int bonus)
    {
        Attack += bonus;
    }

    public void ApplyAttackBonus_Rate(float bonus)
    {
        if (bonus < 1) bonus = 1 + bonus;
        Attack *= bonus;
    }

    // 관통(PercentagePierce) 가산. 인챈트 효과 적용용.
    public void ApplyPierceBonus_Add(float bonus)
    {
        PercentagePierce += bonus;
    }

    public void ApplyCriRateBonus_Add(int bonus)
    {
        CriticalRate += bonus;
    }

    public void ApplyCriRateBonus_Rate(float bonus)
    {
        if (bonus < 1) bonus = 1 + bonus;
        CriticalRate *= bonus;
    }

    public void ApplyCriDmgBonus_Add(int bonus)
    {
        CriticalDamage += bonus;
    }

    public void ApplyCriDmgBonus_Rate(float bonus)
    {
        if (bonus < 1) bonus = 1 + bonus;
        CriticalDamage *= bonus;
    }

    // 인챈트 효과 적용용 (float 가산). 기존 _Add는 int라 소수(예: +0.05)가 0으로 묻힘.
    public void ApplyCriRateBonus_AddF(float bonus)
    {
        CriticalRate += bonus;
    }

    public void ApplyCriDmgBonus_AddF(float bonus)
    {
        CriticalDamage += bonus;
    }
}