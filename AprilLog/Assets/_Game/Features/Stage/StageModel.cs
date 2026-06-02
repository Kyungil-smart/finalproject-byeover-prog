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
    
    /// <summary>
    /// 스폰 요청은 Character_Id를 인자로 가지는 큐를 전달
    /// </summary>
    public event Action<Queue<int>> OnSpawnRequested;
    
    // ---------- 상태 ----------
    private enum State { WaveRunning, WaveTransition }
    private State _state;
    
    // ---------- 기획 시트 데이터 ----------
    private StageData _stageData;
    private List<StageWaveRuleData> _waveRules;
    private StageWaveRuleData _currentRule;
    private SpecialWaveRuleData _specialRule; 
    private System.Random _rng;
    
    // ---------- 타이머 및 상태 데이터 ----------
    private float _waveTimer;
    private float _spawnTimer;
    private float _transitionTimer;
    private float _waveTransitionDelay;
    
    private int _currentWaveIndex;
    private int _waveCount; // ToDo : count 얻는 로직 수정해야 됨
    
    private bool _isSpecialWaveTriggered = false;
    
    // ---------- 생성자 ----------
    public StageModel(StageData stageData , List<StageWaveRuleData> waveRules, SpecialWaveRuleData specialRule,System.Random rng, float transitionTime)
    {
        _stageData = stageData;
        _waveRules = waveRules;
        _rng = rng;
        _currentWaveIndex = 0;
        
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
            
            Queue<int> spawnQueue = new Queue<int>();
            
            for (int i = 0; i < _currentRule.SpawnAmount; i++)
            {
                int characterId = RollDiceForMonster(_currentRule);
                if (characterId > 0)
                {
                    spawnQueue.Enqueue(characterId);
                }
            }
            
            if (spawnQueue.Count > 0)
            {
                OnSpawnRequested?.Invoke(spawnQueue);
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
        
        if (_transitionTimer >= _waveTransitionDelay && aliveMonsterCount == 0)
        {
            _transitionTimer = 0f;
            _state = State.WaveRunning;
            _waveTimer = 0f;
            
            OnWaveStarted?.Invoke(_currentWaveIndex, _waveCount);
        }
    }
    
    private int RollDiceForMonster(StageWaveRuleData rule)
    {
        float roll = (float)(_rng.NextDouble() * 100.0);
        string selectedType = "Normal"; 
        
        float cumulative = rule.NormalChance;
        if (roll <= cumulative)
        {
            selectedType = "Normal";
        }
        else if (roll <= (cumulative += rule.AgileChance))
        {
            selectedType = "Agile";
        }
        else if (roll <= (cumulative += rule.TankChance))
        {
            selectedType = "Tank";
        }
        else if (roll <= (cumulative += rule.RangedChance))
        {
            selectedType = "Ranged";
        }
        else if (roll <= (cumulative += rule.InfestedChance))
        {
            selectedType = "Infested";
        }
        
        int poolId = DataManager.Instance.StageRepo.GetMonsterPoolId(rule.MonsterWavePool_ID, selectedType);
        
        if (poolId < 0) 
        {
            return -1; // 실패시 -1 반환
        }
        
        int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
        
        return characterId;
    }
    
    public float GetWaveProgress()
    {
        if (_currentRule == null || _currentRule.WaveDuration <= 0f) return 0f;
        return _waveTimer / _currentRule.WaveDuration;
    }
}