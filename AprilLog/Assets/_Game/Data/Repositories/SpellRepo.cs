// 담당자 : 정승우
// 설명   : 캐릭터/스킬/인챈트 데이터 저장소

// 수정자 : 김영찬
// 수정 내용 : 스테이터스와 스킬&인첸트 부분 분리

// 수정자 : 김영찬
// 수정 내용 : 새로운 인첸트 데이터 시트에 맞춰 재 정의

// 수정자 : 김영찬
// 수정 내용 : 데이터 저장 및 탐색 로직 변경

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

    [Header("런타임 스킬(Legacy) — GetSkill(int)용. Legacy_SkillDataTable.asset 배선 필수")]
    [SerializeField] private Legacy_SkillDataTable _skillDataTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, Legacy_SkillMasterData> _skillMaster;
    private Dictionary<int, Legacy_SkillData> _skills;
    private Dictionary<int, Legacy_EffectData> _effects;
    private Dictionary<int, Legacy_EnchantMasterData> _enchantMaster;
    private Dictionary<string, Legacy_EnchantLevelData> _enchantLevels;
    private List<Legacy_EnchantWeightData> _enchantWeights;
    
    // ID 개별 탐색 캐시
    private Dictionary<int, SkillTableData> _skillDataByID;
    private Dictionary<int, StatTableData> _statDataByID;
    private Dictionary<int, EffectTableData> _effectDataByID; 
    
    // 그룹, 이름, 레벨 탐색 캐시
    private Dictionary<int, SkillGroupChainData> _skillGroups;
    private Dictionary<int, StatGroupChainData> _statGroups;
    
    private bool _isInitialized;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[SpellRepo] Already initialized. Skip.");
            return;
        }

        _skillDataByID = new ();
        _statDataByID = new ();
        _effectDataByID = new ();

        _skillGroups = new ();
        _statGroups = new ();
        
        // SO List -> Dictionary 변환 (파싱 아님, 이미 메모리에 있는 데이터 옮기기)
        BuildSkillData();
        BuildStatData();
        BuildEffectData();
        // 런타임 콤보/소환/스킬인챈트가 쓰는 Legacy_SkillData 캐시(_skills). _skillDataTable SO를 배선하면 로컬 빌드,
        // 안 했으면 GetSkill에서 Legacy_CharacterRepo(이미 배선됨)로 폴백한다(머지 후 빌드 누락 → 스킬 전부 무동작 복구).
        if (_skillDataTable != null)
            _skills = BuildDictionary(_skillDataTable, nameof(_skillDataTable), r => r.SkillID, false);

        _isInitialized = true;
        Debug.Log($"[SpellRepo] Initialized Complete. Skill Group Count: {_skillGroups.Count}, Stat Group Count: {_statGroups.Count}, Effect Count : {_effectDataByID.Count}");
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
    
    private void BuildSkillData()
    {
        if (_skillTable == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_skillTable)} is not assigned. Empty pool dictionary will be used.");
            return;
        }

        if (_skillTable.rows == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_skillTable)}.rows is null. Empty pool dictionary will be used.");
            return;
        }
        
        foreach (var row in _skillTable.rows)
        {
            if (row == null) continue;

            // 개별 ID 매핑
            _skillDataByID[row.Skill_ID] = row;

            // 그룹 -> 이름 매핑
            if (!_skillGroups.ContainsKey(row.SkillGroup_ID))
                _skillGroups[row.SkillGroup_ID] = new SkillGroupChainData(row.SkillGroup_ID);
            
            _skillGroups[row.SkillGroup_ID].AddData(row);
        }
    }
    
    private void BuildStatData()
    {
        if (_statTable == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_statTable)} is not assigned. Empty pool dictionary will be used.");
            return;
        }

        if (_statTable.rows == null)
        {
            Debug.LogWarning($"[SpellRepo] {nameof(_statTable)}.rows is null. Empty pool dictionary will be used.");
            return;
        }
        
        foreach (var row in _statTable.rows)
        {
            if (row == null) continue;

            _statDataByID[row.StatEnchant_ID] = row;

            if (!_statGroups.ContainsKey(row.StatGroup_ID))
                _statGroups[row.StatGroup_ID] = new StatGroupChainData(row.StatGroup_ID);
            
            _statGroups[row.StatGroup_ID].AddData(row);
        }
    }

    private void BuildEffectData()
    {
        _effectDataByID = BuildDictionary(_effectTable, nameof(_effectTable), r => r.Effect_ID, true);
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
    public Legacy_SkillData GetSkill(int skillId)
    {
        // _skillDataTable 미배선으로 _skills가 비면, 이미 배선돼 동작 중인 Legacy_CharacterRepo의 동일 데이터로 폴백.
        if (_skills == null || _skills.Count == 0)
        {
            var legacy = Legacy_DataManager.Instance != null ? Legacy_DataManager.Instance.CharacterRepo : null;
            if (legacy != null) return legacy.GetSkill(skillId);
        }
        return GetData(_skills, skillId, nameof(GetSkill));
    }
    public Legacy_EffectData GetEffect(int effectId) => GetData(_effects, effectId, nameof(GetEffect));
    public Legacy_EnchantMasterData GetEnchantMaster(int enchantId) => GetData(_enchantMaster, enchantId, nameof(GetEnchantMaster));
    public Legacy_EnchantLevelData GetEnchantLevel(int enchantId, int level) => GetData(_enchantLevels, $"{enchantId}_{level}", nameof(GetEnchantLevel));
    
    // 개별 인첸트, 이펙트 조회
    public SkillTableData GetSkillData(int skillId) => GetData(_skillDataByID, skillId, nameof(GetSkillData));
    public StatTableData GetStatData(int statId) => GetData(_statDataByID, statId, nameof(GetStatData));
    public EffectTableData GetEffectData(int effectId) => GetData(_effectDataByID, effectId, nameof(GetEffectData));
    
    // 인첸트 선택 풀 구성
    public IReadOnlyDictionary<int, SkillGroupChainData> GetAllSkillGroups() => _skillGroups;
    public IReadOnlyDictionary<int, StatGroupChainData> GetAllStatGroups() => _statGroups;
    
    // 특정 그룹 정보 탐색
    public SkillGroupChainData GetSkillGroup(int groupId) => GetData(_skillGroups, groupId, nameof(GetSkillGroup));
    public StatGroupChainData GetStatGroup(int groupId) => GetData(_statGroups, groupId, nameof(GetStatGroup));
    
    // 이름 기반 탐색 (UI 출력 용도)
    public SkillNameChainData GetSkillChainByName(int groupId, int nameIndex)
    {
        var group = GetSkillGroup(groupId);

        if (group == null)
        {
            Debug.LogWarning($"[SpellRepo] GetSkillChainByName data not found. This Group Id is No Available. Group Id: {groupId}");
            return null;
        }

        if (group.SkillNameChainData.TryGetValue(nameIndex, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[SpellRepo] GetSkillChainByName data not found. This NameIndex is No Available. NameIndex: {nameIndex}");
        return null;
    }

    public StatNameChainData GetStatChainByName(int groupId, int nameIndex)
    {
        var group = GetStatGroup(groupId);
        
        if (group == null)
        {
            Debug.LogWarning($"[SpellRepo] GetStatChainByName data not found. This Group Id is No Available. Group Id: {groupId}");
            return null;
        }

        if (group.StatNameChainData.TryGetValue(nameIndex, out var data))
        {
            return data;
        }

        Debug.LogWarning($"[SpellRepo] GetStatChainByName data not found. This NameIndex is No Available. NameIndex: {nameIndex}");
        return null;
    }

    // TryGetValue 시행 및 에러 메세지 발송 메서드
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
