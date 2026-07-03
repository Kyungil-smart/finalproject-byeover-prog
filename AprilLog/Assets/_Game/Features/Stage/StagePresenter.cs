// 담당자 : 김영찬
// Wave시스템 MVP 모델의 P
// (구)StageRunner.cs에서 분화

// 1차 수정자 : 정승우
// 수정내용 : WaveSpawner -> MonsterSpawner로 변경. 타이머는 StageLoopManager가 관리하므로 여기선 Model 갱신만.

// 2차 수정자 : 김영찬
// 수정내용 : 타이머를 StageBootstrapper에서 받아와 뿌리는 형태로 변경하여 모든 Model과 View의 시간 어긋남 방지

// 수정자 : 김영찬
// 수정내용 : 데모버전 DB에 맞춰 최신화

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StageModel과 MonsterSpawner(View)의 중재자
/// </summary>
public class StagePresenter
{
    // ---------- private ----------
    private StageModel _model;
    private MonsterSpawner _spawner; // 팀장님 코드
    private StageData _stageData;
    private System.Random _rng;
    private Action _onStageCompleteCallback;
    
    // ---------- event ----------
    public event Action RequestRewardManager;

    // ---------- 생성자 ----------
    public StagePresenter(StageModel model, MonsterSpawner spawner, StageData stageData, System.Random rng, Action onComplete)
    {
        _model = model;
        _spawner = spawner;
        _stageData = stageData;
        _rng = rng;
        _onStageCompleteCallback = onComplete;

        _model.OnWaveStarted += HandleWaveStarted;
        _model.OnWaveStopped += HandleWaveStopped;
        _model.OnStageClearTriggered += HandleStageClear;
        _model.OnSpawnRequested += HandleSpawnRequested;
        _model.OnDespawnRemainingRequested += HandleDespawnRemaining;
        _model.RequestRewardManager += HandleRequestRewardManager;
        _spawner.IsBossDeath += HandleBossMonsterDied;
    }

    public void SetRewardManager(InGameRewardManager rewardManager)
    {
        _model?.SetRewardManager(rewardManager);
        _spawner?.SetRewardManager(rewardManager);
    }
    
    // ---------- 이벤트 핸들러 ----------
    private void HandleWaveStarted(int waveIndex, int totalWaves)
    {
        // 스폰은 StageModel이 OnSpawnRequested로 직접 구동한다.
        // 여기서는 HUD 등 웨이브 시작 훅으로만 사용(현재 비움).
    }

    private void HandleWaveStopped()
    {
        _spawner.StopSpawning();
    }

    private void HandleStageClear()
    {
        _spawner.StopSpawning();
        _onStageCompleteCallback?.Invoke();
    }
    
    // 모델이 던져준 큐를 스포너에게 전달
    private void HandleSpawnRequested(Queue<StageModel.SpawnCommand> spawnQueue, float spawnDelay)
    {
        if (_spawner != null)
        {
            _spawner.SpawnMonsterBatch(spawnQueue, spawnDelay);
        }
    }

    // 모델의 청소 명령 -> 스포너 실행
    private void HandleDespawnRemaining()
    {
        if (_spawner != null)
        {
            _spawner.DespawnAllAliveMonsters();
        }
    }

    // 스포너 몹 사망 -> 모델에 보스 확인 전달
    private void HandleBossMonsterDied()
    {
        _model.NotifyBossKilled();
    }

    private void HandleRequestRewardManager()
    {
        RequestRewardManager?.Invoke();
    }
    
    // ---------- Update ----------
    public void UpdateSystem(float deltaTime)
    {
        _model.Tick(deltaTime);

        // _model.Tick 도중 스테이지 클리어 콜백이 동기적으로 다음 스테이지 조립(InitAndStart)을 타면
        // 이 프레젠터는 이미 Release되어 _spawner가 null이다(재진입). 한 프레임 건너뛰면 새 프레젠터가 돈다.
        // 스포너의 Tick는 안쓰기 때문에 제거함(26.07.03)
    }
    
    // ---------- 메모리 할당 해제 ----------
    public void Release()
    {
        if (_model != null)
        {
            _model.OnWaveStarted -= HandleWaveStarted;
            _model.OnWaveStopped -= HandleWaveStopped;
            _model.OnStageClearTriggered -= HandleStageClear;
            _model.OnSpawnRequested -= HandleSpawnRequested;
            _model.OnDespawnRemainingRequested -= HandleDespawnRemaining;
            _model.RequestRewardManager -= HandleRequestRewardManager;
        }
        
        if(_spawner != null)
        {
            _spawner.IsBossDeath -= HandleBossMonsterDied;
        }

        _onStageCompleteCallback = null;
        _spawner = null;
        _model = null;
    }
}