using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class LobbyLocalizationAutoBinder
{
    [MenuItem("Tools/Localization/\uC120\uD0DD \uC624\uBE0C\uC81D\uD2B8 \uC790\uB3D9 \uBC14\uC778\uB529(UI)")]
    private static void AutoBindSelection()
    {
        UILocalizationTable table = FindTable();
        if (table == null)
        {
            EditorUtility.DisplayDialog("\uC790\uB3D9 \uBC14\uC778\uB529", "UILocalizationTable \uC5D0\uC14B\uC744 \uCC3E\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.", "\uD655\uC778");
            return;
        }

        Dictionary<string, int> krToId = BuildStaticMap(table, out int skipped);

        GameObject[] roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            EditorUtility.DisplayDialog("\uC790\uB3D9 \uBC14\uC778\uB529", "\uBA3C\uC800 Hierarchy/Project\uC5D0\uC11C \uB85C\uBE44 \uB8E8\uD2B8\uB098 \uD504\uB9AC\uD33D\uC744 \uC120\uD0DD\uD558\uC138\uC694.", "\uD655\uC778");
            return;
        }

        int bound = 0, already = 0, ignored = 0, unmatched = 0;
        var ignoredSamples = new List<string>();
        var unmatchedSamples = new List<string>();

        foreach (GameObject root in roots)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                string cur = (text.text ?? string.Empty).Trim();
                if (cur.Length == 0) continue;

                if (text.GetComponent<LocalizedTextBinder>() != null)
                {
                    already++;
                    continue;
                }

                if (!krToId.TryGetValue(cur, out int id))
                {
                    if (ShouldIgnoreDynamicOrSampleText(cur))
                    {
                        ignored++;
                        AddSample(ignoredSamples, cur);
                        continue;
                    }

                    unmatched++;
                    AddSample(unmatchedSamples, cur);
                    continue;
                }

                var binder = Undo.AddComponent<LocalizedTextBinder>(text.gameObject);
                var so = new SerializedObject(binder);
                so.FindProperty("_id").intValue = id;
                so.FindProperty("_type").enumValueIndex = (int)LocalizingType.UI;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(text.gameObject);
                bound++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LocalizationAutoBind] \uBC14\uC778\uB529 {bound} / \uC774\uBBF8\uC788\uC74C {already} / \uB3D9\uC801\uAC12\uC81C\uC678 {ignored} / \uBBF8\uB9E4\uCE6D {unmatched} " +
                  $"(\uD14C\uC774\uBE14 \uC815\uC801\uD56D\uBAA9 {krToId.Count}\uAC1C, \uD3EC\uB9F7\u00B7\uC911\uBCF5 \uC81C\uC678 {skipped}\uAC1C)\n" +
                  $"\uB3D9\uC801\uAC12 \uC81C\uC678 \uC608\uC2DC: {string.Join(" | ", ignoredSamples)}\n" +
                  $"\uBBF8\uB9E4\uCE6D \uC608\uC2DC: {string.Join(" | ", unmatchedSamples)}");

        EditorUtility.DisplayDialog("\uC790\uB3D9 \uBC14\uC778\uB529 \uC644\uB8CC",
            $"\uBC14\uC778\uB529: {bound}\n\uC774\uBBF8\uC788\uC74C: {already}\n\uB3D9\uC801\uAC12 \uC81C\uC678: {ignored}\n\uBBF8\uB9E4\uCE6D(\uC218\uB3D9 \uD655\uC778 \uD544\uC694): {unmatched}\n\n\uC790\uC138\uD55C \uBAA9\uB85D\uC740 Console \uCC38\uACE0.", "\uD655\uC778");
    }

    private static void AddSample(List<string> samples, string text)
    {
        if (samples.Count < 20)
            samples.Add(text.Replace("\n", "\\n"));
    }

    private static bool ShouldIgnoreDynamicOrSampleText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        string normalized = text.Trim();
        if (!ContainsKorean(normalized)) return true;
        if (Regex.IsMatch(normalized, "\\d")) return true;
        if (Regex.IsMatch(normalized, "([\\uAC00-\\uD7A3])\\1{2,}")) return true;

        return normalized.Contains("{{") ||
               normalized.Contains("}}") ||
               normalized.Contains("\uC124\uBA85\uC774 \uB4E4\uC5B4\uAC11\uB2C8\uB2E4") ||
               normalized.Contains("\uD45C\uC2DC") ||
               normalized.Contains("\uCE7C\uB7FC \uAC12") ||
               normalized.Contains("\uC544\uD2F0\uD329\uD2B8 \uB4F1\uAE09") ||
               normalized.Contains("\uC544\uD2F0\uD329\uD2B8 \uC774\uB984");
    }

    private static bool ContainsKorean(string text)
    {
        return Regex.IsMatch(text, "[\\uAC00-\\uD7A3]");
    }

    private static Dictionary<string, int> BuildStaticMap(UILocalizationTable table, out int skipped)
    {
        var map = new Dictionary<string, int>();
        var dup = new HashSet<string>();
        skipped = 0;

        foreach (LocalizationData row in table.rows)
        {
            if (row == null) continue;
            string kr = (row.KR ?? string.Empty).Replace("\\n", "\n").Trim();
            if (kr.Length == 0 || kr.Contains("{"))
            {
                skipped++;
                continue;
            }

            if (map.ContainsKey(kr))
            {
                map.Remove(kr);
                dup.Add(kr);
                skipped++;
                continue;
            }

            if (dup.Contains(kr))
            {
                skipped++;
                continue;
            }

            map[kr] = row.Language_ID;
        }

        return map;
    }

    private static UILocalizationTable FindTable()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UILocalizationTable"))
        {
            var table = AssetDatabase.LoadAssetAtPath<UILocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
            if (table != null) return table;
        }

        return null;
    }
}
