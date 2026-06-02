// 담당자 : 김영찬
// StageRunner.cs를 대체하는 최상위 컨트롤러

// 1차 수정자 : 정승우
// 수정내용 : StartStage -> StartWave로 변경. 스테이지 내 웨이브 분할 추가.

// 2차 수정자 : 김영찬
// 수정 내용 : 시간과 웨이브 관련 상태를 StageModel에 이관하여 책임 분산

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

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
    public event Action OnStageClearSaved;
    public event Action<bool> OnChapterEnd;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private StageBootstrapper _bootstrapper;
    [SerializeField] private PlayerModel _playerModel;

    [Header("웨이브 설정")]
    [Tooltip("WaveCount가 0일 때 사용하는 기본값")]
    [SerializeField] private int _fallbackWaveCount = 3;

    [Tooltip("웨이브 전환 시 대기 시간(초)")]
    [SerializeField] private float _waveTransitionDelay = 2.0f;

    public float WaveTransitionDelay => _waveTransitionDelay;

    // ---------- 상태 ----------
    private enum State { Idle, RunningStage, StageClear, ChapterEnd }
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

    // ---------- 스테이지 ----------
    private void StartStage()
    {
        _state = State.RunningStage;

        int stageId = GetStageId();
        var stageData = Legacy_DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            EndChapter(true);
            return;
        }

        OnStageChanged?.Invoke(_currentStageIndex);

        // StageData에는 아직 웨이브 수 필드가 없어 기본값을 사용한다.
        // (per-stage 웨이브 수 데이터 연동은 추후 데이터 파이프라인 작업에서 처리)
        int waveCount = _fallbackWaveCount > 0 ? _fallbackWaveCount : 3;
        _bootstrapper.InitAndStart(stageData, waveCount, _rng, ClearStage);
    }

    // ---------- 스테이지 클리어 ----------
    private void ClearStage()
    {
        _state = State.StageClear;

        if (GameManager.Instance != null)
            GameManager.Instance.SaveLocal();

        OnStageClearSaved?.Invoke();

        _currentStageIndex++;

        var chapter = Legacy_DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || _currentStageIndex >= chapter.StageCount)
            EndChapter(true);
        else
            StartStage();
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

    // ---------- 유틸 ----------
    private int GetStageId()
    {
        return _chapterId * 100 + _currentStageIndex + 1;
    }

    public float GetStageProgress()
    {
        var chapter = Legacy_DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || chapter.StageCount == 0) return 0f;
        return (float)_currentStageIndex / chapter.StageCount;
    }
}