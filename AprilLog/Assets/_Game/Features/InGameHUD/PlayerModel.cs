// 담당자 : 정승우
// 설명   : 플레이어 Model -- HP, Shield, 스탯 데이터 + 이벤트

using System;
using UnityEngine;

/// <summary>
/// 플레이어 HP, Shield, 스탯을 관리한다.
/// 값 바뀌면 이벤트 발행. View가 Presenter 통해서 구독함.
/// </summary>
public class PlayerModel : MonoBehaviour, IDamageable
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnHPChanged;          // current, max
    public event Action<int, int> OnShieldChanged;      // current, max
    public event Action OnPlayerDeath;

    // ---------- 데이터 ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int CurrentShield { get; private set; }
    public int MaxShield { get; private set; }

    // 추가 : 홍정옥
    // 내용 : JSON/SO에서 읽어온 캐릭터 기본 공격력과 성장 보너스가 반영된 공격력을 관리
    public int Attack { get; private set; }
    public int BaseAttack { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData data)
    {
        MaxHP = data.MaxHP;
        CurrentHP = data.MaxHP;
        MaxShield = data.Shield;
        CurrentShield = data.Shield;
        
        BaseAttack = data.Attack;
        Attack = data.Attack;

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }

    // 이어하기용 복원
    public void RestoreFromSave(InGameSaveData save)
    {
        CurrentHP = save.playerHP;
        CurrentShield = save.playerShield;

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }

    // ---------- 데미지 ----------
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        // Shield가 먼저 깎이고 남은 게 HP로 넘어감
        if (CurrentShield > 0)
        {
            int shieldDmg = Mathf.Min(amount, CurrentShield);
            CurrentShield -= shieldDmg;
            amount -= shieldDmg;
            OnShieldChanged?.Invoke(CurrentShield, MaxShield);
        }

        if (amount <= 0) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (CurrentHP <= 0)
        {
            OnPlayerDeath?.Invoke();
        }
    }
    
    // 추가 : 홍정옥
    // 내용 : 아웃게임 성장 데이터로 증가한 HP/Shield/Attack 보너스를 PlayerModel에 적용
    public void ApplyStatBonus(int hpBonus, int shieldBonus, int attackBonus)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;

        MaxShield += shieldBonus;
        CurrentShield += shieldBonus;

        Attack = BaseAttack + attackBonus;

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }

    // ---------- 회복 ----------
    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // 아웃게임 성장으로 MaxHP가 올라갈 때
    public void ApplyStatBonus(int hpBonus, int shieldBonus)
    {
        ApplyStatBonus(hpBonus, shieldBonus, 0);
    }
}
