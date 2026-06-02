// 담당자 : 정승우
// 설명   : InGame 씬 초기화 -- Repository -> Model -> System 순서

// 1차 수정자 : 김영찬
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정

// 2차 수정자 : 홍정옥
// 수정내용 : 아웃게임 성장 보너스를 PlayerModel에 반영하는 로직 추가

// 3차 수정자 : 정승우
// 수정내용 : 기획서 v1.03 반영. Shield 삭제, Attack/StunPower/SlowPower 보너스 추가.

// 4차 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 5차 수정자 : 김영찬
// 플레이어 스텟 초기화 시 CharacterStatus 반영하도록 수정

using UnityEngine;

/// <summary>
/// InGame 씬 로드 후 모든 시스템을 의존성 순서대로 초기화한다.
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private SortModel _sortModel;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private CombinationModel _combinationModel;
    [SerializeField] private EnchantModel _enchantModel;

    [Header("System")]
    [SerializeField] private SortSystem _sortSystem;

    private void Start()
    {
        InitializeAll();
    }

    private void InitializeAll()
    {
        Debug.Log("[InGameBootstrap] === InGame 초기화 시작 ===");

        // [1] Repository 초기화는 DataManager 싱글톤에서 처리됨 (김영찬)

        // [2] 이어하기 체크
        bool isResume = GameManager.Instance != null && GameManager.Instance.HasLocalSave();
        Legacy_InGameSaveData saveData = null;

        if (isResume)
        {
            saveData = GameManager.Instance.LoadLocalSaveData();
            Debug.Log("[InGameBootstrap] 이어하기 데이터 로드됨");
        }

        // [3] Model 초기화
        var commonStatus = DataManager.Instance.CharacterRepo.GetCommonStatus(1);
        var characterStatus = DataManager.Instance.CharacterRepo.GetCharacterStatus(1);
        _playerModel.Initialize(commonStatus, characterStatus);

        // 아웃게임 성장 보너스 적용 (홍정옥)
        int characterLevel = GetCharacterLevel();
        DataManager.Instance.ConfigRepo.GetOutGrowthBonusUntilLevel(characterLevel,
            out int hpBonus, out int attackBonus, out int stunBonus, out int slowBonus);
        _playerModel.ApplyStatBonus_OutGameBonus(hpBonus, attackBonus, stunBonus, slowBonus);

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
            seed = saveData.nextStageSeed;
        else
            seed = Random.Range(0, int.MaxValue);

        _sortSystem.Initialize(seed);

        Debug.Log("[InGameBootstrap] === InGame 초기화 완료 ===");
    }

    // 클라우드 데이터에서 캐릭터 레벨 가져오기 (홍정옥)
    private int GetCharacterLevel()
    {
        if (GameManager.Instance == null || GameManager.Instance.CloudData == null)
            return 1;
        return GameManager.Instance.CloudData.characterLevel;
    }
}