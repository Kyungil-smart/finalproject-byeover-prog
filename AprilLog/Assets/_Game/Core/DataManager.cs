// 담당자 : 김영찬
// 설명   : Repository를 관리하는 싱글톤 데이터 매니저

// 수정자 : 최동훈 - 프리팹 경로 수정

// 수정자 : 정승우 - InitRepo 이중 호출 방지 + 아키텍처 연동

// 수정자 : 김영찬 - 새로운 DB 반영하여 Repo 분리 및 신설

using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    private bool _isInitialized;

    // Assets/Resources/DataManager.prefab (폴더/확장자 제외한 이름)
    private const string PrefabResourceName = "DataManager";

    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DataManager>();

                if (_instance == null)
                {
                    // 에디터/빌드 공통: Resources에서 자동 로드해 어떤 씬에서 시작해도 데이터가 보장되도록 함
                    GameObject prefab = Resources.Load<GameObject>(PrefabResourceName);

                    if (prefab != null)
                    {
                        Debug.LogWarning($"[DataManager] 씬에 없어서 Resources/'{PrefabResourceName}'에서 자동 로드.");
                        GameObject go = Instantiate(prefab);
                        go.name = "DataManager_AutoSpawn";
                        _instance = go.GetComponent<DataManager>();
                        _instance.InitRepo();
                    }
                    else
                    {
                        Debug.LogError($"[DataManager] Resources/'{PrefabResourceName}' 프리팹 없음. DataManager.prefab을 Resources 폴더에 넣어주세요.");
                    }
                }
            }
            return _instance;
        }
    }
    
    // ---------- SerializeField ----------
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private StageRepo _stageRepo;
    [SerializeField] private ConfigRepo _configRepo;
    [SerializeField] private SpellRepo _spellRepo; //해당 부분은 아직 기획이 넘어오지 않은 Legacy DB임
    
    // ---------- Public ----------
    public CharacterRepo CharacterRepo => _characterRepo;
    public StageRepo StageRepo => _stageRepo;
    public ConfigRepo ConfigRepo => _configRepo;
    public SpellRepo SpellRepo => _spellRepo;
    
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

    // 에디터/빌드 공통: 첫 씬 로드 전에 DataManager를 보장 (에디터에서만 되던 동작을 빌드까지 확장)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeFirstScene()
    {
        if (_instance != null) return;
        var _ = Instance;
    }

    // ---------- 초기화 ----------
    public void InitRepo()
    {
        // 이중 호출 방지
        if (_isInitialized)
        {
            Debug.Log("[DataManager] 이미 초기화됨. 건너뜀.");
            return;
        }

        InitializeRepo(nameof(_characterRepo), _characterRepo, () => _characterRepo.Initialize());
        InitializeRepo(nameof(_stageRepo), _stageRepo, () => _stageRepo.Initialize());
        InitializeRepo(nameof(_configRepo), _configRepo, () => _configRepo.Initialize());
        InitializeRepo(nameof(_spellRepo), _spellRepo, () => _spellRepo.Initialize());

        _isInitialized = true;
        Debug.Log("[DataManager] Repository 초기화 완료.");
    }

    private void InitializeRepo(string repoName, MonoBehaviour repo, Action initializeAction)
    {
        if (repo == null)
        {
            Debug.LogError($"[DataManager] {repoName} is not assigned. Repository initialization skipped.");
            return;
        }

        try
        {
            initializeAction.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DataManager] {repoName} initialization failed.\n{exception}");
        }
    }
}
