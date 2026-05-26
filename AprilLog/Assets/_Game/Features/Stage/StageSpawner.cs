// 담당자 : 김영찬
// Wave시스템 MVP 모델의 V
// MonsterSpawner 대체

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WavePresenter의 스폰 명령을 유니티 씬 안에서 구현
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<MonsterAI> OnMonsterDied;
    public event Action OnSpawnRoutineFinished;

    // ---------- SerializeField ----------
    [Header("스폰 포인트")]
    [Tooltip("화면 상단 밖 스폰 포인트 7개 (왼->오)")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("참조")]
    [SerializeField] StageLoopManager _stageLoopManager;

    // ---------- Private ----------
    
    private int _spawnIndex;
    private float _spawnTimer;
    private List<MonsterAI> _aliveMonsters = new List<MonsterAI>(32);
    
    // ---------- 이벤트 함수 ----------
    private void Update()
    {
        
    }

    // ---------- 타이머 관련 ----------
    public void GetCurrentTime(float curTime)
    {
        _spawnTimer = curTime;
    }
    
    // ---------- 웨이브 제어 ----------
    public void StartSpawning()
    {
        

        _spawnIndex = 0;
        _spawnTimer = 0f;
    }

    private void SpawnMonster()
    {
        
    }

    private void HandleMonsterDeath(MonsterAI monster)
    {
        monster.OnDeath -= HandleMonsterDeath;
        _aliveMonsters.Remove(monster);

        OnMonsterDied?.Invoke(monster);

        string poolKey = $"Monster_{monster.MonsterID}";
        PoolManager.Instance.Despawn(poolKey, monster.gameObject);
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
