using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 슬롯(리스트/장착) 단일 클릭을 받아 상세 정보 팝업(POPUP_ArtifactInfo)을 연다. (기획서 4)
//          팝업 표시/닫기까지가 UI 영역이며, 내부 데이터 채우기는 OnPopupOpened(Gear_ID)를 구독해 처리한다.
public class ArtifactDetailPopupPresenter : MonoBehaviour
{
    [Header("슬롯 클릭 소스")]
    [SerializeField] private ArtifactListBinder _listBinder;   // 리스트 슬롯 클릭
    [SerializeField] private ArtifactEquipBinder _equipBinder; // 장착 슬롯 클릭(선택)

    [Header("팝업")]
    [SerializeField] private GameObject _popup;     // POPUP_ArtifactInfo
    [SerializeField] private Button _closeButton;   // 닫기 버튼(선택)

    [Header("보유 시에만 활성화되는 버튼 (미보유면 비활성)")]
    [Tooltip("장착 버튼. 미보유 아티팩트면 interactable=false")]
    [SerializeField] private Button _equipButton;
    [Tooltip("레벨업 버튼. 미보유 아티팩트면 interactable=false")]
    [SerializeField] private Button _levelUpButton;

    [Header("장착 버튼 라벨 (선택)")]
    [Tooltip("장착/해제 상태에 따라 텍스트를 바꿀 장착 버튼 내부 TMP. 비우면 라벨은 바뀌지 않는다.")]
    [SerializeField] private TMP_Text _equipButtonLabel;
    [SerializeField] private string _equipLabel = "장착";
    [SerializeField] private string _unequipLabel = "해제";

    [Header("상세 정보 표시")]
    [SerializeField] private Image _gradeBg;            // 등급 배경
    [SerializeField] private Image _artifactIcon;       // 아티팩트 아이콘
    [SerializeField] private TMP_Text _nameText;        // 이름
    [SerializeField] private TMP_Text _gradeText;       // 등급
    [SerializeField] private TMP_Text _equipAttackText; // 장착 시 공격력
    [SerializeField] private TMP_Text _ownedAttackText; // 보유 시 공격력

    [Header("레벨 / 스탯 (현재 → 다음)")]
    [Tooltip("현재 레벨 (Text_ArtifactCurrentLv)")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("현재 레벨 ATK")]
    [SerializeField] private TMP_Text _curAtkText;
    [Tooltip("현재 레벨 HP")]
    [SerializeField] private TMP_Text _curHpText;
    [Tooltip("다음 레벨 ATK (최대면 MAX)")]
    [SerializeField] private TMP_Text _nextAtkText;
    [Tooltip("다음 레벨 HP (최대면 MAX)")]
    [SerializeField] private TMP_Text _nextHpText;

    [Header("레벨업 비용 (보유 / 소모)")]
    [Tooltip("골드 비용 텍스트. '보유/소모' 형식.")]
    [SerializeField] private TMP_Text _goldCostText;
    [Tooltip("강화석 비용 텍스트. '보유/소모' 형식. (강화석 비용 데이터가 없으면 0)")]
    [SerializeField] private TMP_Text _stoneCostText;
    [Tooltip("골드 아이템 ID (item_master). 70001 = 골드")]
    [SerializeField] private int _goldCostItemId = 70001;
    [Tooltip("강화석 아이템 ID (item_master). 70004 = 강화석")]
    [SerializeField] private int _stoneCostItemId = 70004;
    [Tooltip("골드 보유량 조회용. 비우면 자동 탐색.")]
    [SerializeField] private CurrencyModel _currencyModel;

    [Header("특수능력")]
    [Tooltip("특수능력 헤더 (Text_Header_ArtifactInfo) : '특수능력 이름 Lv.레벨' 형식")]
    [SerializeField] private TMP_Text _specialHeaderText;
    [Tooltip("특수능력 이름이 없을 때 표시할 기본 라벨")]
    [SerializeField] private string _specialFallbackName = "특수능력";
    [SerializeField] private TMP_Text _specialDescText; // 특수능력 설명

