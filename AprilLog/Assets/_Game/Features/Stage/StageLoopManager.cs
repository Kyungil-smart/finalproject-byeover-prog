// 담당자 : 김영찬
// StageRunner.cs를 대체하는 최상위 컨트롤러

// 1차 수정자 : 정승우
// 수정내용 : StartStage -> StartWave로 변경. 스테이지 내 웨이브 분할 추가.

using System;
using UnityEngine;

/// <summary>
/// 챕터 내 스테이지 -> 웨이브 전체 루프를 관리하는 컨트롤러.
/// 웨이브 수는 StageData.WaveCount에서 읽고, 없으면 기본값 3.
/// </summary>
public class StageLoopManager : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnStageChanged;
    public event Action<int, int> OnWaveChanged;    // waveIndex, totalWaves
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private PlayerModel _playerModel;

    [Header("웨이브 설정")]
    [Tooltip("WaveCount가 0일 때 사용하는 기본값")]
    [SerializeField] private int _fallbackWaveCount = 3;

    [Tooltip("웨이브 전환 시 대기 시간(초)")]
    [SerializeField] private float _waveTransitionDelay = 2.0f;

    // ---------- 상태 ----------
    private enum State { Idle, WaveRunning, WaveTransition, StageClear, ChapterEnd }
    private State _state;

    private int _chapterId;
    private int _currentStageIndex;
    private int _currentWaveIndex;
    private int _waveCount;
    private float _waveTimer;
    private float _waveTimeLimit;
    private float _transitionTimer;
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
        switch (_state)
        {
            case State.WaveRunning:
                UpdateWaveRunning();
                break;
            case State.WaveTransition:
                UpdateWaveTransition();
                break;
        }
    }

    // ---------- 스테이지 ----------
    private void StartStage()
    {
        int stageId = GetStageId();
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            EndChapter(true);
            return;
        }

        _currentWaveIndex = 0;
        _waveCount = stageData.WaveCount > 0 ? stageData.WaveCount : _fallbackWaveCount;
        _waveTimeLimit = (float)stageData.TimeLimit / _waveCount;

        OnStageChanged?.Invoke(_currentStageIndex);
        StartWave();
    }

    // ---------- 웨이브 ----------
    private void StartWave()
    {
        _state = State.WaveRunning;
        _waveTimer = 0f;

        int stageId = GetStageId();
        _spawner.StartWave(stageId, _currentWaveIndex, _waveCount, _rng);

        OnWaveChanged?.Invoke(_currentWaveIndex, _waveCount);
    }

    private void UpdateWaveRunning()
    {
        _waveTimer += Time.deltaTime;
        if (_waveTimer >= _waveTimeLimit)
            CompleteWave();
    }

    private void CompleteWave()
    {
        _spawner.StopSpawning();
        _currentWaveIndex++;

        if (_currentWaveIndex >= _waveCount)
            ClearStage();
        else
        {
            _state = State.WaveTransition;
            _transitionTimer = 0f;
        }
    }

    private void UpdateWaveTransition()
    {
        _transitionTimer += Time.deltaTime;
        if (_transitionTimer >= _waveTransitionDelay && _spawner.AliveCount == 0)
        {
            _transitionTimer = 0f;
            StartWave();
        }
    }

    // ---------- 스테이지 클리어 ----------
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

    // ---------- 유틸 ----------
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

    public float GetWaveProgress()
    {
        if (_waveTimeLimit <= 0f) return 0f;
        return _waveTimer / _waveTimeLimit;
    }
}