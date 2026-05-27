// 담당자 : 정승우
// 설명   : 데이터 자동화 도구 - JSON -> SO 변환 + FK/PK 검증
// 수정자 : Codex
// 수정내용 : JSON 누락 경고와 기존 JSON 폴더 fallback을 추가

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DataImporter
{
    private const string JSON_FOLDER = "Assets/Resources/Data/Tables";
    private const string LEGACY_JSON_FOLDER = "Assets/_Project/Data/JSON";
    private const string SO_FOLDER = "Assets/_Project/Data/SO";

    [MenuItem("Tools/Data/경로 안내")]
    public static void ShowPaths()
    {
        Debug.Log("===========================================");
        Debug.Log("[DataImporter] 경로 안내");
        Debug.Log($"  JSON 입력 폴더 : {Path.GetFullPath(JSON_FOLDER)}");
        Debug.Log($"  기존 JSON 폴더 : {Path.GetFullPath(LEGACY_JSON_FOLDER)}");
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

        bool hasPrimaryFolder = Directory.Exists(JSON_FOLDER);
        bool hasLegacyFolder = Directory.Exists(LEGACY_JSON_FOLDER);

        if (!hasPrimaryFolder && !hasLegacyFolder)
        {
            Directory.CreateDirectory(JSON_FOLDER);
            Debug.LogWarning($"[DataImporter] JSON 폴더가 없어서 생성함. 여기에 JSON 넣으세요:\n  {Path.GetFullPath(JSON_FOLDER)}");
            return;
        }

        var jsonFiles = CollectJsonFiles();
        if (jsonFiles.Count == 0)
        {
            Debug.LogWarning(
                $"[DataImporter] JSON 파일이 0개입니다.\n" +
                $"  기본 폴더: {Path.GetFullPath(JSON_FOLDER)}\n" +
                $"  기존 폴더: {Path.GetFullPath(LEGACY_JSON_FOLDER)}");
            return;
        }

        Debug.Log($"[DataImporter] 발견된 JSON 파일 {jsonFiles.Count}개:");
        for (int i = 0; i < jsonFiles.Count; i++)
            Debug.Log($"  - {jsonFiles[i]}");

        if (!Directory.Exists(SO_FOLDER))
            Directory.CreateDirectory(SO_FOLDER);
        AssetDatabase.Refresh();

        int totalRows = 0;
        int errors = 0;
        if (Directory.Exists(JSON_FOLDER))
            errors += DataValidator.ValidateAll(JSON_FOLDER);
        if (Directory.Exists(LEGACY_JSON_FOLDER))
            errors += DataValidator.ValidateAll(LEGACY_JSON_FOLDER);

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
        string jsonPath = ResolveJsonPath(jsonFileName, out bool isLegacy);
        if (string.IsNullOrEmpty(jsonPath))
        {
            Debug.LogWarning($"[DataImporter] JSON 없음: {jsonFileName}.json");
            return 0;
        }

        if (isLegacy)
            Debug.LogWarning($"[DataImporter] 기존 JSON 폴더에서 가져옴: {jsonFileName}.json");

        string jsonText = File.ReadAllText(jsonPath);
        var wrapper = JsonUtility.FromJson<DataArray<TData>>(jsonText);

        if (wrapper == null || wrapper.data == null)
        {
            Debug.LogError($"[DataImporter] 파싱 실패: {Path.GetFullPath(jsonPath)}");
            return 0;
        }

        string soTypeName = typeof(TSO).Name;
        string soPath = CombineUnityPath(SO_FOLDER, soTypeName + ".asset");
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

    private static List<string> CollectJsonFiles()
    {
        var files = new List<string>();
        AddJsonFiles(files, JSON_FOLDER, "기본");
        AddJsonFiles(files, LEGACY_JSON_FOLDER, "기존");
        return files;
    }

    private static void AddJsonFiles(List<string> files, string folder, string label)
    {
        if (!Directory.Exists(folder)) return;

        string[] paths = Directory.GetFiles(folder, "*.json");
        for (int i = 0; i < paths.Length; i++)
            files.Add($"{label}: {Path.GetFileName(paths[i])}");
    }

    private static string ResolveJsonPath(string jsonFileName, out bool isLegacy)
    {
        isLegacy = false;

        string primaryPath = CombineUnityPath(JSON_FOLDER, jsonFileName + ".json");
        if (File.Exists(primaryPath))
            return primaryPath;

        string legacyPath = CombineUnityPath(LEGACY_JSON_FOLDER, jsonFileName + ".json");
        if (File.Exists(legacyPath))
        {
            isLegacy = true;
            return legacyPath;
        }

        return null;
    }

    private static string CombineUnityPath(string folder, string fileName)
    {
        return Path.Combine(folder, fileName).Replace("\\", "/");
    }
}

#endif
