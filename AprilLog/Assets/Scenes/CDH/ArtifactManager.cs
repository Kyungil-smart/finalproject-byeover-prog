// 수정자 : 김영찬
// 수정 내용 : 실제 재화에 반영 하는 부분을 GameManager에서 담당 및 GameManager 비 활성화 시 테스트용 로컬 변수 활용 하도록 변경
//           세이브 / 로드를 게임 매니저에서 일괄 담당하도록 수정

using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.IO;

public class ArtifactManager : MonoBehaviour
{
    private const int GoldItemId = 70001;
    private const int UpgradeStoneId = 70004;
    private const int LegendaryShardId = 70005;
    
    public int UpgradeStone => GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null
        ? DataManager.Instance.ResourceRepo.GetItemCount(UpgradeStoneId)
        : _localUpgradeStones;
    public int LegendaryShard => GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null
        ? DataManager.Instance.ResourceRepo.GetItemCount(LegendaryShardId)
        : _localLegendaryShard;
    
    private int _localUpgradeStones;
    private int _localLegendaryShard;
    
    public event Action OnInventoryUpdated;

    public List<ArtifactInstance> MyArtifacts = new List<ArtifactInstance>();
    
    public void InitializeItems(int upgradeStone, int legendaryShard)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            DataManager.Instance.ResourceRepo.SetItemCount(UpgradeStoneId, Mathf.Max(0, upgradeStone));
            DataManager.Instance.ResourceRepo.SetItemCount(LegendaryShardId, Mathf.Max(0, legendaryShard));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localUpgradeStones = Mathf.Max(0, upgradeStone);
            _localLegendaryShard = Mathf.Max(0, legendaryShard);
        }
    }

    private void SaveData()
    {
        if(GameManager.Instance != null && DataManager.Instance != null)
            GameManager.Instance.SaveArtifact(MyArtifacts);
    }

    public void LoadData()
    {
        if(GameManager.Instance != null && DataManager.Instance != null)
        {
            GameManager.Instance.ApplyCloudDataToArtifactManager(this);
            Debug.Log("[로드 완료] 아티팩트 데이터를 불러왔습니다.");
        }
    }

    private void Start()
    {
        LoadData();
        OnInventoryUpdated += SaveData;
    }

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

            var repo = DataManager.Instance.ResourceRepo;
            
            if (grade == "Legendary")
            {
                repo.AddItem(LegendaryShardId, excessCount);
                Debug.Log($"[자동 분해] 레전더리 초과분 {excessCount}개 → 레전더리 조각 지급");
            }

            else
            {
                int reward = excessCount * dismantleData.RewardAmount;
                repo.AddItem(UpgradeStoneId, reward);
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

        if (!artifact.CanLevelUp()) return;

        // 레벨업 비용 = 골드(70001) + 강화석(70004). (아이템 ID는 item_master 기준)
        var repo = DataManager.Instance.GearRepo;
        int goldCost = Mathf.Max(0, repo.GetGearUpgradeCost(artifact.MasterId, artifact.CurrentLevel, GoldItemId));
        int stoneCost = Mathf.Max(0, repo.GetGearUpgradeCost(artifact.MasterId, artifact.CurrentLevel, UpgradeStoneId));

        // 보유 검사 : 골드는 GameManager(영속 재화), 강화석은 UpgradeStone. (GameManager 없으면 골드 무시)
        bool canGold = GameManager.Instance == null || GameManager.Instance.CanAffordCurrency(goldCost, 0);
        if (!canGold || UpgradeStone < stoneCost)
        {
            Debug.LogWarning($"[레벨업] 재화 부족. 골드 {goldCost} / 강화석 {stoneCost}");
            return;
        }

        // 차감 → 레벨업

        // 부트씬 안거치고 로비에서 작동 => GameManager == null
        if (GameManager.Instance != null)
        {
            if(!GameManager.Instance.TrySpendCurrency(goldCost, 0)) return;
            
            if(!DataManager.Instance.ResourceRepo.UseItem(UpgradeStoneId, stoneCost))
            {
                // 골드가 소모 되었으나, 강화석 소모에 실패 할 경우에는 다시 재화 롤백
                DataManager.Instance.ResourceRepo.AddItem(GoldItemId, goldCost);
                GameManager.Instance.SyncAndSaveResourceCloudData();
                return;
            }
            
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }

        else
        {
            // 로컬에서는 골드 차감 없음
            _localUpgradeStones -= stoneCost;
        }
        
        artifact.CurrentLevel++;
        Debug.Log($"[중요] 레벨업 완료: ID {artifact.MasterId}, 레벨 {artifact.CurrentLevel} (골드 -{goldCost}, 강화석 -{stoneCost})");
        OnInventoryUpdated?.Invoke();
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
            if (LegendaryShard >= costData.CostAmount)
            {
                if (DataManager.Instance != null)
                {
                    if(DataManager.Instance.ResourceRepo.UseItem(UpgradeStoneId, costData.CostAmount))
                        item.AscensionCount++;
                }
                else
                {
                    _localLegendaryShard -= costData.CostAmount;
                    item.AscensionCount++;
                }
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
            var repo = DataManager.Instance.ResourceRepo;

            if (grade == "Legendary")
            {
                if (DataManager.Instance != null)
                {
                    repo.AddItem(LegendaryShardId, rewardAmount);
                    GameManager.Instance.SyncAndSaveResourceCloudData();
                }
                else
                {
                    _localLegendaryShard += rewardAmount;
                }
            }
            else
            {
                if (DataManager.Instance != null)
                {
                    repo.AddItem(UpgradeStoneId, rewardAmount);
                    GameManager.Instance.SyncAndSaveResourceCloudData();
                }
                else
                {
                    _localUpgradeStones += rewardAmount;
                }
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

    /// <summary>
    /// 레전더리 조각 증가 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void AddShard(int amount)
    {
        var repo = DataManager.Instance.ResourceRepo;

        if (DataManager.Instance != null)
        {
            repo.AddItem(LegendaryShardId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localLegendaryShard += Mathf.Max(0, amount);
        }
    }

    /// <summary>
    /// 레전더리 조각 소모 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void UseShard(int amount)
    {
        var repo = DataManager.Instance.ResourceRepo;
        
        if (DataManager.Instance != null)
        {
            if(!repo.UseItem(LegendaryShardId, Mathf.Max(0, amount))) return;
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localLegendaryShard -= Mathf.Max(0, amount);
        }
    }

    /// <summary>
    /// 강화석 증가 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void AddStone(int amount)
    {
        var repo = DataManager.Instance.ResourceRepo;

        if (DataManager.Instance != null)
        {
            repo.AddItem(UpgradeStoneId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localUpgradeStones += Mathf.Max(0, amount);
        }
    }

    /// <summary>
    /// 강화석 소모 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void UseStone(int amount)
    {
        var repo = DataManager.Instance.ResourceRepo;
        
        if (DataManager.Instance != null)
        {
            repo.UseItem(UpgradeStoneId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localUpgradeStones -= Mathf.Max(0, amount);
        }
    }
    
    /// <summary>
    /// 레전더리 조각 설정 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void SetShard(int amount)
    {
        if(DataManager.Instance != null)
        {
            DataManager.Instance.ResourceRepo.SetItemCount(LegendaryShardId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localLegendaryShard = Mathf.Max(0, amount);
        }
    }
    
    /// <summary>
    /// 강화석 설정 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void SetStone(int amount)
    {
        if(DataManager.Instance != null)
        {
            DataManager.Instance.ResourceRepo.SetItemCount(UpgradeStoneId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localUpgradeStones = Mathf.Max(0, amount);
        }
    }
}

