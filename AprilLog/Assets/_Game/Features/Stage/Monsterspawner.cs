// 담당자 : 정승우
// 설명   : 규칙 기반 몬스터 스폰 -- StageSpawnRule + MonsterPool 사용

// 1차 수정자 : 정승우
// 수정내용 : 웨이브별 스폰량 증가 + 스폰 간격 감소 로직 추가.
//           StartStage() -> StartWave()로 변경. 웨이브 인덱스에 따라 GrowthType 적용.

// 2차 수정자 : 김영찬
// 수정내용 : 타이머를 StagePresenter에서 뿌리는 형태로 변경하여 모든 Model과 View의 시간 어긋남 방지
// WaveSystem의 V를 담당

using System;
using System.Collections.Generic;
using UnityEngine;

// 수정자 : 정승우
// 수정내용 : 공격 타겟 탐색을 살아있는 몬스터 목록 기준으로 변경하고, 같은 라인 좌측 우선 규칙 추가.

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

/// <summary>
/// StageSpawnRule 기반으로 몬스터를 스폰한다.
/// 웨이브가 올라갈수록 GrowthType에 따라 스폰량이 증가하고 간격이 짧아진다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<MonsterAI> OnMonsterDied;

    // ---------- SerializeField ----------
    [Header("스폰 포인트")]
    [Tooltip("화면 상단 밖 스폰 포인트 7개 (왼->오)")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("공격 타겟 선택")]
    [Tooltip("같은 라인으로 판단할 Y 좌표 오차")]
    [SerializeField] private float _sameLineYThreshold = 0.05f;

    [Tooltip("같은 거리로 판단할 제곱거리 오차")]
    [SerializeField] private float _distanceTieThreshold = 0.0001f;

    // ---------- Private ----------
    private List<SpawnTracker> _activeRules = new List<SpawnTracker>();
    private List<MonsterAI> _aliveMonsters = new List<MonsterAI>(32);
    private System.Random _rng;
    private bool _isRunning;

    // ---------- 웨이브 시작 ----------
    public void StartWave(int stageId, int waveIndex, int totalWaves, System.Random rng)
    {
        _rng = rng;
        _isRunning = true;
        _activeRules.Clear();

        var rules = Legacy_DataManager.Instance.StageRepo.GetSpawnRulesForStage(stageId);

        for (int i = 0; i < rules.Count; i++)
        {
            // 웨이브별 스폰량 계산
            int baseAmount = rules[i].SpawnAmount;
            int adjustedAmount = CalculateWaveAmount(
                baseAmount, rules[i].GrowthType, rules[i].GrowthValue, waveIndex);

            // 웨이브 후반으로 갈수록 스폰 간격 짧아짐
            float baseInterval = rules[i].SpawnInterval;
            float adjustedInterval = Mathf.Max(0.2f, baseInterval - (waveIndex * 0.1f));

            _activeRules.Add(new SpawnTracker
            {
                rule = rules[i],
                spawnAmount = adjustedAmount,
                spawnInterval = adjustedInterval,
                timer = 0f,
                aliveCount = 0,
                spawnedThisWave = 0
            });
        }

        Debug.Log($"[Spawner] 웨이브 {waveIndex + 1}/{totalWaves} 시작. 규칙 {_activeRules.Count}개.");
    }

    public void StopSpawning()
    {
        _isRunning = false;
    }

    // ---------- Update ----------
    public void Tick(float deltaTime)
    {
        if (!_isRunning) return;

        for (int i = 0; i < _activeRules.Count; i++)
        {
            var tracker = _activeRules[i];
            tracker.timer += deltaTime;

            if (tracker.timer >= tracker.spawnInterval
                && tracker.aliveCount < tracker.rule.MaxAlive
                && tracker.spawnedThisWave < tracker.spawnAmount)
            {
                tracker.timer = 0f;

                // 한 번에 최대 3마리까지
                int batchSize = Mathf.Min(
                    tracker.spawnAmount - tracker.spawnedThisWave,
                    tracker.rule.MaxAlive - tracker.aliveCount);
                batchSize = Mathf.Min(batchSize, 3);

                for (int s = 0; s < batchSize; s++)
                    SpawnOne(ref tracker);
            }

            _activeRules[i] = tracker;
        }
    }

    // ---------- 웨이브별 스폰량 계산 ----------
    private int CalculateWaveAmount(int baseAmount, string growthType, float growthValue, int waveIndex)
    {
        switch (growthType)
        {
            case "Add":
                // 웨이브마다 고정값 추가. 예: 기본 20, Add 3 -> 웨이브0=20, 웨이브1=23, 웨이브2=26
                return baseAmount + Mathf.RoundToInt(growthValue * waveIndex);

            case "Rate":
                // 웨이브마다 비율 증가. 예: 기본 20, Rate 0.1 -> 웨이브0=20, 웨이브1=22, 웨이브2=24
                float multiplier = 1f + (growthValue * waveIndex);
                return Mathf.RoundToInt(baseAmount * multiplier);

            case "None":
            default:
                return baseAmount;
        }
    }

    // ---------- 스폰 ----------
    private void SpawnOne(ref SpawnTracker tracker)
    {
        // 풀에서 가중치 기반으로 몬스터 뽑기
        int monsterId = Legacy_DataManager.Instance.StageRepo.PickMonsterFromPool(tracker.rule.MonsterPool_ID, _rng);
        if (monsterId < 0) return;

        // 스폰 위치 결정
        int pointIdx = GetSpawnPointIndex(tracker.rule.SpawnPositionType);
        Vector3 pos = _spawnPoints[pointIdx].position;

        string poolKey = $"Monster_{monsterId}";
        var obj = PoolManager.Instance.Spawn(poolKey, pos, Quaternion.identity);
        if (obj == null) return;

        var ai = obj.GetComponent<MonsterAI>();
        if (ai == null) return;

        var characterRepo = Legacy_DataManager.Instance.CharacterRepo;
        var stats = characterRepo.GetCommonStatus(monsterId);
        var monsterStats = characterRepo.GetMonsterStatus(monsterId);
        ai.Initialize(stats, monsterStats, monsterId);
        ai.OnDeath += HandleMonsterDeath;

        tracker.aliveCount++;
        tracker.spawnedThisWave++;
        _aliveMonsters.Add(ai);
    }

    private int GetSpawnPointIndex(string posType)
    {
        if (posType == "RandomAll")
            return _rng.Next(0, _spawnPoints.Length);

        // SP_1 ~ SP_7
        if (posType.StartsWith("SP_"))
        {
            if (int.TryParse(posType.Substring(3), out int idx))
                return Mathf.Clamp(idx - 1, 0, _spawnPoints.Length - 1);
        }

        return _rng.Next(0, _spawnPoints.Length);
    }

    // ---------- 몬스터 사망 ----------
    private void HandleMonsterDeath(MonsterAI monster)
    {
        monster.OnDeath -= HandleMonsterDeath;
        _aliveMonsters.Remove(monster);

        // tracker의 aliveCount 감소
        for (int i = 0; i < _activeRules.Count; i++)
        {
            var t = _activeRules[i];
            if (t.aliveCount > 0)
            {
                t.aliveCount--;
                _activeRules[i] = t;
            }
        }

        OnMonsterDied?.Invoke(monster);
        string poolKey = $"Monster_{monster.MonsterID}";
        PoolManager.Instance.Despawn(poolKey, monster.gameObject);
    }

    // ---------- 조회 ----------
    public bool IsWaveComplete() => !_isRunning || _aliveMonsters.Count == 0;
    public int AliveCount => _aliveMonsters.Count;

    /// <summary>
    /// 공격 시점에만 살아있는 몬스터 목록을 1회 스캔해서 타겟을 찾는다.
    /// 모바일 비용을 줄이기 위해 정렬, LINQ, FindObjects 계열 검색은 사용하지 않는다.
    /// </summary>
    public MonsterAI FindNearestMonster(Vector2 from)
    {
        MonsterAI nearest = null;
        float closestDistSqr = float.MaxValue;

        for (int i = 0; i < _aliveMonsters.Count; i++)
        {
            MonsterAI monster = _aliveMonsters[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
                continue;

            Vector2 monsterPos = monster.transform.position;
            // sqrt 계산을 피하기 위해 실제 거리 대신 제곱거리를 비교한다.
            float distSqr = (monsterPos - from).sqrMagnitude;
            if (IsBetterAttackTarget(monsterPos, distSqr, nearest, closestDistSqr))
            {
                closestDistSqr = distSqr;
                nearest = monster;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 플레이어 공격용 타겟 조회 API.
    /// SkillSystem은 몬스터 목록을 직접 뒤지지 않고 이 메서드만 사용한다.
    /// </summary>
    public bool TryFindAttackTarget(Vector2 from, out MonsterAI target)
    {
        target = FindNearestMonster(from);
        return target != null;
    }

    /// <summary>
    /// 타겟 우선순위:
    /// 1. 현재 후보와 같은 Y 라인이면 왼쪽 몬스터 우선
    /// 2. 같은 라인이 아니면 제곱거리 기준으로 가까운 몬스터 우선
    /// </summary>
    private bool IsBetterAttackTarget(
        Vector2 candidatePos,
        float candidateDistSqr,
        MonsterAI currentBest,
        float currentBestDistSqr)
    {
        if (currentBest == null)
            return true;

        Vector2 bestPos = currentBest.transform.position;
        bool sameLine = Mathf.Abs(candidatePos.y - bestPos.y) <= _sameLineYThreshold;
        if (sameLine)
        {
            if (candidatePos.x < bestPos.x)
                return true;

            if (candidatePos.x > bestPos.x)
                return false;
        }

        if (candidateDistSqr < currentBestDistSqr - _distanceTieThreshold)
            return true;

        return false;
    }
}

// 스폰 규칙별 진행 상태 추적
[System.Serializable]
public struct SpawnTracker
{
    public StageSpawnRuleData rule;
    public int spawnAmount;         // 이번 웨이브 스폰 총 수 (증가량 적용)
    public float spawnInterval;     // 이번 웨이브 스폰 간격 (웨이브 후반 짧아짐)
    public float timer;
    public int aliveCount;
    public int spawnedThisWave;     // 이번 웨이브에서 이미 스폰한 수
}
