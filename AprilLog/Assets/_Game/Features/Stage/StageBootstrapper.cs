// 담당자 : 김영찬
// 웨이브 시스템 초기화 + 조립기

using System;
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
    public void InitAndStart(int stageId, System.Random rng, Action onStageComplete)
    {
        if (_currentPresenter != null)
        {
            _currentPresenter.Release();
        }
        
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        if (stageData == null)
        {
            Debug.LogError($"[StageBootstrapper] StageData를 찾을 수 없습니다. ID: {stageId}");
            return;
        }
        
        StageModel newModel = new StageModel(stageData, _loopManager.WaveTransitionDelay);
        _currentPresenter = new StagePresenter(newModel, _spawner, stageId, rng, onStageComplete);
    }
}
