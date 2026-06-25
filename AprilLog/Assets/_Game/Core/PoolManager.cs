// 담당자 : 정승우
// 설명   : 오브젝트 풀 관리 - Instantiate/Destroy 대신 이걸 쓴다

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터, 투사체, 이펙트, UI 팝업을 풀링한다.
/// 모든 동적 오브젝트는 반드시 여기를 통해 생성/회수.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // ---------- SerializeField ----------
    [Header("풀 설정")]
    [Tooltip("풀링할 프리팹과 초기 수량 목록")]
    [SerializeField] private List<PoolConfig> _configs;

    // ---------- Private ----------
    private Dictionary<string, Queue<GameObject>> _pools;
    private Dictionary<string, GameObject> _prefabs;
    private Dictionary<string, Transform> _containers;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // Boot에서 워밍업한 몬스터 풀(Monster_11..30 등)이 _InGame까지 유지되도록. 없으면 빌드에서 몬스터 미스폰.

        _pools = new Dictionary<string, Queue<GameObject>>();
        _prefabs = new Dictionary<string, GameObject>();
        _containers = new Dictionary<string, Transform>();
    }

    // ---------- 사전 생성 (Bootstrap에서 호출) ----------
    public void WarmUp()
    {
        for (int i = 0; i < _configs.Count; i++)
        {
            var cfg = _configs[i];
            if (string.IsNullOrEmpty(cfg.key) || cfg.prefab == null)
            {
                Debug.LogWarning($"[Pool] configs[{i}]의 key나 prefab이 비어있음. 건너뜀.");
                continue;
            }

            RegisterPool(cfg.key, cfg.prefab, cfg.initialCount);
        }

        Debug.Log($"[Pool] WarmUp 완료. {_pools.Count}개 풀 등록됨.");
    }

    /// <summary>
    /// 런타임에 풀을 보장한다(이미 있으면 무시). 코드로 만든 프리팹/템플릿 등록용.
    /// </summary>
    public void EnsurePool(string key, GameObject prefab, int initialCount)
    {
        if (string.IsNullOrEmpty(key) || prefab == null) return;
        RegisterPool(key, prefab, initialCount);
    }

    private void RegisterPool(string key, GameObject prefab, int count)
    {
        if (_pools.ContainsKey(key)) return;

        _prefabs[key] = prefab;
        _pools[key] = new Queue<GameObject>(count);

        // Hierarchy 정리용 컨테이너
        var container = new GameObject($"Pool_{key}");
        container.transform.SetParent(transform);
        _containers[key] = container.transform;

        for (int i = 0; i < count; i++)
        {
            var obj = CreateNewObject(key);
            obj.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }

    // ---------- Spawn ----------
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
    {
        if (!_pools.ContainsKey(key))
        {
            Debug.LogError($"[Pool] '{key}' 풀이 없음. Inspector에서 configs 확인.");
            return null;
        }

        if (_pools[key].Count == 0)
        {
            // 풀 바닥나면 5개 더 만듦
            ExpandPool(key, 5);
        }

        var obj = _pools[key].Dequeue();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        var poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    // ---------- Despawn ----------
    public void Despawn(string key, GameObject obj)
    {
        if (obj == null) return;

        var poolable = obj.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolable.OnDespawn();
        }

        obj.SetActive(false);

        if (_containers.ContainsKey(key))
        {
            obj.transform.SetParent(_containers[key]);
        }

        if (_pools.ContainsKey(key))
        {
            _pools[key].Enqueue(obj);
        }
    }

    // 시간 지연 후 자동 회수 (이펙트용)
    public void DespawnAfter(string key, GameObject obj, float delay)
    {
        StartCoroutine(DespawnDelayCoroutine(key, obj, delay));
    }

    private IEnumerator DespawnDelayCoroutine(string key, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 이미 누가 회수했을 수 있음
        if (obj != null && obj.activeSelf)
        {
            Despawn(key, obj);
        }
    }

    // ---------- 일괄 회수 ----------

    // 특정 키의 활성 오브젝트 전부 회수 (스테이지 전환 시)
    public void DespawnAll(string key)
    {
        if (!_containers.ContainsKey(key)) return;

        var container = _containers[key];
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            if (child.activeSelf)
            {
                Despawn(key, child);
            }
        }
    }

    // 모든 풀 전부 회수 (씬 전환 시)
    public void DespawnAllPools()
    {
        foreach (var key in new List<string>(_containers.Keys))
        {
            DespawnAll(key);
        }
    }

    // ---------- 확장 ----------
    private void ExpandPool(string key, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = CreateNewObject(key);
            obj.SetActive(false);
            _pools[key].Enqueue(obj);
        }
    }

    private GameObject CreateNewObject(string key)
    {
        var obj = Instantiate(_prefabs[key], _containers[key]);
        obj.name = $"{key}_{_pools[key].Count}";
        return obj;
    }
}

// ---------- 설정 ----------

[System.Serializable]
public class PoolConfig
{
    [Tooltip("풀 식별 키 (예: Monster_11, Projectile_Basic)")]
    public string key;

    [Tooltip("풀링할 프리팹")]
    public GameObject prefab;

    [Tooltip("시작할 때 미리 만들어둘 수량")]
    public int initialCount;
}
