// 담당자 : 김영찬
// 설명   : 데이터 SO와 Repository 참조를 자동으로 연결하는 에디터 도구
// 수정자 : 정승우
// 수정내용 : DataManager 프리팹, 열린 씬, 프로젝트 씬의 데이터 참조 자동 연결 메뉴 추가

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DataReferenceAutoBinder
{
    private const string SO_FOLDER = "Assets/_Project/Data/SO";
    private const string DATA_MANAGER_PREFAB_PATH = "Assets/Resources/DataManager.prefab";

    private static readonly Dictionary<string, string> FieldAssetMap = new Dictionary<string, string>
    {
        { "_characterMasterTable", "CharacterMasterTable.asset" },
        { "_commonStatusTable", "CommonStatusTable.asset" },
        { "_characterStatusTable", "CharacterStatusTable.asset" },
        { "_monsterStatusTable", "MonsterStatusTable.asset" },
        { "_skillMasterTable", "SkillMasterTable.asset" },
        { "_skillDataTable", "SkillDataTable.asset" },
        { "_effectTable", "EffectDataTable.asset" },
        { "_enchantMasterTable", "EnchantMasterTable.asset" },
        { "_enchantLevelTable", "EnchantLevelTable.asset" },
        { "_enchantWeightTable", "EnchantWeightTable.asset" },
        { "_chapterTable", "ChapterTable.asset" },
        { "_stageTable", "StageDataTable.asset" },
        { "_poolMasterTable", "MonsterPoolMasterTable.asset" },
        { "_poolTable", "MonsterPoolTable.asset" },
        { "_spawnRuleTable", "StageSpawnRuleTable.asset" },
        { "_scalingTable", "MonsterStageScalingTable.asset" },
        { "_inLevelTable", "InLevelTable.asset" },
        { "_outLevelTable", "OutLevelTable.asset" },
        { "_achievementTable", "AchievementDataTable.asset" },
        { "_changeRewardTable", "ChangeRewardTable.asset" },
        { "_languageTable", "LanguageTable.asset" },
    };

    [MenuItem("Tools/Data/Bind References/Bind DataManager Prefab")]
    public static void BindDataManagerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(DATA_MANAGER_PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogError($"[DataReferenceAutoBinder] DataManager 프리팹을 찾을 수 없습니다: {DATA_MANAGER_PREFAB_PATH}");
            return;
        }

        int changedCount = BindInHierarchy(prefabRoot);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, DATA_MANAGER_PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DataReferenceAutoBinder] DataManager 프리팹 참조 연결 완료. 변경 {changedCount}개");
    }

    [MenuItem("Tools/Data/Bind References/Bind Open Scene")]
    public static void BindOpenScene()
    {
        int changedCount = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            changedCount += BindScene(scene);
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[DataReferenceAutoBinder] 열린 씬 참조 연결 완료. 변경 {changedCount}개");
    }

    [MenuItem("Tools/Data/Bind References/Bind All Project Scenes")]
    public static void BindAllProjectScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "데이터 참조 자동 연결",
            "프로젝트의 모든 씬을 열어서 데이터 참조를 자동 연결합니다. 현재 열린 씬은 저장 후 진행됩니다.",
            "실행",
            "취소"))
        {
            return;
        }

        EditorSceneManager.SaveOpenScenes();

        int changedCount = 0;
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            changedCount += BindScene(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[DataReferenceAutoBinder] 모든 씬 참조 연결 완료. 변경 {changedCount}개");
    }

    private static int BindScene(Scene scene)
    {
        int changedCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            changedCount += BindInHierarchy(roots[i]);

        if (changedCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        return changedCount;
    }

    private static int BindInHierarchy(GameObject root)
    {
        int changedCount = 0;
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            changedCount += BindSerializedFields(behaviours[i]);
        }

        return changedCount;
    }

    private static int BindSerializedFields(Object target)
    {
        int changedCount = 0;
        var serializedObject = new SerializedObject(target);

        foreach (KeyValuePair<string, string> pair in FieldAssetMap)
        {
            SerializedProperty property = serializedObject.FindProperty(pair.Key);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) continue;

            Object asset = LoadTableAsset(pair.Value);
            if (asset == null)
            {
                Debug.LogWarning($"[DataReferenceAutoBinder] SO를 찾을 수 없습니다: {pair.Value}");
                continue;
            }

            if (property.objectReferenceValue == asset) continue;

            property.objectReferenceValue = asset;
            changedCount++;
        }

        if (changedCount > 0)
            serializedObject.ApplyModifiedProperties();

        return changedCount;
    }

    private static Object LoadTableAsset(string fileName)
    {
        string path = $"{SO_FOLDER}/{fileName}";
        return AssetDatabase.LoadAssetAtPath<Object>(path);
    }
}
#endif
