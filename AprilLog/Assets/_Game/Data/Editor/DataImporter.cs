// 담당자 : 정승우
// 설명   : 데이터 자동화 도구 - JSON -> SO 변환 + FK/PK 검증

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JSON 파일을 읽어서 SO 에셋으로 자동 변환하고 FK/PK를 검증한다.
/// Unity 메뉴: Tools > Data > Import All
///
/// 워크플로우:
/// 1. 기획자가 Google Sheet -> xlsx 내보내기 (또는 직접 JSON 작성)
/// 2. xlsx -> JSON 변환 (ExcelToJsonConverter로 별도 처리)
/// 3. 이 도구가 JSON -> SO 에셋 자동 생성/갱신
///
/// JSON 파일 위치: Assets/_Project/Data/JSON/
/// SO 에셋 위치: Assets/_Project/Data/SO/
/// </summary>
public static class DataImporter
{
    private const string JSON_FOLDER = "Assets/_Project/Data/JSON";
    private const string SO_FOLDER = "Assets/_Project/Data/SO";

    [MenuItem("Tools/Data/Import All (JSON -> SO)")]
    public static void ImportAll()
    {
        if (!Directory.Exists(JSON_FOLDER))
        {
            Directory.CreateDirectory(JSON_FOLDER);
            Debug.LogWarning($"[DataImporter] {JSON_FOLDER} 폴더가 없어서 생성함. JSON 파일을 넣어주세요.");
            return;
        }

        if (!Directory.Exists(SO_FOLDER))
        {
            Directory.CreateDirectory(SO_FOLDER);
        }

        int totalRows = 0;
        int errors = 0;

        // 1. 검증
        errors = DataValidator.ValidateAll(JSON_FOLDER);
        if (errors > 0)
        {
            Debug.LogError($"[DataImporter] 검증 에러 {errors}개. SO 생성 안 함. 에러 먼저 수정하세요.");
            return;
        }

        // 2. JSON -> SO 변환
        totalRows += ImportTable<CommonStatusTable, CommonStatusData>("common_status");
        totalRows += ImportTable<CharacterMasterTable, CharacterMasterData>("character_master");
        totalRows += ImportTable<CharacterStatusTable, CharacterStatusData>("character_status");
        totalRows += ImportTable<MonsterStatusTable, MonsterStatusData>("monster_status");

        totalRows += ImportTable<SkillMasterTable, SkillMasterData>("skill_master");
        totalRows += ImportTable<SkillDataTable, SkillData>("skill_data");
        totalRows += ImportTable<EffectDataTable, EffectData>("effect_table");

        totalRows += ImportTable<EnchantMasterTable, EnchantMasterData>("enchant_master");
        totalRows += ImportTable<EnchantLevelTable, EnchantLevelData>("enchant_level");
        totalRows += ImportTable<EnchantWeightTable, EnchantWeightData>("enchant_weight");

        totalRows += ImportTable<ChapterTable, ChapterData>("chapter_master");
        totalRows += ImportTable<StageDataTable, StageData>("stage_master");
        totalRows += ImportTable<StageMonsterTable, StageMonsterData>("stage_monster");
        totalRows += ImportTable<MonsterScalingTable, MonsterScalingData>("monster_scaling");

        totalRows += ImportTable<InLevelTable, InLevelData>("in_level");
        totalRows += ImportTable<OutGrowthDataTable, OutGrowthData>("out_growth");

        totalRows += ImportTable<AchievementDataTable, AchievementData>("achievement");
        totalRows += ImportTable<ChangeRewardTable, ChangeRewardData>("change_reward");
        totalRows += ImportTable<LanguageTable, LanguageEntry>("language");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DataImporter] 완료! 총 {totalRows}행 처리. 에러 0개.");
    }

    // 제네릭 SO 생성/갱신
    private static int ImportTable<TSO, TData>(string jsonFileName)
        where TSO : DataTable<TData>
        where TData : class
    {
        // 추가 : 홍정옥
        // 내용 : JSON 루트 폴더만 보지 않고 하위 폴더까지 검색하도록
        string jsonPath = FindJsonPathRecursive(jsonFileName);

        if (string.IsNullOrEmpty(jsonPath))
        {
            // 파일 없으면 조용히 건너뜀 (아직 안 만든 테이블일 수 있음)
            return 0;
        }

        string jsonText = File.ReadAllText(jsonPath);
        var wrapper = JsonUtility.FromJson<DataArray<TData>>(jsonText);

        if (wrapper == null || wrapper.data == null)
        {
            Debug.LogError($"[DataImporter] {jsonPath} 파싱 실패. JSON 형식 확인.");
            return 0;
        }

        string soTypeName = typeof(TSO).Name;

        // 추가 : 홍정옥
        // 내용 : JSON 파일이 위치한 하위 폴더 구조를 기준으로 SO도 같은 하위 폴더에 생성
        string soPath = GetSoPathByJsonPath(jsonPath, soTypeName + ".asset");

        var so = AssetDatabase.LoadAssetAtPath<TSO>(soPath);

        if (so == null)
        {
            so = ScriptableObject.CreateInstance<TSO>();
            AssetDatabase.CreateAsset(so, soPath);
            Debug.Log($"[DataImporter] {soPath} 신규 생성");
        }

        so.rows = new List<TData>(wrapper.data);
        EditorUtility.SetDirty(so);

        Debug.Log($"[DataImporter] {soTypeName}: {wrapper.data.Length}행 갱신 ({jsonPath})");
        return wrapper.data.Length;
    }

    // 추가 : 홍정옥
    // 내용 : JSON 루트 폴더뿐 아니라 하위 폴더까지 검색해서 지정한 JSON 파일을 찾는다.
    // 예시 : Assets/_Project/Data/JSON/Character/common_status.json 검색 가능
    private static string FindJsonPathRecursive(string fileNameWithoutExtension)
    {
        string targetFileName = fileNameWithoutExtension + ".json";

        if (!Directory.Exists(JSON_FOLDER))
        {
            return null;
        }

        string[] matches = Directory.GetFiles(
            JSON_FOLDER,
            targetFileName,
            SearchOption.AllDirectories
        );

        if (matches == null || matches.Length == 0)
        {
            return null;
        }

        if (matches.Length > 1)
        {
            Debug.LogError(
                $"[DataImporter] JSON 파일명 중복: {targetFileName}\n" +
                string.Join("\n", matches) +
                "\n같은 테이블 JSON 파일은 프로젝트 전체에서 하나만 유지해야 합니다."
            );

            return null;
        }

        return matches[0].Replace("\\", "/");
    }

    // 추가 : 홍정옥
    // 내용 : JSON 파일이 들어있는 하위 폴더 구조를 SO 폴더에도 동일하게 반영
    private static string GetSoPathByJsonPath(string jsonPath, string soFileName)
    {
        string normalizedJsonRoot = JSON_FOLDER.Replace("\\", "/").TrimEnd('/');
        string normalizedJsonPath = jsonPath.Replace("\\", "/");

        string jsonDirectory = Path.GetDirectoryName(normalizedJsonPath);
        jsonDirectory = jsonDirectory.Replace("\\", "/");

        string relativeFolder = "";

        if (jsonDirectory.StartsWith(normalizedJsonRoot, StringComparison.Ordinal))
        {
            relativeFolder = jsonDirectory.Substring(normalizedJsonRoot.Length).TrimStart('/');
        }

        string soFolder = string.IsNullOrEmpty(relativeFolder)
            ? SO_FOLDER
            : Path.Combine(SO_FOLDER, relativeFolder).Replace("\\", "/");

        if (!Directory.Exists(soFolder))
        {
            Directory.CreateDirectory(soFolder);
            AssetDatabase.Refresh();
        }

        return Path.Combine(soFolder, soFileName).Replace("\\", "/");
    }
}

