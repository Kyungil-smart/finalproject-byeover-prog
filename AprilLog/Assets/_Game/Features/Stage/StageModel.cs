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
    // ---------- 상태 및 데이터 ----------
    private StageData _stageData;
    private List<StageWaveRuleData> _waveRules;
    private StageWaveRuleData _currentRule;
    private SpecialWaveRuleData _currentSpecialRule; 
    private System.Random _rng;
    
    private enum State { WaveRunning, WaveTransition }
    private State _state;
    
    // 정규 웨이브 변수
    private float _waveTimer;
    private float _spawnTimer;
    private float _transitionTimer;
    private float _waveTransitionDelay;
    
    // 특수 웨이브 변수
    private float _specialWaveActiveTimer; 
    private float _specialSpawnTimer;      
    private bool _isSpecialWaveTriggered;
    private bool _isSpecialWaveFinished;
    private bool _isBossKilled; // 외부(Presenter)에서 찔러주는 보스 사망 여부
    
    private int _currentWaveIndex;
    private int _waveCount;
    private int _stageLevel;
    
    // ---------- 특수 웨이브 스폰타입 정의 ----------
    public enum SpawnType { Normal, Rush, Elite, Gimmick, Boss }
    
    // ---------- 스폰 명령서 구조체 ----------
    public struct SpawnCommand
    {
        public int CharacterId;
        public MonsterStageScalingData ScalingData;
        public int AccumulateCount; // 스탯을 몇 번 누적해서 보정할 것인가?
        public SpawnType Type;
    }
    
    // ---------- 이벤트 ----------
    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveStopped;
    public event Action OnStageClearTriggered;
    public event Action<Queue<SpawnCommand>, float> OnSpawnRequested;
    public event Action OnDespawnRemainingRequested;
    
    // ---------- 생성자 ----------
    public StageModel(StageData stageData, List<StageWaveRuleData> waveRules, System.Random rng, float transitionTime)
    {
        _stageData = stageData;
        _waveRules = waveRules;
        _rng = rng;
        _waveCount = waveRules != null ? waveRules.Count : 0;
        _stageLevel = _stageData.Stage_ID % 100; // 101 -> 1

        _currentWaveIndex = 0;
        _state = State.WaveTransition;
        _waveTransitionDelay = transitionTime;
        _transitionTimer = _waveTransitionDelay; // 시작하자마자 첫 웨이브 진입
    }

    // ---------- Update ----------
    public void Tick(float deltaTime)
    {
        if (_state == State.WaveRunning)
        {
            UpdateWaveRunning(deltaTime);
            UpdateSpecialWave(deltaTime); // 💡 정규 스폰과 정지 없이 백그라운드 병렬 작동!
        }
        else if (_state == State.WaveTransition)
        {
            UpdateWaveTransition(deltaTime);
        }
    }
    
    // ---------- WaveRunning Logic ----------
    private void UpdateWaveRunning(float deltaTime)
    {
        _waveTimer += deltaTime;
        _spawnTimer += deltaTime;
        
        // --- 가속 스폰 로직 (제한 없이 무한정 소환) ---
        int timeFactor = Mathf.FloorToInt(_waveTimer / 20f);
        float currentInterval = Mathf.Max(0.3f, _currentRule.SpawnInterval - (_stageLevel * 0.2f) - ((_currentWaveIndex + 1) * 0.1f) - (timeFactor * 0.1f));
        int currentAmount = Mathf.Min(5, _currentRule.SpawnAmount + Mathf.FloorToInt((_stageLevel - 1) / 2f) + timeFactor);

        // --- 몹 스폰 (제한 없이 무조건 소환) ---
        if (_spawnTimer >= currentInterval)
        {
            _spawnTimer = 0f;
            Queue<SpawnCommand> spawnQueue = new Queue<SpawnCommand>();
            
            for (int i = 0; i < currentAmount; i++)
            {
                SpawnCommand cmd = RollDiceForMonster(_currentRule, SpawnType.Normal);
                if (cmd.CharacterId > 0) spawnQueue.Enqueue(cmd);
            }
            
            if (spawnQueue.Count > 0)
            {
                // 시차 소환: 기준시차(주기×0.2)에 0.3~1.0 가변 배율 적용 (기획 3-6-2)
                float baseStagger = currentInterval * 0.2f;
                float variance = 0.3f + (float)_rng.NextDouble() * 0.7f;
                OnSpawnRequested?.Invoke(spawnQueue, baseStagger * variance);
            }
        }

        // --- 웨이브 종료 판정 (WaveEndType) ---
        bool isWaveEnd = false;
        if (_currentRule.WaveEndType == "TimeOver")
        {
            isWaveEnd = (_waveTimer >= _currentRule.WaveDuration);
        }
        else if (_currentRule.WaveEndType == "TimeOverOrBossKill")
        {
            isWaveEnd = (_waveTimer >= _currentRule.WaveDuration) || _isBossKilled;
        }

        // --- 종료 후처리 (WaveEndAction) ---
        if (isWaveEnd)
        {
            // "DespawnRemaining"이면 남은 몹 청소, "KeepAlive"면 무시
            if (_currentRule.WaveEndAction == "DespawnRemaining")
            {
                OnDespawnRemainingRequested?.Invoke(); 
            }

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
    
    private void UpdateSpecialWave(float deltaTime)
    {
        if (_currentSpecialRule == null || _isSpecialWaveFinished) return;

        // --- 발동 타이밍 체크 (TriggerTime) ---
        if (!_isSpecialWaveTriggered && _waveTimer >= _currentSpecialRule.TriggerTime)
        {
            _isSpecialWaveTriggered = true;
            _specialWaveActiveTimer = 0f;
            _specialSpawnTimer = 0f; // 즉시 스폰을 위해 초기화
        }

        // --- 지속 및 종료 로직 ---
        if (_isSpecialWaveTriggered && !_isSpecialWaveFinished)
        {
            _specialWaveActiveTimer += deltaTime;
            _specialSpawnTimer += deltaTime;

            // 종료 조건 검사 (EndType)
            bool isTimeToEnd = false;
            switch (_currentSpecialRule.EndType)
            {
                case "Instant": break; // 1회성은 아래에서 처리
                case "Duration":
                    if (_specialWaveActiveTimer >= _currentSpecialRule.ActiveDuration) isTimeToEnd = true;
                    break;
                case "WaveEnd": break; // 웨이브 끝날 때까지 유지
            }

            if (isTimeToEnd)
            {
                _isSpecialWaveFinished = true;
                return; 
            }

            // 특수 스폰 실행 (Instant = 1회성). 트리거 직후 한 번만 발동.
            // (float 동등비교 대신 _isSpecialWaveFinished 가드로 1회 보장)
            if (_currentSpecialRule.EndType == "Instant")
            {
                ExecuteSpecialSpawn();
                _isSpecialWaveFinished = true;
                return;
            }

            // Duration / WaveEnd 타입일 때의 주기적 스폰 (WaveType별 간격)
            float spawnInterval = (_currentSpecialRule.WaveType == "Rush") ? 0.2f : 3.0f; // 러시 전용 0.2초 (기획 4-1-2)

            if (_specialSpawnTimer >= spawnInterval)
            {
                _specialSpawnTimer = 0f;
                ExecuteSpecialSpawn();
            }
        }
    }
    
    // ---------- WaveTransition Logic ----------
    private void UpdateWaveTransition(float deltaTime)
    {
        _transitionTimer += deltaTime;

        if (_transitionTimer >= _waveTransitionDelay)
        {
            _transitionTimer = 0f;
            _state = State.WaveRunning;
            _waveTimer = 0f;
            _spawnTimer = 0f;

            // 1. 일반 웨이브 룰 장전
            _currentRule = _waveRules[_currentWaveIndex];

            // 💡 2. 각 웨이브에 종속된 '특수 웨이브 룰' 능동 장전 (조립기 개입 X)
            // (함수명은 프로젝트 구조에 맞게 수정: 예 GetSpecialWaveRuleForWave)
            _currentSpecialRule = DataManager.Instance.StageRepo.GetSpecialWaveRuleForStage(_currentRule.SpecialWave_ID); 

            // 3. 특수 웨이브 상태 초기화
            _isSpecialWaveTriggered = false;
            _isSpecialWaveFinished = false;
            _specialWaveActiveTimer = 0f;
            _specialSpawnTimer = 0f;
            _isBossKilled = false;

            OnWaveStarted?.Invoke(_currentWaveIndex, _waveCount);
        }
    }
    
    // ---------- 연산 함수 ----------
    private SpawnCommand RollDiceForMonster(StageWaveRuleData rule, SpawnType type)
    {
        float roll = (float)(_rng.NextDouble() * 100.0);
        string selectedType = "Normal"; 
        
        float cumulative = rule.NormalChance;
        if (roll <= cumulative) { selectedType = "Normal"; }
        else if (roll <= (cumulative += rule.AgileChance)) { selectedType = "Agile"; }
        else if (roll <= (cumulative += rule.TankChance)) { selectedType = "Tank"; }
        else if (roll <= (cumulative += rule.RangedChance)) { selectedType = "Ranged"; }
        else if (roll <= (cumulative += rule.InfestedChance)) { selectedType = "Infested"; }
        
        int poolId = DataManager.Instance.StageRepo.GetMonsterPoolId(rule.MonsterWavePool_ID, selectedType);
        if (poolId < 0) return new SpawnCommand { CharacterId = -1 }; 
        
        int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
        var scalingData = DataManager.Instance.StageRepo.GetScalingForStage(_stageData.Stage_ID, poolId);

        // 현재 스테이지와 시작 스테이지의 차이를 계산하여 누적 횟수 산출
        int accumulateCount = 0;
        if (scalingData != null)
        {
            // 예: Start가 101이고 현재가 105라면 -> 105 - 101 = 4번 누적!
            // (만약 101스테이지 당시에 1번 누적하고 싶다면 마지막에 +1을 해주면 됩니다)
            accumulateCount = Mathf.Max(0, _stageData.Stage_ID - scalingData.StartStage_ID);
        }

        return new SpawnCommand { 
            CharacterId = characterId, 
            ScalingData = scalingData,
            AccumulateCount = accumulateCount, // 💡 산출된 누적 횟수 포장
            Type = type 
        };
    }
    
    private void ExecuteSpecialSpawn()
    {
        Queue<SpawnCommand> specialQueue = new Queue<SpawnCommand>();
        
        SpawnType sType = Enum.TryParse(_currentSpecialRule.WaveType, out SpawnType parsedType) ? parsedType : SpawnType.Normal;
        
        // 특수 웨이브 타입에 따른 한 틱당 물량 설정
        int spawnAmount = (sType == SpawnType.Rush) ? 2 : 1; // 러시 전용 2마리 (기획 4-1-2)
        
        // 💡 특수 웨이브는 일반 풀이 아니라, 특수 룰에 지정된 전용 풀 ID를 사용!
        int poolId = _currentSpecialRule.MonsterWavePool_ID; 
        
        for (int i = 0; i < spawnAmount; i++)
        {
            int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
            var scalingData = DataManager.Instance.StageRepo.GetScalingForStage(_stageData.Stage_ID, poolId);
            int accumulateCount = (scalingData != null) ? Mathf.Max(0, _stageData.Stage_ID - scalingData.StartStage_ID) : 0;

            if (characterId > 0)
            {
                specialQueue.Enqueue(new SpawnCommand { 
                    CharacterId = characterId, 
                    ScalingData = scalingData,
                    AccumulateCount = accumulateCount,
                    Type = sType 
                });
            }
        }

        if (specialQueue.Count > 0)
        {
            float delay = (sType == SpawnType.Rush) ? 0f : 0.2f;
            OnSpawnRequested?.Invoke(specialQueue, delay);
        }
    }
    
    // ---------- 기타 함수 ----------
    public float GetWaveProgress()
    {
        if (_currentRule == null || _currentRule.WaveDuration <= 0f) return 0f;
        return _waveTimer / _currentRule.WaveDuration;
    }
    
    // 외부(Presenter)에서 보스 처치 소식을 전달할 때 사용
    public void NotifyBossKilled()
    {
        _isBossKilled = true;
    }
}