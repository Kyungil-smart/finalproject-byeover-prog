// 담당자 : 김영찬
// 설명   : Repository를 관리하는 싱글톤 데이터 매니저
// 수정자 : 최동훈 - 프리팹 경로 수정
// 수정자 : 정승우 - InitRepo 이중 호출 방지 + 아키텍처 연동
// 2차 수정 : 해당 파일은 레거시 처리

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Legacy_DataManager : MonoBehaviour
{
    private static Legacy_DataManager _instance;
    private bool _isInitialized;

    public static Legacy_DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<Legacy_DataManager>();

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
                        _instance = go.GetComponent<Legacy_DataManager>();
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
    
    [SerializeField] private Legacy_CharacterRepo _characterRepo;
    [SerializeField] private Legacy_StageRepo _stageRepo;
    [SerializeField] private Legacy_ConfigRepo _configRepo;
    
    public Legacy_CharacterRepo CharacterRepo => _characterRepo;
    public Legacy_StageRepo StageRepo => _stageRepo;
    public Legacy_ConfigRepo ConfigRepo => _configRepo;
    
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

        InitializeRepo(nameof(_characterRepo), _characterRepo, () => _characterRepo.Initialize());
        InitializeRepo(nameof(_stageRepo), _stageRepo, () => _stageRepo.Initialize());
        InitializeRepo(nameof(_configRepo), _configRepo, () => _configRepo.Initialize());

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
