using System.Collections.Generic;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 가챠로 뽑힌 Gear_ID 목록을 받아 유저 아티팩트 데이터에 반영하는 처리기
// ArtifactManager 의 AddArtifact 자동 분해 로직(레어 한도 버그 포함)을 우회하기 위해,
// 획득 반영/한도 검사/초과 자동 분해를 이 스크립트에서 직접 수행
// - 최초 획득(미보유)은 ArtifactManager.AddArtifact 로 등록(수량1, OnInventoryUpdated 발행).
// - 중복 획득은 한도 이내면 직접 +1, 한도 초과면 자동 분해(보상 합산).
// - 보상(강화석/조각)은 ArtifactDismantleReward 로 계산해 실제 재화에 즉시 반영.
// - 누적(마일리지) 보상 구간 통과 수는 ArtifactMileageTracker 에 위임.
public class ArtifactGachaPostProcessor : MonoBehaviour
{
    [Header("시스템 참조")]
    [Tooltip("비우면 GameStateManager.Instance 의 ArtifactManager 를 사용한다.")]
    [SerializeField] private ArtifactManager _artifactManager;
    [Tooltip("뽑기 후 보유 리스트를 강제 갱신할 리스트 바인더(선택). OnInventoryUpdated 가 발행되지 않는 직접 증가 케이스 대응.")]
    [SerializeField] private ArtifactListBinder _listBinder;
    [Tooltip("누적(마일리지) 보상 진행도 추적기")]
    [SerializeField] private ArtifactMileageTracker _mileageTracker;

    private ArtifactManager Manager =>
        _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

    private GearRepo Repo => DataManager.Instance != null ? DataManager.Instance.GearRepo : null;

    // 뽑힌 Gear_ID 목록을 처리한다. (각 획득 건을 순차적으로 정확히 계산)
    public ArtifactGachaResult Process(int gachaId, List<int> drawnGearIds)
    {
        var result = new ArtifactGachaResult();

        ArtifactManager mgr = Manager;
        GearRepo repo = Repo;
        if (mgr == null || repo == null)
        {
            Debug.LogWarning("[ArtifactGachaPostProcessor] ArtifactManager/GearRepo 미연결. 처리 생략.");
            return result;
        }

        if (drawnGearIds != null)
        {
            foreach (int gearId in drawnGearIds)
                ApplyOne(mgr, repo, gearId, result);
        }

        // 자동 분해 보상을 실제 재화에 즉시 반영(수동 분해와 동일 데이터/식)
        if (result.TotalStone > 0) mgr.UpgradeStone += result.TotalStone;
        if (result.TotalShard > 0) mgr.LegendaryShard += result.TotalShard;

        // 직접 +1(중복) 케이스는 OnInventoryUpdated 가 발행되지 않으므로 리스트를 강제 갱신
        if (_listBinder != null) _listBinder.RefreshInventory();

        // 누적(마일리지) 보상 구간 처리 + 즉시 재화 지급
        ApplyMileage(gachaId, drawnGearIds != null ? drawnGearIds.Count : 0, result);

        return result;
    }

    // Gear_ID 하나를 한도 검사와 함께 반영
    private void ApplyOne(ArtifactManager mgr, GearRepo repo, int gearId, ArtifactGachaResult result)
    {
        GearMasterData master = repo.GetGearData(gearId);
        if (master == null)
        {
            Debug.LogWarning($"[ArtifactGachaPostProcessor] Gear_ID {gearId} 데이터를 찾지 못했습니다.");
            return;
        }

        string grade = master.GearGrade;
        ArtifactInstance existing = mgr.MyArtifacts.Find(a => a != null && a.MasterId == gearId);

        // 미보유 → 최초 획득. AddArtifact 가 수량1로 등록하고 OnInventoryUpdated 발행(자동 분해 분기 미진입)
        if (existing == null)
        {
            mgr.AddArtifact(gearId);
            return;
        }

        // 보유 중 → 등급 최대 보유 한도 검사.
        // 최대 누적 보유 = 본체 + 돌파 소비분 + 잔여 중복 = MaxOwned.
        // CurrentCount(잔여 보유) 한도 = MaxOwned - AscensionCount(돌파로 이미 소비한 수량).
        int maxOwned = GetMaxOwned(repo, grade);
        int currentLimit = Mathf.Max(1, maxOwned - existing.AscensionCount);

        if (existing.CurrentCount < currentLimit)
        {
            // 한도 이내 → 보유 수량 직접 증가
            existing.CurrentCount++;
        }
        else
        {
            // 한도 초과 → 유저 데이터에 반영하지 않고 즉시 자동 분해
            ArtifactDismantleReward.Result reward = ArtifactDismantleReward.Calculate(grade, 1);
            result.TotalStone += reward.Stone;
            result.TotalShard += reward.Shard;

            switch (grade)
            {
                case "Legendary": result.LegendaryDecomposed++; break;
                case "Epic": result.EpicDecomposed++; break;
                default: result.RareDecomposed++; break; // Rare
            }
        }
    }

    private int GetMaxOwned(GearRepo repo, string grade)
    {
        GearGradeData gradeData = repo.GetGearGrade(grade);
        // 데이터가 없으면 보수적으로 1(본체만) 처리 → 무한 누적 방지
        return gradeData != null ? Mathf.Max(1, gradeData.MaxOwned) : 1;
    }

