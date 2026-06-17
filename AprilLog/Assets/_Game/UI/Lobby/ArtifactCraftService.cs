using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 "제작(레전더리 조각 5개)" 과 "조각 돌파(레전더리 조각 3개)" 의 데이터 처리 로직.
public class ArtifactCraftService
{
    public const int DefaultCraftCost = 5;         // 제작 소모 조각 수 (기획서)
    public const int DefaultBreakthroughCost = 3;  // 조각 돌파 소모 수 (ArtifactManager.AttemptAscension 내부값과 동일)

    private const string LegendaryGrade = "Legendary";

    private readonly int _craftCost;
    private readonly int _breakthroughCost;

    public int CraftCost => _craftCost;
    public int BreakthroughCost => _breakthroughCost;

    public ArtifactCraftService(int craftCost = DefaultCraftCost, int breakthroughCost = DefaultBreakthroughCost)
    {
        _craftCost = Mathf.Max(1, craftCost);
        _breakthroughCost = Mathf.Max(1, breakthroughCost);
    }

   
    // 매니저 / 리포 해석 (싱글톤. 없으면 null 반환 → 호출부에서 방어)
   
    private ArtifactManager Manager =>
        GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;

    private GearRepo Repo =>
        DataManager.Instance != null ? DataManager.Instance.GearRepo : null;

    public int OwnedShard => Manager != null ? Manager.LegendaryShard : 0;

    public GearMasterData GetMaster(int gearId) => Repo != null ? Repo.GetGearData(gearId) : null;

    public bool IsOwned(int gearId)
    {
        ArtifactManager mgr = Manager;
        return mgr != null && mgr.MyArtifacts.Exists(a => a != null && a.MasterId == gearId);
    }

    public bool IsLegendary(int gearId)
    {
        GearMasterData m = GetMaster(gearId);
        return m != null && m.GearGrade == LegendaryGrade;
    }

    
    // 제작 (미보유 레전더리 → 조각 5개 소모하여 확정 제작)
    
    public bool CanCraft(int gearId)
    {
        ArtifactManager mgr = Manager;
        if (mgr == null) return false;
        if (GetMaster(gearId) == null) return false; // 잘못된 ID
        if (!IsLegendary(gearId)) return false;       // 레전더리만 제작 대상
        if (IsOwned(gearId)) return false;            // 이미 보유면 제작 불가
        return mgr.LegendaryShard >= _craftCost;
    }

    // 제작 실행. 성공 시 true. 조각 차감 + 보유 추가는 한 호출에서 연속 처리(부분 실패 없음).
    public bool TryCraft(int gearId)
    {
        ArtifactManager mgr = Manager;
        if (mgr == null)
        {
            Debug.LogWarning("[ArtifactCraftService] ArtifactManager 가 없어 제작할 수 없습니다.");
            return false;
        }

        if (!CanCraft(gearId))
        {
            Debug.LogWarning($"[ArtifactCraftService] 제작 조건을 만족하지 않습니다. Gear_ID: {gearId} (보유:{IsOwned(gearId)}, 레전더리:{IsLegendary(gearId)}, 조각:{mgr.LegendaryShard}/{_craftCost})");
            return false;
        }

        mgr.LegendaryShard -= _craftCost;   // 1) 조각 차감
        mgr.AddArtifact(gearId);            // 2) 보유 추가(수량 1, OnInventoryUpdated 발행 → 리스트 자동 갱신)
        Debug.Log($"[ArtifactCraftService] 제작 성공. Gear_ID: {gearId}, 잔여 조각: {mgr.LegendaryShard}");
        return true;
    }

    
    // 조각 돌파 (보유 레전더리 → 조각 3개를 대체 재료로 돌파)
    
    public ArtifactInstance GetInstance(int uniqueId)
        => Manager != null ? Manager.MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId) : null;

    public bool CanBreakthroughWithShard(int uniqueId)
    {
        ArtifactManager mgr = Manager;
        if (mgr == null) return false;

        ArtifactInstance inst = GetInstance(uniqueId);
        if (inst == null) return false;

        GearMasterData m = inst.MasterData;
        if (m == null || m.GearGrade != LegendaryGrade) return false;

        if (inst.AscensionCount >= GetMaxAscension(m.GearGrade)) return false; // 최대 돌파 단계 방어

        return mgr.LegendaryShard >= _breakthroughCost;
    }

    // 조각 돌파 실행. ArtifactManager.AttemptAscension(useShard:true) 가 조각 3 차감 + 돌파 처리.
    public bool TryBreakthroughWithShard(int uniqueId)
    {
        ArtifactManager mgr = Manager;
        if (mgr == null) return false;
        if (!CanBreakthroughWithShard(uniqueId)) return false;

        ArtifactInstance inst = GetInstance(uniqueId);
        int before = inst.AscensionCount;

        mgr.AttemptAscension(uniqueId, useShard: true);

        bool success = inst.AscensionCount > before;
        if (success)
            Debug.Log($"[ArtifactCraftService] 조각 돌파 성공. UniqueId: {uniqueId}, 돌파단계: {inst.AscensionCount}, 잔여 조각: {mgr.LegendaryShard}");
        return success;
    }

    // 등급별 최대 돌파 단계. 데이터(GradeTable)가 있으면 우선, 없으면 기획서 기본값으로 폴백.
    private int GetMaxAscension(string grade)
    {
        GearRepo repo = Repo;
        if (repo != null)
        {
            GearGradeData gradeData = repo.GetGearGrade(grade);
            if (gradeData != null) return gradeData.MaxAscension;
        }

        switch (grade)
        {
            case "Rare": return 1;
            case "Epic": return 3;
            case "Legendary": return 5;
            default: return 0;
        }
    }
}
