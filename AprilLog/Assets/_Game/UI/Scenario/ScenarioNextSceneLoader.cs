using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioNextSceneLoader : MonoBehaviour
{
    [SerializeField] private ScenarioDataDriver _driver;
    
    private StoryRepo _storyRepo;
    
    private const string INGAME_SCENE_NAME = "_InGame";
    private const string LOBBY_SCENE_NAME = "_Lobby";
    private const string SCENARIO_TRIGGER_CHAPTER_END = "ChapterEnd";
    private const string SCENARIO_TRIGGER_THEME_END = "ThemeEnd";

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
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted)
        {
            Debug.Log("[ScenarioNextSceneLoader] 튜토리얼 진행중. 튜토리얼 우선");
            return;
        }
        
        SetTriggerNextScene();
    }

    private void GoToInGameScene()
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(INGAME_SCENE_NAME));
        }
        
        GameManager.Instance.LoadInGame();
    }

    private void GoToLobbyScene()
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(LOBBY_SCENE_NAME));
        }
        
        GameManager.Instance.LoadLobby();
    }

    private void PlayContinue(int groupId)
    {
        _driver.Play(groupId);
    }

    private bool IsContinuePlaying(int groupId, out int nextGroupId)
    {
        nextGroupId = -1;
        var curData = _storyRepo.GetTriggerDataByGroupID(groupId);
        if (curData == null) return false;
        
        var curChapterId = curData.Target_ID;
        if (curChapterId == -1) return false;
        
        var nextData = _storyRepo.GetTriggerDataByChapterID(curChapterId, SCENARIO_TRIGGER_THEME_END);
        if (nextData == null) return false;
        
        nextGroupId = nextData.Target_ID;
        
        return nextGroupId != -1;
    }

    private void SetTriggerNextScene()
    {
        _storyRepo ??= DataManager.Instance.StoryRepo;
        var groupId = GameManager.Instance.SelectedScenarioGroupId;
        var data = _storyRepo.GetTriggerDataByGroupID(groupId);
        switch (data.TriggerType)
        {
            case SCENARIO_TRIGGER_CHAPTER_END:
                if(IsContinuePlaying(groupId, out int nextGroupId))
                {
                    PlayContinue(nextGroupId);
                    break;
                }
                GoToLobbyScene();
                break;
            case SCENARIO_TRIGGER_THEME_END:
                GoToLobbyScene();
                break;
            default:
                GoToInGameScene();
                break;
        }
    }
    
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }
}
