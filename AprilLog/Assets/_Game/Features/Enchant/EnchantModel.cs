// 담당자 : 정승우
// 설명   : 인챈트 Model -- 보유 인챈트 목록 관리

// 수정자 : 김영찬
// 설명 : 기획서 - v1.04_인게임 성장 시스템_이균호 > 인첸트 시트 반영 (스킬/스탯 분리, 그룹별 보유 한도 적용), 스킬 파트 연동 이벤트 추가

// 수정자 : 김영찬
// 설명 : 새로운 인첸트 선택 로직에 필요한 변수 및 제어함수 추가

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>
/// 인게임에서 획득한 인챈트 목록과 레벨을 관리한다.
/// 기획서: 스킬 인챈트 최대 5개(스킬 인첸트 중 조합 최대 3개) , 스텟 인첸트 최대 3개
/// </summary>
public class EnchantModel : MonoBehaviour
{
    // ---------- 직렬화 ----------
    [Header("참조")]
    [SerializeField] private EnchantUIModel _uiModel;
    [SerializeField] private EnchantCombinationModel _combinationModel;
    [SerializeField] private EnchantCalculator _calculator;
    
    // ---------- 이벤트 ----------
    // 스킬 인첸트
    // TODO: [스킬 담당자] 스킬 소환 및 인게임 액션 구현 시 아래 이벤트를 구독하여 처리해 주세요.
    public event Action<int, int> OnSkillAcquired;     // Name, Level
    public event Action<int, int> OnSkillLevelUp;      // Name, Level
    public event Action<int> OnSkillRemoved;           // Name
    
    // 스텟 인첸트
    public event Action<int, int> OnStatAcquired;    // Stat_Name, StatLevel
    public event Action<int, int> OnStatLevelUp;     // Stat_Name, StatLevel
    public event Action<int> OnStatRemoved;     // Stat_Name
    
    // ---------- Const ----------
    // 스킬 그룹 ID
    public const int GROUP_NORMAL_SKILL = 100000000;
    public const int GROUP_COMBINATION_SKILL = 200000000;
    public const int GROUP_COMBO_SKILL = 300000000;

    // 스텟 그룹 ID
    public const int GROUP_PROJECTILE_STAT = 100000;
    public const int GROUP_MODEL_STAT = 200000;
    public const int GROUP_CRIT_INTENSIFY_STAT = 300000;
    public const int GROUP_CRIT_CHANCE_STAT = 400000;
    public const int GROUP_CRIT_DAMAGE_STAT = 500000;
    public const int GROUP_SKILL_DAMAGE_STAT = 600000;
    public const int GROUP_EFFECT_INTENSIFY_STAT = 700000;
    public const int GROUP_AREA_EXTENSION_STAT = 800000;
    
    // ---------- 데이터 ----------
    // 스킬: Key = Name_ID, Value = 레벨과 그룹ID 보관
    private Dictionary<int, AcquiredSkillData> _ownedSkills = new ();
    public IReadOnlyDictionary<int, AcquiredSkillData> OwnedSkills => _ownedSkills;
    
    // 스탯: Key = Stat_Name_ID, Value = 레벨과 그룹ID 보관
    private Dictionary<int, AcquiredStatData> _ownedStats = new ();
    public IReadOnlyDictionary<int, AcquiredStatData> OwnedStats => _ownedStats;
    
    // ---------- private  ----------
    // 뽑기 팝업이 뜬 총 횟수를 추적 (스킬/스탯 순서 계산용)
    public int TotalDrawCount { get; private set; }

