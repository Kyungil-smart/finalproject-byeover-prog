using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 미보유 아티팩트 제작 정보 팝업
//          - 미보유 레전더리 슬롯 클릭 시 열려 제작 정보 표시
//          - 제작 버튼은 바로 제작하지 않고 공용 확인 팝업(POPUP_CraftBreakthroughConfirm)을 먼저 띄운다.
//          - 확인 시 ArtifactCraftService 가 조각 5개를 차감하고 아티팩트를 보유 상태로 만든다.
//          - 조각 돌파 진입점(RequestBreakthroughWithShard)도 같은 확인 팝업을 재사용한다.
public class ArtifactCraftPopupPresenter : MonoBehaviour
{
    [Header("제작 팝업 (POPUP_ArtifactCraft)")]
    [Tooltip("팝업 루트. 비우면 이 컴포넌트가 붙은 게임오브젝트를 사용한다.")]
    [SerializeField] private GameObject _popup;
    [SerializeField] private Button _closeButton;          // Button_Close

    [Header("아이콘 슬롯 (IconSlot)")]
    [SerializeField] private Image _gradeBg;               // IMG_GradeBg
    [SerializeField] private Image _artifactIcon;          // IMG_ArtifactIcon

    [Header("정보 텍스트")]
    [SerializeField] private TMP_Text _nameText;           // Text_ArtifactName
    [SerializeField] private TMP_Text _gradeText;          // Text_ArtifactGrade
    [SerializeField] private TMP_Text _equipAttackText;    // Text_EquipAttack
    [SerializeField] private TMP_Text _ownedAttackText;    // Text_OwnedAttack
    [SerializeField] private TMP_Text _specialDescText;    // Text_SpecialAbilityDesc

    [Header("제작 비용 슬롯 (CostSlot)")]
    [SerializeField] private Image _pieceIcon;             // IMG_PieceIcon (선택)
    [SerializeField] private TMP_Text _pieceCountText;     // Text_PieceCount ("보유 / 필요")
    [SerializeField] private Image _pieceGauge;            // Gauge_PieceOwned (Image, Type=Filled)

    [Header("제작 버튼 (Button_Craft)")]
    [SerializeField] private Button _craftButton;
    [SerializeField] private TMP_Text _craftButtonLabel;  // 선택(버튼 내부 텍스트)

    [Header("확인 팝업 (공용)")]
    [SerializeField] private CraftBreakthroughConfirmPopup _confirmPopup; // POPUP_CraftBreakthroughConfirm

    [Header("슬롯 클릭 소스 (선택)")]
    [Tooltip("연결하면 미보유 레전더리 슬롯 클릭 시 자동으로 이 팝업을 연다.")]
    [SerializeField] private ArtifactListBinder _listBinder;

    [Header("UI 갱신 대상 (선택)")]
    [Tooltip("조각 돌파 후 리스트를 강제로 다시 그릴 때 사용. 비워도 동작한다.")]
    [SerializeField] private ArtifactListBinder _refreshTargetBinder;

    [Header("이름 / 설명 표시 (선택)")]
    [Tooltip("연결하면 GearName / Explanation 을 현지화 키로 조회. 비우면 임시 문구로 표시한다.")]
    [SerializeField] private LocalizationManager _localization;
    [SerializeField] private string _nameKeyPrefix = "GEAR_NAME_";
    [SerializeField] private string _specialKeyPrefix = "GEAR_SPECIAL_";

    [Header("제작 비용")]
    [SerializeField] private int _craftCost = ArtifactCraftService.DefaultCraftCost;

    [Header("제작 버튼 문구")]
    [SerializeField] private string _craftLabelNormal = "제작";
    [SerializeField] private string _craftLabelShort = "조각 부족";

    [Header("테스트용 버튼 (기획 확인용)")]
    [Tooltip("클릭 시 레전더리 조각 100개를 지급한다.")]
    [SerializeField] private Button _testGiveShardButton;
    [Tooltip("클릭 시 레전더리 조각을 0개로 초기화한다.")]
    [SerializeField] private Button _testResetShardButton;
    [Tooltip("테스트 버튼 1회 지급량")]
    [SerializeField] private int _testGiveAmount = 100;

    private ArtifactCraftService _service;
    private int _currentGearId = -1;
    private bool _isProcessing;
    private ArtifactManager _subscribedManager;

    private ArtifactCraftService Service
    {
        get
        {
            if (_service == null)
                _service = new ArtifactCraftService(_craftCost);
            return _service;
        }
    }

    private GameObject Root => _popup != null ? _popup : gameObject;

