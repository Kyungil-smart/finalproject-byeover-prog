// 담당자 : 김영찬
// 설명   : 씬과 프리팹의 Missing Script 위치를 찾는 에디터 점검 도구
// 수정자 : 정승우
// 수정내용 : 열린 씬과 프로젝트 프리팹의 누락 스크립트 리포트 메뉴 추가

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptReporter
{
    [MenuItem("Tools/Validation/Report Missing Scripts In Open Scenes")]
    public static void ReportOpenScenes()
    {
        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
                count += ReportInHierarchy(roots[j], scene.path);
        }

        Debug.Log($"[MissingScriptReporter] 열린 씬 Missing Script 점검 완료. 발견 {count}개");
    }

    [MenuItem("Tools/Validation/Report Missing Scripts In Prefabs")]
    public static void ReportPrefabs()
    {
        int count = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) continue;

            count += ReportInHierarchy(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[MissingScriptReporter] 프리팹 Missing Script 점검 완료. 발견 {count}개");
    }

    [MenuItem("Tools/Validation/Report Missing Scripts In All Scenes")]
    public static void ReportAllScenes()
    {
        EditorSceneManager.SaveOpenScenes();

        int count = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject[] roots = scene.GetRootGameObjects();
            for (int j = 0; j < roots.Length; j++)
                count += ReportInHierarchy(roots[j], scenePath);
        }

        Debug.Log($"[MissingScriptReporter] 전체 씬 Missing Script 점검 완료. 발견 {count}개");
    }

    private static int ReportInHierarchy(GameObject root, string assetPath)
    {
        int count = 0;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject target = transforms[i].gameObject;
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);
            if (missingCount <= 0) continue;

            count += missingCount;
            Debug.LogWarning(
                $"[MissingScriptReporter] Missing Script {missingCount}개: {assetPath} / {GetHierarchyPath(target)}",
                target);
        }

        return count;
    }

    private static string GetHierarchyPath(GameObject target)
    {
        string path = target.name;
        Transform current = target.transform.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}
#endif