/// <summary>
/// JSON 데이터의 FK 참조, PK 중복, 범위, Enum 유효성을 검증한다.
/// DataImporter가 SO를 만들기 전에 이걸 먼저 돌려서 에러를 걸러냄.
/// </summary>
public static class DataValidator
{
    public static int ValidateAll(string jsonFolder)
    {
        int errors = 0;

        // PK 가져오기 (다른 테이블이 참조하는 ID 모음)
        var characterIds = LoadPKs<CharacterMasterData>(jsonFolder, "character_master", d => d.Character_ID);
        var skillMasterIds = LoadPKs<SkillMasterData>(jsonFolder, "skill_master", d => d.StandardID);
        var effectIds = LoadPKs<EffectData>(jsonFolder, "effect_table", d => d.EffectID);
        var stageIds = LoadPKs<StageData>(jsonFolder, "stage_master", d => d.StageID);
        var enchantIds = LoadPKs<EnchantMasterData>(jsonFolder, "enchant_master", d => d.EnchantID);

        // FK 참조 검증 -- 이게 핵심. ID가 잘못되면 런타임에 터짐.
        errors += CheckFK<CommonStatusData>(
            jsonFolder,
            "common_status",
            d => d.Character_ID,
            characterIds,
            "CharacterMaster"
        );

        errors += CheckFK<CharacterStatusData>(
            jsonFolder,
            "character_status",
            d => d.Character_ID,
            characterIds,
            "CharacterMaster"
        );

        // 범위 검증
        errors += CheckRange<CharacterStatusData>(
            jsonFolder,
            "character_status",
            d => d.CriticalRate,
            0f,
            1f,
            "CriticalRate"
        );

        errors += CheckRange<CharacterStatusData>(
            jsonFolder,
            "character_status",
            d => d.PercentagePierce,
            0f,
            1f,
            "PercentagePierce"
        );

        if (errors == 0)
        {
            Debug.Log("[DataValidator] 검증 통과. 에러 없음.");
        }

        return errors;
    }

