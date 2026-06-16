// 담당자 : 김영찬
// 설명   : 데이터 자동화 대상 테이블 정의
// 수정자 : 정승우
// 수정내용 : 엑셀 시트명, JSON 파일명, Data 클래스, SO 클래스 정의를 한 곳으로 통합

// 2차 수정자 : 김영찬
// 수정 내용 : 관리중인 리스트와 Legacy 리스트 분리

// 3차 수정자 : 김영찬
// 수정 내용 : CBT 추가 컨텐츠에 적용되는 DB 추가

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
        // Managing List <알파벳 순서로 정렬>
        new DataTableSchema("BattleReward", "battle_reward", "BattleRewardData", "BattleRewardTable", true),
        
        new DataTableSchema("ChangeReward", "change_reward", "ChangeRewardData", "ChangeRewardTable", true),
        new DataTableSchema("ChapterMaster", "chapter_master", "ChapterData", "ChapterTable", true),
        new DataTableSchema("CharacterMaster", "character_master", "CharacterMasterData", "CharacterMasterTable", true),
        new DataTableSchema("CharacterStatus", "character_status", "CharacterStatusData", "CharacterStatusTable", true),
        new DataTableSchema("CommonStatus", "common_status", "CommonStatusData", "CommonStatusTable", true),
        
        new DataTableSchema("EffectTable", "effect_table", "EffectTableData", "EffectMasterTable", true),
        
        new DataTableSchema("FreeGachaBox", "free_gacha_box", "FreeGachaBoxData", "FreeGachaBoxTable", true),
        
        new DataTableSchema("GachaBox", "gacha_box", "GachaBoxData", "GachaBoxTable", true),
        new DataTableSchema("GearMaster", "gear_master", "GearMasterData", "GearMasterTable", true),
        new DataTableSchema("GearGrade", "gear_grade", "GearGradeData", "GearGradeTable", true),
        new DataTableSchema("GearLevel", "gear_level", "GearLevelData", "GearLevelTable", true),
        new DataTableSchema("GearUpgradeCost", "gear_upgrade_cost", "GearUpgradeCostData", "GearUpgradeCostTable", true),
        new DataTableSchema("GearSpecialEffect", "gear_special_effect", "GearSpecialEffectData", "GearSpecialEffectTable", true),
        new DataTableSchema("GearDismantle", "gear_dismantle", "GearDismantleData", "GearDismantleTable", true),
        new DataTableSchema("GearAscensionCost", "gear_ascension_cost", "GearAscensionCostData", "GearAscensionCostTable", true),
        new DataTableSchema("GachaReward", "gacha_reward", "GachaRewardData", "GachaRewardTable", true),
        
        new DataTableSchema("InLevel", "in_level", "InLevelData", "InLevelTable", true),
        
        new DataTableSchema("LegendaryShardExchange", "legendary_shard_exchange", "LegendaryShardExchangeData", "LegendaryShardExchangeTable", true),
        
        new DataTableSchema("MonsterPool", "monster_pool", "MonsterPoolData", "MonsterPoolTable", true),
        new DataTableSchema("MonsterStageScaling", "monster_stage_scaling", "MonsterStageScalingData", "MonsterStageScalingTable", true),
        new DataTableSchema("MonsterStatus", "monster_status", "MonsterStatusData", "MonsterStatusTable", true),
        new DataTableSchema("MonsterWavePool", "monster_wave_pool", "MonsterWavePoolData", "MonsterPoolMasterTable", true),

        new DataTableSchema("OutLevel", "out_level", "OutLevelData", "OutLevelTable", true),
        
        new DataTableSchema("PaidGachaBox", "paid_gacha_box", "PaidGachaBoxData", "PaidGachaBoxTable", true),
        
        new DataTableSchema("SkillTable", "skill_table", "SkillTableData", "SkillEnchantTable", true),
        new DataTableSchema("SpecialWaveRule", "special_wave_rule", "SpecialWaveRuleData", "SpecialWaveRuleTable", true),
        new DataTableSchema("StageMaster", "stage_master", "StageData", "StageDataTable", true),
        new DataTableSchema("StageWaveRule", "stage_wave_rule", "StageWaveRuleData", "StageWaveRuleTable", true),
        new DataTableSchema("Stamina", "stamina", "StaminaData", "StaminaTable", true),
        new DataTableSchema("StatTable", "stat_table", "StatTableData", "StatEnchantTable", true),
        
        new DataTableSchema("UnitTable", "unit_table", "UnitTableData", "UnitMasterTable", true),
        
        // Legacy List
        new DataTableSchema("SkillMaster", "skill_master", "Legacy_SkillMasterData", "Legacy_SkillMasterTable", false),
        new DataTableSchema("SkillData", "skill_data", "Legacy_SkillData", "Legacy_SkillDataTable", false),
        new DataTableSchema("EffectTable", "effect_table", "Legacy_EffectData", "Legacy_EffectDataTable", false),

        new DataTableSchema("EnchantMaster", "enchant_master", "Legacy_EnchantMasterData", "Legacy_EnchantMasterTable", false),
        new DataTableSchema("EnchantLevel", "enchant_level", "Legacy_EnchantLevelData", "Legacy_EnchantLevelTable", false),
        new DataTableSchema("EnchantWeight", "enchant_weight", "Legacy_EnchantWeightData", "Legacy_EnchantWeightTable", false),
        
        new DataTableSchema("MapLanguage", "map_language", "Legacy_MapLanguageData", "Legacy_MapLanguageTable", false),
        
        new DataTableSchema("Achievement", "achievement", "Legacy_AchievementData", "Legacy_AchievementDataTable", false),
        new DataTableSchema("Language", "language", "Legacy_LanguageEntry", "Legacy_LanguageTable", false),
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
