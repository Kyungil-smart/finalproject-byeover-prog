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

    private void OnEnable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked += HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked += HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_equipButton != null) _equipButton.onClick.AddListener(HandleEquipClicked);
    }

    private void OnDisable()
    {
        if (_listBinder != null) _listBinder.OnSlotClicked -= HandleSlotClicked;
        if (_equipBinder != null) _equipBinder.OnSlotClicked -= HandleSlotClicked;
        if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
        if (_equipButton != null) _equipButton.onClick.RemoveListener(HandleEquipClicked);
    }

    // 장착 버튼 클릭 → 현재 표시 중인 아티팩트의 장착/해제를 컨트롤러에 요청.
    private void HandleEquipClicked()
    {
        OnEquipRequested?.Invoke(CurrentGearId);
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

        // 미보유(비활성) 아티팩트는 상세 정보는 보되 장착/레벨업은 할 수 없다.
        bool owned = IsOwned(gearId);
        if (_equipButton != null) _equipButton.interactable = owned;
        if (_levelUpButton != null) _levelUpButton.interactable = owned;

        // 장착 버튼 라벨 : 이미 장착된 아티팩트면 '해제', 아니면 '장착'.
        if (_equipButtonLabel != null)
            _equipButtonLabel.text = (owned && IsEquipped(gearId)) ? _unequipLabel : _equipLabel;

        OnPopupOpened?.Invoke(gearId);
    }

    // 해당 Gear_ID 를 보유 중인지 판정. 인벤토리 매니저가 없으면 미보유로 간주(버튼 비활성).
    private bool IsOwned(int gearId)
    {
        ArtifactManager mgr = _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

        return mgr != null && mgr.MyArtifacts.Exists(a => a != null && a.MasterId == gearId);
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

    private static Sprite LoadIcon(string iconPath)
        => string.IsNullOrEmpty(iconPath) ? null : Resources.Load<Sprite>(iconPath);

    private static ArtifactGrade ToGrade(string gradeName)
        => Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;

    // 해당 Gear_ID 가 현재 장착 중인지 판정.
    private bool IsEquipped(int gearId)
    {
        ArtifactManager mgr = _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

        return mgr != null && mgr.MyArtifacts.Exists(a => a != null && a.MasterId == gearId && a.IsEquipped);
    }

    public void Close()
    {
        if (_popup != null)
            _popup.SetActive(false);
    }
}
