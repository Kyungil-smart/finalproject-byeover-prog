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
    public event Action<bool> OnChapterEnd;
 
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private StageBootstrapper _bootstrapper;
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private PlayerModel _playerModel;
 
    // ---------- 상태 ----------
    private enum State { StageStart, Running, StageClear, ChapterEnd }
    private State _state;
 
    private int _chapterId;
    private int _currentStageIndex;
    private float _stageTimer;
    private float _stageTimeLimit;
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
 
    // ---------- FSM ----------
    private void Update()
    {
        if (_state == State.Running)
            UpdateRunning();
    }
 
    private void StartStage()
    {
        _state = State.StageStart;
 
        int stageId = GetStageId();
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            EndChapter(true);
            return;
        }
 
        _stageTimeLimit = stageData.TimeLimit;
        _stageTimer = 0f;
 
        // 규칙 기반 스폰 시작
        _spawner.StartStage(stageId, _rng);
 
        _state = State.Running;
        OnStageChanged?.Invoke(_currentStageIndex);
    }
 
    private void UpdateRunning()
    {
        _stageTimer += Time.deltaTime;
 
        // 제한 시간 끝나면 스테이지 클리어
        if (_stageTimer >= _stageTimeLimit)
            ClearStage();
    }
 
    private void ClearStage()
    {
        _state = State.StageClear;
        _spawner.StopSpawning();
 
        if (GameManager.Instance != null)
            GameManager.Instance.SaveLocal();
 
        OnStageClearSaved?.Invoke();
 
        _currentStageIndex++;
 
        var chapter = DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || _currentStageIndex >= chapter.StageCount)
            EndChapter(true);
        else
            StartStage();
    }
 
    private void EndChapter(bool isVictory)
    {
        _state = State.ChapterEnd;
        _spawner.StopSpawning();
        OnChapterEnd?.Invoke(isVictory);
    }
 
    private void HandlePlayerDeath()
    {
        EndChapter(false);
    }
 
    private int GetStageId()
    {
        return _chapterId * 100 + _currentStageIndex + 1;
    }
 
    public float GetStageProgress()
    {
        var chapter = DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || chapter.StageCount == 0) return 0f;
        return (float)_currentStageIndex / chapter.StageCount;
    }
}
