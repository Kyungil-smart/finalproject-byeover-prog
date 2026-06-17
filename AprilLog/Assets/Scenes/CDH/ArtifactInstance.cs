using UnityEngine;

[System.Serializable]
public class ArtifactInstance
{
    public int UniqueId;
    public int MasterId;
    public int CurrentLevel = 1;
    public int CurrentCount = 1;
    public bool IsEquipped;
    public int AscensionCount = 0;
    public bool IsAscended => AscensionCount >= 1;

    private GearRepo Repo => DataManager.Instance.GearRepo;
    public GearMasterData MasterData => Repo.GetGearData(MasterId);

    public int GetMaxAscensionLimit()
    {
        var gradeData = Repo.GetGearGrade(MasterData.GearGrade);
        return gradeData != null ? gradeData.MaxAscension : 0;
    }

    public int GetMaxLevelLimit()
    {
        var gradeData = Repo.GetGearGrade(MasterData.GearGrade);
        if (gradeData == null) return 1;

        return gradeData.BaseLevel + (AscensionCount * gradeData.LevelCapIncrease);
    }

    public bool CanLevelUp()
    {
        return CurrentLevel < GetMaxLevelLimit();
    }

    public bool CanAscend()
    {
        int maxAscension = 5;
        return AscensionCount < maxAscension;
    }
}
