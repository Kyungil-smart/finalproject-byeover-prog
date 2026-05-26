// 담당자 : 정승우
// 설명   : 데이터 자동화 도구 - JSON -> SO 변환 + FK/PK 검증

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DataImporter
{
    private const string JSON_FOLDER = "Assets/_Project/Data/JSON";
    private const string SO_FOLDER = "Assets/_Project/Data/SO";

    [MenuItem("Tools/Data/경로 안내")]
    public static void ShowPaths()
    {
        Debug.Log("===========================================");
        Debug.Log("[DataImporter] 경로 안내");
        Debug.Log($"  JSON 입력 폴더 : {Path.GetFullPath(JSON_FOLDER)}");
        Debug.Log($"  SO 출력 폴더   : {Path.GetFullPath(SO_FOLDER)}");
        Debug.Log("  JSON 파일을 입력 폴더에 넣고 'Import All' 실행하세요.");
        Debug.Log("===========================================");
    }

    [MenuItem("Tools/Data/JSON 폴더 열기")]
    public static void OpenJsonFolder()
    {
        if (!Directory.Exists(JSON_FOLDER))
            Directory.CreateDirectory(JSON_FOLDER);
        EditorUtility.RevealInFinder(JSON_FOLDER);
    }

    [MenuItem("Tools/Data/SO 폴더 열기")]
    public static void OpenSOFolder()
    {
        if (!Directory.Exists(SO_FOLDER))
            Directory.CreateDirectory(SO_FOLDER);
        EditorUtility.RevealInFinder(SO_FOLDER);
    }

    [MenuItem("Tools/Data/Import All (JSON -> SO)")]
    public static void ImportAll()
    {
        Debug.Log("===========================================");
        Debug.Log("[DataImporter] Import 시작");
        Debug.Log($"  JSON 입력 : {Path.GetFullPath(JSON_FOLDER)}");
        Debug.Log($"  SO 출력   : {Path.GetFullPath(SO_FOLDER)}");
        Debug.Log("===========================================");

        if (!Directory.Exists(JSON_FOLDER))
        {
            Directory.CreateDirectory(JSON_FOLDER);
            Debug.LogWarning($"[DataImporter] JSON 폴더가 없어서 생성함. 여기에 JSON 넣으세요:\n  {Path.GetFullPath(JSON_FOLDER)}");
            return;
        }

        // 폴더 안에 있는 JSON 파일 목록 출력
        var jsonFiles = Directory.GetFiles(JSON_FOLDER, "*.json");
        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning($"[DataImporter] JSON 파일이 0개입니다. 여기에 넣으세요:\n  {Path.GetFullPath(JSON_FOLDER)}");
            return;
        }

        Debug.Log($"[DataImporter] 발견된 JSON 파일 {jsonFiles.Length}개:");
        for (int i = 0; i < jsonFiles.Length; i++)
            Debug.Log($"  - {Path.GetFileName(jsonFiles[i])}");

        if (!Directory.Exists(SO_FOLDER))
            Directory.CreateDirectory(SO_FOLDER);

        int totalRows = 0;
        int errors = DataValidator.ValidateAll(JSON_FOLDER);
        if (errors > 0)
        {
            Debug.LogError($"[DataImporter] 검증 에러 {errors}개. SO 생성 안 함.");
            return;
        }

        // 캐릭터
        totalRows += ImportTable<CharacterMasterTable, CharacterMasterData>("character_master");
        totalRows += ImportTable<CharacterNameTable, CharacterNameData>("character_name");
        totalRows += ImportTable<CommonStatusTable, CommonStatusData>("common_status");
        totalRows += ImportTable<CharacterStatusTable, CharacterStatusData>("character_status");
        totalRows += ImportTable<MonsterStatusTable, MonsterStatusData>("monster_status");

        // 스킬
        totalRows += ImportTable<SkillMasterTable, SkillMasterData>("skill_master");
        totalRows += ImportTable<SkillDataTable, SkillData>("skill_data");
        totalRows += ImportTable<EffectDataTable, EffectData>("effect_table");

        // 인챈트
        totalRows += ImportTable<EnchantMasterTable, EnchantMasterData>("enchant_master");
        totalRows += ImportTable<EnchantLevelTable, EnchantLevelData>("enchant_level");
        totalRows += ImportTable<EnchantWeightTable, EnchantWeightData>("enchant_weight");

        // 챕터 / 스테이지
        totalRows += ImportTable<ChapterTable, ChapterData>("chapter_master");
        totalRows += ImportTable<MapLanguageTable, MapLanguageData>("map_language");
        totalRows += ImportTable<StageDataTable, StageData>("stage_master");

        // 몬스터 풀 + 스폰
        totalRows += ImportTable<MonsterPoolMasterTable, MonsterPoolMasterData>("monster_pool_master");
        totalRows += ImportTable<MonsterPoolTable, MonsterPoolData>("monster_pool");
        totalRows += ImportTable<StageSpawnRuleTable, StageSpawnRuleData>("stage_spawn_rule");
        totalRows += ImportTable<MonsterStageScalingTable, MonsterStageScalingData>("monster_stage_scaling");

        // 레벨
        totalRows += ImportTable<InLevelTable, InLevelData>("in_level");
        totalRows += ImportTable<OutLevelTable, OutLevelData>("out_level");

        // 보상/업적/언어
        totalRows += ImportTable<ChangeRewardTable, ChangeRewardData>("change_reward");
        totalRows += ImportTable<AchievementDataTable, AchievementData>("achievement");
        totalRows += ImportTable<LanguageTable, LanguageEntry>("language");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("===========================================");
        Debug.Log($"[DataImporter] 완료! 총 {totalRows}행 처리.");
        Debug.Log($"  생성된 SO 위치 : {Path.GetFullPath(SO_FOLDER)}");
        Debug.Log("===========================================");
    }

    private static int ImportTable<TSO, TData>(string jsonFileName)
        where TSO : DataTable<TData>
        where TData : class
    {
        string jsonPath = Path.Combine(JSON_FOLDER, jsonFileName + ".json");
        if (!File.Exists(jsonPath)) return 0;

        string jsonText = File.ReadAllText(jsonPath);
        var wrapper = JsonUtility.FromJson<DataArray<TData>>(jsonText);

        if (wrapper == null || wrapper.data == null)
        {
            Debug.LogError($"[DataImporter] 파싱 실패: {Path.GetFullPath(jsonPath)}");
            return 0;
        }

        string soTypeName = typeof(TSO).Name;
        string soPath = Path.Combine(SO_FOLDER, soTypeName + ".asset");
        var so = AssetDatabase.LoadAssetAtPath<TSO>(soPath);

        if (so == null)
        {
            so = ScriptableObject.CreateInstance<TSO>();
            AssetDatabase.CreateAsset(so, soPath);
            Debug.Log($"  [신규] {soPath}");
        }

        so.rows = new List<TData>(wrapper.data);
        EditorUtility.SetDirty(so);

        Debug.Log($"  {jsonFileName}.json -> {soPath} ({wrapper.data.Length}행)");
        return wrapper.data.Length;
    }
}

