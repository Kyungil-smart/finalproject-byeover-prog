// 담당자 : 김영찬
// 설명 : Repository를 관리하는 싱글톤 타입의 데이터 매니저 개발
// 기대 효과 : 해당 Instance를 통해 Repository를 불러오는 것으로 데이터 파편화 방지 및 휴먼 에러 예방
// 수정 사항 : 에디터 전용 AssetDatabase 동적 로딩 적용으로 데이터메니저가 필요한 단일 테스트씬에서도 부트씬을 거치지 않고 자체 테스트 가능하도록 함
// 최종 수정일 : 26.05.24
// 수정 사항 : 수정자: 최동훈30번째줄 string prefabPath = "Assets/Prefabs/DataManager.prefab"; > "Assets/_Game/Prefabs/DataManager.prefab"; 경로 수정

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;

    // 프로퍼티 호출 시 인스턴스가 없다면 에디터 경로의 프리팹을 강제로 로드합니다.
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
                    // 프리팹 경로 명시
                    string prefabPath = "Assets/_Game/Prefabs/DataManager.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    
                    if (prefab != null)
                    {
                        Debug.LogWarning($"[TestHarness] 씬에 매니저가 없어 '{prefabPath}'에서 에디터 전용으로 동적 로드합니다.");
                        
                        GameObject go = Instantiate(prefab);
                        go.name = "DataManager_TestHarness"; // 테스트 구동용 이름 명시
                        _instance = go.GetComponent<DataManager>();
                        
                        // 테스트 환경이므로 레포지토리 즉시 초기화 트리거
                        _instance.InitRepo();
                    }
                    else
                    {
                        Debug.LogError($"[DataManager] '{prefabPath}' 경로에서 프리팹을 찾을 수 없습니다. 폴더 위치와 프리팹 명칭을 확인해주세요.");
                    }
                    #else
                    // 실제 빌드된 게임(런타임) 환경에서는 반드시 부트 씬을 거쳐야 함을 경고
                    Debug.LogError("[DataManager] 인스턴스가 존재하지 않습니다. 반드시 부트 씬을 통해 진입해야 합니다.");
                    #endif
                }
            }
            return _instance;
        }
    }
    
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private StageRepo _stageRepo;
    [SerializeField] private ConfigRepo _configRepo;
    
    // 읽기 전용
    public CharacterRepo CharacterRepo => _characterRepo;
    public StageRepo StageRepo => _stageRepo;
    public ConfigRepo ConfigRepo => _configRepo;

    private void Awake()
    {
        SetSingleTon();
    }

    #if UNITY_EDITOR
    // 에디터에서 플레이 버튼을 누르는 순간, 첫 씬이 로드(`Awake`)되기도 전에 이 메서드가 강제 선행 실행됩니다.
    // 이를 통해 테스트 씬의 다른 스크립트들이 Start/Awake에서 DataManager를 참조할 때 발생하는 널 익셉션을 완전 차단합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeFirstScene()
    {
        if (_instance != null) return;
        var initTrigger = Instance; // get 프로퍼티를 인위적으로 호출하여 씬 로드 전 조기 메모리 적재
    }
    #endif

    public void InitRepo()
    {
        _characterRepo.Initialize();
        _stageRepo.Initialize();
        _configRepo.Initialize();
    }

    private void SetSingleTon()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); 
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}