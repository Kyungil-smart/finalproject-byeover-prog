// 담당자 : 김영찬
// WaveSystem MVP의 M 담당
// (구)StageRunner.cs에서 분화

// 1차 수정 : 1차 수정된 StageLoopManager.cs에서 시간과 전투중 상태를 분리하여 이식

// 2차 수정 : WaveCount 삭제 되어 해당 변수 사용 스크립트 삭제

// 3차 수정 : _waveCount가 0으로 방치되어 스테이지가 즉시 클리어되던 버그 수정.
//           웨이브 수를 생성자 인자로 주입받도록 복원 (StageLoopManager가 전달).

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시간과 전투 상태를 관리하는 순수 C# 컴포넌트
/// SpawnTracker는 MonsterSpawner.cs에 정의되어 있음. 여기서 중복 선언 금지.
/// </summary>
public class StageModel
{
    // ---------- 이벤트 ----------
    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveStopped;
    public event Action OnStageClearTriggered;
    
    // ---------- 상태 ----------
    private enum State { WaveRunning, WaveTransition }
    private State _state;
    
    // ---------- 타이머 및 상태 데이터 ----------
    private float _waveTimer;
    private float _waveTimeLimit;
    private float _transitionTimer;
    private float _waveTransitionDelay;
    
    private int _currentWaveIndex;
    private int _waveCount; // 스테이지의 총 웨이브 수. 생성자에서 주입.

    // ---------- 생성자 ----------
    public StageModel(StageData stageData , float transitionTime, int waveCount)
    {
        _currentWaveIndex = 0;
        _waveTimeLimit = stageData.TimeLimit;
        _waveCount = waveCount;

        _state = State.WaveTransition;
        _waveTransitionDelay = transitionTime;
        _transitionTimer = 0;
    }

    // ---------- Update ----------
    public void Tick(float deltaTime, int aliveMonsterCount)
    {
        switch (_state)
        {
            case State.WaveRunning:
                UpdateWaveRunning(deltaTime);
                break;
            case State.WaveTransition:
                UpdateWaveTransition(deltaTime, aliveMonsterCount);
                break;
        }
    }
    
    private void UpdateWaveRunning(float deltaTime)
    {
        _waveTimer += deltaTime;
        if (_waveTimer >= _waveTimeLimit)
        {
            OnWaveStopped?.Invoke();
            _currentWaveIndex++;

            if (_currentWaveIndex >= _waveCount)
            {
                OnStageClearTriggered?.Invoke(); 
            }
            else
            {
                _state = State.WaveTransition;
                _transitionTimer = 0f;
            }
        }
    }
    
    private void UpdateWaveTransition(float deltaTime, int aliveMonsterCount)
    {
        _transitionTimer += deltaTime;

        // 기획(1.05): 웨이브 전환은 시간 기반. 몬스터는 방어선에 계속 쌓이는 구조이므로
        // 잔존 몬스터 수와 무관하게 전환한다. (과거 aliveMonsterCount==0 조건은 영구 멈춤 유발)
        if (_transitionTimer >= _waveTransitionDelay)
        {
            _transitionTimer = 0f;
            _state = State.WaveRunning;
            _waveTimer = 0f;
            
            OnWaveStarted?.Invoke(_currentWaveIndex, _waveCount);
        }
    }
}