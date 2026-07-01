using System.Collections.Generic;
using UnityEngine;

public class RewardRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("보상 데이터")]
    [SerializeField] private ChangeRewardTable _changeRewardTable;
    [SerializeField] private BattleRewardTable _battleRewardTable;
    
    // ---------- Dictionary 캐시 ----------
    private List<ChangeRewardData> _changeRewards;
    private Dictionary<int, List<ChangeRewardData>> _changeRewardLookup;
    private List<BattleRewardData> _battleRewards;
    private Dictionary<int, List<BattleRewardData>> _battleRewardLookup;
    
    private bool _isInitialized = false;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (_isInitialized) return;

        _changeRewards = BuildList(_changeRewardTable, nameof(_changeRewardTable));
        
        
        BuildChangeRewardLookup(_changeRewards);
        BuildBattleRewardLookup(_battleRewards);

        _isInitialized = true;
        Debug.Log($"[RewardRepo] 초기화 완료. 변동 보상 매핑: {_changeRewardLookup.Count}개, 전투 보상 매핑: {_battleRewardLookup.Count}개 트리거");
    }
    
    // ---------- 조회 API ----------
    public List<ItemSaveEntry> GetCalculatedChangeRewards(int chapterId, int completedStageCount, bool isFirstClear = false)
    {
        var results = new List<ItemSaveEntry>();
        var targetId = (chapterId * 10) + completedStageCount;

        if (_changeRewardLookup == null || !_changeRewardLookup.TryGetValue(targetId, out var rewardsForThisId))
        {
            return results;
        }

        foreach (var data in rewardsForThisId)
        {
            bool requireFirstClear = (data.RewardRepeat == "FirstClear");
            if (requireFirstClear && !isFirstClear) continue; 

            int step = targetId - data.Start_ID; 
            int finalAmount = data.BaseAmount;

            if (data.GrowthType == "Add")
            {
                finalAmount += Mathf.FloorToInt(data.GrowthValue * step);
            }
            else if (data.GrowthType == "Rate")
            {
                finalAmount = Mathf.FloorToInt(data.BaseAmount * Mathf.Pow(1f + data.GrowthValue, step));
            }

            if (finalAmount > 0)
            {
                results.Add(new ItemSaveEntry { itemId = data.RewardType, amount = finalAmount });
            }
        }
        return results;
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
            Debug.LogWarning($"[ConfigRepo] {tableName} is not assigned. Empty list will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[ConfigRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[ConfigRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
    }
    
    private void BuildChangeRewardLookup(List<ChangeRewardData> changeRewards)
    {
        _changeRewardLookup = new Dictionary<int, List<ChangeRewardData>>();
        if (changeRewards == null) return;

        foreach (var data in changeRewards)
        {
            // Start_ID ~ End_ID 구간을 Unrolling 하여 O(1) 검색이 가능하도록 펼침
            for (int id = data.Start_ID; id <= data.End_ID; id++)
            {
                if (!_changeRewardLookup.ContainsKey(id))
                {
                    _changeRewardLookup[id] = new List<ChangeRewardData>();
                }
                _changeRewardLookup[id].Add(data);
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
            Debug.LogWarning($"[SpellRepo] {methodName} cache is not initialized. Sort Type : ID");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[SpellRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}