// 담당자 : 김영찬
// 웨이브 시스템 초기화 + 조립기

// 수정자 : 김영찬
// 수정내용 : 데모버전 DB에 맞춰 최신화

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 이벤트 연결

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// StageLoopManager에 의해 생명주기가 통제되는 수동적 의존성 주입(Dependency Injection) 및 조립 팩토리
/// </summary>
public class StageBootstrapper : MonoBehaviour
{
    // ---------- MVP 컴포넌트 ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private StageLoopManager _loopManager;
    
    private StagePresenter _currentPresenter;
    private bool _isStageTickPaused;
    
    // ---------- Event for UI ----------
    public event Action<StageModel> OnStageInitComplete;

    // 마지막으로 조립된 스테이지 모델. UI가 늦게 구독해도 즉시 동기화할 수 있도록 캐시한다.
    public StageModel CurrentStageModel { get; private set; }
    
    // ---------- 이벤트 함수 ----------
    private void Update()
    {
        if (_isStageTickPaused) return;

        if (_currentPresenter != null)
        {
            _currentPresenter.UpdateSystem(Time.deltaTime);
        }
    }

    private void OnDestroy()
    {
        if (_currentPresenter != null)
            _currentPresenter.Release();
    }

    // 챕터 종료(승/패) 시 웨이브 진행을 완전히 멈춘다.
    // 스폰 정지 + 남은 몬스터 정리 + presenter Tick 중단.
    public void StopStage()
    {
        _isStageTickPaused = false;

        if (_spawner != null)
        {
            _spawner.StopSpawning();
            _spawner.DespawnAllAliveMonsters();
        }

        if (_currentPresenter != null)
        {
            _currentPresenter.Release();
            _currentPresenter = null; // Update에서 더 이상 Tick 안 함
        }
    }

    public void SetStageTickPaused(bool paused)
    {
        _isStageTickPaused = paused;

        if (paused && _spawner != null)
        {
            _spawner.StopSpawning();
        }
    }

    // ---------- 시스템 조립 ----------
    public void InitAndStart(StageData stageData, System.Random rng, Action onStageComplete)
    {
        // 참조 자동 탐색(씬 배치/런타임 생성 모두 지원)
        if (_spawner == null) _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_loopManager == null) _loopManager = FindFirstObjectByType<StageLoopManager>();
        if (_spawner == null || _loopManager == null)
        {
            Debug.LogError("[StageBootstrapper] MonsterSpawner/StageLoopManager를 찾지 못해 조립할 수 없습니다.");
            return;
        }

        if (_currentPresenter != null)
        {
            _currentPresenter.Release();
        }

        var waveRuleDict = DataManager.Instance.StageRepo.GetSpawnRulesForStage(stageData.Stage_ID);
        
        if (waveRuleDict == null || waveRuleDict.Count == 0)
        {
            Debug.LogError($"[StageBootstrapper] 웨이브 룰을 찾을 수 없습니다. ID: {stageData.Stage_ID}");
            return;
        }
        
        List<StageWaveRuleData> waveRules = waveRuleDict.Values.ToList();

        // 테스트 씬 전용 튜닝: SkillTestStageTuning 컴포넌트가 "씬에 있을 때만" 스폰 간격/수량을 덮어쓴다.
        // 정식 씬에는 이 컴포넌트가 없으므로 영향 없음. (룰은 복제본으로 적용되어 공용 SO 무손상)
        var testTuning = FindFirstObjectByType<SkillTestStageTuning>();
        if (testTuning != null)
            waveRules = testTuning.ApplyTuning(waveRules);

        StageModel newModel = new StageModel(stageData, waveRules, rng, _loopManager.WaveTransitionDelay);
        _currentPresenter = new StagePresenter(newModel, _spawner, stageData, rng, onStageComplete);

        CurrentStageModel = newModel;
        OnStageInitComplete?.Invoke(newModel);
    }
}
