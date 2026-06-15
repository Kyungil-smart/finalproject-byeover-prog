
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class ArtifactManager : MonoBehaviour
{
    public int UpgradeStone;
    public int LegendaryShard;
    private const int N = 10;
    private const int M = 20;
    public event Action OnInventoryUpdated;

    public List<ArtifactInstance> MyArtifacts = new List<ArtifactInstance>();

    public void Initialize()
    {
        Debug.Log("[ArtifactManager] 초기화 완료. 데이터를 로드할 준비가 되었습니다.");
    }

    private int GetMaxAscensionLimit(string grade)
    {
        switch (grade)
        {
            case "Rare": return 1;    
            case "Epic": return 3;      
            case "Legendary": return 5;
            default: return 0;
        }
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
        int baseMax = 0;

        if (master.GearGrade == "Rare") baseMax = 2;
        else if (master.GearGrade == "Epic") baseMax = 4;
        else if (master.GearGrade == "Legendary") baseMax = 6;

        int currentLimit = CalculateDynamicLimit(item, baseMax);

        if (item.CurrentCount > currentLimit)
        {
            int excessCount = item.CurrentCount - currentLimit;
            item.CurrentCount = currentLimit;

            if (grade == "Rare")
            {
                int reward = excessCount * N;
                this.UpgradeStone += reward;
                Debug.Log($"[자동 분해] 레어 초과분 {excessCount}개 → 레어 강화석 지급");
            }
            else if (grade == "Epic")
            {
                int reward = excessCount * M;
                this.UpgradeStone += reward;
                Debug.Log($"[자동 분해] 에픽 초과분 {excessCount}개 → 에픽 강화석 지급");
            }
            else if (grade == "Legendary")
            {
                this.LegendaryShard += excessCount;
                Debug.Log($"[자동 분해] 레전더리 초과분 {excessCount}개 → 레전더리 조각 지급");
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

        int maxAscension = GetMaxAscensionLimit(item.MasterData.GearGrade);
        if (item.AscensionCount >= maxAscension) return;

        if (useShard && item.MasterData.GearGrade == "Legendary")
        {
            if (this.LegendaryShard >= 3)
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
        if (grade == "Rare")
        {
            // 레어 등급 분해 시 강화석 N개 획득
            this.UpgradeStone += (amount * N);
            Debug.Log($"[자동 분해] 레어 {amount}개 분해 → 강화석 {amount * N}개 획득");
        }
        else if (grade == "Epic")
        {
            // 에픽 등급 분해 시 강화석 M개 획득
            this.UpgradeStone += (amount * M);
            Debug.Log($"[자동 분해] 에픽 {amount}개 분해 → 강화석 {amount * M}개 획득");
        }
        else if (grade == "Legendary")
        {
            // 레전더리 분해 시 조각 획득 (기획서 2-4-3-3)
            this.LegendaryShard += amount;
            Debug.Log($"[자동 분해] 레전더리 {amount}개 분해 → 조각 {amount}개 획득");
        }
    }
}

