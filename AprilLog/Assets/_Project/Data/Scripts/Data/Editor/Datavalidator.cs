// ========================================
// 담당자 : 정승우
// 설명   : JSON 데이터 무결성 검증 에디터 도구
// ========================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 변환된 JSON 파일들의 PK 중복, FK 참조, 범위, Enum 유효성을 검증한다.
/// 데이터 에러는 런타임에 터지면 원인 찾기가 극도로 어려우므로 변환 시점에 잡는다.
/// </summary>
public static class DataValidator
{
    // ---------- 메뉴 (단독 실행용) ----------

    [MenuItem("Tools/Data/Validate JSON")]
    public static void ValidateFromMenu()
    {
        string jsonFolder = "Assets/Resources/Data/Tables/";
        int errors = ValidateAll(jsonFolder);

        if (errors == 0)
            Debug.Log("[DataValidator] 검증 통과. 에러 없음.");
        else
            Debug.LogError($"[DataValidator] 검증 완료: {errors}개 에러 발견.");
    }

    // ---------- 메인 검증 ----------

    public static int ValidateAll(string jsonFolder)
    {
        if (!Directory.Exists(jsonFolder))
        {
            Debug.LogWarning($"[DataValidator] 폴더가 없습니다 -> {jsonFolder}");
            return 0;
        }

        int errors = 0;

        // PK 세트 로딩
        var characterIds = LoadPKSet(jsonFolder, "character_master", "Character_ID");
        var characterNameIds = LoadPKSet(jsonFolder, "character_name", "CharacterName");

        // 1. PK 중복 검사
        errors += CheckPKDuplicate(jsonFolder, "character_master", "Character_ID");
        errors += CheckPKDuplicate(jsonFolder, "character_name", "CharacterName");
        errors += CheckPKDuplicate(jsonFolder, "common_status", "Character_ID");
        errors += CheckPKDuplicate(jsonFolder, "character_status", "Character_ID");
        errors += CheckPKDuplicate(jsonFolder, "monster_status", "Character_ID");

        // 2. FK 참조 무결성
        errors += CheckFK(jsonFolder, "common_status", "Character_ID",
                          characterIds, "CharacterMaster");
        errors += CheckFK(jsonFolder, "character_status", "Character_ID",
                          characterIds, "CharacterMaster");
        errors += CheckFK(jsonFolder, "monster_status", "Character_ID",
                          characterIds, "CharacterMaster");
        errors += CheckFK(jsonFolder, "character_master", "CharacterName",
                          characterNameIds, "CharacterName");

        // 3. 범위 검증
        errors += CheckRange(jsonFolder, "common_status",
                             "BaseAttackSpeed", 0.01f, 1.0f);
        errors += CheckRange(jsonFolder, "monster_status",
                             "MoveSpeed", 0.01f, 1.0f);
        errors += CheckRange(jsonFolder, "character_status",
                             "CriticalRate", 0f, 1.0f);
        errors += CheckRange(jsonFolder, "character_status",
                             "PercentagePierce", 0f, 1.0f);

        // 4. Enum 검증
        errors += CheckEnum(jsonFolder, "character_master", "CharacterType",
                            new[] { "Main", "Guide", "Monster" });
        errors += CheckEnum(jsonFolder, "monster_status", "MovementPattern",
                            new[] { "Straight", "Zigzag" });

        // 5. Not Null 검증 (필수 컬럼)
        errors += CheckNotNull(jsonFolder, "common_status",
                               new[] { "Character_ID", "MaxHP", "Attack", "BaseAttackSpeed" });
        errors += CheckNotNull(jsonFolder, "monster_status",
                               new[] { "Character_ID", "Defense", "MoveSpeed", "Range", "EXP" });

        return errors;
    }

    // ---------- PK 중복 검사 ----------

