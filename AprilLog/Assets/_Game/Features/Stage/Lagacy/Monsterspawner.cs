// 담당자 : 정승우
// 설명   : 시간 기반 몬스터 스폰 + 살아있는 몬스터 추적

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 데이터에 따라 시간 간격으로 몬스터를 스폰한다.
/// 몬스터 처치와 무관하게 시간 기반으로 스폰됨.
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

    // ---------- Private ----------
    private List<StageMonsterData> _waveMonsters;
    private int _spawnIndex;
    private float _spawnTimer;
    private List<MonsterAI> _aliveMonsters = new List<MonsterAI>(32);

    // ---------- 웨이브 제어 ----------
    public void StartWave(List<StageMonsterData> allMonsters, int waveIndex)
    {
        _waveMonsters = new List<StageMonsterData>();
        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i].WaveIndex == waveIndex)
                _waveMonsters.Add(allMonsters[i]);
        }

        _spawnIndex = 0;
        _spawnTimer = 0f;
    }

    private void Update()
    {
        if (_waveMonsters == null || _spawnIndex >= _waveMonsters.Count) return;

        _spawnTimer += Time.deltaTime;

        var next = _waveMonsters[_spawnIndex];
        if (_spawnTimer >= next.SpawnDelay)
        {
            SpawnMonster(next);
            _spawnTimer = 0f;
            _spawnIndex++;
        }
    }

    private void SpawnMonster(StageMonsterData data)
    {
        int pointIdx = Mathf.Clamp(data.SpawnPoint - 1, 0, _spawnPoints.Length - 1);
        Vector3 pos = _spawnPoints[pointIdx].position;

        string poolKey = $"Monster_{data.MonsterID}";
        var obj = PoolManager.Instance.Spawn(poolKey, pos, Quaternion.identity);
        if (obj == null) return;

        var ai = obj.GetComponent<MonsterAI>();
        if (ai == null) return;

        var stats = _characterRepo.GetCommonStatus(data.MonsterID);
        ai.Initialize(stats, data.MonsterID);
        ai.OnDeath += HandleMonsterDeath;

        _aliveMonsters.Add(ai);
    }

    private void HandleMonsterDeath(MonsterAI monster)
    {
        monster.OnDeath -= HandleMonsterDeath;
        _aliveMonsters.Remove(monster);

        OnMonsterDied?.Invoke(monster);

        string poolKey = $"Monster_{monster.MonsterID}";
        PoolManager.Instance.Despawn(poolKey, monster.gameObject);
    }

    // 웨이브 완료 판정: 전부 스폰했고 살아있는 몬스터 0
    public bool IsWaveComplete()
    {
        return _waveMonsters != null
            && _spawnIndex >= _waveMonsters.Count
            && _aliveMonsters.Count == 0;
    }

    // SkillSystem이나 BouncingProjectile에서 가장 가까운 몬스터 찾을 때 쓰는 용도
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

    public int AliveCount => _aliveMonsters.Count;
}