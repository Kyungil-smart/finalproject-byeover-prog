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

    // ---------- 장착 (단일 진입점) ----------
    // 장착 상태 변경은 반드시 TryEquip/TryUnequip을 거친다. UI(ArtifactEquipController 등)가 IsEquipped를
    // 직접 토글하면 검증/저장이 누락되고, 로드-저장 복사 경계(GameManager.CloneArtifactList) 도입 후에는
    // 직접 토글분이 어디에도 저장되지 않는다.

    /// <summary>동시 장착 한도. UI 슬롯 수도 이 값을 따른다.</summary>
    public const int MaxEquip = 3;

    /// <summary>장착/해제 성공 시 발행. (OnInventoryUpdated와 분리 - 인벤토리 재동기화/저장 루프를 피한다)</summary>
    public event Action OnEquipmentChanged;

    public int EquippedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < MyArtifacts.Count; i++)
                if (MyArtifacts[i] != null && MyArtifacts[i].IsEquipped) count++;
            return count;
        }
    }

    /// <summary>장착 시도. 미보유/이미 장착/한도 초과면 false(변경 없음). 성공 시 이벤트 발행 + 저장.</summary>
    public bool TryEquip(int uniqueId)
    {
        var inst = MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId);
        if (inst == null || inst.IsEquipped) return false;
        if (EquippedCount >= MaxEquip) return false;

        inst.IsEquipped = true;
        OnEquipmentChanged?.Invoke();
        SaveData();
        return true;
    }

    /// <summary>해제 시도. 미보유/미장착이면 false(변경 없음). 성공 시 이벤트 발행 + 저장.</summary>
    public bool TryUnequip(int uniqueId)
    {
        var inst = MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId);
        if (inst == null || !inst.IsEquipped) return false;

        inst.IsEquipped = false;
        OnEquipmentChanged?.Invoke();
        SaveData();
        return true;
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

            // 지급은 지갑 API로(영속+이벤트 포함). 옛 repo 직접 지급은 영속 호출이 빠져 있어 재시작 시 유실될 수 있었다.
            if (grade == "Legendary")
            {
                AddShard(excessCount);
                Debug.Log($"[자동 분해] 레전더리 초과분 {excessCount}개 → 레전더리 조각 지급");
            }
            else
            {
                int reward = excessCount * dismantleData.RewardAmount;
                AddStone(reward);
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

        // 보유 검사 + 차감 : 골드(70001)+강화석(70004)을 지갑 API로 원자 처리(부족하면 아무것도 차감 안 됨).
        // 옛 코드는 골드/강화석을 서로 다른 API로 차감하고 실패 시 수동 롤백했는데, 그 롤백 경로가 이벤트/영속 타이밍 불일치의 원인이었다.
        // 부트씬 안 거치고 로비 단독 실행 => GameManager == null → 골드 개념 없이 로컬 강화석만.
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.TrySpendResources("아티팩트 레벨업", (GoldItemId, goldCost), (UpgradeStoneId, stoneCost)))
            {
                Debug.LogWarning($"[레벨업] 재화 부족. 골드 {goldCost} / 강화석 {stoneCost}");
                return;
            }
        }
        else
        {
            if (UpgradeStone < stoneCost)
            {
                Debug.LogWarning($"[레벨업] 강화석 부족. {stoneCost}");
                return;
            }
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
            // 옛 코드는 조각 보유를 검사해놓고 강화석(UpgradeStoneId)을 차감하는 재화 불일치 버그가 있었다. 조각(70005)으로 통일.
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.UseResource(LegendaryShardId, costData.CostAmount))
                    item.AscensionCount++;
            }
            else if (_localLegendaryShard >= costData.CostAmount)
            {
                _localLegendaryShard -= costData.CostAmount;
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
        if (dismantleData == null) return;

        int rewardAmount = amount * dismantleData.RewardAmount;

        if (grade == "Legendary")
            AddShard(rewardAmount);
        else
            AddStone(rewardAmount);

        Debug.Log($"[분해 완료] {grade} {amount}개 분해 → {rewardAmount}개 획득");
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
        // 지갑 API 경유(영속+이벤트+사유 로그). 부트 안 거친 테스트 씬(GameManager 없음)만 로컬 변수.
        if (GameManager.Instance != null)
            GameManager.Instance.AddResource(LegendaryShardId, Mathf.Max(0, amount), "아티팩트 조각 지급");
        else
            _localLegendaryShard += Mathf.Max(0, amount);
    }

    /// <summary>
    /// 레전더리 조각 소모 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void UseShard(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UseResource(LegendaryShardId, Mathf.Max(0, amount));
        else
            _localLegendaryShard -= Mathf.Max(0, amount);
    }

    /// <summary>
    /// 강화석 증가 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void AddStone(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddResource(UpgradeStoneId, Mathf.Max(0, amount), "강화석 지급");
        else
            _localUpgradeStones += Mathf.Max(0, amount);
    }

    /// <summary>
    /// 강화석 소모 - 이거 쓰세요. 이제 직접 증감 안됩니다.
    /// </summary>
    /// <param name="amount"></param>
    public void UseStone(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UseResource(UpgradeStoneId, Mathf.Max(0, amount));
        else
            _localUpgradeStones -= Mathf.Max(0, amount);
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

