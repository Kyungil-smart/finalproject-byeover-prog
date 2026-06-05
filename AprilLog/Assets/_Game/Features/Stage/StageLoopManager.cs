// 담당자 : 김영찬
// StageRunner.cs를 대체하는 최상위 컨트롤러

// 1차 수정자 : 정승우
// 수정내용 : StartStage -> StartWave로 변경. 스테이지 내 웨이브 분할 추가.

// 2차 수정자 : 김영찬
// 수정 내용 : 시간과 웨이브 관련 상태를 StageModel에 이관하여 책임 분산

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 수정자 : 김영찬
// 수정내용 : 데모버전 DB에 맞춰 최신화

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 이벤트 연결

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
        // 참조가 비어 있으면 자동 탐색(씬 배치/런타임 생성 모두 지원)
        if (_bootstrapper == null) _bootstrapper = FindFirstObjectByType<StageBootstrapper>();
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_bootstrapper == null || _playerModel == null)
        {
            Debug.LogError("[StageLoopManager] StageBootstrapper/PlayerModel을 찾지 못해 챕터를 시작할 수 없습니다.");
            return;
        }

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
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            EndChapter(true);
            return;
        }

        OnStageChanged?.Invoke(stageId);

        // 웨이브 수는 StageModel이 데이터(StageWaveRuleData 목록)에서 직접 산출한다.
        _bootstrapper.InitAndStart(stageData, _rng, ClearStage);
    }

    // ---------- 스테이지 클리어 ----------
    private void ClearStage()
    {
        _state = State.StageClear;

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
}