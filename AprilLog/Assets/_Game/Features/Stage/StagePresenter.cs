// 담당자 : 김영찬
// Wave시스템 MVP 모델의 P
// (구)StageRunner.cs에서 분화

// 1차 수정자 : 정승우
// 수정내용 : WaveSpawner -> MonsterSpawner로 변경. 타이머는 StageLoopManager가 관리하므로 여기선 Model 갱신만.

// 2차 수정자 : 김영찬
// 수정내용 : 타이머를 StageBootstrapper에서 받아와 뿌리는 형태로 변경하여 모든 Model과 View의 시간 어긋남 방지

using System;
using UnityEngine;

/// <summary>
/// StageModel과 MonsterSpawner(View)의 중재자
/// </summary>
public class StagePresenter
{
    // ---------- private ----------
    private StageModel _model;
    private MonsterSpawner _spawner; // 팀장님 코드
    private int _stageId;
    private System.Random _rng;
    private Action _onStageCompleteCallback;

    // ---------- 생성자 ----------
    public StagePresenter(StageModel model, MonsterSpawner spawner, int stageId, System.Random rng, Action onComplete)
    {
        _model = model;
        _spawner = spawner;
        _stageId = stageId;
        _rng = rng;
        _onStageCompleteCallback = onComplete;

        _model.OnWaveStarted += HandleWaveStarted;
        _model.OnWaveStopped += HandleWaveStopped;
        _model.OnStageClearTriggered += HandleStageClear;
        
    }
    
    // ---------- 이벤트 핸들러 ----------
    private void HandleWaveStarted(int waveIndex, int totalWaves)
    {
        _spawner.StartWave(_stageId, waveIndex, totalWaves, _rng);
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
    
    // ---------- Update ----------
    public void UpdateSystem(float deltaTime)
    {
        int aliveCount = _spawner != null ? _spawner.AliveCount : 0;
        _model.Tick(deltaTime, aliveCount);
        _spawner.Tick(deltaTime);
    }
    
    // ---------- 메모리 할당 해제 ----------
    public void Release()
    {
        if (_model != null)
        {
            _model.OnWaveStarted -= HandleWaveStarted;
            _model.OnWaveStopped -= HandleWaveStopped;
            _model.OnStageClearTriggered -= HandleStageClear;
        }

        _onStageCompleteCallback = null;
        _spawner = null;
        _model = null;
    }
}