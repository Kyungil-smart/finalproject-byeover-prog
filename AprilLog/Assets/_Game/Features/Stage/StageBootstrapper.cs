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
    [SerializeField] WavePresenter _currentPresenter;
    
    // ---------- 조립 구동 ----------
    public void InitAndStart(int stageId, Action onCompleteCallback)
    {
        // 1. 기존 Presenter가 있다면 해제 (Soft Reset)
        _currentPresenter?.Release();
        
        // 2. DataManager에서 스테이지 데이터 로드
        var stageData = DataManager.Instance.StageRepo.GetStage(stageId);
        // (필요하다면 몬스터 스폰 데이터도 로드)
        
        // 3. 새 WaveModel 생성 및 데이터 주입
        StageModel newModel = new StageModel(stageData);
        
        // // 4. 새 WavePresenter 생성 (이때 매니저에게 받은 Action을 Presenter까지 전달)
        // _currentPresenter = new WavePresenter(newModel, _spawner, _ui, onStageComplete);
        //
        // // 5. 시스템 구동!
        // _currentPresenter.StartSystem();
    }
}
