using System.Collections.Generic;
using UnityEngine;

public static class RewardCalculator
{
    private const string GROWTH_TYPE_ADD = "Add";
    private const string GROWTH_TYPE_RATE = "Rate";

    private static RewardRepo _rewardRepo;
    private static StageRepo _stageRepo;

    public static Dictionary<int, List<ItemSaveEntry>> GetAmountFirstChapter(List<RewardRecipe> rewardList)
    {
        var result = new Dictionary<int, List<ItemSaveEntry>>();
        _rewardRepo ??= DataManager.Instance.RewardRepo;
        _stageRepo ??= DataManager.Instance.StageRepo;

        if (rewardList == null || rewardList.Count == 0)
        {
            Debug.LogWarning("Reward List is empty");
            return result;
        }

        foreach (var list in rewardList)
        {
            var data = _rewardRepo.GetChangeRewardRule(list.RewardId);
            int rewardType = data.RewardType;
            int amount = data.BaseAmount;

            for (int i = 0; i < list.currentStep; i++)
            {
                int temp = _stageRepo.GetChapterIdByStep(list.TargetId, i);
                if(!result.TryGetValue(temp, out List<ItemSaveEntry> items))
                {
                    items = new List<ItemSaveEntry>();
                    result[temp] = items;
                }
                
                items.Add(new ItemSaveEntry{ itemId = rewardType, amount = amount });
            }
        }
        
        return result;
    }
    
    public static Dictionary<int, List<ItemSaveEntry>> GetAmountFirstStage(List<RewardRecipe> rewardList)
    {
        var result = new Dictionary<int, List<ItemSaveEntry>>();
        _rewardRepo ??= DataManager.Instance.RewardRepo;
        _stageRepo ??= DataManager.Instance.StageRepo;
        
        if (rewardList == null || rewardList.Count == 0)
        {
            Debug.LogWarning("Reward List is empty");
            return result;
        }

        foreach (var list in rewardList)
        {
            var data = _rewardRepo.GetChangeRewardRule(list.RewardId);
            int rewardType = data.RewardType;
            int amount = data.BaseAmount;

            for (int i = 0; i < list.currentStep; i++)
            {
                int temp = _stageRepo.GetStageIdByStep(list.TargetId, i);
                if(!result.TryGetValue(temp, out List<ItemSaveEntry> items))
                {
                    items = new List<ItemSaveEntry>();
                    result[temp] = items;
                }
                
                items.Add(new ItemSaveEntry{ itemId = rewardType, amount = amount });
            }
        }
        
        return result;
    }
    
    public static List<ItemSaveEntry> GetAmountRepeatStage(List<RewardRecipe> rewardList)
    {
        var result = new List<ItemSaveEntry>();
        _rewardRepo ??= DataManager.Instance.RewardRepo;
        
        if (rewardList == null || rewardList.Count == 0)
        {
            Debug.LogWarning("Reward List is empty");
            return result;
        }

        foreach (var list in rewardList)
        {
            var data = _rewardRepo.GetChangeRewardRule(list.RewardId);
            int rewardType = data.RewardType;
            int amount = data.BaseAmount;

            if (data.GrowthType == GROWTH_TYPE_ADD)
            {
                amount += Mathf.FloorToInt(data.GrowthValue * list.currentStep);
            }
            else if (data.GrowthType == GROWTH_TYPE_RATE)
            {
                amount = Mathf.FloorToInt(amount * (1 + data.GrowthValue * list.currentStep));
            }

            result.Add(new ItemSaveEntry{ itemId = rewardType, amount = amount });
        }

        return result;
    }
}