    // ---------- 초기화 ----------
    public void Initialize()
    {
        CountReset();
        _ownedSkills.Clear();
        _ownedStats.Clear();
        // 머지 후 씬 미배선 방어: EnchantUIModel(_model)이 인스펙터에 안 꽂혀 있으면
        // 같은 오브젝트에서 찾거나 새로 생성한다 (상호 참조 자동 복구). 없으면 여기서 NRE 나며 InitializeAll 전체가 중단됨.
        if (_uiModel == null)
            _uiModel = GetComponent<EnchantUIModel>() ?? gameObject.AddComponent<EnchantUIModel>();
        _uiModel.InitUIModel();
        
        if (_combinationModel == null)
            _combinationModel = GetComponent<EnchantCombinationModel>() ?? gameObject.AddComponent<EnchantCombinationModel>();
        _combinationModel.InitCombinationModel();
        
        if (_calculator == null)
            _calculator = GetComponent<EnchantCalculator>() ?? gameObject.AddComponent<EnchantCalculator>();
        _calculator.InitCalculator();
    }

    public void OnDestroy()
    {
        if (_uiModel != null) _uiModel.Discard();
        if (_combinationModel != null) _combinationModel.Discard();
        if (_calculator != null) _calculator.Discard();
    }

    // ---------- 획득 한도 체크 ----------
    /// <summary>
    /// 새로운 스킬을 획득할 수 있는 자리(여유 슬롯)가 있는지 확인합니다.
    /// </summary>
    public bool CanAcquireNewSkill(int nameId, int groupId)
    {
        // 이미 보유 중인 스킬이라면 단순히 레벨업이므로 슬롯을 차지하지 않음 (항상 true)
        if (HasSkill(nameId))
        {
            return true;
        } 

        // 전체 스킬 한도 체크 (최대 5개)
        if (_ownedSkills.Count >= 5)
        {
            return false;
        }

        // 조합 스킬 한도 체크 (최대 3개)
        if (groupId == GROUP_COMBINATION_SKILL)
        {
            int combinationCount = 0;
            foreach (var skill in _ownedSkills.Values)
            {
                if (skill.GroupID == GROUP_COMBINATION_SKILL)
                {
                    combinationCount++;
                }
            }
            
            if (combinationCount >= 3)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 새로운 스탯을 획득할 수 있는 자리(여유 슬롯)가 있는지 확인합니다.
    /// </summary>
    public bool CanAcquireNewStat(int statNameId, int groupId)
    {
        // 이미 보유 중인 스탯이면 통과
        if (HasStat(statNameId))
        {
            return true;
        }
        
        // 스탯 전체 한도 체크 (최대 3개)
        if (_ownedStats.Count >= 3)
        {
            return false;
        }

        return true;
    }
    
    // ---------- 조작 ----------
    public void AcquireSkill(int nameId, int groupId)
    {
        // 신규 SpellRepo는 그룹/이름 인덱스 미존재 시 null 반환 → 옛 '항상 non-null' 전제로 짠 .GetNextLevelData가 NRE.
        var chain = DataManager.Instance.SpellRepo.GetSkillChainByName(groupId, nameId);
        if (chain == null) { Debug.LogError($"[EnchantModel] SkillChain 없음 group={groupId} name={nameId} — SpellRepo SO 배선/인덱스 확인. 획득 스킵."); return; }

        if (_ownedSkills.ContainsKey(nameId))
        {
            var data = _ownedSkills[nameId];
            data.Data = chain.GetNextLevelData(data.Level);
            data.Level++;
            _ownedSkills[nameId] = data;

            OnSkillLevelUp?.Invoke(nameId, data.Level);
        }
        else
        {
            var data = chain.GetNextLevelData(0);
            _ownedSkills[nameId] = new AcquiredSkillData { Level = 1, GroupID = groupId, Data =  data };
            OnSkillAcquired?.Invoke(nameId, 1);
        }
    }

    public void AcquireStat(int statNameId, int groupId)
    {
        var chain = DataManager.Instance.SpellRepo.GetStatChainByName(groupId, statNameId);
        if (chain == null) { Debug.LogError($"[EnchantModel] StatChain 없음 group={groupId} name={statNameId} — SpellRepo SO 배선/인덱스 확인. 획득 스킵."); return; }

        if (_ownedStats.ContainsKey(statNameId))
        {
            var data = _ownedStats[statNameId];
            data.Data = chain.GetNextLevelData(data.Level);
            data.Level++;
            _ownedStats[statNameId] = data;

            OnStatLevelUp?.Invoke(statNameId, data.Level);
        }
        else
        {
            var data = chain.GetNextLevelData(0);
            _ownedStats[statNameId] = new AcquiredStatData { Level = 1, GroupID = groupId, Data = data };
            OnStatAcquired?.Invoke(statNameId, 1);
        }
    }

    public void RemoveSkill(int nameId)
    {
        if (_ownedSkills.Remove(nameId))
            OnSkillRemoved?.Invoke(nameId);
    }

    public void RemoveStat(int statNameId)
    {
        if (_ownedStats.Remove(statNameId))
            OnStatRemoved?.Invoke(statNameId);
    }

    // ---------- 조회 ----------
    public int GetHeldSkillCount() => _ownedSkills.Count;
    public int GetHeldStatCount() => _ownedStats.Count;

    public bool HasSkill(int nameId) => _ownedSkills.ContainsKey(nameId);
    public bool HasStat(int statNameId) => _ownedStats.ContainsKey(statNameId);

    public int GetSkillLevel(int nameId) => _ownedSkills.TryGetValue(nameId, out var data) ? data.Level : 0;
    public int GetStatLevel(int statNameId) => _ownedStats.TryGetValue(statNameId, out var data) ? data.Level : 0;

    // ---------- 세이브 / 로드 ----------
    public void RestoreFromSave(List<AcquiredEnchantSaveData> saves)
    {
        Initialize(); 
        if (saves == null) return;

        foreach (var save in saves)
        {
            int targetId = save.EnchantId;
            int targetLevel = save.Level;   // 복원 레벨은 save.Level. (옛 코드가 save.EnchantId를 넣어 레벨이 ID값으로 손상됐음)

            // 스킬 테이블에서 검색
            int? skillGroupId = FindSkillGroupId(targetId);
            if (skillGroupId.HasValue)
            {
                _ownedSkills[targetId] = new AcquiredSkillData { Level = targetLevel, GroupID = skillGroupId.Value };
                continue; 
            }

            // 스탯 테이블에서 검색
            int? statGroupId = FindStatGroupId(targetId);
            if (statGroupId.HasValue)
            {
                _ownedStats[targetId] = new AcquiredStatData { Level = targetLevel, GroupID = statGroupId.Value };
                continue;
            }

            Debug.LogWarning($"[EnchantModel] 세이브 복구 실패. 삭제되었거나 유효하지 않은 인챈트 ID: {targetId}");
        }
    }

    public List<AcquiredEnchantSaveData> ToSaveData()
    {
        var list = new List<AcquiredEnchantSaveData>();

        foreach (var pair in _ownedSkills)
            list.Add(new AcquiredEnchantSaveData { EnchantId = pair.Key, Level = pair.Value.Level });

        foreach (var pair in _ownedStats)
            list.Add(new AcquiredEnchantSaveData { EnchantId = pair.Key, Level = pair.Value.Level });

        return list;
    }
    
    // ---------- 보조 함수 ----------
    private int? FindSkillGroupId(int nameId)
    {
        foreach (var group in DataManager.Instance.SpellRepo.GetAllSkillGroups().Values)
        {
            if (group.SkillNameChainData.ContainsKey(nameId))
            {
                return group.SkillGroup_ID;
            }
        }
        return null;
    }

    private int? FindStatGroupId(int statNameId)
    {
        foreach (var group in DataManager.Instance.SpellRepo.GetAllStatGroups().Values)
        {
            if (group.StatNameChainData.ContainsKey(statNameId))
            {
                return group.StatGroup_ID;
            }
        }
        return null;
    }

    public void CountUpDrawCount()
    {
        TotalDrawCount++;
    }

    private void CountReset()
    {
        TotalDrawCount = 0;
    }
}