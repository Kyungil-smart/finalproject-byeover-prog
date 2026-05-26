// 담당자 : 김영찬
// StageRunner.cs를 대체하는 최상위 컨트롤러

using System;
using UnityEngine;

/// <summary>
/// 플레이어가 씬에 진입한 순간부터 챕터 내 모든 스테이지가 완료되거나 중도 포기할 때까지의 전체 게임 루프 상태(State)를 관리하는 컨트롤러
/// </summary>
public class StageLoopManager : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnStageChanged;
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;     // isVictory

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private WaveSystemBootstrapper _bootstrapper;
    [SerializeField] private PlayerModel _playerModel;

    // ---------- 상태 ----------
    public enum State { StageStart, WaveRunning, WaveComplete, StageClear, ChapterEnd }
    private State _state;

    private int _chapterId;
    private int _currentStageIndex;
    private System.Random _rng;

    // ---------- 초기화 ----------
    public void StartChapter(int chapterId, int startStageIndex, int seed)
    {
        _chapterId = chapterId;
        _currentStageIndex = startStageIndex;
        _rng = new System.Random(seed);

        _playerModel.OnPlayerDeath += HandlePlayerDeath;

        StartStage();
    }

    private void OnDisable()
    {
        if (_playerModel != null)
            _playerModel.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void StartStage()
    {
        SetState(State.StageStart);
        
        int currentStageId = GetStageId();
        
        OnStageChanged?.Invoke(_currentStageIndex);
        
        _bootstrapper.InitAndStart(currentStageId, OnStageComplete);
    }

    private void OnStageComplete()
    {
        SetState(State.StageClear);

        // 로컬 세이브
        if (GameManager.Instance != null)
            GameManager.Instance.SaveLocal();

        OnStageClearSaved?.Invoke();

        // 다음 스테이지
        _currentStageIndex++;

        var chapter = DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (_currentStageIndex >= chapter.StageCount)
        {
            EndChapter(true);
        }
        else
        {
            StartStage();
        }
    }

    private void EndChapter(bool isVictory)
    {
        SetState(State.ChapterEnd);
        OnChapterEnd?.Invoke(isVictory);
    }

    private void HandlePlayerDeath()
    {
        EndChapter(false);
    }

    // 스테이지 ID = 챕터ID * 100 + 순번 + 1
    private int GetStageId()
    {
        return _chapterId * 100 + _currentStageIndex + 1;
    }

    // 현재 스테이지 진행률 (0~1). HUD 진행도 바에 사용.
    public float GetStageProgress()
    {
        var chapter = DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || chapter.StageCount == 0) return 0f;
        return (float)_currentStageIndex / chapter.StageCount;
    }

    public State SetState(State state)
    {
        return _state = state;
    }
}
