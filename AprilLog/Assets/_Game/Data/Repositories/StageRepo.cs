// 담당자 : 정승우
// 설명   : 챕터/스테이지/스폰 규칙 데이터 저장소

// 수정자 : 김영찬
// 최신 DB에 맞춰 테이블 갱신
// 최종 수정일 : 26.06.02

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
    
    [Header("스폰 규칙 데이터")]
    [SerializeField] private StageWaveRuleTable _waveRuleTable;
    [SerializeField] private SpecialWaveRuleTable _specialRuleTable;
    
    [Header("스폰 보정 데이터")]
    [SerializeField] private MonsterStageScalingTable _scalingTable;

    private Dictionary<int, ChapterData> _chapters;
    private Dictionary<int, StageData> _stages;
    // (Chapter_ID, StageOrder) -> StageData. Stage_ID 체계가 불규칙(챕터1~5=1000~1049, 챕터6~10=1100~1149)이라
    // 산술(chapterId*100+order)로 못 구한다 → 데이터에서 역조회한다. -> ToDo : 다시 ID 체계 맞춰서 해결함. 코드 수정 필요
    private Dictionary<(int, int), StageData> _stageByChapterOrder;
    private Dictionary<int, MonsterWavePoolData> _poolMasters;
    private Dictionary<int, List<MonsterPoolData>> _pools;
    private Dictionary<int ,List<StageWaveRuleData>> _waveRules;
    private Dictionary<int, SpecialWaveRuleData> _specialRules;
    private List<MonsterStageScalingData> _scalingRules;
    
    // 챕터/스테이지의 연결 순서를 인덱스로 저장하는 Mapping Dict -> 변동보상 및 로비의 스테이지 선택 등에서 사용
    private Dictionary<int, int> _chapterStepMapping;
    private Dictionary<int, int> _stageStepMapping;
    private Dictionary<int, int> _stepIndexToChapterIdMapping;
    private Dictionary<int, int> _stepIndexToStageIdMapping;
    
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
        _stageByChapterOrder = BuildStageByChapterOrder();
        _poolMasters = BuildDictionary(_poolMasterTable, nameof(_poolMasterTable), r => r.MonsterPool_ID);

        // MonsterPool과 WaveRules는 ID 기준 그룹핑
        _pools = BuildPoolDictionary();
        _waveRules = BuildWaveDictionary();
        
        _specialRules = BuildDictionary(_specialRuleTable, nameof(_specialRuleTable), r => r.SpecialWave_ID);
        _scalingRules = BuildList(_scalingTable, nameof(_scalingTable));

        BuildStepMapping(GetValidChapterList(), GetValidStageList());
        _isInitialized = true;
        Debug.Log($"[StageRepo] 초기화 완료. Chapters: {_chapters.Count}, Stages: {_stages.Count}, PoolMasters: {_poolMasters.Count}, Pools: {_pools.Count}, SpawnRules: {_waveRules.Count}, ScalingRules: {_scalingRules.Count}");
    }

    public void ExportStepMapping(out Dictionary<int, int> chapterStepMapping, out Dictionary<int, int> stageStepMapping)
    {
        chapterStepMapping = _chapterStepMapping;
        stageStepMapping = _stageStepMapping;
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

    /// <summary>챕터 + 스테이지 순서(StageOrder, 1-base)로 데이터의 Stage_ID를 조회한다.
    /// Stage_ID가 불규칙 체계라 산술 계산 대신 역조회한다. 없으면 -1.</summary>
    public int GetStageId(int chapterId, int stageOrder)
    {
        if (_stageByChapterOrder == null)
        {
            Debug.LogWarning("[StageRepo] StageByChapterOrder cache is not initialized.");
            return -1;
        }

        if (_stageByChapterOrder.TryGetValue((chapterId, stageOrder), out var data))
            return data.Stage_ID;

        Debug.LogWarning($"[StageRepo] Stage not found for Chapter {chapterId}, Order {stageOrder}.");
        return -1;
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
    
    public int GetMonsterPoolId(int wavePoolId, string type)
    {
        if (_poolMasters == null) return -1;

        int fallbackPoolId = -1;
        foreach (var data in _poolMasters.Values)
        {
            if (data.MonsterWavePool_ID != wavePoolId) continue;

            // 타입까지 일치하면 즉시 반환 (정상 경로)
            if (data.WavePoolType == type)
                return data.MonsterPool_ID;

            // 같은 웨이브풀의 첫 풀을 폴백 후보로 기억
            // (데이터의 MonsterPoolType가 비어있거나 불일치할 때 대비 — 매 프레임 스팸/스폰 실패 방지)
            if (fallbackPoolId < 0)
                fallbackPoolId = data.MonsterPool_ID;
        }

        if (fallbackPoolId >= 0)
            return fallbackPoolId; // 타입 매칭 실패 → 같은 웨이브풀 첫 풀로 폴백

        // 웨이브풀 자체가 없을 때만 경고 (이건 진짜 데이터 누락)
        Debug.LogWarning($"[StageRepo] WavePool_ID {wavePoolId}에 연결된 MonsterPool이 없습니다.");
        return -1;
    }
    
    // 여러 챕터에 걸친 보상을 계산하기 위해 챕터번호/스테이지 번호만 따로 리스트업해서 외부(RewardRepo)로 전송함
    private List<int> GetValidChapterList()
    {
        var list = new List<int>();
        HashSet<int> registered = new HashSet<int> { 9901, 9801 }; // 튜토리얼은 미리 제외하고 시작

        foreach (var data in _chapterTable.rows)
        {
            if(registered.Contains(data.Chapter_ID)) continue;
            list.Add(data.Chapter_ID);
            registered.Add(data.Chapter_ID);
        }
        
        return list;
    }

    private List<int> GetValidStageList()
    {
        var list = new List<int>();
        HashSet<int> registered = new HashSet<int>{ 990101, 980101, 980102 }; // 튜토리얼은 미리 제외하고 시작
        
        foreach (var data in _stageTable.rows)
        {
            if(registered.Contains(data.Stage_ID)) continue;
            list.Add(data.Stage_ID);
            registered.Add(data.Stage_ID);
        }
        
        return list;
    }
    
    public int GetChapterIdByStep(int targetChapterId, int step)
    {
        int index = GetIndexByChapterId(targetChapterId);
        if(index == -1) return -1;
        
        int temp = index - step;
        int id = GetChapterIdByIndex(temp);
        
        return id;
    }
    
    public int GetStageIdByStep(int targetStageId, int step)
    {
        int index = GetIndexByStageId(targetStageId);
        if(index == -1) return -1;

        int temp = index - step;
        int id = GetStageIdByIndex(temp);
        
        return id;
    }

    public int GetIndexByChapterId(int chapterId)
    {
        if(_chapterStepMapping.TryGetValue(chapterId, out int index))
        {
            return index;
        }

        Debug.LogError($"[StageRepo] Chapter ID {chapterId}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
    }

    public int GetIndexByStageId(int stageId)
    {
        if(_stageStepMapping.TryGetValue(stageId, out int index))
        {
            return index;
        }
        
        Debug.LogError($"[StageRepo] Stage ID {stageId}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
    }

    public int GetChapterIdByIndex(int index)
    {
        if (_stepIndexToChapterIdMapping.TryGetValue(index, out int id))
        {
            return id;
        }
    
        Debug.LogError($"[StageRepo] Index {index}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
    }

    public int GetStageIdByIndex(int index)
    {
        if (_stepIndexToStageIdMapping.TryGetValue(index, out int id))
        {
            return id;
        }
    
        Debug.LogError($"[StageRepo] Index {index}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
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

    public Dictionary<int, int> GetStepIndexToChapterIdMappingData()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[StageRepo] {nameof(StageRepo)} is not initialized. Not initialized.");
            return new Dictionary<int, int>();
        }
        
        return _stepIndexToChapterIdMapping;
    }

    // (Chapter_ID, StageOrder) -> StageData 역조회 맵 구성. Stage_ID가 불규칙이라 런타임은 챕터/순서로 찾는다.
    private Dictionary<(int, int), StageData> BuildStageByChapterOrder()
    {
        var result = new Dictionary<(int, int), StageData>();
        if (_stages == null) return result;

        foreach (var stage in _stages.Values)
        {
            var key = (stage.Chapter_ID, stage.StageOrder);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[StageRepo] 중복 (Chapter {stage.Chapter_ID}, Order {stage.StageOrder}). Stage_ID {stage.Stage_ID} 스킵, 먼저 들어온 것 유지.");
                continue;
            }
            result.Add(key, stage);
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

        if (_waveRuleTable == null)
        {
            Debug.LogWarning($"[StageRepo] {nameof(_waveRuleTable)} is not assigned. Empty pool dictionary will be used.");
            return result;
        }

        if (_waveRuleTable.rows == null)
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
    
    private void BuildStepMapping(List<int> validChapterIds ,List<int> validStageIds)
    {
        _chapterStepMapping = new Dictionary<int, int>();
        _stageStepMapping = new Dictionary<int, int>();
        _stepIndexToChapterIdMapping = new Dictionary<int, int>();
        _stepIndexToStageIdMapping = new Dictionary<int, int>();
        
        for (int i = 0; i < validChapterIds.Count; i++)
        {
            _chapterStepMapping[validChapterIds[i]] = i; 
            _stepIndexToChapterIdMapping[i] = validChapterIds[i];
        }
        
        for (int i = 0; i < validStageIds.Count; i++)
        {
            _stageStepMapping[validStageIds[i]] = i; 
            _stepIndexToStageIdMapping[i] = validStageIds[i];
        }
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
