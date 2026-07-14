// 담당자 : 김영찬
// WaveSystem MVP의 M 담당
// (구)StageRunner.cs에서 분화

// 1차 수정 : 1차 수정된 StageLoopManager.cs에서 시간과 전투중 상태를 분리하여 이식

// 2차 수정 : WaveCount 삭제 되어 해당 변수 사용 스크립트 삭제

// 수정자 : 김영찬
// 수정내용 : 데모버전 DB에 맞춰 최신화

// 수정자 : 김영찬
// 수정내용 : 인게임 UI에 넘겨줄 정보 이벤트 연결

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
    private InGameRewardManager _rewardManager;
    private Queue<int> _pendingRewards;

    public enum WaveState
    {
        WaveRunning,
        WaveTransition
    }

    private WaveState _state;

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

    private const string REWARD_TRIGGER_WAVECLEAR = "WaveClear";
    private const string REWARD_TRIGGER_ELITEKILL = "EliteKill";
    private const string REWARD_TRIGGER_BOSSKILL = "BossKill";

    // ---------- 특수 웨이브 스폰타입 정의 ----------
    public enum SpawnType
    {
        Normal,
        Rush,
        Elite,
        Gimmick,
        Boss
    }

    // ---------- 스폰 명령서 구조체 ----------
    public struct SpawnCommand
    {
        public int CharacterId;
        public MonsterStageScalingData ScalingData;
        public int AccumulateCount; // 스탯을 몇 번 누적해서 보정할 것인가?
        public SpawnType Type;
        public bool IsContainBattleReward;
        public int TriggerTargetId;
    }

    // ---------- 이벤트 ----------
    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveStopped;
    public event Action OnStageClearTriggered;
    public event Action<Queue<SpawnCommand>, float> OnSpawnRequested;
    public event Action OnDespawnRemainingRequested;
    public event Action<float> OnTimeChanged;
    public event Action<WaveState> OnWaveStateChanged;
    public event Action<SpawnType> OnSpecialWaveEntered;
    public event Action RequestRewardManager;

    // ---------- 생성자 ----------
    public StageModel(StageData stageData, List<StageWaveRuleData> waveRules, System.Random rng, float transitionTime)
    {
        _stageData = stageData;
        _waveRules = waveRules;
        _rng = rng;
        _waveCount = waveRules != null ? waveRules.Count : 0;
        // 챕터 내 스테이지 순서(1~7)를 난이도 스케일 입력으로 쓴다.
        // (옛 Stage_ID % 100은 구 chapterId*100 체계 가정이라 신 불규칙 Stage_ID(1000/1100 체계)에선
        //  0으로 리셋되거나 들쭉날쭉해 스폰 가속/물량이 왜곡됐다. StageData.StageOrder가 정확한 1~7 값.)
        _stageLevel = _stageData.StageOrder;

        _currentWaveIndex = 0;
        _state = WaveState.WaveTransition;
        _waveTransitionDelay = transitionTime;
        _transitionTimer = _waveTransitionDelay; // 시작하자마자 첫 웨이브 진입
    }

    public void SetRewardManager(InGameRewardManager rewardManager)
    {
        _rewardManager = rewardManager;
        FlushPendingRewards();
    }

    // ---------- Update ----------
    public void Tick(float deltaTime)
    {
        if (_state == WaveState.WaveRunning)
        {
            UpdateWaveRunning(deltaTime);
            UpdateSpecialWave(deltaTime); // 💡 정규 스폰과 정지 없이 백그라운드 병렬 작동!
            OnTimeChanged?.Invoke(_currentRule.WaveDuration - _waveTimer);
        }
        else if (_state == WaveState.WaveTransition)
        {
            UpdateWaveTransition(deltaTime);
            OnTimeChanged?.Invoke(_waveTransitionDelay - _transitionTimer);
        }
    }

    // ---------- WaveRunning Logic ----------
    private void UpdateWaveRunning(float deltaTime)
    {
        _waveTimer += deltaTime;
        _spawnTimer += deltaTime;

        // --- 가속 스폰 로직 (제한 없이 무한정 소환) ---
        float currentInterval = Mathf.Max(0.3f,
            _currentRule.SpawnInterval - (_stageLevel * 0.2f) - ((_currentWaveIndex + 1) * 0.1f));
        int currentAmount =
            Mathf.Min(5, _currentRule.SpawnAmount + Mathf.FloorToInt((_stageLevel - 1) / 2f));
        if (TutorialInGameDirector.TryGetTutorialSpawnOverride(out float tutorialInterval, out int tutorialAmount))
        {
            currentInterval = tutorialInterval;
            currentAmount = tutorialAmount;
        }

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
                _state = WaveState.WaveTransition;
                _transitionTimer = 0f;
                OnWaveStateChanged?.Invoke(_state);
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
            OnSpecialWaveEntered?.Invoke(Enum.TryParse(_currentSpecialRule.WaveType, out SpawnType parsedType)
                ? parsedType
                : SpawnType.Normal);
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
                // 웨이브 종료 시 보상 지급
                if (TutorialInGameDirector.ShouldSkipBattleRewardTriggerForTutorial())
                {
                    _isSpecialWaveFinished = true;
                    return;
                }

                var repo = DataManager.Instance.RewardRepo;
                var data = repo.GetBattleRewardTrigger(_currentSpecialRule.SpecialWave_ID);
                if(data == null) return;
                
                if (data.Contains(REWARD_TRIGGER_WAVECLEAR))
                {
                    if (_rewardManager == null)
                    {
                        RequestRewardManager?.Invoke();
                        _pendingRewards ??= new Queue<int>();
                        _pendingRewards.Enqueue(_currentSpecialRule.SpecialWave_ID);
                    }
                    else
                    {
                        _rewardManager.AddBattleReward(_currentSpecialRule.SpecialWave_ID);
                    }
                }

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
            float spawnInterval = (_currentSpecialRule.WaveType == "Rush")
                ? _currentSpecialRule.SpecialSpawnInterval
                : 3.0f;

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

            // 일반 웨이브 룰 불러오기
            _currentRule = _waveRules[_currentWaveIndex];
            Debug.Log(
                $"{_currentRule.Stage_ID}의 {_currentRule.WaveOrder} 웨이브룰 삽입. 웨이브 풀 ID : {_currentRule.MonsterWavePool_ID}");

            // 일반 웨이브 상태 초기화
            _waveTimer = 0f;
            float currentInterval = Mathf.Max(0.3f,
                _currentRule.SpawnInterval - (_stageLevel * 0.2f) - ((_currentWaveIndex + 1) * 0.1f));
            if (TutorialInGameDirector.TryGetTutorialSpawnOverride(out float tutorialInterval, out _))
            {
                currentInterval = tutorialInterval;
            }
            _spawnTimer = currentInterval;

            // 각 웨이브에 종속된 특수 웨이브 룰 불러오기
            _currentSpecialRule =
                DataManager.Instance.StageRepo.GetSpecialWaveRuleForStage(_currentRule.SpecialWave_ID);

            // 특수 웨이브 상태 초기화
            if (_currentSpecialRule != null)
            {
                Debug.Log(
                    $"적용된 Special Rule ID : {_currentSpecialRule.SpecialWave_ID}. 추가 웨이브 풀 ID : {_currentSpecialRule.MonsterWavePool_ID}");
                float spawnInterval = (_currentSpecialRule.WaveType == "Rush")
                    ? _currentSpecialRule.SpecialSpawnInterval
                    : 3.0f;
                _specialSpawnTimer = spawnInterval;
            }
            else
            {
                _specialSpawnTimer = 0f;
            }

            _isSpecialWaveTriggered = false;
            _isSpecialWaveFinished = false;
            _specialWaveActiveTimer = 0f;
            // _isBossKilled는 여기서 리셋하지 않는다(스테이지 단위 유지 - StageModel이 스테이지마다 새로 생성됨).
            // 보스 스폰 웨이브(TimeOver)와 보스킬 판정 웨이브(TimeOverOrBossKill)가 분리된 구성에서
            // 판정 웨이브 진입 시 리셋하면 이전 웨이브에서 잡은 보스 킬이 지워져 시간 종료로만 승리하던 버그.

            // 웨이브 상태 변경 및 전파
            _state = WaveState.WaveRunning;
            OnWaveStarted?.Invoke(_currentWaveIndex, _waveCount);
            OnWaveStateChanged?.Invoke(_state);
        }
    }

    // ---------- 연산 함수 ----------
    private SpawnCommand RollDiceForMonster(StageWaveRuleData rule, SpawnType type)
    {
        // 타입 확률(NormalChance/AgileChance 등)은 데이터에서 0~1 분수다. roll도 0~1로 맞춘다.
        // (기존 *100은 roll 0~100 vs 누적합 ~1.0 이라 거의 항상 첫 분기 실패 → 전부 Normal로 폴백됐다.)
        float roll = (float)_rng.NextDouble();
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
        if (poolId < 0) return new SpawnCommand { CharacterId = -1 };

        int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
        var scalingData = DataManager.Instance.StageRepo.GetScalingForStage(_stageData.Stage_ID, rule.MonsterWavePool_ID);

        // 현재 스테이지와 시작 스테이지의 차이를 계산하여 누적 횟수 산출
        int accumulateCount = 0;
        if (scalingData != null)
        {
            // 예: Start가 101이고 현재가 105라면 -> 105 - 101 = 4번 누적!
            // (만약 101스테이지 당시에 1번 누적하고 싶다면 마지막에 +1을 해주면 됩니다)
            accumulateCount = Mathf.Max(0, _stageData.Stage_ID - scalingData.StartStage_ID);
        }

        return new SpawnCommand
        {
            CharacterId = characterId,
            ScalingData = scalingData,
            AccumulateCount = accumulateCount, // 💡 산출된 누적 횟수 포장
            Type = type,
            IsContainBattleReward = false
        };
    }

    private void ExecuteSpecialSpawn()
    {
        Queue<SpawnCommand> specialQueue = new Queue<SpawnCommand>();

        SpawnType sType = Enum.TryParse(_currentSpecialRule.WaveType, out SpawnType parsedType)
            ? parsedType
            : SpawnType.Normal;

        // 특수 웨이브 타입에 따른 한 틱당 물량 설정.
        // 러시는 원래 2마리(기획 4-1-2)였으나 '몬스터 총량 50% 너프' 요청으로 1마리로 하향.
        // (총량 = 틱 수 × 한 틱당 물량 → 물량 절반이면 총량도 절반. EndType가 Instant/Duration/WaveEnd 무엇이든 동일하게 50% 감소,
        //  초당 생성률도 10→5마리/초로 떨어져 화면 과밀도 완화. 되돌리려면 러시만 2로 복구)
        int spawnAmount = _currentSpecialRule.SpecialSpawnAmount;

        // 특수 웨이브는 일반 풀이 아니라, 특수 룰에 지정된 전용 풀을 사용!
        // 단, 룰의 MonsterWavePool_ID는 웨이브풀 '그룹' ID(1006~1008)라서 GetMonsterPoolId로
        // 실제 몬스터풀 ID(6~8)로 변환해야 한다. (직접 넘기면 풀 미발견 → 특수 웨이브 전부 스폰 실패였음)
        int poolId = -1;
        if (_currentSpecialRule.MonsterWavePool_ID > 0)
            poolId = DataManager.Instance.StageRepo.GetMonsterPoolId(_currentSpecialRule.MonsterWavePool_ID,
                _currentSpecialRule.WaveType);

        // 특수 웨이브가 그룹 미지정(MonsterWavePool_ID=0)이면 현재 정규 웨이브의 풀로 폴백한다(물량러시 의도).
        // (옛 하드코딩 1001은 신 풀ID 체계(1000xxx/1001xxx)에 존재하지 않아 항상 -1 → 러시가 빈 웨이브로 떨어졌음.)
        if (poolId < 0 && _currentRule != null)
            poolId = DataManager.Instance.StageRepo.GetMonsterPoolId(_currentRule.MonsterWavePool_ID, "Normal");

        if (poolId < 0) return; // 그래도 못 찾으면 스폰 포기 (경고 스팸 방지)

        // 특수 웨이브 보상 관련 : 웨이브 클리어가 보상 트리거인 경우는 위의 UpdateSpecialWave에서 정리 함
        // 전투 보상 중 특수 웨이브에 관여 된 트리거는 웨이브 클리어, 보스 킬, 엘리트 킬
        // 보스 킬, 엘리트 킬은 몬스터 사망 체인에 보상 지급을 호출해야되므로 스폰커멘드 구조체에 해당 트리거 ID를 삽입 하도록 함

        bool shouldAttachBattleReward = !TutorialInGameDirector.ShouldSkipBattleRewardTriggerForTutorial();

        for (int i = 0; i < spawnAmount; i++)
        {
            int characterId = DataManager.Instance.StageRepo.PickMonsterFromPool(poolId, _rng);
            // 스케일링 테이블의 MonsterPool_ID 열에는 실제로 웨이브풀 ID가 들어 있다(일반 웨이브 수정과 동일 기준).
            // resolved poolId를 넘기면 매칭이 안 돼 특수 웨이브(러시/보스)만 스테이지 보정을 못 받는다.
            var scalingData = DataManager.Instance.StageRepo.GetScalingForStage(_stageData.Stage_ID, _currentSpecialRule.MonsterWavePool_ID);
            int accumulateCount =
                (scalingData != null) ? Mathf.Max(0, _stageData.Stage_ID - scalingData.StartStage_ID) : 0;

            if (characterId > 0)
            {
                specialQueue.Enqueue(new SpawnCommand
                {
                    CharacterId = characterId,
                    ScalingData = scalingData,
                    AccumulateCount = accumulateCount,
                    Type = sType,
                    IsContainBattleReward = shouldAttachBattleReward,
                    TriggerTargetId = _currentSpecialRule.SpecialWave_ID
                });
            }
        }

        if (specialQueue.Count > 0)
        {
            float baseStagger = _currentSpecialRule.SpecialSpawnInterval * 0.2f;
            float variance = 0.3f + (float)_rng.NextDouble() * 0.7f;
            float delay = (sType == SpawnType.Rush) ? baseStagger * variance : 0.2f;
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

    // 보상 매니저를 못찾았을 경우 차후 지급
    private void FlushPendingRewards()
    {
        if(_pendingRewards == null || _pendingRewards.Count == 0) return;
        while (_pendingRewards.Count > 0)
        {
            _rewardManager.AddBattleReward(_pendingRewards.Dequeue());
        }
    }
}