    // PK 목록 가져오기
    private static HashSet<int> LoadPKs<T>(string folder, string fileName, Func<T, int> keySelector)
    {
        var set = new HashSet<int>();

        // 추가 : 홍정옥
        // 내용 : PK 검증 시 JSON 하위 폴더까지 검색
        string path = FindJsonPathRecursive(folder, fileName);

        if (string.IsNullOrEmpty(path))
        {
            return set;
        }

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null)
        {
            return set;
        }

        for (int i = 0; i < wrapper.data.Length; i++)
        {
            int key = keySelector(wrapper.data[i]);

            if (!set.Add(key))
            {
                Debug.LogError($"[Validator] PK 중복: {fileName}에 ID={key}가 2개 이상");
            }
        }

        return set;
    }

    // FK 참조 검증
    private static int CheckFK<T>(
        string folder,
        string fileName,
        Func<T, int> fkSelector,
        HashSet<int> validPKs,
        string refTableName
    )
    {
        int errors = 0;

        // 추가 : 홍정옥
        // 내용 : FK 검증 시 JSON 하위 폴더까지 검색
        string path = FindJsonPathRecursive(folder, fileName);

        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null)
        {
            return 0;
        }

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

    // 범위 검증
    private static int CheckRange<T>(
        string folder,
        string fileName,
        Func<T, float> valueSelector,
        float min,
        float max,
        string fieldName
    )
    {
        int errors = 0;

        // 추가 : 홍정옥
        // 내용 : 범위 검증 시 JSON 하위 폴더까지 검색
        string path = FindJsonPathRecursive(folder, fileName);

        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        var wrapper = JsonUtility.FromJson<DataArray<T>>(File.ReadAllText(path));
        if (wrapper?.data == null)
        {
            return 0;
        }

        for (int i = 0; i < wrapper.data.Length; i++)
        {
            float val = valueSelector(wrapper.data[i]);

            if (val < min || val > max)
            {
                Debug.LogError($"[Validator] 범위 에러: {fileName}[{i}]의 {fieldName}={val}이 {min}~{max} 밖");
                errors++;
            }
        }

        return errors;
    }

    // 추가 : 홍정옥
    // 내용 : DataValidator에서도 JSON 하위 폴더까지 검색해서 검증할 파일을 찾음
    private static string FindJsonPathRecursive(string folder, string fileNameWithoutExtension)
    {
        string targetFileName = fileNameWithoutExtension + ".json";

        if (!Directory.Exists(folder))
        {
            return null;
        }

        string[] matches = Directory.GetFiles(
            folder,
            targetFileName,
            SearchOption.AllDirectories
        );

        if (matches == null || matches.Length == 0)
        {
            return null;
        }

        if (matches.Length > 1)
        {
            Debug.LogError(
                $"[Validator] JSON 파일명 중복: {targetFileName}\n" +
                string.Join("\n", matches) +
                "\n같은 테이블 JSON 파일은 하나만 유지해야 합니다."
            );

            return null;
        }

        return matches[0].Replace("\\", "/");
    }
}

/// <summary>
/// xlsx -> JSON 변환 도구.
///
/// EPPlus 라이브러리 설치 방법:
/// 1. NuGet에서 EPPlus 다운로드 (MIT 라이선스 버전)
/// 2. EPPlus.dll을 Assets/Plugins/Editor/ 에 넣기
/// 3. 아래 코드의 주석을 해제
///
/// EPPlus 없으면 이 기능은 쓸 수 없음.
/// 대안: 기획자가 Google Sheet에서 직접 JSON 형식으로 내보내기.
/// </summary>
public static class ExcelToJsonConverter
{
    [MenuItem("Tools/Data/Convert Excel to JSON")]
    public static void ConvertAll()
    {
        Debug.Log("[ExcelToJson] EPPlus 미설치. Assets/Plugins/Editor/에 EPPlus.dll 넣고 주석 해제 필요.");
        Debug.Log("[ExcelToJson] 당장은 JSON 파일을 직접 만들어서 Data/JSON/ 에 넣으면 됨.");
    }
}

#endif