    [Header("이름 / 설명 현지화 (선택)")]
    [Tooltip("연결하면 GearName / Explanation 을 현지화 키로 조회. 비우면 임시 문구로 표시한다.")]
    [SerializeField] private LocalizationManager _localization;
    [SerializeField] private string _nameKeyPrefix = "GEAR_NAME_";
    [SerializeField] private string _specialKeyPrefix = "GEAR_SPECIAL_";

    [Header("보유 판정 소스")]
    [Tooltip("비우면 GameStateManager.Instance 의 ArtifactManager 를 사용한다.")]
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("제작 팝업 연동 (선택)")]
    [Tooltip("연결하면 미보유 레전더리 슬롯 클릭은 상세 팝업 대신 제작 팝업(POPUP_ArtifactCraft)을 연다.")]
    [SerializeField] private ArtifactCraftPopupPresenter _craftPopup;

    // 팝업이 열릴 때 발행 (인자 = Gear_ID). 데이터 담당이 구독해 팝업 내용을 채운다.
    public event Action<int> OnPopupOpened;

    // 장착 버튼 클릭 시 발행 (인자 = Gear_ID). 장착 컨트롤러가 구독해 실제 장착/해제를 처리한다.
    public event Action<int> OnEquipRequested;

    public int CurrentGearId { get; private set; }

    // 보유/장착/레벨업 판정에 쓰는 인벤토리 매니저. (인스펙터에 지정 안 하면 GameStateManager 사용)
    private ArtifactManager Manager =>
        _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

    private void OnEnable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked += HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked += HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_equipButton != null) _equipButton.onClick.AddListener(HandleEquipClicked);
        if (_levelUpButton != null) _levelUpButton.onClick.AddListener(HandleLevelUpClicked);
    }

    private void OnDisable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked -= HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked -= HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
        if (_equipButton != null) _equipButton.onClick.RemoveListener(HandleEquipClicked);
        if (_levelUpButton != null) _levelUpButton.onClick.RemoveListener(HandleLevelUpClicked);
    }

    // 장착 버튼 클릭 → 현재 표시 중인 아티팩트의 장착/해제를 컨트롤러에 요청.
    private void HandleEquipClicked()
    {
        OnEquipRequested?.Invoke(CurrentGearId);
        NotifyTutorialEquip();
    }

    // 튜토리얼 아티팩트 장착 단계면 장착으로 다음 단계 진행
    private void NotifyTutorialEquip()
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;
        TutorialStep step = tm.CurrentStep;
        if (step != null && step.advanceMode == TutorialAdvanceMode.GameAction && step.gameAction == TutorialGameAction.ArtifactEquip)
            tm.AdvanceStep();
    }

    private void Awake()
    {
        if (_currencyModel == null)
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (_popup != null)
            _popup.SetActive(false);
    }

    private void HandleSlotClicked(int gearId)
    {
        // 미보유 레전더리 아티팩트는 상세 팝업이 아니라 제작 팝업(POPUP_ArtifactCraft)으로 위임한다.
        if (_craftPopup != null && _craftPopup.IsCraftTarget(gearId))
        {
            _craftPopup.OpenForGear(gearId);
            return;
        }

        CurrentGearId = gearId;

        if (_popup != null)
            _popup.SetActive(true);

        // 이름/등급/장착·보유 능력/특수능력 등 상세 정보를 채운다.
        Populate(gearId);

        // 현재 레벨 / 스탯(ATK·DEF) / 레벨업 비용 + 장착·레벨업 버튼 상태 갱신.
        RefreshOwnedState(gearId);

        OnPopupOpened?.Invoke(gearId);
        NotifyTutorialArtifactOpen();
    }

    // 튜토리얼 아티팩트 선택 단계면 상세창 열림으로 다음 단계 진행
    private void NotifyTutorialArtifactOpen()
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;
        TutorialStep step = tm.CurrentStep;
        if (step != null && step.advanceMode == TutorialAdvanceMode.GameAction && step.gameAction == TutorialGameAction.ArtifactOpen)
            tm.AdvanceStep();
    }

    // 보유 여부에 따른 버튼 활성/라벨 + 레벨·스탯·비용 표시를 갱신한다.
    // (팝업 오픈 시 / 레벨업 직후 공통 호출)
    private void RefreshOwnedState(int gearId)
    {
        ArtifactInstance inst = FindInstance(gearId);
        bool owned = inst != null;

        // 미보유(비활성) 아티팩트는 상세 정보는 보되 장착/레벨업은 할 수 없다.
        if (_equipButton != null) _equipButton.interactable = owned;
        // 레벨업은 보유 + 최대 레벨 미만일 때만.
        if (_levelUpButton != null) _levelUpButton.interactable = owned && inst.CanLevelUp();

        // 장착 버튼 라벨 : 이미 장착된 아티팩트면 '해제', 아니면 '장착'.
        if (_equipButtonLabel != null)
            _equipButtonLabel.text = (owned && inst.IsEquipped) ? _unequipLabel : _equipLabel;

        PopulateLevelStats(gearId, inst);
    }

    // 레벨업 버튼 클릭 → 현재 아티팩트를 1레벨 올리고(매니저가 비용/한도 검사) 표시를 갱신한다.
    private void HandleLevelUpClicked()
    {
        ArtifactManager mgr = Manager;
        ArtifactInstance inst = FindInstance(CurrentGearId);
        if (mgr == null || inst == null) return;

        mgr.RequestUpgrade(inst.UniqueId);   // 강화석 부족/최대 레벨이면 매니저가 무시
        RefreshOwnedState(CurrentGearId);    // 레벨/스탯/비용/버튼 상태 다시 표시
    }

    private ArtifactInstance FindInstance(int gearId)
    {
        ArtifactManager mgr = Manager;
        return mgr != null ? mgr.MyArtifacts.Find(a => a != null && a.MasterId == gearId) : null;
    }

    // 이름/등급/장착·보유 공격력/특수능력 설명을 채운다. (제작·분해 팝업과 동일한 방식)
    private void Populate(int gearId)
    {
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        GearMasterData master = repo != null ? repo.GetGearData(gearId) : null;
        if (master == null)
        {
            Debug.LogWarning($"[ArtifactDetail] 아티팩트 데이터를 찾을 수 없습니다. ID: {gearId}");
            return;
        }

        ArtifactGrade grade = ToGrade(master.GearGrade);

        if (_gradeBg != null)
        {
            _gradeBg.sprite = ArtifactGradeInfo.SlotSprite(grade);
            _gradeBg.color = Color.white;
        }
        if (_artifactIcon != null)
        {
            Sprite icon = LoadIcon(master.IconSpriteKey);
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
    }

    // 현재/다음 레벨 ATK·HP + 골드·강화석 비용(보유/소모) 표시. (inst == null 이면 미보유 → 기본 레벨 기준)
    private void PopulateLevelStats(int gearId, ArtifactInstance inst)
    {
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        GearMasterData master = repo != null ? repo.GetGearData(gearId) : null;
        if (master == null) return;

        GearLevelData levelData = repo.GetGearLevel(gearId);
        int startLevel = levelData != null ? levelData.StartLevel : 1;
        int level = inst != null ? inst.CurrentLevel : startLevel;
        bool isMax = inst != null && !inst.CanLevelUp();

        if (_levelText != null)
            _levelText.text = $"Lv.{level}";

        int atkPer = levelData != null ? levelData.AttackValue : 0;
        int hpPer  = levelData != null ? levelData.MaxHPValue  : 0;

        // 현재 레벨 스탯
        if (_curAtkText != null) _curAtkText.text = ComputeStat(master.AttackBaseAmount, atkPer, level).ToString();
        if (_curHpText != null)  _curHpText.text  = ComputeStat(master.MaxHPBaseAmount,  hpPer,  level).ToString();

        // 다음 레벨 스탯 (최대 레벨이면 MAX)
        if (_nextAtkText != null) _nextAtkText.text = isMax ? "MAX" : ComputeStat(master.AttackBaseAmount, atkPer, level + 1).ToString();
        if (_nextHpText != null)  _nextHpText.text  = isMax ? "MAX" : ComputeStat(master.MaxHPBaseAmount,  hpPer,  level + 1).ToString();

        // 레벨업 비용 (보유 / 소모)
        int ownedGold  = _currencyModel != null ? _currencyModel.Gold
                       : (GameManager.Instance != null ? GameManager.Instance.Gold : 0);
        int ownedStone = Manager != null ? Manager.UpgradeStone : 0;
        SetCostText(_goldCostText,  ownedGold,  CostOf(repo, gearId, level, _goldCostItemId),  inst, isMax);
        SetCostText(_stoneCostText, ownedStone, CostOf(repo, gearId, level, _stoneCostItemId), inst, isMax);

        // 특수능력 헤더 = '이름 Lv.레벨'. 레벨은 돌파 수(AscensionCount) 기준.
        if (_specialHeaderText != null)
        {
            GearSpecialEffectData eff = repo.GetGearSpecialEffect(master.Special_ID);
            string name = eff != null ? ResolveSpecialName(eff.Special) : _specialFallbackName;
            int specialLevel = inst != null ? inst.AscensionCount : 0;
            _specialHeaderText.text = $"{name} Lv.{specialLevel}";
        }
    }

    // 특수능력 코드(Special) → 표시 이름. 데이터에 한글 이름이 없어 코드로 매핑한다.
    // (문구는 확정되면 수정)
    private string ResolveSpecialName(string code) => code switch
    {
        "ATKPercent"      => "공격력 증가",
        "HPPercent"       => "체력 증가",
        "CriticalRate"    => "치명타 확률",
        "GoldBonus"       => "골드 획득",
        "PlainDMG"        => "추가 피해",
        "FireDMG"         => "화염 피해",
        "IceDMG"          => "냉기 피해",
        "LightingDMG"     => "전격 피해",
        "WindDMG"         => "바람 피해",
        "WaterDMG"        => "물 피해",
        "ElementDMG"      => "속성 피해",
        "WaveHealPencent" => "웨이브 회복",
        "AutoDMG"         => "자동 포탑 피해",
        "RecipeDMG"       => "조합 피해",
        "ComboDMG"        => "콤보 피해",
        "Execute"         => "처형",
        "Revive"          => "부활",
        "CastPerKillCount"=> "처치 시 시전",
        "Reroll"          => "리롤",
        _                 => string.IsNullOrEmpty(code) ? _specialFallbackName : code
    };

    private static int CostOf(GearRepo repo, int gearId, int level, int itemId)
    {
        int c = repo.GetGearUpgradeCost(gearId, level, itemId);
        return c > 0 ? c : 0;
    }

    // 비용 표시 : 미보유 '-', 최대 레벨 'MAX', 그 외 '보유/소모'(부족하면 빨강).
    private void SetCostText(TMP_Text text, int owned, int cost, ArtifactInstance inst, bool isMax)
    {
        if (text == null) return;
        if (inst == null) { text.text = "-"; return; }
        if (isMax) { text.text = "MAX"; return; }

        text.text = $"{owned}/{cost}";
        text.color = owned >= cost ? Color.white : Color.red;
    }

    // 시트 정의 : 최종값 = base + (base × perLevel) × (현재레벨 - 1)
    private static int ComputeStat(int baseAmount, int perLevelValue, int level)
    {
        return baseAmount + (baseAmount * perLevelValue) * Mathf.Max(0, level - 1);
    }

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

    // 보유 시 공격력 : 보유 특수효과(OwnedSpecial_ID)의 기본 값(베스트 에포트).
    private int ResolveOwnedAttack(GearMasterData gear)
    {
        GearRepo repo = DataManager.Instance != null ? DataManager.Instance.GearRepo : null;
        if (repo == null) return 0;
        GearSpecialEffectData eff = repo.GetGearSpecialEffect(gear.OwnedSpecial_ID);
        return eff != null ? Mathf.RoundToInt(eff.BaseAmount) : 0;
    }

    private static Sprite LoadIcon(int iconId)
    {
        // ToDo : 아이콘 받아서 경로 확정되면 수정 할 것
        return null;
    }

    private static ArtifactGrade ToGrade(string gradeName)
        => Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;

    public void Close()
    {
        if (_popup != null)
            _popup.SetActive(false);
    }
}
