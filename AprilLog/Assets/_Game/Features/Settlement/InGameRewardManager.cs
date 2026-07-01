using System.Collections.Generic;
using UnityEngine;

public class InGameRewardManager : MonoBehaviour
{
    private Dictionary<int, int> _accumulatedRewards = new (); // itemId, amount
    private RewardRepo _repo;

    // 몬스터 AI나 웨이브 시스템이 호출
    public void AddBattleReward(int targetId)
    {
        if (_repo == null)
            _repo = DataManager.Instance.RewardRepo;
        
        _accumulatedRewards ??= new Dictionary<int, int>();
        
        var rewards = _repo.GetBattleRewards(targetId);
        
        foreach (var item in rewards)
        {
            _accumulatedRewards.TryAdd(item.itemId, 0);
            _accumulatedRewards[item.itemId] += item.amount;
        }
    }

    // 정산 시점에 바구니를 통째로 반환
    public Dictionary<int, int> GetAndClearAccumulatedRewards()
    {
        _accumulatedRewards ??= new Dictionary<int, int>();
        
        var result = _accumulatedRewards;
        _accumulatedRewards.Clear();
        return result;
    }

    public void LoadRewardData(List<ItemSaveEntry> savedData)
    {
        foreach (var data in savedData)
        {
            _accumulatedRewards.TryAdd(data.itemId, 0);
            _accumulatedRewards[data.itemId] += data.amount;
        }
    }

    public List<ItemSaveEntry> ExportRewardData()
    {
        var list = new List<ItemSaveEntry>();
        
        foreach (var data in _accumulatedRewards)
        {
            list.Add(new ItemSaveEntry { itemId = data.Key, amount = data.Value });
        }
        
        return list;
    }
}

