// 생성자 : 김영찬
// 설명 : 인게임에서 사용하는 임시 구조체 및 클래스를 정리하는 스크립트입니다.
// 주의 사항 : 기능 별로 region으로 분류하면 다른 작업자가 보기 편합니다.

using System;
using System.Collections.Generic;
using UnityEngine;

#region Sort 보조 구조체

/// <summary>
/// Sort 보조 구조체
/// </summary>
[Serializable]
public struct WaitingCombo
{
    public int[] unitTypes;
    public WaitingDifficulty difficulty;

    public int FilledCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < unitTypes.Length; i++)
                if (unitTypes[i] >= 0) c++;
            return c;
        }
    }
}

#endregion

#region SpellRepo 지원

/// <summary>
/// 스킬 인첸트 데이터를 이름으로 묶음
/// </summary>
public class SkillNameChainData
{
    public int Name_ID { get; private set; } 
    public int MaxLevel { get; private set; }
    public Dictionary<int, SkillTableData> LevelDataMap { get; private set; }

    public SkillNameChainData(int nameId)
    {
        Name_ID = nameId;
        LevelDataMap = new Dictionary<int, SkillTableData>();
        MaxLevel = 0;
    }

    public void AddData(SkillTableData data)
    {
        LevelDataMap[data.Level] = data;
        if (data.Level > MaxLevel) MaxLevel = data.Level;
    }

    public SkillTableData GetNextLevelData(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return null;
        return LevelDataMap.TryGetValue(currentLevel + 1, out var nextData) ? nextData : null;
    }
}

/// <summary>
/// 스킬 인첸트 데이터를 그룹 ID로 묶음
/// </summary>
public class SkillGroupChainData
{
    public int SkillGroup_ID { get; private set; }
    public Dictionary<int, SkillNameChainData> SkillNameChainData { get; private set; } // Key: Name

    public SkillGroupChainData(int groupId)
    {
        SkillGroup_ID = groupId;
        SkillNameChainData = new Dictionary<int, SkillNameChainData>();
    }

    public void AddData(SkillTableData data)
    {
        if (!SkillNameChainData.ContainsKey(data.Name))
            SkillNameChainData[data.Name] = new SkillNameChainData(data.Name);
        
        SkillNameChainData[data.Name].AddData(data);
    }
}

/// <summary>
/// 스텟 인첸트 데이터를 이름으로 묶음
/// </summary>
public class StatNameChainData
{
    public int Stat_Name_ID { get; private set; }
    public int MaxLevel { get; private set; }
    public Dictionary<int, StatTableData> LevelDataMap { get; private set; }

    public StatNameChainData(int statNameId)
    {
        Stat_Name_ID = statNameId;
        LevelDataMap = new Dictionary<int, StatTableData>();
        MaxLevel = 0;
    }

    public void AddData(StatTableData data)
    {
        LevelDataMap[data.StatLevel] = data;
        if (data.StatLevel > MaxLevel) MaxLevel = data.StatLevel;
    }

    public StatTableData GetNextLevelData(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return null;
        return LevelDataMap.TryGetValue(currentLevel + 1, out var nextData) ? nextData : null;
    }
}

/// <summary>
/// 스텟 인첸트 데이터를 그룹 ID로 묶음
/// </summary>
public class StatGroupChainData
{
    public int StatGroup_ID { get; private set; }
    public Dictionary<int, StatNameChainData> StatNameChainData { get; private set; } // Key: Stat_Name

    public StatGroupChainData(int groupId)
    {
        StatGroup_ID = groupId;
        StatNameChainData = new Dictionary<int, StatNameChainData>();
    }

    public void AddData(StatTableData data)
    {
        if (!StatNameChainData.ContainsKey(data.StatName))
            StatNameChainData[data.StatName] = new StatNameChainData(data.StatName);
        
        StatNameChainData[data.StatName].AddData(data);
    }
}

#endregion

#region GearRepo 지원

/// <summary>
/// 기어 업그레이드 코스트를 저장 및 계산 도우미
/// </summary>
public class GearUpgradeSupporter
{
    public int GearId { get; private set; }
    public int StartLevel { get; private set; }
    public int EndLevel { get; private set; }
    public Dictionary<int, UpgradeCostData> UpgradeCosts { get; private set; }

    public GearUpgradeSupporter(int gearId, int startLevel, int endLevel)
    {
        GearId = gearId;
        StartLevel = startLevel;
        EndLevel = endLevel;
        UpgradeCosts = new Dictionary<int, UpgradeCostData>();
    }

