using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬의 LocalizedTextBinder 중 현재 로컬라이징 테이블에 ID가 없는(=[id]로 깨지는) 것을 찾아
// 전체 하이라키 경로/ID/타입/오브젝트 텍스트와 함께 콘솔에 출력한다.
public static class LocalizationBinderAudit
{
    [MenuItem("Tools/Localization/깨진 바인더 감사(테이블에 없는 ID)")]
    private static void AuditBrokenBinders()
    {
        Dictionary<LocalizingType, HashSet<int>> known = BuildKnownIds();

        var sb = new StringBuilder();
        int total = 0, broken = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (LocalizedTextBinder binder in root.GetComponentsInChildren<LocalizedTextBinder>(true))
                {
                    total++;
                    var so = new SerializedObject(binder);
                    int id = so.FindProperty("_id").intValue;
                    var type = (LocalizingType)so.FindProperty("_type").enumValueIndex;

                    bool exists = known.TryGetValue(type, out HashSet<int> set) && set.Contains(id);
                    if (exists) continue;

                    broken++;
                    var tmp = binder.GetComponent<TMP_Text>();
                    string curText = tmp != null ? tmp.text.Replace("\n", "\\n") : "(no TMP)";
                    sb.AppendLine($"[{type} {id}] path={GetPath(binder.transform)} | text=\"{curText}\"");
                }
            }
        }

        Debug.Log($"[BinderAudit] 총 바인더 {total}개 중 테이블에 없는(깨진) {broken}개:\n{sb}");
    }

    // 테이블에 ID가 없는(=[id]로 깨지는) 바인더 컴포넌트를 씬에서 제거한다.
    // 잘못 붙은 바인더(되돌린 항목/코드관리 텍스트)를 정리할 때 사용. Undo 가능.
    [MenuItem("Tools/Localization/테이블에 없는 ID 바인더 제거")]
    private static void RemoveBrokenBinders()
    {
        Dictionary<LocalizingType, HashSet<int>> known = BuildKnownIds();
        var targets = new List<(LocalizedTextBinder binder, string info)>();

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (LocalizedTextBinder binder in root.GetComponentsInChildren<LocalizedTextBinder>(true))
                {
                    var so = new SerializedObject(binder);
                    int id = so.FindProperty("_id").intValue;
                    var type = (LocalizingType)so.FindProperty("_type").enumValueIndex;
                    bool exists = known.TryGetValue(type, out HashSet<int> set) && set.Contains(id);
                    if (!exists)
                        targets.Add((binder, $"[{type} {id}] {GetPath(binder.transform)}"));
                }
            }
        }

        if (targets.Count == 0)
        {
            Debug.Log("[BinderRemove] 제거할(테이블에 없는 ID) 바인더가 없습니다.");
            return;
        }

        var sb = new StringBuilder();
        foreach (var (binder, info) in targets)
        {
            sb.AppendLine(info);
            Undo.DestroyObjectImmediate(binder);
        }
        Debug.Log($"[BinderRemove] 테이블에 없는 ID 바인더 {targets.Count}개 제거(Undo 가능):\n{sb}");
    }

    // 바인더가 없고, 테이블에도 없는 한글 정적 텍스트를 전체 경로와 함께 리포트한다.
    // 각 항목이 코드로 바뀌는 동적 필드로 의심되면 [동적?] 태그를 붙인다(바인딩 금지 후보).
    [MenuItem("Tools/Localization/미바인딩 한글 텍스트 리포트(경로+동적추정)")]
    private static void ReportUnboundKoreanTexts()
    {
        HashSet<string> uiKr = BuildUiKrSet();

        var sb = new StringBuilder();
        int count = 0;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponent<LocalizedTextBinder>() != null) continue;

                    string cur = (text.text ?? string.Empty).Trim();
                    if (cur.Length == 0 || !ContainsKorean(cur)) continue;

                    string label = cur.Replace("\n", "\\n");
                    bool inTable = uiKr.Contains(cur);
                    string dyn = LooksDynamic(text.gameObject.name, cur) ? " [동적?]" : "";

                    count++;
                    sb.AppendLine($"{(inTable ? "[테이블O]" : "[테이블X]")}{dyn} path={GetPath(text.transform)} | text=\"{label}\"");
                }
            }
        }

        Debug.Log($"[UnboundReport] 바인더 없는 한글 텍스트 {count}개 (테이블X=신규필요, 동적?=바인딩 금지 후보):\n{sb}");
    }

    // 오브젝트 이름/텍스트로 코드가 채우는 동적 필드를 추정한다.
    private static bool LooksDynamic(string objName, string text)
    {
        string n = objName.ToLowerInvariant();
        string[] nameHints = { "name", "desc", "title", "value", "count", "amount", "price", "gold", "dia", "level", "lv", "이름", "설명", "값", "수량", "가격", "chapter", "챕터" };
        foreach (string h in nameHints)
            if (n.Contains(h)) return true;

        // 포맷 템플릿/치환 토큰/샘플 문구
        return Regex.IsMatch(text, "\\d") || text.Contains("{") ||
               text.Contains("N개") || text.Contains("N회") ||
               text.Contains("설명이 들어갑니다") || text.Contains("긴 챕터 이름");
    }

    private static HashSet<string> BuildUiKrSet()
    {
        var set = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:UILocalizationTable"))
        {
            var table = AssetDatabase.LoadAssetAtPath<UILocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
            if (table == null || table.rows == null) continue;
            foreach (LocalizationData row in table.rows)
                if (row != null && row.KR != null) set.Add(row.KR.Replace("\\n", "\n").Trim());
        }
        return set;
    }

    private static bool ContainsKorean(string text) => Regex.IsMatch(text, "[\\uAC00-\\uD7A3]");

    private static Dictionary<LocalizingType, HashSet<int>> BuildKnownIds()
    {
        var result = new Dictionary<LocalizingType, HashSet<int>>();
        AddTable<UILocalizationTable>(result, LocalizingType.UI);
        AddTable<GearLocalizationTable>(result, LocalizingType.Gear);
        AddTable<EnchantLocalizationTable>(result, LocalizingType.Enchant);
        AddTable<HousingLocalizationTable>(result, LocalizingType.Housing);
        AddTable<ChapterLocalizationTable>(result, LocalizingType.Chapter);
        return result;
    }

    private static void AddTable<T>(Dictionary<LocalizingType, HashSet<int>> result, LocalizingType type)
        where T : DataTable<LocalizationData>
    {
        var ids = new HashSet<int>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            var table = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (table == null || table.rows == null) continue;
            foreach (LocalizationData row in table.rows)
                if (row != null) ids.Add(row.Language_ID);
        }
        result[type] = ids;
    }

    private static string GetPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }
}
