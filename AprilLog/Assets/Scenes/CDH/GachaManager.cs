using System.Linq;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    public void ExecuteGacha(int gachaId, int count = 1)
    {
        var boxData = DataManager.Instance.GearRepo.GetGachaBox(gachaId);
        if (boxData == null) return;

        for (int i = 0; i < count; i++)
        {
            string selectedGrade = DetermineGrade(boxData);
            int selectedGearId = SelectRandomGearByGrade(selectedGrade);

            if (selectedGearId != 0)
            {
                GameStateManager.Instance.ArtifactManager.AddArtifact(selectedGearId);
                Debug.Log($"가챠 성공! {selectedGrade} 등급의 기어(ID: {selectedGearId})를 획득했습니다.");
            }
        }
    }

    public void ExecuteSingleGacha(int gachaId)
    {
        ExecuteGacha(gachaId, 1);
    }

    public void ExecuteTenGacha(int gachaId)
    {
        ExecuteGacha(gachaId, 10);
    }

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

