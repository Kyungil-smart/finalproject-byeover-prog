using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 아티팩트 장착 / 해제 로직(UI 영역).
//          규칙 : 최대 3개 장착, 빈 칸이 있으면 자동 장착, 가득 차면 교체할 슬롯을 선택해 교체.
//          데이터는 CDH 담당 ArtifactManager 의 public 멤버(MyArtifacts, ArtifactInstance.IsEquipped)만
//          사용한다. (다른 담당자 스크립트는 수정하지 않는다)
//          - 상세 팝업(POPUP_ArtifactInfo)의 장착 버튼 클릭 → 현재 아티팩트 장착/해제(토글)
//          - 장착칸이 가득 차면 ArtifactEquipBinder 를 슬롯 선택 모드로 전환 → 고른 슬롯과 교체
public class ArtifactEquipController : MonoBehaviour
{
    [Header("연동")]
    [Tooltip("FittedArtifacts 3칸 표시 바인더")]
    [SerializeField] private ArtifactEquipBinder _equipBinder;
    [Tooltip("장착 버튼 이벤트 소스(상세 팝업 프레젠터)")]
    [SerializeField] private ArtifactDetailPopupPresenter _detailPopup;
    [Tooltip("장착/해제 후 리스트 갱신용(선택)")]
    [SerializeField] private ArtifactListBinder _listBinder;

    [Header("데이터 매니저 (선택)")]
    [Tooltip("비우면 GameStateManager.Instance 의 ArtifactManager 를 사용한다.")]
    [SerializeField] private ArtifactManager _artifactManager;

    [Header("교체 안내 (선택)")]
    [Tooltip("장착칸이 가득 차 교체할 슬롯을 골라야 할 때 켜는 안내 오브젝트(예: '교체할 슬롯을 선택하세요').")]
    [SerializeField] private GameObject _replaceHint;

    private const int MaxEquip = 3;

    // 각 장착 슬롯에 들어있는 인스턴스(없으면 null). 인덱스 = ArtifactEquipBinder 슬롯 인덱스.
    private readonly ArtifactInstance[] _equipped = new ArtifactInstance[MaxEquip];

    // 장착칸이 가득 찼을 때, 교체를 기다리는(새로 장착하려는) 인스턴스.
    private ArtifactInstance _pending;

    private ArtifactManager Manager =>
        _artifactManager != null
            ? _artifactManager
            : (GameStateManager.Instance != null ? GameStateManager.Instance.ArtifactManager : null);

    private void OnEnable()
    {
        if (_detailPopup != null) _detailPopup.OnEquipRequested += HandleEquipRequested;
        if (_equipBinder != null) _equipBinder.OnSlotIndexClicked += HandleReplaceSlotSelected;
    }

    private void OnDisable()
    {
        if (_detailPopup != null) _detailPopup.OnEquipRequested -= HandleEquipRequested;
        if (_equipBinder != null) _equipBinder.OnSlotIndexClicked -= HandleReplaceSlotSelected;
    }

    private void Start()
    {
        if (_replaceHint != null) _replaceHint.SetActive(false);
        SyncFromData();
    }

    // 데이터(IsEquipped)를 보고 장착 슬롯을 초기 동기화한다. (보통 시작 시 장착 없음)
    private void SyncFromData()
    {
        ArtifactManager mgr = Manager;
        if (mgr == null || _equipBinder == null) return;

        for (int i = 0; i < _equipped.Length; i++) _equipped[i] = null;
        _equipBinder.ClearAll();

        int slot = 0;
        foreach (ArtifactInstance inst in mgr.MyArtifacts)
        {
            if (inst == null || !inst.IsEquipped) continue;
            if (slot >= MaxEquip) { inst.IsEquipped = false; continue; } // 한도 초과 방어
            EquipIntoSlot(slot, inst);
            slot++;
        }
    }

