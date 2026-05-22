// 담당자 : 정승우
// 설명   : InGame 씬 초기화 -- Repository -> Model -> System 순서

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정

using UnityEngine;

/// <summary>
/// InGame 씬 로드 후 모든 시스템을 의존성 순서대로 초기화한다.
/// 이어하기 데이터가 있으면 세이브에서 복원.
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    // ---------- SerializeField ----------
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

        // [1] Repository 초기화는 DataManager가 싱글톤 화 되었음으로 필요 없음 - 수정자 : 김영찬
        
        // [2] 이어하기 체크
        bool isResume = GameManager.Instance != null && GameManager.Instance.HasLocalSave();
        InGameSaveData saveData = null;

        if (isResume)
        {
            saveData = GameManager.Instance.LoadLocalSaveData();
            Debug.Log("[InGameBootstrap] 이어하기 데이터 로드됨");
        }

        // [3] Model 초기화
        var playerStats = DataManager.Instance.CharacterRepo.GetCommonStatus(1);  // 주인공 ID = 1
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