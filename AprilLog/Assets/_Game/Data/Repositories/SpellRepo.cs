// 담당자 : 정승우
// 설명   : 캐릭터/스킬/인챈트 데이터 저장소

// 수정자 : 김영찬
// 수정 내용 : 스테이터스와 스킬&인첸트 부분 분리

// 수정자 : 김영찬
// 수정 내용 : 새로운 인첸트 데이터 시트에 맞춰 재 정의

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬, 인챈트 관련 SO를 Inspector에서 받아서
/// Dictionary로 캐싱한다. 런타임에서는 Dictionary 조회만.
/// Initialize()에서만 LINQ 씀 (한 번이라 GC 괜찮음).
/// </summary>
public class SpellRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("스킬 인첸트")]
    [SerializeField] private SkillEnchantTable _skillTable;
    [SerializeField] public EffectMasterTable _effectTable;

    [Header("스텟 인챈트")]
    [SerializeField] private StatEnchantTable _statTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, Legacy_SkillMasterData> _skillMaster;
    private Dictionary<int, Legacy_SkillData> _skills;
    private Dictionary<int, Legacy_EffectData> _effects;
    private Dictionary<int, Legacy_EnchantMasterData> _enchantMaster;
    private Dictionary<string, Legacy_EnchantLevelData> _enchantLevels;
    private List<Legacy_EnchantWeightData> _enchantWeights;
    
    private Dictionary<int, List<SkillTableData>> _skillDataSortByName;
    private Dictionary<int, SkillTableData> _skillDataSortByID;
    private Dictionary<int, List<StatTableData>> _statDataSortByName;
    private Dictionary<int, StatTableData> _statDataSortByID;
    private Dictionary<int, EffectTableData> _effectData;
    
    
    
    
    private bool _isInitialized;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[SpellRepo] Already initialized. Skip.");
            return;
        }

        // SO List -> Dictionary 변환 (파싱 아님, 이미 메모리에 있는 데이터 옮기기)
        _skillDataSortByName = BuildSkillDictionarySortByName();
        _statDataSortByName = BuildStatDictionarySortByName();
        _skillDataSortByID = BuildDictionary(_skillTable, nameof(_skillTable), r => r.Skill_ID, true);
        _statDataSortByID = BuildDictionary(_statTable, nameof(_statTable), r => r.StatEnchant_ID, true);
        _effectData = BuildDictionary(_effectTable, nameof(_effectTable), r => r.Effect_ID, true);

        _isInitialized = true;
        Debug.Log($"[SpellRepo] 초기화 완료. " +
            $"SkillMaster: {_skillMaster.Count}, Skills: {_skills.Count}, Effects: {_effects.Count}, " +
            $"Enchants: {_enchantMaster.Count}, EnchantLevels: {_enchantLevels.Count}, EnchantWeights: {_enchantWeights.Count}");
    }

    // ---------- Build Dictionary ----------
    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector,
        bool isRequired)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            LogMissingTable(tableName, isRequired);
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[SpellRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[SpellRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[SpellRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }
    
    private Dictionary<int, List<SkillTableData>> BuildSkillDictionarySortByName()
    {
        var result = new Dictionary<int, List<SkillTableData>>();

        if (_skillTable == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_skillTable)} is not assigned. Empty pool dictionary will be used.");
            return result;
        }

        if (_skillTable.rows == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_skillTable)}.rows is null. Empty pool dictionary will be used.");
            return result;
        }

        for (int i = 0; i < _skillTable.rows.Count; i++)
        {
            SkillTableData row = _skillTable.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {nameof(_skillTable)}.rows[{i}] is null. Skip.");
                continue;
            }

            if (!result.TryGetValue(row.Name, out var pool))
            {
                pool = new List<SkillTableData>();
                result.Add(row.Name, pool);
            }

            pool.Add(row);
        }

        return result;
    }
    
    private Dictionary<int, List<StatTableData>> BuildStatDictionarySortByName()
    {
        var result = new Dictionary<int, List<StatTableData>>();

        if (_statTable == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_statTable)} is not assigned. Empty pool dictionary will be used.");
            return result;
        }

        if (_statTable.rows == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_statTable)}.rows is null. Empty pool dictionary will be used.");
            return result;
        }

        for (int i = 0; i < _statTable.rows.Count; i++)
        {
            StatTableData row = _statTable.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {nameof(_statTable)}.rows[{i}] is null. Skip.");
                continue;
            }

            if (!result.TryGetValue(row.Stat_Name, out var pool))
            {
                pool = new List<StatTableData>();
                result.Add(row.Stat_Name, pool);
            }

            pool.Add(row);
        }

        return result;
    }
    

    private void LogMissingTable(string tableName, bool isRequired)
    {
        string message = $"[SpellRepo] {tableName} is not assigned. Empty data will be used.";
        if (isRequired)
            Debug.LogError(message);
        else
            Debug.LogWarning(message);
    }

    // ---------- 조회 API ----------
    public Legacy_SkillMasterData GetSkillMaster(int standardId) => GetData(_skillMaster, standardId, nameof(GetSkillMaster));
    public Legacy_SkillData GetSkill(int skillId) => GetData(_skills, skillId, nameof(GetSkill));
    public Legacy_EffectData GetEffect(int effectId) => GetData(_effects, effectId, nameof(GetEffect));
    public Legacy_EnchantMasterData GetEnchantMaster(int enchantId) => GetData(_enchantMaster, enchantId, nameof(GetEnchantMaster));
    public Legacy_EnchantLevelData GetEnchantLevel(int enchantId, int level) => GetData(_enchantLevels, $"{enchantId}_{level}", nameof(GetEnchantLevel));
    
    /// <summary>
    /// 스킬 ID로 스킬 인첸트 데이터를 찾음
    /// </summary>
    /// <param name="skillId">스킬의 고유 ID</param>
    /// <returns>스킬 인첸트 데이터</returns>
    public SkillTableData GetSkillData(int skillId) => GetData(_skillDataSortByID, skillId, nameof(GetSkillData));
    
    /// <summary>
    /// 스텟 ID로 스텟 인첸트 데이터를 찾음
    /// </summary>
    /// <param name="statId">스텟의 고유 ID</param>
    /// <returns>스텟 인첸트 데이터</returns>
    public StatTableData GetStatData(int statId) => GetData(_statDataSortByID, statId, nameof(GetStatData));
    public EffectTableData GetEffectData(int effectID) => GetData(_effectData, effectID, nameof(GetEffectData));

    /// <summary>
    /// 스킬 이름과 스킬 레벨로 스킬 인첸트 데이터 테이블을 찾음
    /// </summary>
    /// <param name="nameIndex">스킬 이름의 ID</param>
    /// <param name="level">스킬 인첸트 레벨</param>
    /// <returns>스킬 인첸트 데이터</returns>
    public SkillTableData GetSkillData(int nameIndex, int level)
    {
        if (_skillDataSortByName == null)
        {
            Debug.LogWarning("[SpellRepo] GetSkillData cache is not initialized. Sort Type : Name");
            return null;
        }

        if (_skillDataSortByName.TryGetValue(nameIndex, out var temp))
        {
            foreach (var data in temp)
            {
                if(data.Level == level) 
                {
                    return data;
                }
            }
        }
        
        Debug.LogWarning($"[SpellRepo] GetSkillData data not found. Key: {nameIndex}, Level: {level}");
        return null;
    }

    /// <summary>
    /// 스텟 이름과 스텟 레벨로 스텟 인첸트 데이터 테이블을 찾음
    /// </summary>
    /// <param name="nameIndex">스텟 이름의 ID</param>
    /// <param name="level">스텟 인첸트 레벨</param>
    /// <returns>스텟 인첸트 데이터</returns>
    public StatTableData GetStatData(int nameIndex, int level)
    {
        if (_statDataSortByName == null)
        {
            Debug.LogWarning("[SpellRepo] GetStatData cache is not initialized. Sort Type : Name");
            return null;
        }
        
        if (_statDataSortByName.TryGetValue(nameIndex, out var temp))
        {
            foreach (var data in temp)
            {
                if(data.StatLevel == level) 
                {
                    return data;
                }
            }
        }
        
        Debug.LogWarning($"[SpellRepo] GetSkillData data not found. Key: {nameIndex}, Level: {level}");
        return null;
    }

    // 전체 조회 (인챈트 선택 로직에서 필요)
    public IReadOnlyDictionary<int, Legacy_EnchantMasterData> GetAllEnchantMasters()
    {
        if (_enchantMaster == null)
        {
            Debug.LogWarning("[SpellRepo] EnchantMaster cache is not initialized. Empty dictionary will be returned.");
            _enchantMaster = new Dictionary<int, Legacy_EnchantMasterData>();
        }

        return _enchantMaster;
    }

    public IReadOnlyList<Legacy_EnchantWeightData> GetEnchantWeights()
    {
        if (_enchantWeights == null)
        {
            Debug.LogWarning("[SpellRepo] EnchantWeight cache is not initialized. Empty list will be returned.");
            _enchantWeights = new List<Legacy_EnchantWeightData>();
        }

        return _enchantWeights;
    }

    // 안전 조회 (키가 없을 수 있는 경우)
    public bool TryGetEffect(int id, out Legacy_EffectData data)
    {
        data = null;
        return _effects != null && _effects.TryGetValue(id, out data);
    }

    private TData GetData<TKey, TData>(Dictionary<TKey, TData> dictionary, TKey key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[SpellRepo] {methodName} cache is not initialized. Sort Type : ID");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[SpellRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}
