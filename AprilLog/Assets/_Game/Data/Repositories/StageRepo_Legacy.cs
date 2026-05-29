// 담당자 : 정승우
// 설명   : 챕터/스테이지/스폰 규칙 데이터 저장소

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 진행, 몬스터 풀, 스폰 규칙을 관리한다.
/// </summary>
public class Legacy_StageRepo : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private ChapterTable _chapterTable;
    [SerializeField] private StageDataTable _stageTable;
    [SerializeField] private MonsterPoolMasterTable _poolMasterTable;
    [SerializeField] private MonsterPoolTable _poolTable;
    [SerializeField] private StageSpawnRuleTable _spawnRuleTable;
    [SerializeField] private MonsterStageScalingTable _scalingTable;

    private Dictionary<int, ChapterData> _chapters;
    private Dictionary<int, StageData> _stages;
    private Dictionary<int, MonsterWavePoolData> _poolMasters;
    private Dictionary<int, List<MonsterPoolData>> _pools;
    private List<StageSpawnRuleData> _spawnRules;
    private List<MonsterStageScalingData> _scalingRules;
    private bool _isInitialized;

    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[StageRepo] Already initialized. Skip.");
            return;
        }

        _chapters = BuildDictionary(_chapterTable, nameof(_chapterTable), r => r.Chapter_ID);
        _stages = BuildDictionary(_stageTable, nameof(_stageTable), r => r.Stage_ID);
        _poolMasters = BuildDictionary(_poolMasterTable, nameof(_poolMasterTable), r => r.MonsterPool_ID);

        // MonsterPool은 풀ID 기준 그룹핑
        _pools = BuildPoolDictionary();
        _spawnRules = BuildList(_spawnRuleTable, nameof(_spawnRuleTable));
        _scalingRules = BuildList(_scalingTable, nameof(_scalingTable));
        _isInitialized = true;
        Debug.Log($"[StageRepo] 초기화 완료. Chapters: {_chapters.Count}, Stages: {_stages.Count}, PoolMasters: {_poolMasters.Count}, Pools: {_pools.Count}, SpawnRules: {_spawnRules.Count}, ScalingRules: {_scalingRules.Count}");
    }

    public ChapterData GetChapter(int id)
    {
        if (_chapters == null)
        {
            Debug.LogWarning("[StageRepo] Chapter cache is not initialized. Empty dictionary will be used.");
            _chapters = new Dictionary<int, ChapterData>();
        }

        if (_chapters.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[StageRepo] Chapter not found. Id: {id}");
        return null;
    }

    public StageData GetStage(int id)
    {
        if (_stages == null)
        {
            Debug.LogWarning("[StageRepo] Stage cache is not initialized. Empty dictionary will be used.");
            _stages = new Dictionary<int, StageData>();
        }

        if (_stages.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[StageRepo] Stage not found. Id: {id}");
        return null;
    }

    // 특정 스테이지에 적용되는 스폰 규칙 목록
    public List<StageSpawnRuleData> GetSpawnRulesForStage(int stageId)
    {
        if (_spawnRules == null)
        {
            Debug.LogWarning("[StageRepo] SpawnRules cache is not initialized. Empty list will be used.");
            _spawnRules = new List<StageSpawnRuleData>();
        }

        var result = new List<StageSpawnRuleData>();
        for (int i = 0; i < _spawnRules.Count; i++)
        {
            var rule = _spawnRules[i];
            if (stageId >= rule.StartStage_ID && stageId <= rule.EndStage_ID)
                result.Add(rule);
        }
        return result;
    }

    // 특정 풀에서 가중치 기반으로 몬스터 1마리 뽑기
    public int PickMonsterFromPool(int poolId, System.Random rng)
    {
        if (_pools == null)
        {
            Debug.LogWarning("[StageRepo] MonsterPool cache is not initialized. Empty dictionary will be used.");
            _pools = new Dictionary<int, List<MonsterPoolData>>();
        }

        if (!_pools.TryGetValue(poolId, out var pool) || pool.Count == 0)
        {
            Debug.LogWarning($"[StageRepo] Monster pool not found or empty. PoolId: {poolId}");
            return -1;
        }

        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += pool[i].Weight;

        if (totalWeight <= 0)
        {
            Debug.LogWarning($"[StageRepo] Monster pool total weight is zero. PoolId: {poolId}");
            return -1;
        }

        if (rng == null)
            rng = new System.Random();
        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += pool[i].Weight;
            if (roll < cumulative)
                return pool[i].Character_ID;
        }

        return pool[pool.Count - 1].Character_ID;
    }

    // 특정 스테이지에 적용되는 스케일링 규칙
    public MonsterStageScalingData GetScalingForStage(int stageId, int poolId)
    {
        if (_scalingRules == null)
        {
            Debug.LogWarning("[StageRepo] ScalingRules cache is not initialized. Empty list will be used.");
            _scalingRules = new List<MonsterStageScalingData>();
        }

        for (int i = 0; i < _scalingRules.Count; i++)
        {
            var rule = _scalingRules[i];
            if (stageId >= rule.StartStage_ID && stageId <= rule.EndStage_ID
                && rule.MonsterPool_ID == poolId)
                return rule;
        }
        return null;
    }

    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[StageRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }

    private Dictionary<int, List<MonsterPoolData>> BuildPoolDictionary()
    {
        var result = new Dictionary<int, List<MonsterPoolData>>();

        if (_poolTable == null)
        {
            Debug.LogWarning($"[StageRepo] {nameof(_poolTable)} is not assigned. Empty pool dictionary will be used.");
            return result;
        }

        if (_poolTable.rows == null)
        {
            Debug.LogWarning($"[StageRepo] {nameof(_poolTable)}.rows is null. Empty pool dictionary will be used.");
            return result;
        }

        for (int i = 0; i < _poolTable.rows.Count; i++)
        {
            MonsterPoolData row = _poolTable.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {nameof(_poolTable)}.rows[{i}] is null. Skip.");
                continue;
            }

            if (!result.TryGetValue(row.MonsterPool_ID, out var pool))
            {
                pool = new List<MonsterPoolData>();
                result.Add(row.MonsterPool_ID, pool);
            }

            pool.Add(row);
        }

        return result;
    }

    private List<TData> BuildList<TData>(DataTable<TData> table, string tableName)
        where TData : class
    {
        var result = new List<TData>();

        if (table == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName} is not assigned. Empty list will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
    }
}