    public void AddData(GearUpgradeCostData data)
    {
        if (!UpgradeCosts.ContainsKey(data.Type))
            UpgradeCosts[data.Type] = new UpgradeCostData(data.Type ,data.BaseAmount, data.GrowthValue);
    }
    
    public int? CalculateUpgradeCosts(int curLevel, int costType)
    {
        if(curLevel < StartLevel || curLevel > EndLevel)
        {
            Debug.LogWarning($"[GearRepo] {curLevel}Level is wrong range Gear Level in this Gear");
            return null;
        }
        
        foreach (var data in UpgradeCosts.Values)
        {
            if (data.Type == costType)
            {
                return Mathf.FloorToInt(data.BaseAmount * data.GrowthValue * curLevel);
            }
        }
        
        return null;
    }
}

public class UpgradeCostData
{
    public int Type { get; private set; }
    public int BaseAmount { get; private set; }
    public float GrowthValue { get; private set; }
    public UpgradeCostData(int type, int baseAmount, float growthValue)
    {
        Type = type;
        BaseAmount = baseAmount;
        GrowthValue = growthValue;
    }
}

#endregion

#region 인첸트 시스템 지원

public enum EnchantType { Skill, Stat }

[Serializable]
public class EnchantCandidate
{
    public EnchantType Type;
    public int Name_ID;            
    public int Specific_ID;        
    public int Level;              
    public float Weight;           
    
    public SkillTableData SkillData; 
    public StatTableData StatData;   
}

[Serializable]
public class EnchantProbabilityConfig
{
    [Header("스킬/스탯 통합 풀 등장 비율 (기본 100 : 100)")]
    public float SkillPoolBaseWeight = 100f;
    public float StatPoolBaseWeight = 100f;

    [Header("스킬 - 1~2개 보유 시 (기획서 3-3-1-1 표의 단계 1)")]
    public float SkillStage1_HeldWeight = 30f;
    public float SkillStage1_UnheldWeight = 70f;

    [Header("스킬 - 3~4개 보유 시 (해당 표의 단계 2)")]
    public float SkillStage2_HeldWeight = 50f;
    public float SkillStage2_UnheldWeight = 50f;

    [Header("스킬 - 5개 보유 시 (해당 표의 단계 3)")]
    public float SkillStage3_HeldWeight = 80f;
    public float SkillStage3_UnheldWeight = 20f;

    [Header("스탯 확률 (기획서 3-3-4)")]
    public float Stat_HeldWeight = 60f;
    public float Stat_UnheldWeight = 40f;
}

[Serializable]
public class EnchantDisplayData
{
    public int EnchantId;
    public string Name;
    public string Description;
    public int Level;
    public string ImageKey;
    public string TypeLabel;   // 카드 타입 표시용 (Presenter가 stat-type 기반으로 채움)
}

[Serializable]
public class AcquiredSkillData
{
    public int Level;
    public int GroupID;
    public SkillTableData Data;
}

[Serializable]
public class AcquiredStatData
{
    public int Level;
    public int GroupID;
    public StatTableData Data;
}

public struct EffectSpec
{
    public int Id { get;  private set; }
    public float CalValue { get; private set; }
    public float CalDuration { get; private set; }
    public float CalInterval { get; private set; }

    public EffectSpec(int id, float calValue, float calDuration, float calInterval)
    {
        Id = id;
        CalValue = calValue;
        CalDuration = calDuration;
        CalInterval = calInterval;
    }
}

public class FusionEnchantData
{
    public int EnchantId { get; private set; }
    public int Sort1 { get; private set; }
    public int Sort2 { get; private set; }
    public int Sort3 { get; private set; }
    public int IconImageKey { get; private set; }

    public FusionEnchantData(int enchantId, int sort1, int sort2, int sort3, int iconImageKey)
    {
        EnchantId = enchantId;
        Sort1 = sort1;
        Sort2 = sort2;
        Sort3 = sort3;
        IconImageKey = iconImageKey;
    }
    
    public void LevelUp()
    {
        EnchantId += 1;
    }
}

#endregion

#region 인첸트 도감 지원

[Serializable]
public class EnchantBookDisplayData
{
    public int EnchantId;
    public string Name;
    public string Type;
    public bool IsOwned;
    public int Level;
    public int MaxLevel;
}

#endregion

#region PlayerModel 지원

public enum PlayerStatus
{
    Hp,
    Attack,
    CriticalRate,
    CriticalDamage,
    FlatPierce,
    PercentagePierce,
    EffectPower,
    HicCount,
    AoE,
    MaxTargets,
    AttackSpeed
}

public enum CalFormula
{
    Add,
    Rate,
    None
}

#endregion