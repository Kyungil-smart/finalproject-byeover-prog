// 담당자 : 정승우
// 설명   : 챕터 -> 스테이지 -> 웨이브 진행 FSM

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 2차 수정자 : 정승우
// 수정내용 : 웨이브 수를 StageData.WaveCount에서 읽도록 변경. 데이터 드리븐.

using System;
using UnityEngine;

/// <summary>
/// 1챕터 안에 여러 스테이지, 1스테이지 안에 여러 웨이브.
/// 웨이브 수는 StageMaster의 WaveCount 컬럼에서 읽는다.
/// </summary>
public class StageRunner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnStageChanged;
    public event Action<int, int> OnWaveChanged;    // waveIndex, totalWaves
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private StageRepo _stageRepo;
    [SerializeField] private PlayerModel _playerModel;

    [Header("웨이브 설정")]
    [Tooltip("웨이브 전환 시 대기 시간(초). 남은 몬스터 정리용.")]
    [SerializeField] private float _waveTransitionDelay = 2.0f;

    [Tooltip("WaveCount가 0이거나 없을 때 사용하는 기본값")]
    [SerializeField] private int _fallbackWaveCount = 3;

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
        var stageData = _stageRepo.GetStage(stageId);
        if (stageData == null)
        {
            EndChapter(true);
            return;
        }

        _currentWaveIndex = 0;

        // 데이터에서 웨이브 수 읽기. 0이면 fallback 사용.
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

    // ---------- 유틸 ----------
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

    public float GetWaveProgress()
    {
        if (_waveTimeLimit <= 0f) return 0f;
        return _waveTimer / _waveTimeLimit;
    }
}