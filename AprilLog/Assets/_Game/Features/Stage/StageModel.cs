// 담당자 : 김영찬
// WaveSystem MVP의 M 담당
// (구)StageRunner.cs에서 분화

// 1차 수정 : 1차 수정된 StageLoopManager.cs에서 시간과 전투중 상태를 분리하여 이식

// 2차 수정 : WaveCount 삭제 되어 해당 변수 사용 스크립트 삭제

// 수정자 : 김영찬
// 수정내용 : 데모버전 DB에 맞춰 최신화

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
    public event Action<int> OnSpawnRequested;
    
    // ---------- 상태 ----------
    private enum State { WaveRunning, WaveTransition }
    private State _state;
    
    // ---------- 기획 시트 데이터 ----------
    private StageData _stageData;
    private List<StageWaveRuleData> _waveRules;
    private StageWaveRuleData _currentRule;
    private System.Random _rng;
    
    // ---------- 타이머 및 상태 데이터 ----------
    private float _waveTimer;
    private float _spawnTimer;
    private float _transitionTimer;
    private float _waveTransitionDelay;
    
    private int _currentWaveIndex;
    private int _waveCount; // 스테이지의 총 웨이브 수. 웨이브 룰 개수로 산출.

    // ---------- 생성자 ----------
    public StageModel(StageData stageData , List<StageWaveRuleData> waveRules, System.Random rng, float transitionTime)
    {
        _stageData = stageData;
        _waveRules = waveRules;
        _rng = rng;
        _currentWaveIndex = 0;
        _waveCount = waveRules != null ? waveRules.Count : 0;

        _state = State.WaveTransition;
        _waveTransitionDelay = transitionTime;
        _transitionTimer = _waveTransitionDelay;
    }

    // ---------- Update ----------
    public void Tick(float deltaTime, int aliveMonsterCount)
    {
        if (_state == State.WaveRunning)
            UpdateWaveRunning(deltaTime);
        else if (_state == State.WaveTransition)
            UpdateWaveTransition(deltaTime, aliveMonsterCount);
    }
    
    private void UpdateWaveRunning(float deltaTime)
    {
        _waveTimer += deltaTime;
        _spawnTimer += deltaTime;
        
        // 스폰 간격(SpawnInterval)이 지났을 때
        if (_spawnTimer >= _currentRule.SpawnInterval)
        {
            _spawnTimer = 0f;
            
            for (int i = 0; i < _currentRule.SpawnAmount; i++)
            {
                int characterId = RollDiceForMonster(_currentRule);
                if (characterId > 0)
                {
                    OnSpawnRequested?.Invoke(characterId);
                }
            }
        }
        
        // 웨이브 종료 시간이 되었을 때
        if (_waveTimer >= _currentRule.WaveDuration)
        {
            OnWaveStopped?.Invoke();
            _currentWaveIndex++;

            if (_currentWaveIndex >= _waveRules.Count)
                OnStageClearTriggered?.Invoke();
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
        // 잔존 몬스터 수와 무관하게 전환한다. (aliveMonsterCount==0 조건은 영구 멈춤 유발)
        if (_transitionTimer >= _waveTransitionDelay)
        {
            _transitionTimer = 0f;
            _state = State.WaveRunning;
            _waveTimer = 0f;
            _spawnTimer = 0f;

            // 이번 웨이브의 규칙을 확정한다. (미설정 시 UpdateWaveRunning에서 NRE 발생)
            _currentRule = _waveRules[_currentWaveIndex];

            OnWaveStarted?.Invoke(_currentWaveIndex, _waveCount);
        }
    }
    
    private int RollDiceForMonster(StageWaveRuleData rule)
    {
        int poolId = rule.MonsterWavePool_ID; 
        
        int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
        
        return characterId; 
    }
    
    public float GetWaveProgress()
    {
        if (_currentRule == null || _currentRule.WaveDuration <= 0f) return 0f;
        return _waveTimer / _currentRule.WaveDuration;
    }
}