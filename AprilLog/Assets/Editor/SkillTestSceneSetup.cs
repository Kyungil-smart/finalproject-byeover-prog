#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Skill_TEST 씬의 테스트 설정(SkillTestStageTuning: 스폰 간격 ×0.5, 수량 ×1)을
// Skill_TEST2 씬에 동일하게 적용한다. 씬에 컴포넌트가 있을 때만 StageBootstrapper가 튜닝을 먹인다.
public static class SkillTestSceneSetup
{
    const string TargetScene = "Assets/Scenes/Skill_TEST2.unity";
    const float SpawnIntervalMultiplier = 0.5f; // Skill_TEST와 동일 (스폰 5초→2.5초)
    const float SpawnAmountMultiplier = 1f;      // Skill_TEST와 동일 (수량 그대로)

    [MenuItem("Tools/SkillTest/Apply Tuning to Skill_TEST2")]
    public static void ApplyToSkillTest2()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) == null)
        {
            Debug.LogError($"[SkillTestSetup] 씬 없음: {TargetScene}");
            return;
        }

        string original = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);

        var comp = Object.FindObjectOfType<SkillTestStageTuning>();
        if (comp == null)
        {
            var go = new GameObject("SkillTestStageTuning");
            comp = go.AddComponent<SkillTestStageTuning>();
            Debug.Log("[SkillTestSetup] SkillTestStageTuning 오브젝트 생성.");
        }
        else
        {
            Debug.Log("[SkillTestSetup] 기존 SkillTestStageTuning 발견 — 값만 갱신.");
        }

        // 비공개 [SerializeField] 값은 SerializedObject로 설정.
        var so = new SerializedObject(comp);
        so.FindProperty("_spawnIntervalMultiplier").floatValue = SpawnIntervalMultiplier;
        so.FindProperty("_spawnAmountMultiplier").floatValue = SpawnAmountMultiplier;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SkillTestSetup] Skill_TEST2 적용·저장 완료 (간격 ×{SpawnIntervalMultiplier}, 수량 ×{SpawnAmountMultiplier}).");

        if (!string.IsNullOrEmpty(original) && original != TargetScene)
            EditorSceneManager.OpenScene(original);
    }
}
#endif
