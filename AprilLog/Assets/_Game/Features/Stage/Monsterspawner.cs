// 담당자 : 정승우
// 설명   : 규칙 기반 몬스터 스폰 -- StageSpawnRule + MonsterPool 사용

// 1차 수정자 : 정승우
// 수정내용 : 웨이브별 스폰량 증가 + 스폰 간격 감소 로직 추가.
//           StartStage() -> StartWave()로 변경. 웨이브 인덱스에 따라 GrowthType 적용.

// 2차 수정자 : 김영찬
// 수정내용 : 타이머를 StagePresenter에서 뿌리는 형태로 변경하여 모든 Model과 View의 시간 어긋남 방지
// WaveSystem의 V를 담당

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 수정자 : 정승우
// 수정내용 : 공격 타겟 탐색을 살아있는 몬스터 목록 기준으로 변경하고, 같은 라인 좌측 우선 규칙 추가.

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 수정자 : 김영찬
// 몬스터 및 웨이브 관련 DB에 맞춰 소환 로직 최신화 및 책임 분산

/// <summary>
/// StageSpawnRule 기반으로 몬스터를 스폰한다.
/// 웨이브가 올라갈수록 GrowthType에 따라 스폰량이 증가하고 간격이 짧아진다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<MonsterAI, bool> OnMonsterDied;
    public event Action IsBossDeath;

    // ---------- SerializeField ----------
    [Header("고정 스폰 포인트")]
    [Tooltip("화면 상단 밖 스폰 포인트 7개 (왼->오)")]
    [SerializeField] private Transform[] _spawnPoints;
    
    [Header("일반 소환 라인 지정")]
    [Tooltip("일반 소환은 지정된 라인 위에서 작동함 (Y값 지정)")]
    [SerializeField] private float _normalSpawnLineY;
    [Tooltip("일반 소환은 지정된 라인 위에서 작동함 (X 범위 최소값 지정)")]
    [SerializeField] private float _normalSpawnLineXMin;
    [Tooltip("일반 소환은 지정된 라인 위에서 작동함 (X 범위 최대값 지정)")]
    [SerializeField] private float _normalSpawnLineXMax;

    [Header("참조")]
    [Tooltip("스폰된 몬스터가 공격할 플레이어. 비어 있으면 런타임에 자동 탐색")]
    [SerializeField] private PlayerModel _playerModel;

    [Header("공격 타겟 선택")]
    [Tooltip("같은 거리로 판단할 제곱거리 오차")]
    [SerializeField] private float _distanceTieThreshold = 0.0001f;

    // ---------- Private ----------
    private List<MonsterAI> _aliveMonsters = new List<MonsterAI>(32);
    private System.Random _rng;
    
    // ---------- For Gizmo ----------
    public Transform[] SpawnPoints => _spawnPoints;
    public float NormalSpawnLineY => _normalSpawnLineY;
    public float NormalSpawnLineXMin => _normalSpawnLineXMin;
    public float NormalSpawnLineXMax => _normalSpawnLineXMax;

    // ---------- Update ----------
    public void Tick(float deltaTime)
    {

    }

    // 일반 스폰 라인(상단 Y + 좌우 X 범위)을 외부에서 설정. InGameBootstrap이 카메라 기준으로 맞춤.
    public void SetNormalSpawnLine(float y, float xMin, float xMax)
    {
        _normalSpawnLineY = y;
        _normalSpawnLineXMin = xMin;
        _normalSpawnLineXMax = xMax;
    }

    // ---------- 스폰 ----------
    
    /// <summary>
    /// 모델에서 받아오는 소환 정보(중재자 릴레이)
    /// </summary>
    /// <param name="queue">모델에서 지정해준 스폰 정보</param>
    /// <param name="spawnDelay">이번 queue의 스폰 딜레이(코루틴 참조용)</param>
    public void SpawnMonsterBatch(Queue<StageModel.SpawnCommand> queue, float spawnDelay)
    {
        StartCoroutine(ProcessSpawnQueue(queue, spawnDelay));
    }
    
    // 순차 소환 코루틴
    private IEnumerator ProcessSpawnQueue(Queue<StageModel.SpawnCommand> queue, float delay)
    {
        Debug.Log($"{delay}초의 간격을 두고 총 {queue.Count}만큼 생산명령 접수.");
        while (queue.Count > 0)
        {
            var cmd = queue.Dequeue();
            Vector3 spawnPos = PickSpawnPosition(cmd.Type);
            
            bool isBoss = cmd.Type == StageModel.SpawnType.Elite || cmd.Type == StageModel.SpawnType.Boss;
            
            var ai = SpawnMonster(cmd.CharacterId, spawnPos, isBoss);
            Debug.Log($"Monster ID : {cmd.CharacterId} 소환됨");

            if (ai != null && cmd.ScalingData != null)
            {
                // 💡 [수정] 스케일링 데이터와 함께 '누적 횟수'도 전달!
                ai.ApplyScaling(cmd.ScalingData, cmd.AccumulateCount);
            }

            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }
    }

    private Vector3 PickSpawnPosition(StageModel.SpawnType type)
    {
        _rng ??= new System.Random();

        // 일반(Normal)이나 물량(Rush) 몬스터는 바운더리 안에서 무작위 스폰!
        if (type == StageModel.SpawnType.Normal || type == StageModel.SpawnType.Rush)
        {
            float randomProgress = (float)_rng.NextDouble();
            float randomX = Mathf.Lerp(_normalSpawnLineXMin, _normalSpawnLineXMax, randomProgress);
            return new Vector3(randomX, _normalSpawnLineY, 0f);
        }
        
        // 차후에 보스 연출이나 엘리트/기믹 소환 시에는 고정 포인트 사용!
        // 배열 안전성 방어 체크
        if (_spawnPoints == null || _spawnPoints.Length == 0) 
        {
            float randomX = Mathf.Lerp(_normalSpawnLineXMin, _normalSpawnLineXMax, (float)_rng.NextDouble());
            return new Vector3(randomX, _normalSpawnLineY, 0f);
        }

        // 고정 포인트 중 하나를 무작위로 고름
        int idx = _rng.Next(0, _spawnPoints.Length);
        return _spawnPoints[idx] != null ? _spawnPoints[idx].position : Vector3.zero;
    }

    private MonsterAI SpawnMonster(int characterId, Vector3 position, bool isBoss)
    {
        var characterRepo = DataManager.Instance.CharacterRepo;
        var stats = characterRepo.GetCommonStatus(characterId);
        var monsterStats = characterRepo.GetMonsterStatus(characterId);

        string poolKey = ResolveMonsterPoolKey(characterId, monsterStats);

        var obj = PoolManager.Instance.Spawn(poolKey, position, Quaternion.identity);
        if (obj == null) return null;

        var ai = obj.GetComponent<MonsterAI>();
        if (ai != null)
        {
            ai.Initialize(stats, monsterStats, characterId, isBoss);
            ai.SetPlayerModel(ResolvePlayerModel());
            ai.OnDeath += HandleMonsterDeath;
            _aliveMonsters.Add(ai);
        }
        return ai;
    }
    
    public void StopSpawning()
    {
        StopAllCoroutines(); // 진행 중인 시차 소환 정지
    }
    
    // ---------- 필드 청소 로직 ----------
    public void DespawnAllAliveMonsters()
    {
        // 💡 중요: 리스트를 순회하면서 지울 때는 역순(for문 감소)으로 순회해야 에러가 안 납니다.
        for (int i = _aliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterAI monster = _aliveMonsters[i];
            
            // 이벤트 해제
            monster.OnDeath -= HandleMonsterDeath;
            
            var monsterStats = DataManager.Instance.CharacterRepo.GetMonsterStatus(monster.MonsterID);
            string poolKey = ResolveMonsterPoolKey(monster.MonsterID, monsterStats);
            
            // 강제 소멸 (경험치를 주거나 OnDeath 이벤트를 터뜨리지 않음)
            PoolManager.Instance.Despawn(poolKey, monster.gameObject);
        }
        _aliveMonsters.Clear();
    }

    /// <summary>
    /// Character_ID → 오브젝트 풀 키.
    /// 1순위: 몬스터 데이터(MonsterStatusData.PrefabKey)에 지정된 값
    /// 2순위: 비어 있으면 "Monster_{Character_ID}" 관례로 폴백
    /// </summary>
    private static string ResolveMonsterPoolKey(int characterId, MonsterStatusData monsterStats)
    {
        if (monsterStats != null && !string.IsNullOrEmpty(monsterStats.PrefabKey))
            return monsterStats.PrefabKey;

        return $"Monster_{characterId}";
    }

    private PlayerModel ResolvePlayerModel()
    {
        if (_playerModel == null)
            _playerModel = FindFirstObjectByType<PlayerModel>();

        return _playerModel;
    }

    // 스폰 포인트 7개 중 무작위 위치 선택 (위치 미지정 스폰용)
    private Vector3 PickSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0) return Vector3.zero;
        _rng ??= new System.Random();
        
        int idx = _rng.Next(0, _spawnPoints.Length);
        return _spawnPoints[idx] != null ? _spawnPoints[idx].position : Vector3.zero;
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
    private void HandleMonsterDeath(MonsterAI monster, bool isKamikaze, bool isBoss)
    {
        monster.OnDeath -= HandleMonsterDeath;
        _aliveMonsters.Remove(monster);

        OnMonsterDied?.Invoke(monster, isKamikaze);
        
        if(isBoss)
        {
            IsBossDeath?.Invoke();
        }

        // 스폰 때와 동일한 키 해석을 써야 풀이 어긋나지 않는다.
        var monsterStats = DataManager.Instance.CharacterRepo.GetMonsterStatus(monster.MonsterID);
        string poolKey = ResolveMonsterPoolKey(monster.MonsterID, monsterStats);
        PoolManager.Instance.Despawn(poolKey, monster.gameObject);
    }

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

    /// <summary>엘리트/보스 우선 타겟팅 (번개 벼락 404). 살아있는 보스가 있으면 그 중 최단거리, 없으면 일반 최단거리.
    /// (현재 Elite/Boss 모두 isBoss=true로 스폰되므로 사실상 '엘리트·보스 우선'.)</summary>
    public bool TryFindPriorityTarget(Vector2 from, out MonsterAI target)
    {
        target = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < _aliveMonsters.Count; i++)
        {
            MonsterAI m = _aliveMonsters[i];
            if (m == null || !m.gameObject.activeInHierarchy || !m.IsBoss) continue;
            float sq = ((Vector2)m.transform.position - from).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; target = m; }
        }
        if (target != null) return true;
        return TryFindAttackTarget(from, out target);   // 보스 없으면 최단거리
    }

    /// <summary>
    /// 살아있는 몬스터 읽기 전용 목록 (장판/광역 스킬 판정용).
    /// 항목이 null이거나 비활성일 수 있으므로 호출 측에서 거를 것.
    /// </summary>
    public IReadOnlyList<MonsterAI> AliveMonsters => _aliveMonsters;

    /// <summary>
    /// 타겟 우선순위 (전투 기획 5-3):
    /// 1. 거리가 가장 가까운 몬스터
    /// 2. 거리가 같을 때 → 좌측(작은 x) 우선
    /// 3. 위치가 완전 동일할 때 → 먼저 스폰된 몬스터 우선
    ///    (_aliveMonsters는 스폰 순서대로 추가되고, 더 나을 때만 교체하므로 먼저 들어온 것이 유지됨)
    /// </summary>
    private bool IsBetterAttackTarget(
        Vector2 candidatePos,
        float candidateDistSqr,
        MonsterAI currentBest,
        float currentBestDistSqr)
    {
        if (currentBest == null)
            return true;

        // 1순위: 더 가까운 몬스터
        if (candidateDistSqr < currentBestDistSqr - _distanceTieThreshold)
            return true;
        if (candidateDistSqr > currentBestDistSqr + _distanceTieThreshold)
            return false;

        // 거리가 같을 때 → 2순위: 좌측 우선
        Vector2 bestPos = currentBest.transform.position;
        if (candidatePos.x < bestPos.x)
            return true;
        if (candidatePos.x > bestPos.x)
            return false;

        // 위치 완전 동일 → 3순위: 먼저 스폰된 것 유지
        return false;
    }
}