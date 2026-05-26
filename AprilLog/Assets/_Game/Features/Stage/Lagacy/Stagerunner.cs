// 담당자 : 정승우
// 설명   : 챕터 -> 스테이지 -> 웨이브 진행 FSM

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 게임 흐름을 챕터 -> 스테이지 -> 웨이브 단위로 관리한다.
/// 각 스테이지 클리어 시 로컬 세이브, 챕터 끝나면 클라우드 저장.
/// </summary>
public class StageRunner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnStageChanged;
    public event Action<int> OnWaveChanged;
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;     // isVictory

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private PlayerModel _playerModel;

    // ---------- 상태 ----------
    private enum State { StageStart, WaveRunning, WaveComplete, StageClear, ChapterEnd }
    private State _state;

    private int _chapterId;
    private int _currentStageIndex;
    private int _currentWaveIndex;
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
        if (_state == State.WaveRunning)
            UpdateWaveRunning();
    }

    private void StartStage()
    {
        _state = State.StageStart;
        _currentWaveIndex = 0;

        var stageData = DataManager.Instance.StageRepo.GetStage(GetStageId());
        _stageTimeLimit = stageData.TimeLimit;
        _stageTimer = 0f;

        OnStageChanged?.Invoke(_currentStageIndex);
        StartWave();
    }

    private void StartWave()
    {
        _state = State.WaveRunning;
        OnWaveChanged?.Invoke(_currentWaveIndex);

        var monsters = DataManager.Instance.StageRepo.GetStageMonsters(GetStageId());
        _spawner.StartWave(monsters, _currentWaveIndex);
    }

    private void UpdateWaveRunning()
    {
        _stageTimer += Time.deltaTime;

        if (_spawner.IsWaveComplete() || _stageTimer >= _stageTimeLimit)
            CompleteWave();
    }

    private void CompleteWave()
    {
        _state = State.WaveComplete;
        _currentWaveIndex++;

        var stageData = DataManager.Instance.StageRepo.GetStage(GetStageId());

        if (_currentWaveIndex >= stageData.WaveCount)
            ClearStage();
        else
            StartWave();
    }

    private void ClearStage()
    {
        _state = State.StageClear;

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
        _state = State.ChapterEnd;
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
}