    // 해당 가챠가 '레전더리 확정(천장)' 박스인지. 누적보상/천장 표시는 이 박스에서만 동작한다.
    private bool IsLegendaryGacha(int gachaId)
    {
        GachaBoxData box = Repo != null ? Repo.GetGachaBox(gachaId) : null;
        return box != null && box.PityType == "RandomLegendary" && box.PityCount > 0;
    }

    // 해당 가챠의 전체 누적 뽑기 횟수(레전더리 박스만 추적됨).
    public int GetDrawTotal(int gachaId) =>
        _mileageTracker != null ? _mileageTracker.GetTotalDrawCount(gachaId) : 0;

    // 천장까지 남은 뽑기 횟수. 레전더리(천장) 가챠가 아니면 -1.
    public int RemainingToPity(int gachaId)
    {
        GachaBoxData box = Repo != null ? Repo.GetGachaBox(gachaId) : null;
        if (box == null || box.PityType != "RandomLegendary" || box.PityCount <= 0)
            return -1;

        int total = GetDrawTotal(gachaId);
        return box.PityCount - (total % box.PityCount);
    }

    private void ApplyMileage(int gachaId, int drawCount, ArtifactGachaResult result)
    {
        if (_mileageTracker == null || drawCount <= 0)
            return;

        // 누적(마일리지) 보상은 '레전더리(천장) 가챠'에서만 지급한다. 일반 가챠(1·2번)는 제외.
        if (!IsLegendaryGacha(gachaId))
            return;

        int earned = _mileageTracker.RegisterDraws(gachaId, drawCount);
        if (earned <= 0)
            return;

        result.MileageRewardCount = earned;

        GearRepo repo = Repo;
        if (repo == null)
            return;

        // 보상 데이터(GachaReward)는 (Gacha_ID, MileageCount) 로 구간별로 분리되어 있고,
        // 마지막 구간(Reset=true)을 지나면 다시 첫 구간부터 순환한다.
        // 이번 뽑기로 도달한 '가장 최근 구간'의 MileageCount 키를 산출해 그 보상을 조회한다.
        int milestone = ResolveMileageMilestone(repo, gachaId);
        if (milestone <= 0)
            return;

        GachaRewardData reward = repo.GetGachaReward(gachaId, milestone);
        if (reward == null)
            return;

        result.MileageRewardItem = reward.FirstRewardItem;
        result.MileageRewardAmount = reward.FirstRewardAmount;

        // 누적 보상 아이템(데이터의 FirstRewardItem)은 뽑기권/랜덤기어보상/재료 등 아이템 타입이라,
        // 현재 프로젝트엔 이를 지급할 공용 아이템/인벤토리 시스템이 없다. 지금은 결과 팝업에 '표시만' 한다.
        // [지급 연결 지점] 아이템 지급 API 가 생기면 여기서 한 번만 호출한다. 예:
        //   ItemInventory.Grant(reward.FirstRewardItem, reward.FirstRewardAmount * earned);
    }

    // 가챠별 마일리지 1사이클 누적 한도(데이터의 마지막 구간 = Reset 지점). 최초 1회만 데이터에서 산출해 캐시.
    private readonly Dictionary<int, int> _mileageCycleCache = new Dictionary<int, int>();

    // 현재 누적 뽑기 횟수로부터 이번에 도달한 마일리지 구간(데이터 MileageCount 키)을 계산한다.
    // 마지막 구간을 지나면 다시 첫 구간부터 순환하므로, 누적 횟수를 1사이클 내 위치로 환산해 구간을 구한다.
    private int ResolveMileageMilestone(GearRepo repo, int gachaId)
    {
        int stepSize = _mileageTracker.StepSize;
        if (stepSize <= 0)
            return 0;

        int cycle = GetMileageCycle(repo, gachaId, stepSize);
        if (cycle <= 0)
            return 0;

        // RegisterDraws 직후이므로 이번 뽑기가 반영된 누적 횟수다.
        int totalAfter = _mileageTracker.GetTotalDrawCount(gachaId);
        if (totalAfter <= 0)
            return 0;

        int within = ((totalAfter - 1) % cycle) + 1;                  // 1..cycle (사이클 내 위치)
        return ((within - 1) / stepSize + 1) * stepSize;              // stepSize 단위 올림 → MileageCount 키
    }

    // 데이터에 정의된 마일리지 구간(stepSize 간격)을 훑어 1사이클 누적 한도(마지막 구간 = Reset 지점)를 구한다.
    private int GetMileageCycle(GearRepo repo, int gachaId, int stepSize)
    {
        if (_mileageCycleCache.TryGetValue(gachaId, out int cached))
            return cached;

        int cycle = 0;
        // 안전장치: 최대 100구간까지만 탐색(미정의 구간 1회 조회 시 GearRepo 경고 로그가 1번 남는다).
        for (int m = stepSize; m <= stepSize * 100; m += stepSize)
        {
            if (repo.GetGachaReward(gachaId, m) == null)
                break;
            cycle = m;
        }

        _mileageCycleCache[gachaId] = cycle;
        return cycle;
    }
}
