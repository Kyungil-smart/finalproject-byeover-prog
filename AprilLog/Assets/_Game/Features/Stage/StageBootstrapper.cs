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
    
    // ---------- Event for UI ----------
    public event Action<StageModel> OnStageInitComplete;
    
    // ---------- 이벤트 함수 ----------
    private void Update()
    {
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
        
        StageModel newModel = new StageModel(stageData, waveRules, rng, _loopManager.WaveTransitionDelay);
        _currentPresenter = new StagePresenter(newModel, _spawner, stageData, rng, onStageComplete);
        
        OnStageInitComplete?.Invoke(newModel);
    }
}
