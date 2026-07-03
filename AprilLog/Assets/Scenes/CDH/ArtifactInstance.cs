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

    public bool CanAffordLevelUp
    {
        get
        {
            int cost = DataManager.Instance.GearRepo.GetGearUpgradeCost(MasterId, CurrentLevel, 70001);
            return GameStateManager.Instance.ArtifactManager.UpgradeStone >= cost;
        }
    }

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
        return AscensionCount < GetMaxAscensionLimit();
    }

    /// <summary>저장/로드 복사 경계용 깊은 복사. CloudData와 런타임 목록(ArtifactManager.MyArtifacts)이
    /// 같은 인스턴스를 공유하면 저장 호출 없이도 변경이 스며들어 저장 누락 버그를 은닉하므로, 경계에서 항상 복사한다.</summary>
    public ArtifactInstance Clone()
    {
        return new ArtifactInstance
        {
            UniqueId = UniqueId,
            MasterId = MasterId,
            CurrentLevel = CurrentLevel,
            CurrentCount = CurrentCount,
            IsEquipped = IsEquipped,
            AscensionCount = AscensionCount,
        };
    }
}
