// 작성자 : 김영찬
// SpellRepo에서 사용하는 데이터를 목적에 맞춰 가공하기 위한 클래스들
// 기대 효과 : SpellRepo에서 중복해서 데이터를 캐싱 할 필요 없음

using System.Collections.Generic;

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
        if (!StatNameChainData.ContainsKey(data.Stat_Name))
            StatNameChainData[data.Stat_Name] = new StatNameChainData(data.Stat_Name);
        
        StatNameChainData[data.Stat_Name].AddData(data);
    }
}
