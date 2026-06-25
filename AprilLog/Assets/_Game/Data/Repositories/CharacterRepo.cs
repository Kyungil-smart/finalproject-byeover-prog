// 담당자 : 정승우
// 설명   : 캐릭터/스킬/인챈트 데이터 저장소

// 수정자 : 김영찬
// 수정 내용 : 스테이터스와 스킬&인첸트 부분 분리

// 수정자 : 김영찬
// 수정 내용 : 검색 항목에 Unit 추가

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 관련 SO를 Inspector에서 받아서
/// Dictionary로 캐싱한다. 런타임에서는 Dictionary 조회만.
/// Initialize()에서만 LINQ 씀 (한 번이라 GC 괜찮음).
/// </summary>
public class CharacterRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("플레이어 기본 데이터")]
    [SerializeField] private CharacterMasterTable _characterMasterTable;
    [SerializeField] private CharacterStatusTable _characterStatusTable;
    
    [Header("퍼즐 블록 데이터")]
    [SerializeField] private UnitMasterTable _unitMasterTable;
    
    [Header("몬스터 기본 데이터")]
    [SerializeField] private MonsterStatusTable _monsterStatusTable;
    
    [Header("공용 기본 데이터")]
    [SerializeField] private CommonStatusTable _commonStatusTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, CharacterMasterData> _characterMaster;
    private Dictionary<int, CommonStatusData> _commonStatus;
    private Dictionary<int, CharacterStatusData> _characterStatus;
    private Dictionary<int, MonsterStatusData> _monsterStatus;
    private Dictionary<int, UnitTableData> _unitData;
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

        _isInitialized = true;
        Debug.Log($"[CharacterRepo] 초기화 완료. " +
                  $"CharacterMaster: {_characterMaster.Count}, CommonStatus: {_commonStatus.Count}, " +
                  $"CharacterStatus: {_characterStatus.Count}, MonsterStatus: {_monsterStatus.Count}");
    }

    // ---------- Section initialization ----------
    private void InitializeCharacterTables()
    {
        _characterMaster = BuildDictionary(_characterMasterTable, nameof(_characterMasterTable), r => r.Character_ID, true);
        _commonStatus = BuildDictionary(_commonStatusTable, nameof(_commonStatusTable), r => r.Character_ID, true);
        _characterStatus = BuildDictionary(_characterStatusTable, nameof(_characterStatusTable), r => r.Character_ID, true);
        _monsterStatus = BuildDictionary(_monsterStatusTable, nameof(_monsterStatusTable), r => r.Character_ID, true);
        _unitData = BuildDictionary(_unitMasterTable, nameof(_unitMasterTable), r => r.UnitID, true);
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
    public UnitTableData GetUnitData(int id) => GetData(_unitData, id, nameof(GetUnitData));

    // 안전 조회 (키가 없을 수 있는 경우)
    public bool TryGetCommonStatus(int id, out CommonStatusData data)
    {
        data = null;
        return _commonStatus != null && _commonStatus.TryGetValue(id, out data);
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
