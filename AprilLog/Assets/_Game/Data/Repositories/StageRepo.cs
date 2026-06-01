// 담당자 : 정승우
// 설명   : 챕터/스테이지/스폰 규칙 데이터 저장소

// 수정자 : 김영찬
// 최신 DB에 맞춰 테이블 갱신
// 최종 수정일 : 26.05.29

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 스테이지 진행, 몬스터 풀, 스폰 규칙을 관리한다.
/// </summary>
public class StageRepo : MonoBehaviour
{
    [Header("스테이지 진행 데이터")]
    [SerializeField] private ChapterTable _chapterTable;
    [SerializeField] private StageDataTable _stageTable;
    
    [Header("몬스터 풀 데이터")]
    [SerializeField] private MonsterPoolMasterTable _poolMasterTable;
    [SerializeField] private MonsterPoolTable _poolTable;
    
    [FormerlySerializedAs("_spawnRuleTable")]
    [Header("스폰 규칙 데이터")]
    [SerializeField] private StageWaveRuleTable _waveRuleTable;
    [SerializeField] private SpecialWaveRuleTable _specialRuleTable;
    
    [Header("스폰 보정 데이터")]
    [SerializeField] private MonsterStageScalingTable _scalingTable;

    private Dictionary<int, ChapterData> _chapters;
    private Dictionary<int, StageData> _stages;
    private Dictionary<int, MonsterWavePoolData> _poolMasters;
    private Dictionary<int, List<MonsterPoolData>> _pools;
    private Dictionary<int ,List<StageWaveRuleData>> _waveRules;
    private Dictionary<int, SpecialWaveRuleData> _specialRules;
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

        // MonsterPool과 WaveRules는 ID 기준 그룹핑
        _pools = BuildPoolDictionary();
        _waveRules = BuildWaveDictionary();
        
        _specialRules = BuildDictionary(_specialRuleTable, nameof(_specialRuleTable), r => r.SpecialWave_ID);
        _scalingRules = BuildList(_scalingTable, nameof(_scalingTable));
        _isInitialized = true;
        Debug.Log($"[StageRepo] 초기화 완료. Chapters: {_chapters.Count}, Stages: {_stages.Count}, PoolMasters: {_poolMasters.Count}, Pools: {_pools.Count}, SpawnRules: {_waveRules.Count}, ScalingRules: {_scalingRules.Count}");
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
    public Dictionary<int, StageWaveRuleData> GetSpawnRulesForStage(int stageId)
    {
        if (_waveRules == null)
        {
            Debug.LogWarning("[StageRepo] SpawnRules cache is not initialized. Empty list will be used.");
            _waveRules = new();
        }

        var result = new Dictionary<int, StageWaveRuleData>();

        if (!_waveRules.TryGetValue(stageId, out var data))
        {
            Debug.LogWarning($"[StageRepo] Stage Rule not found or empty. StageId: {stageId}");
            return null;
        }

        foreach (var d in data)
        {
            result.Add(d.WaveOrder, d);
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

    // 특정 스테이지에 적용되는 특별 소환 규칙
    public SpecialWaveRuleData GetSpecialWaveRuleForStage(int id)
    {
        if (id == 0)
        {
            Debug.Log("[StageRepo] This Wave Not Contain Special Wave.");
            return null;
        }
        
        if (_specialRules == null)
        {
            Debug.LogWarning("[StageRepo] Special Wave Rule cache is not initialized. Empty dictionary will be used.");
            _specialRules = new ();
        }

        if (_specialRules.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[StageRepo] Special Wave Rule not found. Id: {id}");
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
    
    private Dictionary<int, List<StageWaveRuleData>> BuildWaveDictionary()
    {
        var result = new Dictionary<int, List<StageWaveRuleData>>();

        if (_poolTable == null)
        {
            Debug.LogWarning($"[StageRepo] {nameof(_waveRuleTable)} is not assigned. Empty pool dictionary will be used.");
            return result;
        }

        if (_poolTable.rows == null)
        {
            Debug.LogWarning($"[StageRepo] {nameof(_waveRuleTable)}.rows is null. Empty pool dictionary will be used.");
            return result;
        }

        for (int i = 0; i < _waveRuleTable.rows.Count; i++)
        {
            StageWaveRuleData row = _waveRuleTable.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {nameof(_waveRuleTable)}.rows[{i}] is null. Skip.");
                continue;
            }

            if (!result.TryGetValue(row.Stage_ID, out var pool))
            {
                pool = new List<StageWaveRuleData>();
                result.Add(row.Stage_ID, pool);
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
