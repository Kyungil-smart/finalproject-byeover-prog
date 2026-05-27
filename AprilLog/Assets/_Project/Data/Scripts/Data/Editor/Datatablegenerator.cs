// 담당자 : 정승우
// 설명   : DataClasses.cs 기반 SO 테이블 파일 자동 생성기
// 수정자 : Codex
// 수정내용 : SO 테이블 생성 경로를 런타임 데이터 폴더로 통일하고 1클래스 1파일 규칙 반영

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DataTableGenerator
{
    // ---------- 경로 설정 ----------

    private const string DATA_CLASSES_PATH = "Assets/_Game/Data/DataClasses.cs";
    private const string OUTPUT_FOLDER = "Assets/_Game/Data/";

    // ---------- 메뉴 ----------

    [MenuItem("Tools/Data/Generate SO Table Files")]
    public static void GenerateAll()
    {
        if (!File.Exists(DATA_CLASSES_PATH))
        {
            Debug.LogError($"[TableGenerator] DataClasses.cs를 찾을 수 없습니다 -> {DATA_CLASSES_PATH}");
            return;
        }

        if (!Directory.Exists(OUTPUT_FOLDER))
            Directory.CreateDirectory(OUTPUT_FOLDER);

        // DataTable<T> 베이스 클래스 생성
        GenerateBaseClass();

        int created = 0;
        int skipped = 0;

        for (int i = 0; i < DataTableSchemaRegistry.Schemas.Count; i++)
        {
            var schema = DataTableSchemaRegistry.Schemas[i];
            string dataClass = schema.DataClassName;
            string tableClass = schema.TableClassName;
            string filePath = Path.Combine(OUTPUT_FOLDER, tableClass + ".cs");

            if (File.Exists(filePath))
            {
                skipped++;
                continue;
            }

            string code =
                "// 담당자 : 김영찬\n" +
                $"// 설명   : {tableClass} ScriptableObject 데이터 테이블\n" +
                "// 수정자 : Codex\n" +
                "// 수정내용 : DataTableGenerator로 생성\n" +
                "\n" +
                "using UnityEngine;\n" +
                "\n" +
                $"[CreateAssetMenu(fileName = \"{tableClass}\", menuName = \"Data/{tableClass}\")]\n" +
                $"public class {tableClass} : DataTable<{dataClass}>\n" +
                "{\n" +
                "}\n";

            File.WriteAllText(filePath, code, new UTF8Encoding(false));
            created++;

            Debug.Log($"[TableGenerator] 생성: {tableClass}.cs");
        }

        AssetDatabase.Refresh();

        Debug.Log($"[TableGenerator] 완료: {created}개 생성, {skipped}개 이미 존재 (스킵).");

        if (created > 0)
        {
            Debug.Log("[TableGenerator] 신규 파일이 생성됐으니 SO를 다시 Import 하세요.");
            Debug.Log("  -> Tools > Data > Import All (JSON -> SO)");
        }
    }

    // ---------- 베이스 클래스 ----------

    private static void GenerateBaseClass()
    {
        string filePath = Path.Combine(OUTPUT_FOLDER, "DataTable.cs");
        if (File.Exists(filePath)) return;

        string code =
            "// 담당자 : 김영찬\n" +
            "// 설명   : ScriptableObject 데이터 테이블 베이스 클래스\n" +
            "// 수정자 : Codex\n" +
            "// 수정내용 : DataTableGenerator로 생성\n" +
            "\n" +
            "using System.Collections.Generic;\n" +
            "using UnityEngine;\n" +
            "\n" +
            "public abstract class DataTable<T> : ScriptableObject\n" +
            "{\n" +
            "    [Header(\"데이터\")]\n" +
            "    public List<T> rows = new List<T>();\n" +
            "}\n";

        File.WriteAllText(filePath, code, new UTF8Encoding(false));
        Debug.Log("[TableGenerator] 생성: DataTable.cs (베이스 클래스)");
    }
}
#endif
