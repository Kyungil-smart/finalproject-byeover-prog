// 담당자 : 김영찬
// 설명   : Repository를 관리하는 싱글톤 데이터 매니저
// 수정자 : 최동훈 - 프리팹 경로 수정
// 수정자 : 정승우 - InitRepo 이중 호출 방지 + 아키텍처 연동

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    private bool _isInitialized;

    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DataManager>();

                if (_instance == null)
                {
#if UNITY_EDITOR
                    string prefabPath = "Assets/_Game/Prefabs/DataManager.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    if (prefab != null)
                    {
                        Debug.LogWarning($"[DataManager] 씬에 없어서 '{prefabPath}'에서 에디터 전용 로드.");
                        GameObject go = Instantiate(prefab);
                        go.name = "DataManager_TestHarness";
                        _instance = go.GetComponent<DataManager>();
                        _instance.InitRepo();
                    }
                    else
                    {
                        Debug.LogError($"[DataManager] '{prefabPath}'에 프리팹 없음. 경로 확인.");
                    }
#else
                    Debug.LogError("[DataManager] 인스턴스 없음. Boot 씬을 통해 진입해야 합니다.");
#endif
                }
            }
            return _instance;
        }
    }
    
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private StageRepo _stageRepo;
    [SerializeField] private ConfigRepo _configRepo;
    
    public CharacterRepo CharacterRepo => _characterRepo;
    public StageRepo StageRepo => _stageRepo;
    public ConfigRepo ConfigRepo => _configRepo;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeFirstScene()
    {
        if (_instance != null) return;
        var _ = Instance;
    }
#endif

    // ---------- 초기화 ----------
    public void InitRepo()
    {
        // 이중 호출 방지
        if (_isInitialized)
        {
            Debug.Log("[DataManager] 이미 초기화됨. 건너뜀.");
            return;
        }

        _characterRepo.Initialize();
        _stageRepo.Initialize();
        _configRepo.Initialize();

        _isInitialized = true;
        Debug.Log("[DataManager] Repository 초기화 완료.");
    }
}