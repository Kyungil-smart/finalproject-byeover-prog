// 담당자 : 정승우
// 설명   : 인챈트 도감 Model -- 전체 목록 + 보유 여부

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 2차 수정자 : 김영찬
// 수정내용 : 기획서 - v1.04_인게임 성장 시스템_이균호 > 인첸트 시트 반영
// Legacy_DataManager 걷어내고 SpellRepo 연동 (스킬/스탯 도감 분리 로드 적용)

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

        // 💡 1. 신규 DataManager와 SpellRepo 사용
        if (DataManager.Instance == null || DataManager.Instance.SpellRepo == null)
        {
            Debug.LogWarning("[EnchantBookModel] SpellRepo is missing. Empty enchant book will be used.");
            OnBookUpdated?.Invoke();
            return;
        }

        var spellRepo = DataManager.Instance.SpellRepo;

        if (_enchantModel == null)
            Debug.LogWarning("[EnchantBookModel] EnchantModel is not assigned. Owned state will be treated as false.");
        
        // 스킬 도감 데이터 추가 (Name_ID 체인 기준)
        var skillGroups = spellRepo.GetAllSkillGroups();
        if (skillGroups != null)
        {
            foreach (var group in skillGroups.Values)
            {
                foreach (var chain in group.SkillNameChainData.Values)
                {
                    int nameId = chain.Name_ID;
                    bool isOwned = _enchantModel != null && _enchantModel.HasSkill(nameId);
                    int level = _enchantModel != null ? _enchantModel.GetSkillLevel(nameId) : 0;

                    _allEntries.Add(new EnchantBookDisplayData
                    {
                        EnchantId = nameId, 
                        Name = $"SkillName_{nameId}", // ToDo: 나중에 로컬라이징 테이블(언어DB)과 연동. 현재 임시값임
                        Type = "스킬",
                        IsOwned = isOwned,
                        Level = level,
                        MaxLevel = chain.MaxLevel
                    });
                }
            }
        }
        
        // 스탯 도감 데이터 추가 (Stat_Name_ID 체인 기준)
        var statGroups = spellRepo.GetAllStatGroups();
        if (statGroups != null)
        {
            foreach (var group in statGroups.Values)
            {
                foreach (var chain in group.StatNameChainData.Values)
                {
                    int statNameId = chain.Stat_Name_ID;
                    bool isOwned = _enchantModel != null && _enchantModel.HasStat(statNameId);
                    int level = _enchantModel != null ? _enchantModel.GetStatLevel(statNameId) : 0;

                    _allEntries.Add(new EnchantBookDisplayData
                    {
                        EnchantId = statNameId,
                        Name = $"StatName_{statNameId}", // ToDo: 나중에 로컬라이징 테이블과 연동. 현재 임시값임
                        Type = "스탯",
                        IsOwned = isOwned,
                        Level = level,
                        MaxLevel = chain.MaxLevel
                    });
                }
            }
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
