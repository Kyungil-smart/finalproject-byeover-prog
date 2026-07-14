using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 아티팩트 분해 팝업 전체 흐름을 담당한다. (분해 버튼 → 팝업 방식으로 변경된 신규 플로우)
//          1) 분해 버튼 클릭 → 팝업 열기
//          2) Decomposition_SlotList : 보유 아티팩트를 정렬해 나열, 클릭으로 선택
//          3) Decomposition_SelectSlotList : 선택한 아티팩트 표시(클릭하면 선택 해제)
//          4) RewardPreviewSlot : 선택분을 분해했을 때의 예상 보상(강화석/조각) 표시 (값 있는 종류만)
//          5) Button_DCM : 분해 확인 팝업(예/아니오) → '예' 면 실제 분해
//             Button_Rare&EpicSelect : 보유 레어·에픽 일괄 선택 / 닫기 버튼 : 팝업 닫기
//          실제 분해/보상 지급은 데이터 담당의 ArtifactManager.ManualDisassemble 를 호출한다(타 스크립트 비수정).
//          예상 보상 수치는 ArtifactManager 의 지급식을 그대로 미러링한다(아래 보상 배율 주석 참고).
public class ArtifactDecomposePopupPresenter : MonoBehaviour
{
    [Header("열기 버튼 (Artifacts_Slot / Button_Decomposition)")]
    [SerializeField] private Button _openButton;

    [Header("분해 팝업 (POPUP_Decomposition)")]
    [SerializeField] private GameObject _popup;
    [SerializeField] private Button _closeButton;          // 닫기 버튼
    [Tooltip("팝업 뒤의 보유 슬롯 패널(Artifacts_Slot). 팝업은 오버레이이므로 열고 닫는 동안 항상 켜둔다. " +
             "분해 버튼에 남아있을 수 있는 예전 OnClick(SetActive off)을 덮어쓰기 위함.")]
    [SerializeField] private GameObject _artifactsSlotPanel;

    [Header("보유 목록 / 선택 목록")]
    [Tooltip("Decomposition_SlotList 의 Content (보유 아티팩트 슬롯 생성 부모)")]
    [SerializeField] private Transform _sourceContent;
    [Tooltip("Decomposition_SelectSlotList 의 Content (선택된 슬롯 생성 부모)")]
    [SerializeField] private Transform _selectContent;
    [Tooltip("분해 슬롯 프리팹(ArtifactDecomposeSlotView 부착)")]
    [SerializeField] private ArtifactDecomposeSlotView _slotPrefab;

    [Header("정렬 드롭다운 (Decomposition_SlotList)")]
    [SerializeField] private TMP_Dropdown _sortingDropdown;

    [Header("예상 보상 (RewardPreviewSlot)")]
    [Tooltip("RewardPreviewSlot 들이 생성될 부모")]
    [SerializeField] private Transform _rewardContent;
    [SerializeField] private RewardPreviewSlotView _rewardSlotPrefab;
    [Tooltip("강화석 아이콘(레어·에픽 분해 보상)")]
    [SerializeField] private Sprite _upgradeStoneIcon;
    [Tooltip("조각 아이콘(레전더리 분해 보상)")]
    [SerializeField] private Sprite _shardIcon;

    [Header("동작 버튼")]
    [Tooltip("Button_DCM : 분해 확인 팝업 열기")]
    [SerializeField] private Button _decomposeButton;
    [Tooltip("Button_Rare&EpicSelect : 보유 레어·에픽 일괄 선택")]
    [SerializeField] private Button _rareEpicSelectButton;

    [Header("분해 확인 팝업")]
    [SerializeField] private GameObject _confirmPopup;
    [SerializeField] private Button _confirmYesButton;     // 예 : 분해
    [SerializeField] private Button _confirmNoButton;      // 아니오 : 확인 팝업 닫기

