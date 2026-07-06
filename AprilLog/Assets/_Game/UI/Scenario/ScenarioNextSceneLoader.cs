using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioNextSceneLoader : MonoBehaviour
{
    [SerializeField] private ScenarioDataDriver _driver;
    private const string INGAME_SCENE_NAME = "_InGame";

    private void OnEnable()
    {
        _driver.OnFinished += HandleOnFinished;
    }

    private void OnDisable()
    {
        _driver.OnFinished -= HandleOnFinished;
    }

    private void HandleOnFinished()
    {
        // 튜토리얼 매니저가 가동 중이고, 완료가 되지 않았다면 동작은 튜토리얼 매니저가 우선임
        if (TutorialManager.Instance != null)
        {
            if (!TutorialManager.Instance.IsCompleted) return;
        }
        
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(INGAME_SCENE_NAME));
        }
        
        GameManager.Instance.LoadInGame();
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }
}
