// 담당자 : 정승우
// 설명   : 규칙 기반 몬스터 스폰 -- StageSpawnRule + MonsterPool 사용

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StageSpawnRule에 따라 몬스터를 스폰한다.
/// 기존 웨이브별 직접 배치 방식에서 규칙 기반 스폰으로 변경됨.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<MonsterAI> OnMonsterDied;

    // ---------- SerializeField ----------
    [Header("스폰 포인트")]
    [Tooltip("화면 상단 밖 스폰 포인트 7개 (왼->오)")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("참조")]
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private StageRepo _stageRepo;

    // ---------- Private ----------
    private List<SpawnTracker> _activeRules = new List<SpawnTracker>();
    private List<MonsterAI> _aliveMonsters = new List<MonsterAI>(32);
    private System.Random _rng;
    private bool _isRunning;

    // ---------- 스폰 시작 ----------
    public void StartStage(int stageId, System.Random rng)
    {
        _rng = rng;
        _isRunning = true;
        _activeRules.Clear();

        var rules = _stageRepo.GetSpawnRulesForStage(stageId);
        for (int i = 0; i < rules.Count; i++)
        {
            _activeRules.Add(new SpawnTracker
            {
                rule = rules[i],
                timer = 0f,
                aliveCount = 0
            });
        }
    }

    public void StopSpawning()
    {
        _isRunning = false;
    }

    // ---------- Update ----------
    private void Update()
    {
        if (!_isRunning) return;

        for (int i = 0; i < _activeRules.Count; i++)
        {
            var tracker = _activeRules[i];
            tracker.timer += Time.deltaTime;

            if (tracker.timer >= tracker.rule.SpawnInterval
                && tracker.aliveCount < tracker.rule.MaxAlive)
            {
                tracker.timer = 0f;
                int amount = tracker.rule.SpawnAmount;

                for (int s = 0; s < amount; s++)
                {
                    if (tracker.aliveCount >= tracker.rule.MaxAlive) break;
                    SpawnOne(tracker);
                }
            }

            _activeRules[i] = tracker;
        }
    }

    private void SpawnOne(SpawnTracker tracker)
    {
        // 풀에서 가중치 기반으로 몬스터 뽑기
        int monsterId = _stageRepo.PickMonsterFromPool(tracker.rule.MonsterPool_ID, _rng);
        if (monsterId < 0) return;

        // 스폰 위치 결정
        int pointIdx = GetSpawnPointIndex(tracker.rule.SpawnPositionType);
        Vector3 pos = _spawnPoints[pointIdx].position;

        string poolKey = $"Monster_{monsterId}";
        var obj = PoolManager.Instance.Spawn(poolKey, pos, Quaternion.identity);
        if (obj == null) return;

        var ai = obj.GetComponent<MonsterAI>();
        if (ai == null) return;

        var stats = _characterRepo.GetCommonStatus(monsterId);
        ai.Initialize(stats, monsterId);
        ai.OnDeath += HandleMonsterDeath;

        tracker.aliveCount++;
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

    public bool IsStageComplete() => _aliveMonsters.Count == 0;
    public int AliveCount => _aliveMonsters.Count;

    public MonsterAI FindNearestMonster(Vector2 from)
    {
        MonsterAI nearest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _aliveMonsters.Count; i++)
        {
            float dist = Vector2.Distance(from, _aliveMonsters[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = _aliveMonsters[i];
            }
        }

        return nearest;
    }
}

// 스폰 규칙별 진행 상태 추적
[System.Serializable]
public struct SpawnTracker
{
    public StageSpawnRuleData rule;
    public float timer;
    public int aliveCount;
}