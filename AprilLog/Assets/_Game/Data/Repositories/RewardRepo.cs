using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public class RewardRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("보상 데이터")]
    [SerializeField] private ChangeRewardTable _changeRewardTable;
    [SerializeField] private BattleRewardTable _battleRewardTable;
    
    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, ChangeRewardData> _changeRewardDict; // ChangeReward_ID
    private List<RangeData> _changeRewardFirst;
    private List<RangeData> _changeRewardRepeat;
    private List<BattleRewardData> _battleRewardList;
    private Dictionary<int, List<BattleRewardData>> _battleRewardLookup;
    private Dictionary<int, int> _chapterStepMapping;
    private Dictionary<int, int> _stageStepMapping;
    private Dictionary<int, int> _stepToChapterIdMapping;
    private Dictionary<int, int> _stepToStageIdMapping;
    
    private bool _isInitialized = false;
    private bool _isStageStepMappingSetted = false;
    
    // ---------- Const ----------
    private const string FIRST_CLEAR = "FirstClear";
    private const string REPEAT_CLEAR = "RepeatClear";

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (_isInitialized) return;
        
        _changeRewardDict = BuildDictionary(_changeRewardTable, nameof(_changeRewardTable), r => r.ChangeReward_ID);
        _battleRewardList = BuildList(_battleRewardTable, nameof(_battleRewardTable));
        
        BuildChangeRewardLookup(_changeRewardDict);
        BuildBattleRewardLookup(_battleRewardList);

        _isInitialized = true;
        Debug.Log($"[RewardRepo] 초기화 완료. 변동 보상 매핑: {_changeRewardDict.Count}개, 전투 보상 매핑: {_battleRewardLookup.Count}개 트리거");
    }
    
    public void BuildStepMapping(List<int> validChapterIds ,List<int> validStageIds)
    {
        _chapterStepMapping = new Dictionary<int, int>();
        _stageStepMapping = new Dictionary<int, int>();
        _stepToChapterIdMapping = new Dictionary<int, int>();
        _stepToStageIdMapping = new Dictionary<int, int>();
        
        for (int i = 0; i < validChapterIds.Count; i++)
        {
            _chapterStepMapping[validChapterIds[i]] = i; 
            _stepToChapterIdMapping[i] = validChapterIds[i];
        }
        
        for (int i = 0; i < validStageIds.Count; i++)
        {
            _stageStepMapping[validStageIds[i]] = i; 
            _stepToStageIdMapping[i] = validStageIds[i];
        }
        
        _isStageStepMappingSetted = true;
    }
    
    // ---------- 조회 API ----------
    public ChangeRewardData GetChangeRewardRule(int ruleId) => GetData(_changeRewardDict, ruleId, nameof(GetChangeRewardRule));
    public int GetChapterIdByStep(int targetChapterId, int step)
    {
        if (!_chapterStepMapping.TryGetValue(targetChapterId, out int index))
        {
            Debug.LogError($"[RewardRepo] Chapter ID {targetChapterId}에 해당하는 ID를 찾을 수 없습니다.");
            return -1;
        }
        
        int temp = index - step;
        
        if (_stepToChapterIdMapping.TryGetValue(temp, out int id))
        {
            return id;
        }
    
        Debug.LogError($"[RewardRepo] Step {step}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
    }
    
    public int GetStageIdByStep(int targetStageId, int step)
    {
        if (!_stageStepMapping.TryGetValue(targetStageId, out int index))
        {
            Debug.LogError($"[RewardRepo] Stage ID {targetStageId}에 해당하는 ID를 찾을 수 없습니다.");
            return -1;
        }
        
        int temp = index - step;
        
        if (_stepToStageIdMapping.TryGetValue(temp, out int id))
        {
            return id;
        }
    
        Debug.LogError($"[RewardRepo] Step {step}에 해당하는 ID를 찾을 수 없습니다.");
        return -1;
    }

    public void GetFirstStageRewards(int stageId, out List<RewardRecipe> rewardList)
    {
        rewardList = new ();
        
        if(_changeRewardFirst == null || _changeRewardFirst.Count == 0 || !_isStageStepMappingSetted) return;

        foreach (var data in _changeRewardFirst)
        {
            if (stageId >= data.StartId && stageId <= data.EndId)
            {
                int step = _stageStepMapping[stageId] - _stageStepMapping[data.StartId];
                rewardList.Add(new RewardRecipe {TargetId = stageId, RewardId = data.DataId ,currentStep = step});
            }
        }
    }
    
    public void GetFirstChapterRewards(int chapterId, out List<RewardRecipe> rewardList)
    {
        rewardList = new ();
        
        if(_changeRewardFirst == null || _changeRewardFirst.Count == 0 || !_isStageStepMappingSetted) return;

        foreach (var data in _changeRewardFirst)
        {
            if (chapterId >= data.StartId && chapterId <= data.EndId)
            {
                int step = _chapterStepMapping[chapterId] - _chapterStepMapping[data.StartId];
                rewardList.Add(new RewardRecipe {TargetId = chapterId, RewardId = data.DataId ,currentStep = step,});
            }
        }
    }

    public void GetRepeatStageRewards(int stageId, out List<RewardRecipe> rewardList)
    {
        rewardList = new ();
        
        if(_changeRewardRepeat == null || _changeRewardRepeat.Count == 0 || !_isStageStepMappingSetted) return;
        foreach (var data in _changeRewardRepeat)
        {
            if (stageId >= data.StartId && stageId <= data.EndId)
            {
                int step = _stageStepMapping[stageId] - _chapterStepMapping[data.StartId];
                rewardList.Add(new RewardRecipe {TargetId = stageId, RewardId = data.DataId ,currentStep = step});
            }
        }
    }
    
    public List<ItemSaveEntry> GetBattleRewards(int targetId)
    {
        var results = new List<ItemSaveEntry>();

        if (_battleRewardLookup == null || !_battleRewardLookup.TryGetValue(targetId, out var rewards))
        {
            return results;
        }

        foreach (var data in rewards)
        {
            if (data.FirstRewardAmount > 0)
            {
                results.Add(new ItemSaveEntry { itemId = data.FirstRewardItem, amount = data.FirstRewardAmount });
            }
            
            if (data.SecondRewardAmount > 0)
            {
                results.Add(new ItemSaveEntry { itemId = data.SecondRewardItem, amount = data.SecondRewardAmount });
            }
        }
        return results;
    }

    public List<string> GetBattleRewardTrigger(int targetId)
    {
        var list = GetData(_battleRewardLookup, targetId, nameof(_battleRewardLookup));
        var results = new List<string>();

        if (list.Count > 0)
        {
            foreach (var data in list)
            {
                results.Add(data.RewardTrigger);
            }
        }
        
        return results;
    }
    
    // ---------- Build Dictionary ----------
    private List<TData> BuildList<TData>(DataTable<TData> table, string tableName)
        where TData : class
    {
        var result = new List<TData>();

        if (table == null)
        {
            Debug.LogWarning($"[RewardRepo] {tableName} is not assigned. Empty list will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[RewardRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[RewardRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
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
            Debug.LogWarning($"[RewardRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[RewardRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[RewardRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[RewardRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }
    
    private void BuildChangeRewardLookup(Dictionary<int, ChangeRewardData> changeRewards)
    {
        _changeRewardFirst = new ();
        _changeRewardRepeat = new ();
        if (changeRewards == null) return;

        foreach (var data in changeRewards.Values)
        {
            switch (data.RewardRepeat)
            {
                case FIRST_CLEAR:
                    _changeRewardFirst.Add( new RangeData
                    {
                        StartId = data.Start_ID, EndId = data.End_ID, DataId = data.ChangeReward_ID
                    });
                    break;
                case REPEAT_CLEAR:
                    _changeRewardRepeat.Add( new RangeData
                    {
                        StartId = data.Start_ID, EndId = data.End_ID, DataId = data.ChangeReward_ID
                    });
                    break;
            }
        }
    }
    
    private void BuildBattleRewardLookup(List<BattleRewardData> battleRewards)
    {
        _battleRewardLookup = new Dictionary<int, List<BattleRewardData>>();
        if (battleRewards == null) return;

        foreach (var data in battleRewards)
        {
            // 하나의 Trigger_ID에 여러 보상(골드, 다이아 등)이 겹쳐 있을 수 있으므로 리스트로 그룹화
            if (!_battleRewardLookup.ContainsKey(data.Target_ID))
            {
                _battleRewardLookup[data.Target_ID] = new List<BattleRewardData>();
            }
            _battleRewardLookup[data.Target_ID].Add(data);
        }
    }
    
    private TData GetData<TKey, TData>(Dictionary<TKey, TData> dictionary, TKey key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[RewardRepo] {methodName} cache is not initialized. Sort Type : ID");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[RewardRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}