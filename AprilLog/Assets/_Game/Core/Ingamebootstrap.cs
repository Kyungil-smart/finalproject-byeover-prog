// 담당자 : 정승우
// 설명   : InGame 씬 초기화 -- Repository -> Model -> System 순서

using UnityEngine;

/// <summary>
/// InGame 씬 로드 후 모든 시스템을 의존성 순서대로 초기화한다.
/// 이어하기 데이터가 있으면 세이브에서 복원.
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("Repository")]
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private StageRepo _stageRepo;
    [SerializeField] private ConfigRepo _configRepo;

    [Header("Model")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private SortModel _sortModel;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private CombinationModel _combinationModel;
    [SerializeField] private EnchantModel _enchantModel;

    [Header("System")]
    [SerializeField] private SortSystem _sortSystem;

    // TODO
    // StageRunner, CombatSystem 등 추가되면 여기에 SerializeField 넣기
    // [SerializeField] private StageRunner _stageRunner;
    // [SerializeField] private CombatSystem _combatSystem;
    // [SerializeField] private InGameGrowthSystem _growthSystem;

    private void Start()
    {
        InitializeAll();
    }

    private void InitializeAll()
    {
        Debug.Log("[InGameBootstrap] === InGame 초기화 시작 ===");

        // [1] Repository
        _characterRepo.Initialize();
        _stageRepo.Initialize();
        _configRepo.Initialize();

        // [2] 이어하기 체크
        bool isResume = GameManager.Instance != null && GameManager.Instance.HasLocalSave();
        InGameSaveData saveData = null;

        if (isResume)
        {
            saveData = GameManager.Instance.LoadLocalSaveData();
            Debug.Log("[InGameBootstrap] 이어하기 데이터 로드됨");
        }

        // [3] Model 초기화
        var playerStats = _characterRepo.GetCommonStatus(1);  // 주인공 ID = 1
        _playerModel.Initialize(playerStats);
        _combinationModel.Initialize();
        _enchantModel.Initialize();

        if (isResume && saveData != null)
        {
            _playerModel.RestoreFromSave(saveData);
            _enchantModel.RestoreFromSave(saveData.acquiredEnchants);
        }

        // [4] Sort 초기화
        int seed;
        if (isResume && saveData != null)
        {
            seed = saveData.nextStageSeed;
        }
        else
        {
            seed = Random.Range(0, int.MaxValue);
        }

        _sortSystem.Initialize(seed);

        Debug.Log("[InGameBootstrap] === InGame 초기화 완료 ===");
    }
}