// 담당자 : 정승우
// 설명   : 플레이어 Model -- HP, Attack, 스탯 데이터 + 이벤트

// 1차 수정자 : 홍정옥
// 수정내용 : 아웃게임 성장 보너스 적용 로직 추가

// 2차 수정자 : 정승우
// 수정내용 : 기획서 v1.03 반영. Shield 삭제, Attack/StunPower/SlowPower 추가.

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
    public int Attack { get; private set; }
    public int StunPower { get; private set; }
    public int SlowPower { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private int _baseAttack;

    // ---------- 초기화 ----------
    public void Initialize(CommonStatusData data)
    {
        MaxHP = data.MaxHP;
        CurrentHP = data.MaxHP;
        _baseAttack = data.Attack;
        Attack = data.Attack;
        StunPower = 0;
        SlowPower = 0;
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
    public void ApplyStatBonus(int hpBonus, int attackBonus, int stunBonus, int slowBonus)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;
        Attack = _baseAttack + attackBonus;
        StunPower += stunBonus;
        SlowPower += slowBonus;

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // 단순 HP만 올릴 때 (인게임 레벨업 등)
    public void ApplyStatBonus(int hpBonus)
    {
        MaxHP += hpBonus;
        CurrentHP += hpBonus;
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }
}