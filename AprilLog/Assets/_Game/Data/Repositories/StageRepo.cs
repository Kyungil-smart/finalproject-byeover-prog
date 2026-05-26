// 담당자 : 정승우
// 설명   : 챕터/스테이지/스폰 규칙 데이터 저장소

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스테이지 진행, 몬스터 풀, 스폰 규칙을 관리한다.
/// </summary>
public class StageRepo : MonoBehaviour
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
    private Dictionary<int, MonsterPoolMasterData> _poolMasters;
    private Dictionary<int, List<MonsterPoolData>> _pools;
    private List<StageSpawnRuleData> _spawnRules;
    private List<MonsterStageScalingData> _scalingRules;

    public void Initialize()
    {
        _chapters = _chapterTable.rows.ToDictionary(r => r.Chapter_ID);
        _stages = _stageTable.rows.ToDictionary(r => r.Stage_ID);
        _poolMasters = _poolMasterTable.rows.ToDictionary(r => r.MonsterPool_ID);

        // MonsterPool은 풀ID 기준 그룹핑
        _pools = new Dictionary<int, List<MonsterPoolData>>();
        for (int i = 0; i < _poolTable.rows.Count; i++)
        {
            var row = _poolTable.rows[i];
            if (!_pools.ContainsKey(row.MonsterPool_ID))
                _pools[row.MonsterPool_ID] = new List<MonsterPoolData>();
            _pools[row.MonsterPool_ID].Add(row);
        }

        _spawnRules = _spawnRuleTable.rows;
        _scalingRules = _scalingTable.rows;

        Debug.Log($"[StageRepo] 초기화 완료. 챕터 {_chapters.Count}, 스테이지 {_stages.Count}, 풀 {_poolMasters.Count}");
    }

    public ChapterData GetChapter(int id) => _chapters.TryGetValue(id, out var d) ? d : null;
    public StageData GetStage(int id) => _stages.TryGetValue(id, out var d) ? d : null;

    // 특정 스테이지에 적용되는 스폰 규칙 목록
    public List<StageSpawnRuleData> GetSpawnRulesForStage(int stageId)
    {
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
        if (!_pools.TryGetValue(poolId, out var pool) || pool.Count == 0)
            return -1;

        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += pool[i].Weight;

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
        for (int i = 0; i < _scalingRules.Count; i++)
        {
            var rule = _scalingRules[i];
            if (stageId >= rule.StartStage_ID && stageId <= rule.EndStage_ID
                && rule.MonsterPool_ID == poolId)
                return rule;
        }
        return null;
    }
}