public static class DataValidator
{
    public static int ValidateAll(string jsonFolder)
    {
        int errors = 0;

        var characterIds = LoadPKs<CharacterMasterData>(jsonFolder, "character_master", d => d.Character_ID);
        var stageIds = LoadPKs<StageData>(jsonFolder, "stage_master", d => d.Stage_ID);

        errors += CheckFK<CommonStatusData>(jsonFolder, "common_status",
            d => d.Character_ID, characterIds, "CharacterMaster");
        errors += CheckFK<CharacterStatusData>(jsonFolder, "character_status",
            d => d.Character_ID, characterIds, "CharacterMaster");
        errors += CheckFK<MonsterStatusData>(jsonFolder, "monster_status",
            d => d.Character_ID, characterIds, "CharacterMaster");

        errors += CheckRange<CharacterStatusData>(jsonFolder, "character_status",
            d => d.CriticalRate, 0f, 1f, "CriticalRate");
        errors += CheckRange<CharacterStatusData>(jsonFolder, "character_status",
            d => d.PercentagePierce, 0f, 1f, "PercentagePierce");

        if (errors == 0)
            Debug.Log("[DataValidator] 검증 통과.");

        return errors;
    }

    private static HashSet<int> LoadPKs<T>(string folder, string fileName, Func<T, int> keySelector)
    {
        var set = new HashSet<int>();
        string path = Path.Combine(folder, fileName + ".json");
        if (!File.Exists(path)) return set;

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null) return set;

        for (int i = 0; i < wrapper.data.Length; i++)
        {
            int key = keySelector(wrapper.data[i]);
            if (!set.Add(key))
                Debug.LogError($"[Validator] PK 중복: {fileName}에 ID={key}가 2개 이상");
        }
        return set;
    }

    private static int CheckFK<T>(string folder, string fileName,
        Func<T, int> fkSelector, HashSet<int> validPKs, string refTableName)
    {
        int errors = 0;
        string path = Path.Combine(folder, fileName + ".json");
        if (!File.Exists(path)) return 0;

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null) return 0;

        for (int i = 0; i < wrapper.data.Length; i++)
        {
            int fk = fkSelector(wrapper.data[i]);
            if (fk != 0 && !validPKs.Contains(fk))
            {
                Debug.LogError($"[Validator] FK 에러: {fileName}[{i}]의 ID={fk}가 {refTableName}에 없음");
                errors++;
            }
        }
        return errors;
    }

    private static int CheckRange<T>(string folder, string fileName,
        Func<T, float> valueSelector, float min, float max, string fieldName)
    {
        int errors = 0;
        string path = Path.Combine(folder, fileName + ".json");
        if (!File.Exists(path)) return 0;

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null) return 0;

        for (int i = 0; i < wrapper.data.Length; i++)
        {
            float val = valueSelector(wrapper.data[i]);
            if (val < min || val > max)
            {
                Debug.LogError($"[Validator] 범위 에러: {fileName}[{i}]의 {fieldName}={val}");
                errors++;
            }
        }
        return errors;
    }
}

public static class ExcelToJsonConverter
{
    [MenuItem("Tools/Data/Convert Excel to JSON")]
    public static void ConvertAll()
    {
        Debug.Log("[ExcelToJson] EPPlus 미설치. JSON 파일을 직접 만들어서 Data/JSON/에 넣으세요.");
    }
}
#endif