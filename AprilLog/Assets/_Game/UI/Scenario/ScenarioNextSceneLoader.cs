using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1차 수정자 : 조규민
// 수정 내용 : 시나리오 다시보기 모드에서는 정식 스토리 종료 후 전투/로비 전환 로직을 실행하지 않도록 예외 처리,
// StoryTriggerTable 문자열 공백과 GameManager 미연결 상태에서도 다음 씬 전환이 끊기지 않도록 방어 처리

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
        // 추가: 조규민 - 다시보기 종료 복귀는 TempStoryToGameFlow가 담당하므로 정식 전투/로비 이동을 막는다.
        if (ReplayStorySelectionContext.IsReplayMode)
            return;

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
            return;
        }
        
        GameManager.Instance.LoadInGame();
    }

    private void GoToLobbyScene()
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(LOBBY_SCENE_NAME));
            return;
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
        if (GameManager.Instance == null)
        {
            GoToInGameScene();
            return;
        }

        _storyRepo ??= DataManager.Instance.StoryRepo;
        if (_storyRepo == null)
        {
            Debug.LogWarning("[ScenarioNextSceneLoader] StoryRepo를 찾지 못했습니다.", this);
            GoToInGameScene();
            return;
        }

        var groupId = GameManager.Instance.SelectedScenarioGroupId;
        var data = _storyRepo.GetTriggerDataByGroupID(groupId);
        if (data == null)
        {
            Debug.LogWarning($"[ScenarioNextSceneLoader] StoryTriggerData를 찾지 못했습니다. GroupID: {groupId}", this);
            GoToInGameScene();
            return;
        }

        switch (NormalizeTriggerType(data.TriggerType))
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

    private static string NormalizeTriggerType(string triggerType)
    {
        return string.IsNullOrWhiteSpace(triggerType) ? string.Empty : triggerType.Trim();
    }
    
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }
}
