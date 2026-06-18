
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
        if (artifact == null) return;

        int upgradeStoneId = 70001;
        int cost = DataManager.Instance.GearRepo.GetGearUpgradeCost(artifact.MasterId, artifact.CurrentLevel, upgradeStoneId);
        Debug.Log($"[디버그] ID:{artifact.MasterId} 레벨:{artifact.CurrentLevel} 비용:{cost}");

        if (cost <= 0)
        {
            Debug.LogWarning($"강화 비용이 0입니다! 데이터 테이블(GearUpgradeCostTable)을 확인하세요.");
            return;
        }

        if (artifact.CanLevelUp() && this.UpgradeStone >= cost)
        {
            this.UpgradeStone -= cost;
            artifact.CurrentLevel++;
            Debug.Log($"[중요] 매니저 내부 레벨업 결과: ID {artifact.MasterId}, 레벨 {artifact.CurrentLevel}");
            OnInventoryUpdated?.Invoke();
        }
    }

    private int GenerateNewUniqueId() { return Random.Range(1000, 9999); }

    public void AttemptAscension(int uniqueId, bool useShard = false)
    {
        var item = MyArtifacts.Find(a => a.UniqueId == uniqueId);
        if (item == null) return;

        var gradeData = DataManager.Instance.GearRepo.GetGearGrade(item.MasterData.GearGrade);
        if (gradeData == null || item.AscensionCount >= gradeData.MaxAscension) return;

        string materialType = useShard ? "Shard" : "SameGear";
        var costData = DataManager.Instance.GearRepo.GetAscensionCosts(item.MasterData.GearGrade, materialType);

        if (useShard)
        {
            if (this.LegendaryShard >= costData.CostAmount)
            {
                this.LegendaryShard -= costData.CostAmount;
                item.AscensionCount++;
            }
        }

        else if (item.CurrentCount > costData.CostAmount)
        { 
            item.CurrentCount -= costData.CostAmount;
            item.AscensionCount++;
        }

        OnInventoryUpdated?.Invoke();
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