    private static int CheckPKDuplicate(string folder, string tableName, string pkField)
    {
        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return 0;

        int errors = 0;
        var seen = new HashSet<string>();

        foreach (var row in rows)
        {
            if (!row.ContainsKey(pkField)) continue;

            string val = row[pkField].ToString();
            if (!seen.Add(val))
            {
                Debug.LogError(
                    $"[DataValidator] PK 중복: {tableName}에 {pkField}={val}이 2개 이상 있습니다.");
                errors++;
            }
        }

        return errors;
    }

    // ---------- FK 참조 검사 ----------

    private static int CheckFK(string folder, string tableName, string fkField,
                                HashSet<string> pkSet, string refTableName)
    {
        if (pkSet == null || pkSet.Count == 0) return 0;

        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return 0;

        int errors = 0;

        foreach (var row in rows)
        {
            if (!row.ContainsKey(fkField)) continue;

            string val = NormalizeId(row[fkField].ToString());
            if (!pkSet.Contains(val))
            {
                Debug.LogError(
                    $"[DataValidator] FK 에러: {tableName}의 {fkField}={val}이 " +
                    $"{refTableName}에 존재하지 않습니다.");
                errors++;
            }
        }

        return errors;
    }

    // ---------- 범위 검사 ----------

    private static int CheckRange(string folder, string tableName,
                                   string field, float min, float max)
    {
        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return 0;

        int errors = 0;

        foreach (var row in rows)
        {
            if (!row.ContainsKey(field)) continue;

            if (float.TryParse(row[field].ToString(), out float val))
            {
                if (val < min || val > max)
                {
                    string id = GetRowId(row);
                    Debug.LogError(
                        $"[DataValidator] 범위 에러: {tableName} {id}의 " +
                        $"{field}={val}이 범위({min}~{max})를 벗어납니다.");
                    errors++;
                }
            }
        }

        return errors;
    }

    // ---------- Enum 검사 ----------

    private static int CheckEnum(string folder, string tableName,
                                  string field, string[] validValues)
    {
        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return 0;

        int errors = 0;
        var validSet = new HashSet<string>(validValues);

        foreach (var row in rows)
        {
            if (!row.ContainsKey(field)) continue;

            string val = row[field].ToString().Trim();
            if (!validSet.Contains(val))
            {
                string id = GetRowId(row);
                Debug.LogError(
                    $"[DataValidator] Enum 에러: {tableName} {id}의 " +
                    $"{field}=\"{val}\"은 [{string.Join(", ", validValues)}] 중 하나여야 합니다.");
                errors++;
            }
        }

        return errors;
    }

    // ---------- Not Null 검사 ----------

    private static int CheckNotNull(string folder, string tableName, string[] fields)
    {
        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return 0;

        int errors = 0;

        foreach (var row in rows)
        {
            foreach (string field in fields)
            {
                if (!row.ContainsKey(field) || row[field] == null ||
                    string.IsNullOrEmpty(row[field].ToString()))
                {
                    string id = GetRowId(row);
                    Debug.LogError(
                        $"[DataValidator] Null 에러: {tableName} {id}의 " +
                        $"{field}가 비어있습니다. (Not Null 제약 위반)");
                    errors++;
                }
            }
        }

        return errors;
    }

    // ---------- 유틸리티 ----------

    private static HashSet<string> LoadPKSet(string folder, string tableName, string pkField)
    {
        var rows = LoadJsonRows(folder, tableName);
        if (rows == null) return new HashSet<string>();

        var set = new HashSet<string>();
        foreach (var row in rows)
        {
            if (row.ContainsKey(pkField))
                set.Add(NormalizeId(row[pkField].ToString()));
        }
        return set;
    }

    private static List<Dictionary<string, object>> LoadJsonRows(string folder, string tableName)
    {
        string path = Path.Combine(folder, tableName + ".json");
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);

