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
// 추가: 조규민 - 정산 저장에서 현재 챕터와 클리어 스테이지 수를 읽을 수 있도록 공개 속성 추가

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

    public int CurrentChapterId => _chapterId;
    public int CompletedStageCount => Mathf.Max(0, _currentStageIndex);

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

    public void SetRewardManager(InGameRewardManager rewardManager)
    {
        _bootstrapper.SetRewardManager(rewardManager);
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
        Debug.Log($"[StageLoopManager] ▶ 시작: 챕터{_chapterId} 스테이지{_currentStageIndex + 1} (Stage_ID={stageId})");
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            // GetStageId가 -1((챕터,순서) 못 찾음)이거나 해당 Stage_ID 데이터가 없을 때.
            // 첫 스테이지(index 0)부터 없으면 이건 '챕터 완료'가 아니라 데이터/배선 오류다.
            //   EndChapter(true)로 처리하면 가짜 승리 + 정산(ShowSettlement) NRE로 이어져 진짜 원인이 가려진다 → 명확히 로그하고 중단.
            if (_currentStageIndex == 0)
            {
                Debug.LogError($"[StageLoopManager] 챕터 {_chapterId}의 첫 스테이지를 찾지 못했습니다(Stage_ID={stageId}). " +
                               $"StageDataTable에 (Chapter_ID={_chapterId}, StageOrder=1) 행이 있는지 + StageRepo._stageTable 배선/Reimport를 확인하세요. 챕터 시작 중단.");
                _state = State.Idle;
                return;
            }

            // 중간 이후에서 못 찾음 = 마지막 스테이지를 지나 정상적으로 챕터 완료된 경로.
            EndChapter(true);
            return;
        }

        // 스테이지 데이터는 있는데 웨이브 룰이 없으면 StageBootstrapper가 조립을 포기(return)해
        // 클리어 콜백이 영영 안 오는 소프트락이 된다(예: 980102 웨이브 룰 미작성). 첫 스테이지면 명확히 중단,
        // 중간 이후면 해당 스테이지를 건너뛰고 다음으로 진행한다(룰 데이터가 채워지면 자동으로 정상 플레이).
        var spawnRules = DataManager.Instance.StageRepo.GetSpawnRulesForStage(stageId);
        if (spawnRules == null || spawnRules.Count == 0)
        {
            if (_currentStageIndex == 0)
            {
                Debug.LogError($"[StageLoopManager] Stage_ID {stageId}의 웨이브 룰이 없어 챕터를 시작할 수 없습니다. StageWaveRuleTable을 확인하세요.");
                _state = State.Idle;
                return;
            }

            Debug.LogWarning($"[StageLoopManager] Stage_ID {stageId}의 웨이브 룰이 없어 스테이지를 건너뜁니다(소프트락 방지). StageWaveRuleTable에 룰을 채워야 정상 플레이됩니다.");
            ClearStage();
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

        // 스테이지 클리어 자동 체크포인트 저장 제거(기획 #300): 한 판 도중 이어하기 가능한 세이브를 남기지 않는다.
        // 강제종료/크래시로도 이어하기 안 되게. 이어하기 세이브는 '로비로' 버튼(SaveCurrentProgressForResume)에서만 기록.

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
        // 이미 승/패가 확정됐으면 중복 처리 금지 (예: 클리어와 사망 동시 발생)
        if (_state == State.ChapterEnd) return;
        _state = State.ChapterEnd;

        // 승/패 확정 시 웨이브 진행(스폰·Tick)을 완전히 멈춘다.
        // 이게 없으면 패배/승리 후에도 몬스터가 계속 스폰된다.
        if (_bootstrapper != null)
            _bootstrapper.StopStage();

        // 전투 종료 시 진행 중 스킬 루틴 정지 + 잔존 VFX 일괄 정리.
        // (정산 팝업이 Time.timeScale=0으로 뜨면 held VFX 루틴이 WaitForSeconds에 멈춰 자체 Destroy에 도달 못 해
        //  방전 구슬·벽, 하이드로펌프·파도 빔, 파이어브레스 수정구 등이 정산 화면에 남던 문제 해소.)
        FindFirstObjectByType<SkillSystem>()?.ClearActiveSkillVfx();

        OnChapterEnd?.Invoke(isVictory);
    }

    private void HandlePlayerDeath()
    {
        EndChapter(false);
    }

    // ---------- 유틸 ----------
    private int GetStageId()
    {
        // 데이터의 Stage_ID는 불규칙 체계(챕터1~5=1000~, 챕터6~10=1100~)라 산술(chapterId*100+...)로 못 구한다.
        // StageRepo에서 (챕터, 순서)로 역조회. 못 찾으면 -1 → StartStage의 GetStage(-1)=null → 챕터 종료
        // (마지막 스테이지 이후 정상 종료와 동일 경로).
        int stageOrder = _currentStageIndex + 1;   // StageOrder는 1-base
        return DataManager.Instance.StageRepo.GetStageId(_chapterId, stageOrder);
    }

    public float GetStageProgress()
    {
        var chapter = DataManager.Instance.StageRepo.GetChapter(_chapterId);
        if (chapter == null || chapter.StageCount == 0) return 0f;
        return (float)_currentStageIndex / chapter.StageCount;
    }
}
