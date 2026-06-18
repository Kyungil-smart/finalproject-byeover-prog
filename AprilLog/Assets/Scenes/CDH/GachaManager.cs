using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    private Dictionary<int, int> _gachaCounts = new Dictionary<int, int>();

    private List<GachaRewardData> GetRewardDataFromRepo(int gachaId)
    {
        var repo = DataManager.Instance.GearRepo;

        var field = typeof(GearRepo).GetField("_gachaRewardTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null) return new List<GachaRewardData>();

        var table = field.GetValue(repo) as GachaRewardTable;
        if (table == null || table.rows == null) return new List<GachaRewardData>();

        return table.rows.Where(r => r.Gacha_ID == gachaId).ToList();
    }

    public List<int> ExecuteGacha (int gachaId, int count = 1)
    {
        List<int> results = new List<int>();

        var boxData = DataManager.Instance.GearRepo.GetGachaBox(gachaId);
        if (boxData == null) return results;

        _gachaCounts[gachaId] = _gachaCounts.GetValueOrDefault(gachaId, 0) + count;
        int currentCount = _gachaCounts[gachaId];

        for (int i = 0; i < count; i++)
        {
            string selectedGrade = CheckPity(boxData, currentCount - (count - 1 - i));
            int selectedGearId = SelectRandomGearByGrade(selectedGrade);

            if (selectedGearId != 0)
            {
                GameStateManager.Instance.ArtifactManager.AddArtifact(selectedGearId);
                results.Add(selectedGearId);
                Debug.Log($"가챠 성공! {selectedGrade} 등급의 기어(ID: {selectedGearId})를 획득했습니다.");
            }
        }

        CheckMileageReward(gachaId, currentCount);
        return results;
    }

    // 무료 가챠 구현 예정
   /* public List<int> ExecuteFreeGacha(int gachaId)
    {
        List<int> results = new List<int>();

        var freeData = DataManager.Instance.GearRepo.GetFreeGachaBox(gachaId);
        if (freeData == null) return results;

        // 1. 쿨타임 체크 (예시 로직)
        // 2. 광고 시청 완료 여부 체크 (AdRequired가 true일 경우)

        ExecuteGacha(gachaId, freeData.Count);
        Debug.Log($"무료 가챠 실행: {gachaId}");
    }*/

    private void CheckMileageReward(int gachaId, int currentTotalCount)
    {
        var rewardList = GetRewardDataFromRepo(gachaId);
        var reward = rewardList.FirstOrDefault(r => r.MileageCount == currentTotalCount);

        if (reward != null)
        {
            Debug.Log($"마일리지 달성! {reward.FirstRewardAmount}개 지급");

            if (reward.Reset)
            {
                _gachaCounts[gachaId] = 0;
            }
        }
    }

    private string CheckPity(GachaBoxData box, int currentCount)
    {
        if (box.PityType == "RandomLegendary" && currentCount >= box.PityCount)
        {
            return "Legendary";
        }
        return DetermineGrade(box);
    }

   /* public List<int> ExecuteSingleGacha(int gachaId) => ExecuteGacha(gachaId, 1);

    public List<int> ExecuteTenGacha(int gachaId) => ExecuteGacha(gachaId, 10);*/

    public void ExecuteSingleGacha(int gachaId) => ExecuteGacha(gachaId, 1);

    public void ExecuteTenGacha(int gachaId) => ExecuteGacha(gachaId, 10);

    private string DetermineGrade(GachaBoxData box)
    {
        float rand = Random.value;

        if (rand < box.RareRate) return "Rare";
        if (rand < box.RareRate + box.EpicRate) return "Epic";
        return "Legendary";
    }

    private int SelectRandomGearByGrade(string grade)
    {
        var repo = DataManager.Instance.GearRepo;
        var field = typeof(GearRepo).GetField("_gearTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null) return 0;

        var table = field.GetValue(repo) as GearMasterTable;
        if (table == null || table.rows == null) return 0;

        var filtered = table.rows.Where(g => g.GearGrade == grade).ToList();

        if (filtered.Count == 0) return 0;

        return filtered[UnityEngine.Random.Range(0, filtered.Count)].Gear_ID;
    }
}

