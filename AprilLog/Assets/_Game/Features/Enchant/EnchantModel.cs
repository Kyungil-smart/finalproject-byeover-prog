// 담당자 : 정승우
// 설명   : 인챈트 Model -- 보유 인챈트 목록 관리

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인게임에서 획득한 인챈트 목록과 레벨을 관리한다.
/// 기획서: 스킬 인챈트 최대 5개, 조합 최대 3개, 콤보 최대 5개.
/// </summary>
public class EnchantModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnEnchantAcquired;    // enchantId, level
    public event Action<int, int> OnEnchantLevelUp;     // enchantId, newLevel
    public event Action<int> OnEnchantRemoved;          // enchantId

    // ---------- 데이터 ----------
    private Dictionary<int, int> _ownedEnchants = new Dictionary<int, int>();

    public int GetOwnedCount() => _ownedEnchants.Count;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        _ownedEnchants.Clear();
    }

    public void RestoreFromSave(List<AcquiredEnchant> saves)
    {
        _ownedEnchants.Clear();
        if (saves == null) return;

        for (int i = 0; i < saves.Count; i++)
            _ownedEnchants[saves[i].enchantId] = saves[i].level;
    }

    // ---------- 조작 ----------
    public void AcquireEnchant(int enchantId)
    {
        if (_ownedEnchants.ContainsKey(enchantId))
        {
            _ownedEnchants[enchantId]++;
            OnEnchantLevelUp?.Invoke(enchantId, _ownedEnchants[enchantId]);
        }
        else
        {
            _ownedEnchants[enchantId] = 1;
            OnEnchantAcquired?.Invoke(enchantId, 1);
        }
    }

    public void RemoveEnchant(int enchantId)
    {
        if (_ownedEnchants.Remove(enchantId))
            OnEnchantRemoved?.Invoke(enchantId);
    }

    // ---------- 조회 ----------
    public bool HasEnchant(int enchantId) => _ownedEnchants.ContainsKey(enchantId);
    public int GetEnchantLevel(int enchantId) => _ownedEnchants.TryGetValue(enchantId, out int lv) ? lv : 0;

    public List<AcquiredEnchant> ToSaveData()
    {
        var list = new List<AcquiredEnchant>();
        foreach (var pair in _ownedEnchants)
            list.Add(new AcquiredEnchant { enchantId = pair.Key, level = pair.Value });
        return list;
    }
}