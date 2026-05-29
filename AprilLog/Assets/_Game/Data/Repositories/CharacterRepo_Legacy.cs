// 담당자 : 정승우
// 설명   : 캐릭터/스킬/인챈트 데이터 저장소

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터, 스킬, 인챈트 관련 SO를 Inspector에서 받아서
/// Dictionary로 캐싱한다. 런타임에서는 Dictionary 조회만.
/// Initialize()에서만 LINQ 씀 (한 번이라 GC 괜찮음).
/// </summary>
public class Legacy_CharacterRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("캐릭터")]
    [SerializeField] private CharacterMasterTable _characterMasterTable;
    [SerializeField] private CommonStatusTable _commonStatusTable;
    [SerializeField] private CharacterStatusTable _characterStatusTable;
    [SerializeField] private MonsterStatusTable _monsterStatusTable;

    [Header("스킬")]
    [SerializeField] private Legacy_SkillMasterTable _skillMasterTable;
    [SerializeField] private Legacy_SkillDataTable _skillDataTable;
    [SerializeField] private Legacy_EffectDataTable _effectTable;

    [Header("인챈트")]
    [SerializeField] private Legacy_EnchantMasterTable _enchantMasterTable;
    [SerializeField] private Legacy_EnchantLevelTable _enchantLevelTable;
    [SerializeField] private Legacy_EnchantWeightTable _enchantWeightTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, CharacterMasterData> _characterMaster;
    private Dictionary<int, CommonStatusData> _commonStatus;
    private Dictionary<int, CharacterStatusData> _characterStatus;
    private Dictionary<int, MonsterStatusData> _monsterStatus;
    private Dictionary<int, Legacy_SkillMasterData> _skillMaster;
    private Dictionary<int, Legacy_SkillData> _skills;
    private Dictionary<int, Legacy_EffectData> _effects;
    private Dictionary<int, Legacy_EnchantMasterData> _enchantMaster;
    private Dictionary<string, Legacy_EnchantLevelData> _enchantLevels;
    private List<Legacy_EnchantWeightData> _enchantWeights;
    private bool _isInitialized;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[CharacterRepo] Already initialized. Skip.");
            return;
        }

        // SO List -> Dictionary 변환 (파싱 아님, 이미 메모리에 있는 데이터 옮기기)
        InitializeCharacterTables();
        InitializeSkillTables();
        InitializeEnchantTables();

        _isInitialized = true;
        Debug.Log($"[CharacterRepo] 초기화 완료. " +
            $"CharacterMaster: {_characterMaster.Count}, CommonStatus: {_commonStatus.Count}, " +
            $"CharacterStatus: {_characterStatus.Count}, MonsterStatus: {_monsterStatus.Count}, " +
            $"SkillMaster: {_skillMaster.Count}, Skills: {_skills.Count}, Effects: {_effects.Count}, " +
            $"Enchants: {_enchantMaster.Count}, EnchantLevels: {_enchantLevels.Count}, EnchantWeights: {_enchantWeights.Count}");
    }

    // ---------- Section initialization ----------
    private void InitializeCharacterTables()
    {
        _characterMaster = BuildDictionary(_characterMasterTable, nameof(_characterMasterTable), r => r.Character_ID, true);
        _commonStatus = BuildDictionary(_commonStatusTable, nameof(_commonStatusTable), r => r.Character_ID, true);
        _characterStatus = BuildDictionary(_characterStatusTable, nameof(_characterStatusTable), r => r.Character_ID, true);
        _monsterStatus = BuildDictionary(_monsterStatusTable, nameof(_monsterStatusTable), r => r.Character_ID, true);
    }

    private void InitializeSkillTables()
    {
        _skillMaster = BuildDictionary(_skillMasterTable, nameof(_skillMasterTable), r => r.StandardID, false);
        _skills = BuildDictionary(_skillDataTable, nameof(_skillDataTable), r => r.SkillID, false);
        _effects = BuildDictionary(_effectTable, nameof(_effectTable), r => r.EffectID, false);
    }

    private void InitializeEnchantTables()
    {
        _enchantMaster = BuildDictionary(_enchantMasterTable, nameof(_enchantMasterTable), r => r.EnchantID, false);
        _enchantLevels = BuildEnchantLevelDictionary();
        _enchantWeights = BuildList(_enchantWeightTable, nameof(_enchantWeightTable), false);
    }

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
            Debug.LogWarning($"[CharacterRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[CharacterRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[CharacterRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }

    private Dictionary<string, Legacy_EnchantLevelData> BuildEnchantLevelDictionary()
    {
        var result = new Dictionary<string, Legacy_EnchantLevelData>();

        if (_enchantLevelTable == null)
        {
            LogMissingTable(nameof(_enchantLevelTable), false);
            return result;
        }

        if (_enchantLevelTable.rows == null)
        {
            Debug.LogWarning($"[CharacterRepo] {nameof(_enchantLevelTable)}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < _enchantLevelTable.rows.Count; i++)
        {
            Legacy_EnchantLevelData row = _enchantLevelTable.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[CharacterRepo] {nameof(_enchantLevelTable)}.rows[{i}] is null. Skip.");
                continue;
            }

            string key = $"{row.EnchantID}_{row.Level}";
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[CharacterRepo] {nameof(_enchantLevelTable)} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }

    private List<TData> BuildList<TData>(DataTable<TData> table, string tableName, bool isRequired)
        where TData : class
    {
        var result = new List<TData>();

        if (table == null)
        {
            LogMissingTable(tableName, isRequired);
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[CharacterRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[CharacterRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
    }

    private void LogMissingTable(string tableName, bool isRequired)
    {
        string message = $"[CharacterRepo] {tableName} is not assigned. Empty data will be used.";
        if (isRequired)
            Debug.LogError(message);
        else
            Debug.LogWarning(message);
    }

    // ---------- 조회 API ----------
    public CommonStatusData GetCommonStatus(int id) => GetData(_commonStatus, id, nameof(GetCommonStatus));
    public CharacterStatusData GetCharacterStatus(int id) => GetData(_characterStatus, id, nameof(GetCharacterStatus));
    public MonsterStatusData GetMonsterStatus(int id) => GetData(_monsterStatus, id, nameof(GetMonsterStatus));
    public Legacy_SkillMasterData GetSkillMaster(int standardId) => GetData(_skillMaster, standardId, nameof(GetSkillMaster));
    public Legacy_SkillData GetSkill(int skillId) => GetData(_skills, skillId, nameof(GetSkill));
    public Legacy_EffectData GetEffect(int effectId) => GetData(_effects, effectId, nameof(GetEffect));
    public Legacy_EnchantMasterData GetEnchantMaster(int enchantId) => GetData(_enchantMaster, enchantId, nameof(GetEnchantMaster));
    public Legacy_EnchantLevelData GetEnchantLevel(int enchantId, int level) => GetData(_enchantLevels, $"{enchantId}_{level}", nameof(GetEnchantLevel));

    // 전체 조회 (인챈트 선택 로직에서 필요)
    public IReadOnlyDictionary<int, Legacy_EnchantMasterData> GetAllEnchantMasters()
    {
        if (_enchantMaster == null)
        {
            Debug.LogWarning("[CharacterRepo] EnchantMaster cache is not initialized. Empty dictionary will be returned.");
            _enchantMaster = new Dictionary<int, Legacy_EnchantMasterData>();
        }

        return _enchantMaster;
    }

    public IReadOnlyList<Legacy_EnchantWeightData> GetEnchantWeights()
    {
        if (_enchantWeights == null)
        {
            Debug.LogWarning("[CharacterRepo] EnchantWeight cache is not initialized. Empty list will be returned.");
            _enchantWeights = new List<Legacy_EnchantWeightData>();
        }

        return _enchantWeights;
    }

    // 안전 조회 (키가 없을 수 있는 경우)
    public bool TryGetCommonStatus(int id, out CommonStatusData data)
    {
        data = null;
        return _commonStatus != null && _commonStatus.TryGetValue(id, out data);
    }

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
            Debug.LogWarning($"[CharacterRepo] {methodName} cache is not initialized. Key: {key}");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[CharacterRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}
