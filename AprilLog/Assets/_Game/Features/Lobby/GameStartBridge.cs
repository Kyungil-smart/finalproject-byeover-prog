// 1차 수정자 : 조규민
// 수정 내용 : 저장된 동일 챕터 전투에 재진입할 때 ChapterStart 스토리 판정을 건너뛰고 진행도를 바로 복원하도록 수정

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
    private const int StaminaId = 10001;
    

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if(_controller == null) 
            _controller = GetComponent<PageMainLobbyController>();
        if(_staminaModel == null)
            _staminaModel = FindFirstObjectByType<StaminaModel>(FindObjectsInactive.Include);
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
        // 튜토리얼의 StageStart 단계는 게임 스타트로 인게임에 재진입해야 하는 단계다.
        bool tutorialStageStart = TutorialManager.Instance != null
            && !TutorialManager.Instance.IsCompleted
            && IsTutorialStageStartStep();

        // 튜토리얼 진행 중에는 게임 스타트를 튜토리얼이 우선한다. StageStart 단계만 통과시킨다.
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCompleted
            && !tutorialStageStart)
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

        bool _hasResumeSave = HasResumeSaveForChapter(chapterId);
        if (!ConsumeStamina(chapterId, _hasResumeSave))
        {
            Debug.Log($"[GameStartBridge] 스태미너가 소모되지 않았습니다. 이동을 취소합니다");
            return;
        }

        // 튜토리얼 StageStart 재진입은 스토리 자동재생을 건너뛰고 바로 인게임으로 들어간다.
        // 인자 없는 GameStart를 써서 SelectedChapterId를 건드리지 않는다(0 유지). 그래야
        // InGameBootstrap이 튜토리얼 재진입 챕터를 잡고, TutorialInGameDirector도 튜토리얼 런으로 인식해
        // step14 시퀀스(대화 버블)를 재생한다. (스토리 분기는 튜토리얼 이후 추가된 기능이라 개입시키지 않는다.)
        if (tutorialStageStart)
        {
            GameStart();
            return;
        }

        // 추가: 조규민 - 이어하기는 신규 챕터 진입이 아니므로 초회 스토리 판정보다 우선해 저장 전투로 바로 이동한다.
        if (_hasResumeSave)
        {
            Debug.Log($"[GameStartBridge] 챕터 {chapterId} 저장 전투 이어하기. ChapterStart 스토리를 건너뜁니다.");
            GameStart(chapterId);
            return;
        }

        SetNextScene(chapterId);
    }
    
    // 현재 튜토리얼 단계가 게임 스타트(StageStart)로 인게임에 재진입하는 단계인지.
    private static bool IsTutorialStageStartStep()
    {
        TutorialStep step = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStep : null;
        return step != null
            && step.scene == TutorialScene.Lobby
            && string.Equals(step.highlightTargetId, "StageStartButton", StringComparison.Ordinal);
    }

    private void SetNextScene(int chapterId)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameStartBridge] GameManager가 없습니다. 1챕터 1스테이지로 이동.");
            GameStart();
            return;
        }
        
        var repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        var stageId = repo != null ? repo.GetStageId(chapterId,1) : -1;
        if (stageId == -1)
        {
            Debug.LogWarning("잘못된 챕터 ID입니다. 1챕터 1스테이지로 이동.");
            GameStart();
            return;
        }
        
        int groupId = GetScenarioGroupId(chapterId);
        if (groupId == -1)
        {
            Debug.LogWarning($"[GameStartBridge] 해당 챕터에 맞는 시나리오 그룹 ID를 찾을수 없습니다 : {chapterId}. 스토리를 건너뜁니다.");
            GameStart(stageId);
            return;
        }
        
        if (IsFirstSeenScenario(groupId))
        {
            GameStart(stageId);
        }
        else
        {
            GameManager.Instance.SelectedChapterId = stageId;
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
    
    private void GameStart(int stageId)
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(INGAME_SCENE_NAME));
            return;
        }
        GameManager.Instance.LoadInGame(stageId);
    }

    private void StoryStart(int groupId)
    {
        if (GameManager.Instance == null)
        {
            StartCoroutine(LoadSceneCoroutine(STORY_SCENE_NAME));
            return;
        }

        GameManager.Instance.LoadScenarioByGroupId(groupId);
    }

    private bool HasResumeSaveForChapter(int _chapterId)
    {
        GameManager _gameManager = GameManager.Instance;
        if (_gameManager == null || !_gameManager.HasLocalSave())
        {
            return false;
        }

        InGameSaveData _saveData = _gameManager.LoadLocalSaveData();
        return _saveData != null && _saveData.chapterId == _chapterId;
    }

    private bool ConsumeStamina(int _chapterId, bool _hasResumeSave)
    {
        _stageRepo ??= DataManager.Instance.StageRepo;
        var data = _stageRepo.GetChapter(_chapterId);
        if (data == null)
        {
            Debug.LogWarning("[GameStartBridge] 해당 챕터의 정보를 찾을수 없습니다. 스태미너 소모를 중단합니다.");
            return false;
        }

        if (_hasResumeSave)
        {
            Debug.Log("[GameStartBridge] 저장된 진행사항 확인. 스태미너 소모 0");
            return true;
        }
        
        int cost = Mathf.Max(0, data.StaminaCost);
        if (cost == 0) return true;

        if (_staminaModel == null)
            _staminaModel = FindFirstObjectByType<StaminaModel>(FindObjectsInactive.Include);

        if (_staminaModel != null)
            return _staminaModel.Spend(cost);

        var resourceRepo = DataManager.Instance != null ? DataManager.Instance.ResourceRepo : null;
        var staminaSlot = resourceRepo != null ? resourceRepo.GetStaminaSlot(StaminaId) : null;
        if (staminaSlot == null)
        {
            Debug.LogWarning("[GameStartBridge] StaminaModel/ResourceRepo를 찾지 못해 행동력을 차감할 수 없습니다. 이동을 취소합니다.");
            return false;
        }

        bool result = resourceRepo.UseStamina(StaminaId, cost);
        if (result && GameManager.Instance != null)
            GameManager.Instance.SyncAndSaveResourceCloudData();

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
