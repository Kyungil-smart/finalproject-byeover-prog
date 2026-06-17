
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ArtifactManager : MonoBehaviour
{
    public int UpgradeStone;
    public int LegendaryShard;
    public event Action OnInventoryUpdated;

    public List<ArtifactInstance> MyArtifacts = new List<ArtifactInstance>();

    public void Initialize()
    {
        Debug.Log("[ArtifactManager] 초기화 완료. 데이터를 로드할 준비가 되었습니다.");
    }

    public void AddArtifact(int masterId)
    {
        var existing = MyArtifacts.Find(a => a.MasterId == masterId);

        if (existing != null)
        {
            existing.CurrentCount++;
            Debug.Log($"[중복] 아티팩트 ID {masterId} 보유 수량: {existing.CurrentCount}");

            CheckAndHandleExcess(existing);
        }

        else
        {
            ArtifactInstance newArtifact = new ArtifactInstance();
            newArtifact.UniqueId = GenerateNewUniqueId();
            newArtifact.MasterId = masterId;
            newArtifact.CurrentLevel = 1;
            newArtifact.CurrentCount = 1;
            newArtifact.IsEquipped = false;

            MyArtifacts.Add(newArtifact);
        }

        OnInventoryUpdated?.Invoke();
    }

    private void CheckAndHandleExcess(ArtifactInstance item)
    {
        var master = item.MasterData;
        if (master == null) return;

        string grade = master.GearGrade;

        var dismantleData = DataManager.Instance.GearRepo.GetGearDismantleData(grade);
        if (dismantleData == null) return;

        int baseMax = DataManager.Instance.GearRepo.GetGearGrade(grade).MaxOwned;
        int currentLimit = CalculateDynamicLimit(item, baseMax);

        if (item.CurrentCount > currentLimit)
        {
            int excessCount = item.CurrentCount - currentLimit;
            item.CurrentCount = currentLimit;

            if (grade == "Legendary")
            {
                this.LegendaryShard += excessCount;
                Debug.Log($"[자동 분해] 레전더리 초과분 {excessCount}개 → 레전더리 조각 지급");
            }

            else
            {
                int reward = excessCount * dismantleData.RewardAmount;
                this.UpgradeStone += reward;
                Debug.Log($"[자동 분해] {grade} 초과분 {excessCount}개 → 에픽 강화석 지급");
            }
           
        }
    }

    private int CalculateDynamicLimit(ArtifactInstance item, int baseMax)
    {
        int limit = baseMax - 1;

        if (item.IsAscended)
        {
            limit -= 1;
        }

        return limit;
    }

    public void RequestUpgrade(int uniqueId)
    {
        var artifact = MyArtifacts.Find(a => a.UniqueId == uniqueId);

        if (artifact != null && artifact.CanLevelUp())
        {
            artifact.CurrentLevel++;
            Debug.Log($"[인벤토리] {artifact.UniqueId} 강화 성공! 레벨: {artifact.CurrentLevel}");
        }
    }

    private int GenerateNewUniqueId() { return Random.Range(1000, 9999); }

    public void AttemptAscension(int uniqueId, bool useShard = false)
    {
        var item = MyArtifacts.Find(a => a.UniqueId == uniqueId);
        if (item == null) return;

        var gradeData = DataManager.Instance.GearRepo.GetGearGrade(item.MasterData.GearGrade);
        if (gradeData == null) return;

        if (item.AscensionCount >= gradeData.MaxAscension) return;

        if (useShard && item.MasterData.GearGrade == "Legendary")
        {
            int shardCost = 3;

            if (this.LegendaryShard >= shardCost)
            {
                this.LegendaryShard -= 3;
                item.AscensionCount++;
            }
        }
        else if (item.CurrentCount > 1)
        {
            item.CurrentCount--;
            item.AscensionCount++;
        }
    }

    public void ManualDisassemble(int uniqueId, int count)
    {
        var item = MyArtifacts.Find(a => a.UniqueId == uniqueId);
        if (item == null || item.IsEquipped) return;

        int availableCount = item.CurrentCount - 1;
        int countToDisassemble = Mathf.Min(count, availableCount);

        if (countToDisassemble <= 0) return;

        item.CurrentCount -= countToDisassemble;

        GiveReward(item.MasterData.GearGrade, countToDisassemble);

        OnInventoryUpdated?.Invoke();
    }

    private void GiveReward(string grade, int amount)
    {
        var dismantleData = DataManager.Instance.GearRepo.GetGearDismantleData(grade);

        if (dismantleData != null)
        {
            int rewardAmount = amount * dismantleData.RewardAmount;

            if (grade == "Legendary")
            {
                this.LegendaryShard += rewardAmount;
            }
            else
            {
                this.UpgradeStone += rewardAmount;
            }
            Debug.Log($"[분해 완료] {grade} {amount}개 분해 → {rewardAmount}개 획득");
        }
    }

    public void LevelUpArtifact(ArtifactInstance data)
    {
        RequestUpgrade(data.UniqueId);

        OnInventoryUpdated?.Invoke();
    }

    public void AscendArtifact(ArtifactInstance data)
    {
        AttemptAscension(data.UniqueId, false);

        OnInventoryUpdated?.Invoke();
    }
}

