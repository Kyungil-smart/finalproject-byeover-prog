// 담당자 : 정승우
// 설명   : 플레이어 Model -- HP, 스탯 데이터 + 이벤트

using System;
using UnityEngine;

/// <summary>
/// 플레이어 HP, 스탯을 관리한다. 값 바뀌면 이벤트 발행.
/// </summary>
public class PlayerModel : MonoBehaviour, IDamageable
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnHPChanged;
    public event Action OnPlayerDeath;

    // ---------- 데이터 ----------
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData data)
    {
        MaxHP = data.MaxHP;
        CurrentHP = data.MaxHP;
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

    public void ApplyStatBonus(int hpBonus)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }
}