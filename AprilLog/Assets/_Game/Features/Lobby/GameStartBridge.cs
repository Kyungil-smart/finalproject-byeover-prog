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
    private StageRepo _repo;
    
    private const string INGAME_SCENE_NAME = "_InGame";
    private const string STORY_SCENE_NAME = "_Story";
    

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
        _repo ??= DataManager.Instance.StageRepo;
        int chapterId = _repo.GetChapterIdByIndex(index);
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
        
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GameStartBridge] GameManager가 없습니다. 1챕터 1스테이지로 이동.");
            GameStart();
            return;
        }
        
        GameManager.Instance.SelectedChapterId = chapterId;
        GameStart();
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

    private bool ConsumeStamina(int chapterId)
    {
        _repo ??= DataManager.Instance.StageRepo;
        var data = _repo.GetChapter(chapterId);
        if (data == null)
        {
            Debug.LogWarning("[GameStartBridge] 해당 챕터의 정보를 찾을수 없습니다. 스태미너 소모를 중단합니다.");
            return false;
        }
        
        bool result = _staminaModel.Spend(data.StaminaCost);
        return result;
    }
    
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }
}
