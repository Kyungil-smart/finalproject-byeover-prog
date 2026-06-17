#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 머지로 삭제된 스크립트(GrowthView, Outgamegrowthsystem)가 로비 씬들에 'missing 컴포넌트'로 남아
// 인스펙터에서 SerializedObjectNotCreatableException(Object at index 0 is null)을 유발한다.
// 이 도구는 해당 missing 컴포넌트들을 안전하게(유니티 공식 API) 일괄 제거한다. 에디터 전용.
public static class MissingScriptCleanup
{
    // 진단된 로비 씬 4종 (Assets 기준 경로)
    static readonly string[] TargetScenes =
    {
        "Assets/Scenes/_Lobby.unity",
        "Assets/Scenes/CDH/_LobbyTest.unity",
        "Assets/Scenes/HJO/HJO_Lobby.unity",
        "Assets/_Project/Scenes/Lobby.unity",
    };

    [MenuItem("Tools/Cleanup/Remove Missing Scripts in Lobby Scenes")]
    public static void CleanLobbyScenes()
    {
        string original = EditorSceneManager.GetActiveScene().path;
        int grand = 0;
        foreach (var path in TargetScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"[MissingScriptCleanup] 씬 없음, 건너뜀: {path}");
                continue;
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = CleanScene(scene);
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[MissingScriptCleanup] {path}: missing 스크립트 {n}개 제거");
            grand += n;
        }
        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original);
        Debug.Log($"[MissingScriptCleanup] 완료 — 로비 씬에서 총 {grand}개 missing 스크립트 제거.");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts in Open Scene")]
    public static void CleanOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int n = CleanScene(scene);
        if (n > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[MissingScriptCleanup] 현재 씬 '{scene.name}': missing 스크립트 {n}개 제거.");
    }

    static int CleanScene(Scene scene)
    {
        int total = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
        return total;
    }
}
#endif
