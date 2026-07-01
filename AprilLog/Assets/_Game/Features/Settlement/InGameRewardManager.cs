using System.Collections.Generic;
using UnityEngine;

public class InGameRewardManager : MonoBehaviour
{
    private readonly Dictionary<int, int> _accumulatedRewards = new (); // itemId, amount

    // 몬스터 AI나 웨이브 시스템이 호출
    public void AddBattleReward(int targetId)
    {
        var rewards = DataManager.Instance.RewardRepo.GetBattleRewards(targetId);
        foreach (var item in rewards)
        {
            _accumulatedRewards.TryAdd(item.itemId, 0);
            _accumulatedRewards[item.itemId] += item.amount;
        }
    }

    // 정산 시점에 바구니를 통째로 반환
    public Dictionary<int, int> GetAndClearAccumulatedRewards()
    {
        if (_accumulatedRewards == null || _accumulatedRewards.Count == 0)
        {
            return new Dictionary<int, int>();
        }
        
        var result = _accumulatedRewards;
        _accumulatedRewards.Clear();
        return result;
    }
}