    [Header("데이터 소스")]
    [Tooltip("비우면 GameStateManager.Instance 의 ArtifactManager 를 사용한다.")]
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("예상 보상 배율 (ArtifactManager 지급식과 동일하게 유지)")]
    [Tooltip("레어 1개 분해 시 강화석 수 (ArtifactManager N)")]
    [SerializeField] private int _stonePerRare = 10;
    [Tooltip("에픽 1개 분해 시 강화석 수 (ArtifactManager M)")]
    [SerializeField] private int _stonePerEpic = 20;
    [Tooltip("레전더리 1개 분해 시 조각 수")]
    [SerializeField] private int _shardPerLegendary = 1;

    // 정렬 옵션 인덱스 = 정렬 드롭다운 옵션 순서와 동일해야 한다.
    // 0 기본(ID) / 1 등급 높은순 / 2 등급 낮은순
    private enum SortType { Default, GradeDesc, GradeAsc }

    private readonly List<ArtifactDecomposeSlotView> _sourceSlots = new List<ArtifactDecomposeSlotView>();
    private readonly List<ArtifactDecomposeSlotView> _selectSlots = new List<ArtifactDecomposeSlotView>();
    private readonly List<RewardPreviewSlotView> _rewardSlots = new List<RewardPreviewSlotView>();

    // 선택된 개체 UniqueId 집합
    private readonly HashSet<int> _selectedIds = new HashSet<int>();
    private SortType _currentSort = SortType.Default;

    private void Awake()
    {
        Bind(_openButton, Open, "분해 열기(Button_Decomposition)");
        Bind(_closeButton, Close, "닫기");
        Bind(_decomposeButton, OpenConfirm, "Button_DCM");
        Bind(_rareEpicSelectButton, SelectRareAndEpic, "Button_Rare&EpicSelect");
        Bind(_confirmYesButton, ConfirmDecompose, "확인-예");
        Bind(_confirmNoButton, CloseConfirm, "확인-아니오");

        if (_sortingDropdown != null)
        {
            _sortingDropdown.onValueChanged.RemoveListener(HandleSortChanged);
            _sortingDropdown.onValueChanged.AddListener(HandleSortChanged);
        }
    }

    private void OnEnable()
    {
        SubscribeLocalization();
        UpdateLocalizedDropdown();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedDropdown;
    }

    private void Start()
    {
        SetActive(_popup, false);
        SetActive(_confirmPopup, false);
    }

    private void SubscribeLocalization()
    {
        if (LocalizationManager.Instance == null) return;
        LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedDropdown;
        LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedDropdown;
    }

    private void UpdateLocalizedDropdown()
    {
        if (_sortingDropdown == null) return;

        bool isKorean = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage == "ko"
            : Application.systemLanguage == SystemLanguage.Korean;
        string[] labels = isKorean
            ? new[] { "정렬", "등급순", "이름순", "공격순", "HP순" }
            : new[] { "Sort", "Grade", "Name", "Attack", "HP" };

        int count = Mathf.Min(_sortingDropdown.options.Count, labels.Length);
        for (int i = 0; i < count; i++)
            _sortingDropdown.options[i].text = labels[i];

        _sortingDropdown.RefreshShownValue();
    }

    // ==================================================================
    // 팝업 열기 / 닫기
    // ==================================================================
    public void Open()
    {
        _selectedIds.Clear();
        BuildSourceList();
        RefreshSelectionViews();

        // 보유 슬롯 패널은 팝업 뒤에 그대로 둔다(예전 SetActive off 가 남아있어도 여기서 되돌림).
        SetActive(_artifactsSlotPanel, true);
        SetActive(_confirmPopup, false);
        SetActive(_popup, true);
    }

    public void Close()
    {
        SetActive(_confirmPopup, false);
        SetActive(_popup, false);

        // 팝업을 닫아도 보유 슬롯 패널이 계속 보이도록 복구한다.
        SetActive(_artifactsSlotPanel, true);
    }

    // ==================================================================
    // 보유 목록 (Decomposition_SlotList)
    // ==================================================================
    // 분해 가능 = 보유 중 + 미장착 + 분해 가능 수량(보유 - 본체 1개) > 0
    private void BuildSourceList()
    {
        ClearSourceSlots();

        if (_sourceContent == null || _slotPrefab == null)
        {
            Debug.LogWarning("[ArtifactDecompose] 보유 목록 Content 또는 슬롯 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        ArtifactManager mgr = ResolveManager();
        if (mgr == null)
        {
            Debug.LogWarning("[ArtifactDecompose] ArtifactManager 를 찾지 못해 보유 목록을 만들 수 없습니다.", this);
            return;
        }

        List<ArtifactInstance> decomposable = new List<ArtifactInstance>();
        foreach (ArtifactInstance inst in mgr.MyArtifacts)
        {
            if (inst == null || inst.IsEquipped)
                continue;
            if (inst.CurrentCount - 1 <= 0)        // 본체 1개는 분해 불가
                continue;
            decomposable.Add(inst);
        }

        SortInstances(decomposable);

        foreach (ArtifactInstance inst in decomposable)
        {
            ArtifactGrade grade = ToGrade(inst.MasterData != null ? inst.MasterData.GearGrade : null);
            ArtifactDecomposeSlotView slot = Instantiate(_slotPrefab, _sourceContent);
            slot.Bind(inst, grade, inst.CurrentCount - 1);
            slot.SelectionToggled += ToggleSelection;
            _sourceSlots.Add(slot);
        }
    }

    private void SortInstances(List<ArtifactInstance> list)
    {
        switch (_currentSort)
        {
            case SortType.GradeDesc:
                list.Sort((a, b) => GradeRank(b).CompareTo(GradeRank(a)));
                break;
            case SortType.GradeAsc:
                list.Sort((a, b) => GradeRank(a).CompareTo(GradeRank(b)));
                break;
            default:
                list.Sort((a, b) => a.MasterId.CompareTo(b.MasterId));
                break;
        }
    }

    private void HandleSortChanged(int index)
    {
        _currentSort = (SortType)Mathf.Clamp(index, 0, (int)SortType.GradeAsc);
        BuildSourceList();
        RefreshSelectionViews();   // 정렬 후에도 기존 선택 표시 유지
    }

    // ==================================================================
    // 선택 토글
    // ==================================================================
    private void ToggleSelection(int uniqueId)
    {
        if (!_selectedIds.Remove(uniqueId))
            _selectedIds.Add(uniqueId);

        RefreshSelectionViews();
    }

    // 일괄 선택 : 보유 레어·에픽 전부 선택(이미 만들어진 보유 목록 기준).
    private void SelectRareAndEpic()
    {
        foreach (ArtifactDecomposeSlotView slot in _sourceSlots)
        {
            if (slot != null && (slot.Grade == ArtifactGrade.Rare || slot.Grade == ArtifactGrade.Epic))
                _selectedIds.Add(slot.UniqueId);
        }
        RefreshSelectionViews();
    }

    // 선택 상태를 보유 슬롯 하이라이트 / 선택 목록 / 예상 보상에 한꺼번에 반영.
    private void RefreshSelectionViews()
    {
        foreach (ArtifactDecomposeSlotView slot in _sourceSlots)
        {
            if (slot != null)
                slot.SetSelected(_selectedIds.Contains(slot.UniqueId));
        }

        BuildSelectList();
        BuildRewardPreview();
    }

    // ==================================================================
    // 선택 목록 (Decomposition_SelectSlotList)
    // ==================================================================
    private void BuildSelectList()
    {
        ClearSelectSlots();

        if (_selectContent == null || _slotPrefab == null)
            return;

        ArtifactManager mgr = ResolveManager();
        if (mgr == null)
            return;

        foreach (int uniqueId in _selectedIds)
        {
            ArtifactInstance inst = mgr.MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId);
            if (inst == null)
                continue;

            ArtifactGrade grade = ToGrade(inst.MasterData != null ? inst.MasterData.GearGrade : null);
            ArtifactDecomposeSlotView slot = Instantiate(_slotPrefab, _selectContent);
            slot.Bind(inst, grade, inst.CurrentCount - 1);
            slot.SetSelected(true);
            slot.SelectionToggled += ToggleSelection;   // 선택 목록에서 클릭 시 선택 해제
            _selectSlots.Add(slot);
        }
    }

    // ==================================================================
    // 예상 보상 (RewardPreviewSlot)
    // ==================================================================
    // 강화석 = 레어 분해수 * _stonePerRare + 에픽 분해수 * _stonePerEpic
    // 조각   = 레전더리 분해수 * _shardPerLegendary
    // 값이 0 인 보상 종류는 슬롯을 만들지 않는다(보상이 강화석뿐이면 강화석 슬롯만 보임).
    private void BuildRewardPreview()
    {
        ClearRewardSlots();

        if (_rewardContent == null || _rewardSlotPrefab == null)
            return;

        ArtifactManager mgr = ResolveManager();
        if (mgr == null)
            return;

        int stone = 0;
        int shard = 0;

        foreach (int uniqueId in _selectedIds)
        {
            ArtifactInstance inst = mgr.MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId);
            if (inst == null)
                continue;

            int spare = Mathf.Max(0, inst.CurrentCount - 1);
            switch (ToGrade(inst.MasterData != null ? inst.MasterData.GearGrade : null))
            {
                case ArtifactGrade.Rare: stone += spare * _stonePerRare; break;
                case ArtifactGrade.Epic: stone += spare * _stonePerEpic; break;
                case ArtifactGrade.Legendary: shard += spare * _shardPerLegendary; break;
            }
        }

        if (stone > 0)
            AddRewardSlot(_upgradeStoneIcon, stone);
        if (shard > 0)
            AddRewardSlot(_shardIcon, shard);
    }

    private void AddRewardSlot(Sprite icon, int amount)
    {
        RewardPreviewSlotView slot = Instantiate(_rewardSlotPrefab, _rewardContent);
        slot.SetReward(icon, amount);
        _rewardSlots.Add(slot);
    }

    // ==================================================================
    // 분해 확인 팝업 / 실제 분해
    // ==================================================================
    private void OpenConfirm()
    {
        if (_selectedIds.Count == 0)
        {
            Debug.Log("[ArtifactDecompose] 선택된 아티팩트가 없어 분해를 진행하지 않습니다.");
            return;
        }
        SetActive(_confirmPopup, true);
    }

    private void CloseConfirm() => SetActive(_confirmPopup, false);

    // '예' : 선택한 개체들을 분해 가능 수량만큼 실제 분해 → 팝업 닫기.
    private void ConfirmDecompose()
    {
        ArtifactManager mgr = ResolveManager();
        if (mgr != null)
        {
            // 분해 중 MyArtifacts 가 변할 수 있으니 대상 목록을 먼저 확정한다.
            List<int> targets = new List<int>(_selectedIds);
            foreach (int uniqueId in targets)
            {
                ArtifactInstance inst = mgr.MyArtifacts.Find(a => a != null && a.UniqueId == uniqueId);
                if (inst == null)
                    continue;

                int spare = inst.CurrentCount - 1;
                if (spare > 0)
                    mgr.ManualDisassemble(uniqueId, spare);
            }
        }

        _selectedIds.Clear();
        Close();
    }

    // ==================================================================
    // 헬퍼
    // ==================================================================
    private void ClearSourceSlots() => ClearSlots(_sourceSlots);
    private void ClearSelectSlots() => ClearSlots(_selectSlots);

    private void ClearSlots(List<ArtifactDecomposeSlotView> slots)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i] != null)
            {
                slots[i].SelectionToggled -= ToggleSelection;
                Destroy(slots[i].gameObject);
            }
        }
        slots.Clear();
    }

    private void ClearRewardSlots()
    {
        for (int i = _rewardSlots.Count - 1; i >= 0; i--)
        {
            if (_rewardSlots[i] != null)
                Destroy(_rewardSlots[i].gameObject);
        }
        _rewardSlots.Clear();
    }

    private ArtifactManager ResolveManager()
    {
        if (_artifactManager != null)
            return _artifactManager;
        return GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null;
    }

    private static ArtifactGrade ToGrade(string gradeName)
    {
        return Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;
    }

    private static int GradeRank(ArtifactInstance inst)
    {
        return (int)ToGrade(inst.MasterData != null ? inst.MasterData.GearGrade : null);
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action, string label)
    {
        if (button == null)
        {
            Debug.LogWarning($"[ArtifactDecompose] '{label}' 버튼이 연결되지 않았습니다.", this);
            return;
        }
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SetActive(GameObject go, bool value)
    {
        if (go != null && go.activeSelf != value)
            go.SetActive(value);
    }
}