    private void Awake()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_craftButton != null) _craftButton.onClick.AddListener(OnClickCraft);
        if (_testGiveShardButton != null) _testGiveShardButton.onClick.AddListener(Test_GiveShards);
        if (_testResetShardButton != null) _testResetShardButton.onClick.AddListener(Test_ResetShards);
    }

    private void OnEnable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked += HandleSlotClicked;
        TrySubscribeInventory();
    }

    private void OnDisable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked -= HandleSlotClicked;
        if (_subscribedManager != null)
        {
            _subscribedManager.OnInventoryUpdated -= HandleInventoryUpdated;
            _subscribedManager = null;
        }
    }

    private void Start()
    {
        Root.SetActive(false);
    }
    
    // 외부(상세 프레젠터 등)에서 사용하는 진입점
    // 이 Gear_ID 가 제작 대상(미보유 레전더리)인지 판정.
    public bool IsCraftTarget(int gearId)
        => Service.IsLegendary(gearId) && !Service.IsOwned(gearId);

    private void HandleSlotClicked(int gearId)
    {
        if (IsCraftTarget(gearId))
            OpenForGear(gearId);
    }

    // 미보유 레전더리 아티팩트 제작 팝업을 연다.
    public void OpenForGear(int gearId)
    {
        // 확인 팝업이 열려 있는 동안 빠른 슬롯 전환은 무시(중복/꼬임 방어).
        if (_confirmPopup != null && _confirmPopup.IsOpen) return;

        if (Service.GetMaster(gearId) == null)
        {
            Debug.LogWarning($"[ArtifactCraftPopup] 아티팩트 데이터를 찾을 수 없습니다. ID: {gearId}");
            return;
        }
        if (Service.IsOwned(gearId))
        {
            Debug.LogWarning($"[ArtifactCraftPopup] 이미 보유 중인 아티팩트입니다. ID: {gearId}");
            return;
        }
        if (!Service.IsLegendary(gearId))
        {
            Debug.LogWarning($"[ArtifactCraftPopup] 레전더리 등급이 아니므로 조각 제작 대상이 아닙니다. ID: {gearId}");
            return;
        }

        _currentGearId = gearId;
        Root.SetActive(true);
        Refresh();
    }
    // 표시 갱신
    private void Refresh()
    {
        if (_currentGearId < 0) return;

        GearMasterData master = Service.GetMaster(_currentGearId);
        if (master == null) return;

        ArtifactGrade grade = ToGrade(master.GearGrade);

        if (_gradeBg != null) _gradeBg.color = ArtifactGradeInfo.SlotColor(grade);
        if (_artifactIcon != null)
        {
            Sprite icon = LoadIcon(master.IconSprite);
            if (icon != null)
            {
                _artifactIcon.sprite = icon;
                _artifactIcon.enabled = true;
            }
        }

        if (_nameText != null) _nameText.text = ResolveName(master);
        if (_gradeText != null) _gradeText.text = ArtifactGradeInfo.DisplayName(grade);
        if (_equipAttackText != null) _equipAttackText.text = $"장착 시 공격력 +{master.AttackBaseAmount}";
        if (_ownedAttackText != null) _ownedAttackText.text = $"보유 시 공격력 +{ResolveOwnedAttack(master)}";
        if (_specialDescText != null) _specialDescText.text = ResolveSpecialDesc(master);

        // 제작 비용 슬롯 (보유 / 필요 + 게이지)
        int owned = Service.OwnedShard;
        int need = Service.CraftCost;
        if (_pieceCountText != null) _pieceCountText.text = $"{owned} / {need}";
        if (_pieceGauge != null) _pieceGauge.fillAmount = Mathf.Clamp01((float)owned / need);

        // 제작 버튼 상태
        bool canCraft = Service.CanCraft(_currentGearId);
        if (_craftButton != null) _craftButton.interactable = canCraft;
        if (_craftButtonLabel != null) _craftButtonLabel.text = canCraft ? _craftLabelNormal : _craftLabelShort;
    }

    
    // 제작 흐름 : 버튼 클릭 → 확인 팝업 → 확정
    
    private void OnClickCraft()
    {
        if (_currentGearId < 0) return;

        if (!Service.CanCraft(_currentGearId))
        {
            Debug.LogWarning("[ArtifactCraftPopup] 조각이 부족합니다.");
            Refresh();
            return;
        }

        // 확인 팝업이 없으면(미연결) 바로 제작, 있으면 한 번 더 확인.
        if (_confirmPopup == null)
        {
            ConfirmCraft(_currentGearId);
            return;
        }
        if (_confirmPopup.IsOpen) return;

        int gearId = _currentGearId; // 빠른 슬롯 전환 대비 캡처
        _confirmPopup.Open(ArtifactConfirmType.Craft, Service.CraftCost, () => ConfirmCraft(gearId));
    }

    private void ConfirmCraft(int gearId)
    {
        if (_isProcessing) return; // 빠른 연속 확정 방어
        _isProcessing = true;

        bool ok = Service.TryCraft(gearId); // 조각 차감 + 보유 추가(OnInventoryUpdated → 리스트 자동 갱신)

        _isProcessing = false;

        if (ok)
            Close(); // 제작 완료 → 팝업 닫기 (리스트/조각 UI 는 이벤트로 갱신됨)
        else
            Refresh();
    }

    
    // 조각 돌파 진입점 (보유 레전더리 돌파 버튼이 호출) — 공용 확인 팝업 재사용
    
    public void RequestBreakthroughWithShard(int uniqueId)
    {
        if (!Service.CanBreakthroughWithShard(uniqueId))
        {
            Debug.LogWarning("[ArtifactCraftPopup] 조각 돌파 조건을 만족하지 않습니다.");
            return;
        }

        if (_confirmPopup == null)
        {
            DoBreakthrough(uniqueId);
            return;
        }
        if (_confirmPopup.IsOpen) return;

        _confirmPopup.Open(ArtifactConfirmType.Breakthrough, Service.BreakthroughCost, () => DoBreakthrough(uniqueId));
    }

    private void DoBreakthrough(int uniqueId)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        bool ok = Service.TryBreakthroughWithShard(uniqueId);

        _isProcessing = false;

        // AttemptAscension 은 OnInventoryUpdated 를 발행하지 않으므로 리스트/팝업을 수동 갱신한다.
        if (ok) ForceRefreshInventoryUI();
    }

    
    // 닫기 / 갱신 / 구독
    
    public void Close()
    {
        // 확인 팝업이 열려 있는 동안에는 제작 팝업을 닫지 않는다(중복 닫힘 방어).
        if (_confirmPopup != null && _confirmPopup.IsOpen) return;

        _currentGearId = -1;
        Root.SetActive(false);
    }

    private void HandleInventoryUpdated()
    {
        // 가챠/제작/분해 등으로 인벤토리가 바뀌면, 팝업이 열려 있을 때만 표시를 갱신한다.
        if (Root.activeSelf) Refresh();
    }

    private void TrySubscribeInventory()
    {
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr == null || mgr == _subscribedManager) return;

        if (_subscribedManager != null)
            _subscribedManager.OnInventoryUpdated -= HandleInventoryUpdated;

        mgr.OnInventoryUpdated += HandleInventoryUpdated;
        _subscribedManager = mgr;
    }

    private void ForceRefreshInventoryUI()
    {
        ArtifactListBinder binder = _refreshTargetBinder != null ? _refreshTargetBinder : _listBinder;
        if (binder != null) binder.RefreshInventory();
        Refresh();
    }

    
    // 표시 헬퍼
    
    private string ResolveName(GearMasterData gear)
    {
        if (_localization != null)
            return _localization.Get(_nameKeyPrefix + gear.Gear_ID);
        return $"{gear.GearGrade} #{gear.Gear_ID}";
    }

    private string ResolveSpecialDesc(GearMasterData gear)
    {
        if (_localization != null)
        {
            string desc = _localization.Get(_specialKeyPrefix + gear.Special_ID);
            if (!string.IsNullOrEmpty(desc)) return desc;
        }

        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        GearSpecialEffectData eff = repo != null ? repo.GetGearSpecialEffect(gear.Special_ID) : null;
        return eff != null ? eff.Special : "특수능력 정보 없음";
    }

    // 보유 시 공격력 : 보유 특수효과(OwnedSpecial_ID)의 기본 값을 사용(베스트 에포트).
    private int ResolveOwnedAttack(GearMasterData gear)
    {
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        if (repo == null) return 0;
        GearSpecialEffectData eff = repo.GetGearSpecialEffect(gear.OwnedSpecial_ID);
        return eff != null ? Mathf.RoundToInt(eff.BaseAmount) : 0;
    }

    private static Sprite LoadIcon(string iconPath)
    {
        return string.IsNullOrEmpty(iconPath) ? null : Resources.Load<Sprite>(iconPath);
    }

    private static ArtifactGrade ToGrade(string gradeName)
    {
        return System.Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;
    }

    // ==================================================================
    // 테스트용 버튼 (기획 확인용) — 버튼 클릭으로 조각 지급/초기화
    // ==================================================================
    // 버튼 클릭 시 레전더리 조각 100개(_testGiveAmount) 지급.
    public void Test_GiveShards()
    {
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr == null) { Debug.LogWarning("[ArtifactCraftPopup] ArtifactManager 없음."); return; }

        mgr.LegendaryShard += _testGiveAmount;
        Debug.Log($"[ArtifactCraftPopup] 테스트: 조각 +{_testGiveAmount} → 현재 {mgr.LegendaryShard}");
        Refresh();
    }

    // 버튼 클릭 시 레전더리 조각을 0개로 초기화.
    public void Test_ResetShards()
    {
        ArtifactManager mgr = GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
        if (mgr == null) { Debug.LogWarning("[ArtifactCraftPopup] ArtifactManager 없음."); return; }

        mgr.LegendaryShard = 0;
        Debug.Log("[ArtifactCraftPopup] 테스트: 조각 0개로 초기화");
        Refresh();
    }
}
