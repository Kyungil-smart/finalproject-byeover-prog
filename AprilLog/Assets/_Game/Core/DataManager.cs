// 담당자 : 김영찬
// 설명 : Repository를 관리하는 싱글톤 타입의 데이터 매니저 개발
// 기대 효과 : 해당 Instance를 통해 Repository를 불러오는 것으로 데이터 파편화 방지 및 휴먼 에러 예방

using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    
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

    public void InitRepo()
    {
        _characterRepo.Initialize();
        _stageRepo.Initialize();
        _configRepo.Initialize();
    }

    private void SetSingleTon()
    {
        if (Instance != null)
        {
            Destroy(gameObject); 
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
