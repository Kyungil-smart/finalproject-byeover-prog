// 담당자 : 정승우
// 설명   : 챕터 -> 스테이지 -> 웨이브 진행 FSM

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using System.Collections;
using UnityEngine;
 
/// <summary>
/// 스테이지 단위로 게임 흐름을 관리한다.
/// 웨이브 개념이 삭제되고 시간 기반 스테이지 진행으로 변경됨.
/// </summary>
public class StageRunner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnStageChanged;
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;
 
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private StageRepo _stageRepo;
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
        var stageData = _stageRepo.GetStage(stageId);
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
 
        var chapter = _stageRepo.GetChapter(_chapterId);
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
        var chapter = _stageRepo.GetChapter(_chapterId);
        if (chapter == null || chapter.StageCount == 0) return 0f;
        return (float)_currentStageIndex / chapter.StageCount;
    }
}