        // JsonUtility는 Dictionary를 못 쓰므로 간이 파서 사용
        // "data" 배열 안의 각 오브젝트를 Dictionary로 변환
        return SimpleJsonParser.ParseArray(json);
    }

    /// <summary>
    /// ID 비교 시 소수점 제거 (1.0 -> 1)
    /// </summary>
    private static string NormalizeId(string val)
    {
        if (double.TryParse(val, out double d) && d == Math.Floor(d))
            return ((int)d).ToString();
        return val;
    }

    /// <summary>
    /// 로그에 행을 식별하기 위한 ID 문자열 추출
    /// </summary>
    private static string GetRowId(Dictionary<string, object> row)
    {
        if (row.ContainsKey("Character_ID"))
            return $"Character_ID={row["Character_ID"]}";
        if (row.ContainsKey("CharacterName"))
            return $"CharacterName={row["CharacterName"]}";

        var first = row.FirstOrDefault();
        return $"{first.Key}={first.Value}";
    }
}

// ---------- 간이 JSON 파서 ----------

/// <summary>
/// UnityEngine.JsonUtility가 Dictionary를 지원하지 않으므로,
/// 검증 전용으로 JSON을 Dictionary 리스트로 파싱하는 간이 파서.
/// </summary>
public static class SimpleJsonParser
{
    public static List<Dictionary<string, object>> ParseArray(string json)
    {
        var result = new List<Dictionary<string, object>>();

        // "data" 키 안의 배열을 찾아서 파싱
        int dataStart = json.IndexOf("[");
        int dataEnd = json.LastIndexOf("]");
        if (dataStart < 0 || dataEnd < 0)
            return result;

        string arrayContent = json.Substring(dataStart + 1, dataEnd - dataStart - 1);

        // 각 {} 블록을 파싱
        int depth = 0;
        int objStart = -1;

        for (int i = 0; i < arrayContent.Length; i++)
        {
            char c = arrayContent[i];

            if (c == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && objStart >= 0)
                {
                    string objStr = arrayContent.Substring(objStart, i - objStart + 1);
                    var dict = ParseObject(objStr);
                    if (dict != null)
                        result.Add(dict);
                    objStart = -1;
                }
            }
        }

        return result;
    }

    private static Dictionary<string, object> ParseObject(string obj)
    {
        var dict = new Dictionary<string, object>();

        // 중괄호 제거
        obj = obj.Trim().TrimStart('{').TrimEnd('}').Trim();

        // key:value 쌍 파싱
        int i = 0;
        while (i < obj.Length)
        {
            // 키 찾기
            int keyStart = obj.IndexOf('"', i);
            if (keyStart < 0) break;
            int keyEnd = obj.IndexOf('"', keyStart + 1);
            if (keyEnd < 0) break;

            string key = obj.Substring(keyStart + 1, keyEnd - keyStart - 1);

            // : 찾기
            int colonIdx = obj.IndexOf(':', keyEnd + 1);
            if (colonIdx < 0) break;

            // 값 파싱
            i = colonIdx + 1;
            while (i < obj.Length && obj[i] == ' ') i++;

            object value;
            if (i < obj.Length && obj[i] == '"')
            {
                // 문자열
                int valEnd = obj.IndexOf('"', i + 1);
                value = obj.Substring(i + 1, valEnd - i - 1);
                i = valEnd + 1;
            }
            else if (i < obj.Length && obj[i] == '[')
            {
                // 배열
                int bracketEnd = obj.IndexOf(']', i);
                value = obj.Substring(i, bracketEnd - i + 1);
                i = bracketEnd + 1;
            }
            else if (i < obj.Length && (obj[i] == 't' || obj[i] == 'f'))
            {
                // bool
                if (obj.Substring(i).StartsWith("true"))
                {
                    value = true; i += 4;
                }
                else
                {
                    value = false; i += 5;
                }
            }
            else
            {
                // 숫자
                int numEnd = i;
                while (numEnd < obj.Length && obj[numEnd] != ',' && obj[numEnd] != '}')
                    numEnd++;
                string numStr = obj.Substring(i, numEnd - i).Trim();
                if (double.TryParse(numStr, out double d))
                    value = d;
                else
                    value = numStr;
                i = numEnd;
            }

            dict[key] = value;

            // 다음 쉼표 찾기
            while (i < obj.Length && obj[i] != ',') i++;
            i++;
        }

        return dict;
    }
}
#endif