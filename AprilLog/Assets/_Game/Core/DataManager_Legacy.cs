// 담당자 : 김영찬
// 설명   : Repository를 관리하는 싱글톤 데이터 매니저
// 수정자 : 최동훈 - 프리팹 경로 수정
// 수정자 : 정승우 - InitRepo 이중 호출 방지 + 아키텍처 연동
// 2차 수정 : 해당 파일은 레거시 처리
// 수정자 : 최동훈
// 3차 수정 : 프리팹 경로 수정

using System;
using UnityEngine;

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
                    // 씬에 없으면 Resources에서 로드한다(에디터·빌드 공통).
                    // Legacy_DataManager는 어느 씬에도 없어서, 예전엔 에디터만 AssetDatabase로 자동 로드되고
                    // 빌드(APK)에선 null → 인챈트 등이 Instance.CharacterRepo에서 NRE로 먹통이었다.
                    GameObject prefab = Resources.Load<GameObject>("Legacy_DataManager");
                    if (prefab != null)
                    {
                        GameObject go = Instantiate(prefab);
                        go.name = "Legacy_DataManager";
                        _instance = go.GetComponent<Legacy_DataManager>();
                        _instance.InitRepo();
                    }
                    else
                    {
                        Debug.LogError("[Legacy_DataManager] Resources/Legacy_DataManager 프리팹을 찾지 못함. Assets/Resources/Legacy_DataManager.prefab 확인.");
                    }
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

    // 첫 씬 로드 전에 Resources에서 미리 생성(에디터·빌드 공통). 인챈트 등 어디서 접근해도 null이 아니도록.
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
