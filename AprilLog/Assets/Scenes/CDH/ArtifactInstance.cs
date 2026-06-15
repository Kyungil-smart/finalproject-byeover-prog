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

    public GearMasterData MasterData => DataManager.Instance.GearRepo.GetGearData(MasterId);

    public int GetMaxLevel()
    {
        return 5 + (AscensionCount * 5);
    }

    public bool CanLevelUp()
    {
        var levelData = DataManager.Instance.GearRepo.GetGearLevel(MasterId);

        if (levelData == null) return false;

        return CurrentLevel < levelData.EndLevel;
    }
}