    // 장착 버튼 클릭(토글) : 이미 장착됐으면 해제, 아니면 장착.
    private void HandleEquipRequested(int gearId)
    {
        ArtifactManager mgr = Manager;
        if (mgr == null) { Debug.LogWarning("[ArtifactEquip] ArtifactManager 가 없습니다."); return; }

        ArtifactInstance inst = mgr.MyArtifacts.Find(a => a != null && a.MasterId == gearId);
        if (inst == null) { Debug.LogWarning($"[ArtifactEquip] 보유하지 않은 아티팩트입니다. Gear_ID:{gearId}"); return; }

        if (inst.IsEquipped)
        {
            Unequip(inst);
            CloseDetail();
            return;
        }

        int empty = FindEmptySlot();
        if (empty >= 0)
        {
            EquipIntoSlot(empty, inst);   // 빈 칸 자동 장착
            CloseDetail();
        }
        else
        {
            BeginReplaceSelection(inst);  // 가득 참 → 교체할 슬롯 선택
        }
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < _equipped.Length; i++)
            if (_equipped[i] == null) return i;
        return -1;
    }

    private void BeginReplaceSelection(ArtifactInstance inst)
    {
        _pending = inst;
        if (_equipBinder != null) _equipBinder.SlotSelectionMode = true;
        if (_replaceHint != null) _replaceHint.SetActive(true);
        CloseDetail(); // 상세 팝업을 닫아 장착 슬롯이 보이도록 한다.
        Debug.Log("[ArtifactEquip] 장착칸이 가득 찼습니다. 교체할 슬롯을 선택하세요.");
    }

    private void HandleReplaceSlotSelected(int slotIndex)
    {
        if (_pending == null) { EndReplaceSelection(); return; }
        if (slotIndex < 0 || slotIndex >= _equipped.Length) return;

        ArtifactInstance old = _equipped[slotIndex];
        if (old != null) old.IsEquipped = false; // 기존 장착 해제

        EquipIntoSlot(slotIndex, _pending);       // 새 아티팩트로 교체
        _pending = null;
        EndReplaceSelection();
    }

    // 교체 선택을 취소(취소 버튼/배경 클릭 등)할 때 외부에서 호출.
    public void CancelReplaceSelection()
    {
        _pending = null;
        EndReplaceSelection();
    }

    private void EndReplaceSelection()
    {
        if (_equipBinder != null) _equipBinder.SlotSelectionMode = false;
        if (_replaceHint != null) _replaceHint.SetActive(false);
    }

    private void EquipIntoSlot(int index, ArtifactInstance inst)
    {
        if (_equipBinder == null || inst == null) return;

        GearMasterData data = inst.MasterData;
        if (data == null) { Debug.LogWarning($"[ArtifactEquip] 마스터 데이터가 없습니다. MasterId:{inst.MasterId}"); return; }

        inst.IsEquipped = true;
        _equipped[index] = inst;

        ArtifactGrade grade = ToGrade(data.GearGrade);
        bool isMax = inst.CurrentLevel >= inst.GetMaxLevelLimit();
        _equipBinder.SetEquipped(index, data, grade, inst.CurrentLevel, inst.AscensionCount, isMax);

        RefreshList();
        Debug.Log($"[ArtifactEquip] 장착 완료. slot={index}, Gear_ID={inst.MasterId}");
    }

    private void Unequip(ArtifactInstance inst)
    {
        int index = System.Array.IndexOf(_equipped, inst);
        if (index >= 0)
        {
            _equipped[index] = null;
            if (_equipBinder != null) _equipBinder.ClearSlot(index);
        }
        inst.IsEquipped = false;
        RefreshList();
        Debug.Log($"[ArtifactEquip] 해제 완료. Gear_ID={inst.MasterId}");
    }

    private void RefreshList()
    {
        if (_listBinder != null) _listBinder.RefreshInventory();
    }

    private void CloseDetail()
    {
        if (_detailPopup != null) _detailPopup.Close();
    }

    private static ArtifactGrade ToGrade(string gradeName)
        => System.Enum.TryParse(gradeName, out ArtifactGrade grade) ? grade : ArtifactGrade.Rare;
}
