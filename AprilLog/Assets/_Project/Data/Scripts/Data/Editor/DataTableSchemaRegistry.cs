// 담당자 : 김영찬
// 설명   : 데이터 자동화 대상 테이블 정의
// 수정자 : 정승우
// 수정내용 : 엑셀 시트명, JSON 파일명, Data 클래스, SO 클래스 정의를 한 곳으로 통합

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public readonly struct DataTableSchema
{
    public readonly string SheetName;
    public readonly string JsonName;
    public readonly string DataClassName;
    public readonly string TableClassName;
    public readonly bool IsRequired;

    public DataTableSchema(
        string sheetName,
        string jsonName,
        string dataClassName,
        string tableClassName,
        bool isRequired)
    {
        SheetName = sheetName;
        JsonName = jsonName;
        DataClassName = dataClassName;
        TableClassName = tableClassName;
        IsRequired = isRequired;
    }
}

public static class DataTableSchemaRegistry
{
    public static readonly IReadOnlyList<DataTableSchema> Schemas = new List<DataTableSchema>
    {
        new DataTableSchema("CharacterMaster", "character_master", "CharacterMasterData", "CharacterMasterTable", true),
        new DataTableSchema("CharacterName", "character_name", "CharacterNameData", "CharacterNameTable", true),
        new DataTableSchema("CommonStatus", "common_status", "CommonStatusData", "CommonStatusTable", true),
        new DataTableSchema("CharacterStatus", "character_status", "CharacterStatusData", "CharacterStatusTable", true),
        new DataTableSchema("MonsterStatus", "monster_status", "MonsterStatusData", "MonsterStatusTable", true),

        new DataTableSchema("SkillMaster", "skill_master", "SkillMasterData", "SkillMasterTable", true),
        new DataTableSchema("SkillData", "skill_data", "SkillData", "SkillDataTable", true),
        new DataTableSchema("EffectTable", "effect_table", "EffectData", "EffectDataTable", true),

        new DataTableSchema("EnchantMaster", "enchant_master", "EnchantMasterData", "EnchantMasterTable", true),
        new DataTableSchema("EnchantLevel", "enchant_level", "EnchantLevelData", "EnchantLevelTable", true),
        new DataTableSchema("EnchantWeight", "enchant_weight", "EnchantWeightData", "EnchantWeightTable", true),

        new DataTableSchema("ChapterMaster", "chapter_master", "ChapterData", "ChapterTable", true),
        new DataTableSchema("MapLanguage", "map_language", "MapLanguageData", "MapLanguageTable", true),
        new DataTableSchema("StageMaster", "stage_master", "StageData", "StageDataTable", true),

        new DataTableSchema("MonsterPoolMaster", "monster_pool_master", "MonsterPoolMasterData", "MonsterPoolMasterTable", true),
        new DataTableSchema("MonsterPool", "monster_pool", "MonsterPoolData", "MonsterPoolTable", true),
        new DataTableSchema("StageSpawnRule", "stage_spawn_rule", "StageSpawnRuleData", "StageSpawnRuleTable", true),
        new DataTableSchema("MonsterStageScaling", "monster_stage_scaling", "MonsterStageScalingData", "MonsterStageScalingTable", true),

        new DataTableSchema("InLevel", "in_level", "InLevelData", "InLevelTable", true),
        new DataTableSchema("OutLevel", "out_level", "OutLevelData", "OutLevelTable", true),

        new DataTableSchema("ChangeReward", "change_reward", "ChangeRewardData", "ChangeRewardTable", true),
        new DataTableSchema("Achievement", "achievement", "AchievementData", "AchievementDataTable", false),
        new DataTableSchema("Language", "language", "LanguageEntry", "LanguageTable", false),
    };

    private static readonly Dictionary<string, DataTableSchema> SheetMap = BuildSheetMap();
    private static readonly Dictionary<string, DataTableSchema> JsonMap = BuildJsonMap();

    public static bool TryGetBySheetName(string sheetName, out DataTableSchema schema)
    {
        return SheetMap.TryGetValue(NormalizeName(sheetName), out schema);
    }

    public static bool TryGetByJsonName(string jsonName, out DataTableSchema schema)
    {
        return JsonMap.TryGetValue(NormalizeName(jsonName), out schema);
    }

    public static string ToDefaultJsonName(string sheetName)
    {
        string result = Regex.Replace(sheetName.Trim(), @"(?<!^)(?=[A-Z])", "_");
        return result.ToLower().Replace(" ", "_");
    }

    private static Dictionary<string, DataTableSchema> BuildSheetMap()
    {
        var map = new Dictionary<string, DataTableSchema>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Schemas.Count; i++)
            map[NormalizeName(Schemas[i].SheetName)] = Schemas[i];
        return map;
    }

    private static Dictionary<string, DataTableSchema> BuildJsonMap()
    {
        var map = new Dictionary<string, DataTableSchema>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < Schemas.Count; i++)
            map[NormalizeName(Schemas[i].JsonName)] = Schemas[i];
        return map;
    }

    private static string NormalizeName(string value)
    {
        return value.Trim().Replace(".json", "", StringComparison.OrdinalIgnoreCase).Replace("_", "").Replace(" ", "");
    }
}
#endif
