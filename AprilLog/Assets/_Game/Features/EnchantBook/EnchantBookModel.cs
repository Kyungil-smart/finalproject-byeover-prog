// 담당자 : 정승우
// 설명   : 인챈트 도감 Model -- 전체 목록 + 보유 여부

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 인챈트 목록과 보유 여부를 관리한다.
/// 도감 화면에서 필터/정렬할 때 사용.
/// </summary>
public class EnchantBookModel : MonoBehaviour
{
    public event Action OnBookUpdated;

    [Header("참조")]
    [SerializeField] private EnchantModel _enchantModel;

    private List<EnchantBookDisplayData> _allEntries = new List<EnchantBookDisplayData>();

    public void Initialize()
    {
        RefreshEntries();
    }

    public void RefreshEntries()
    {
        _allEntries.Clear();
        var allMasters = DataManager.Instance.CharacterRepo.GetAllEnchantMasters();

        foreach (var pair in allMasters)
        {
            var master = pair.Value;
            _allEntries.Add(new EnchantBookDisplayData
            {
                EnchantId = master.EnchantID,
                Name = master.Name,
                Type = master.EnchantType,
                IsOwned = _enchantModel.HasEnchant(master.EnchantID),
                Level = _enchantModel.GetEnchantLevel(master.EnchantID),
                MaxLevel = master.MaxLevel
            });
        }

        OnBookUpdated?.Invoke();
    }

    public List<EnchantBookDisplayData> GetFiltered(BookFilter filter)
    {
        if (filter == BookFilter.All) return _allEntries;

        var result = new List<EnchantBookDisplayData>();
        for (int i = 0; i < _allEntries.Count; i++)
        {
            bool match = filter == BookFilter.Owned ? _allEntries[i].IsOwned : !_allEntries[i].IsOwned;
            if (match) result.Add(_allEntries[i]);
        }
        return result;
    }
}

[System.Serializable]
public class EnchantBookDisplayData
{
    public int EnchantId;
    public string Name;
    public string Type;
    public bool IsOwned;
    public int Level;
    public int MaxLevel;
}
