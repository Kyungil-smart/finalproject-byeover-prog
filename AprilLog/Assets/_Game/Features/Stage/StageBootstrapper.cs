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
        _currentPresenter.Release();
    }

    // ---------- 시스템 조립 ----------
    public void InitAndStart(StageData stageData, System.Random rng, Action onStageComplete)
    {
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
