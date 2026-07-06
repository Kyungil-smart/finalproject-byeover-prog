using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 스타트 버튼을 누를 때, 인덱스를 확인하여 스테이지 및 스토리를 연결해주는 브릿지 역할
/// </summary>
public class GameStartBridge : MonoBehaviour
{
    [SerializeField] private PageMainLobbyController _controller;
    [SerializeField] private StaminaModel _staminaModel;
    private StageRepo _stageRepo;
    private StoryRepo _storyRepo;
    
    private const string INGAME_SCENE_NAME = "_InGame";
    private const string STORY_SCENE_NAME = "_Story";
    private const string SCENARIO_TRIGGER_TYPE_CHAPTER_START = "ChapterStart";
    

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if(_controller == null) 
            _controller = GetComponent<PageMainLobbyController>();
        if(_staminaModel == null)
            _staminaModel = FindAnyObjectByType<StaminaModel>();
    }

    private void OnEnable()
    {
        _controller.OnGameStart += SetStage;
    }

    private void OnDisable()
    {
        _controller.OnGameStart -= SetStage;
    }

    private void SetStage(int index)
    {
        // 튜토리얼 매니저가 가동 중이고, 완료가 되지 않았다면 동작은 튜토리얼 매니저가 우선임
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted)
        {
            Debug.Log("[GameStartBridge] 튜토리얼 진행중. 튜토리얼 우선");
            return;
        }
        
        _stageRepo ??= DataManager.Instance.StageRepo;
        int chapterId = _stageRepo.GetChapterIdByIndex(index);
        if (chapterId == -1)
        {
            Debug.LogError($"[GameStartBridge] 잘못된 인덱스 접근입니다. Index {index}를 확인해주세요");
            return;
        }

        if (!ConsumeStamina(chapterId))
        {
            Debug.Log($"[GameStartBridge] 스태미너가 소모되지 않았습니다. 이동을 취소합니다");
            return;
        }
        
        SetNextScene(chapterId);
    }
    
    private void SetNextScene(int chapterId)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameStartBridge] GameManager가 없습니다. 1챕터 1스테이지로 이동.");
            GameStart();
            return;
        }
        
        int groupId = GetScenarioGroupId(chapterId);
        if (groupId == -1)
        {
            Debug.LogWarning($"[GameStartBridge] 해당 챕터에 맞는 시나리오 그룹 ID를 찾을수 없습니다 : {chapterId}. 스토리를 건너뜁니다.");
            GameStart(chapterId);
            return;
        }
        
        if (IsFirstSeenScenario(groupId))
        {
            GameStart(groupId);
        }
        else
        {
            GameManager.Instance.SelectedChapterId = chapterId;
            StoryStart(groupId);
        }
    }

    private void GameStart()
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(INGAME_SCENE_NAME));
            return;
        }
        GameManager.Instance.LoadInGame();
    }
    
    private void GameStart(int chapterId)
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(INGAME_SCENE_NAME));
            return;
        }
        GameManager.Instance.LoadInGame(chapterId);
    }

    private void StoryStart(int groupId)
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(STORY_SCENE_NAME));
        }

        GameManager.Instance.LoadScenarioByGroupId(groupId);
    }

    private bool ConsumeStamina(int chapterId)
    {
        _stageRepo ??= DataManager.Instance.StageRepo;
        var data = _stageRepo.GetChapter(chapterId);
        if (data == null)
        {
            Debug.LogWarning("[GameStartBridge] 해당 챕터의 정보를 찾을수 없습니다. 스태미너 소모를 중단합니다.");
            return false;
        }
        
        bool result = _staminaModel.Spend(data.StaminaCost);
        return result;
    }

    private bool IsFirstSeenScenario(int groupId)
    {
        if (groupId == -1)
        {
            Debug.LogError($"[GameStartBridge] 잘못된 인덱스 접근입니다. Group Id {groupId}를 확인해주세요");
            return false;
        }

        if (GameManager.Instance != null) return GameManager.Instance.IsFirstReadScenario(groupId);
        
        Debug.LogWarning("[GameStartBridge] GameManager가 없습니다. 초회 시나리오 판정 건너뛰고 False 반환됨.");
        return false;
    }
    
    private int GetScenarioGroupId(int chapterId)
    {
        _storyRepo ??= DataManager.Instance.StoryRepo;
        var data = _storyRepo.GetTriggerDataByChapterID(chapterId, SCENARIO_TRIGGER_TYPE_CHAPTER_START);
        
        if (data != null)
        {
            return data.Story_ID;
        }
        
        return -1;
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }
}
