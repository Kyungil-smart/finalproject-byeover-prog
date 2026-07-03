using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 스타트 버튼을 누를 때, 인덱스를 확인하여 스테이지 및 스토리를 연결해주는 브릿지 역할
/// </summary>
public class GameStartBridge : MonoBehaviour
{
    [SerializeField] private PageMainLobbyController _controller;
    private Dictionary<int, int> _stepIndexToChapterIdMapping;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if(_controller == null) 
            _controller = GetComponent<PageMainLobbyController>();
    }

    private void OnEnable()
    {
        _controller.OnGameStart += SetStage;
    }

    private void OnDisable()
    {
        _controller.OnGameStart -= SetStage;
    }

    private void GetMappingData()
    {
        _stepIndexToChapterIdMapping = DataManager.Instance.StageRepo.GetStepIndexToChapterIdMappingData();
    }

    private void SetStage(int index)
    {
        if (_stepIndexToChapterIdMapping == null || _stepIndexToChapterIdMapping.Count == 0)
        {
            GetMappingData();
        }
        
        if(_stepIndexToChapterIdMapping == null || _stepIndexToChapterIdMapping.Count == 0)
        {
            Debug.LogError("맵핑 데이터를 불러올 수 없습니다. 스테이지 탐색을 종료합니다. 1챕터 1스테이지로 이동.");
            GameStart();
            return;
        }
        
        int chapterId = _stepIndexToChapterIdMapping[index];
        
    }

    private void GameStart()
    {
        if (GameManager.Instance == null)
        {
            
            return;
        }
        
        GameManager.Instance.LoadInGame();
    }
}
