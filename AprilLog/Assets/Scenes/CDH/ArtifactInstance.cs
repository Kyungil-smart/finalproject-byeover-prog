using UnityEngine;

[System.Serializable]
public class ArtifactInstance
{
    public int UniqueId;
    public int MasterId;
    public int CurrentLevel = 1;
    public bool IsEquipped;

    public GearMasterData MasterData => DataManager.Instance.GearRepo.GetGearData(MasterId);

    public bool CanLevelUp()
    {
        var levelData = DataManager.Instance.GearRepo.GetGearLevel(MasterId);

        if (levelData == null) return false;

        return CurrentLevel < levelData.EndLevel;
    }
}
