using System;
using UnityEngine;

// 작성자 : 홍정옥
// 설명   : 장착 아티팩트 슬롯(FittedArtifacts 하위, 총 3개)을 관리한다.
//          - 장착 안 됨 : 해당 슬롯의 EmptySlot 오브젝트를 켠다.
//          - 장착 됨    : Slot_Aritfact 프리팹(ArtifactEquipSlotView)을 슬롯에 생성하고 EmptySlot 을 끈다.
//          어떤 아티팩트가 장착됐는지는 유저 인벤토리 데이터이므로, 연동되면 SetEquipped / ClearSlot 을 호출한다.
public class ArtifactEquipBinder : MonoBehaviour
{
    [Serializable]
    private class EquipSlot
    {
        [Tooltip("장착 프리팹이 생성될 부모 (FittedArtifacts 하위 슬롯)")]
        public Transform container;
        [Tooltip("비었을 때 켜는 EmptySlot 오브젝트")]
        public GameObject emptySlot;
        [Tooltip("교체 슬롯 선택 모드일 때 켜는 강조 오브젝트(글로우/테두리 등). 선택")]
        public GameObject selectHighlight;
    }

    [Header("장착 슬롯 (FittedArtifacts 하위, 3개)")]
    [SerializeField] private EquipSlot[] _slots = new EquipSlot[3];

    [Tooltip("장착 시 생성할 프리팹 (Slot_Aritfact)")]
    [SerializeField] private ArtifactEquipSlotView _equipPrefab;

    private ArtifactEquipSlotView[] _spawned;
    private Action<int>[] _slotHandlers;

    // 슬롯 단일 클릭 → 상세 정보 팝업용 (인자 = Gear_ID)
    public event Action<int> OnSlotClicked;

    // 슬롯 단일 클릭 → 슬롯 인덱스 선택용 (교체 대상 선택 등). SlotSelectionMode 가 true 일 때만 발행.
    public event Action<int> OnSlotIndexClicked;

    // true 이면 슬롯 클릭이 '인덱스 선택'(OnSlotIndexClicked)으로 동작한다. (가득 찼을 때 교체 슬롯 선택용)
    public bool SlotSelectionMode { get; set; }

    // 교체 슬롯 선택 모드 강조 표시 : 모든 슬롯의 selectHighlight 를 일괄 토글한다.
    // (어떤 칸이든 눌러서 교체할 수 있음을 시각적으로 알린다)
    public void SetSelectionHighlight(bool on)
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && _slots[i].selectHighlight != null)
                _slots[i].selectHighlight.SetActive(on);
        }
    }

    public int SlotCount => _slots != null ? _slots.Length : 0;

    private void Awake()
    {
        _spawned = new ArtifactEquipSlotView[_slots.Length];
        _slotHandlers = new Action<int>[_slots.Length];
    }

    private void Start()
    {
        // 기본 : 전부 비어 있는 상태(EmptySlot 켜짐)로 시작.
        ClearAll();
        SetSelectionHighlight(false);
    }

    public void ClearAll()
    {
        for (int i = 0; i < _slots.Length; i++)
            ClearSlot(i);
    }

    // 장착 : index 슬롯에 프리팹을 생성하고 데이터를 채운다. (인벤토리 연동 시 호출)
    public void SetEquipped(int index, GearMasterData data, ArtifactGrade grade, int level, int ascensionStage, bool isMaxLevel)
    {
        if (!IsValid(index) || _equipPrefab == null)
        {
            Debug.LogWarning($"[ArtifactEquipBinder] 장착 실패. index={index}, 프리팹 연결 확인.", this);
            return;
        }

        DestroySpawned(index);

        EquipSlot slot = _slots[index];
        // worldPositionStays=false 로 부모(container) 로컬 기준 생성 후, 위치/스케일을 컨테이너에 맞춰 초기화.
        ArtifactEquipSlotView view = Instantiate(_equipPrefab, slot.container, false);
        ResetRect(view.transform);
        view.SetData(data, grade);
        view.SetOwnedState(level, ascensionStage, isMaxLevel);

        int captured = index; // 클릭 시 슬롯 인덱스를 알 수 있도록 캡처
        Action<int> handler = gearId => OnSlotClickedInternal(captured, gearId);
        view.Clicked += handler;
        _slotHandlers[index] = handler;
        _spawned[index] = view;

        if (slot.emptySlot != null)
            slot.emptySlot.SetActive(false);
    }

    // 해제 : index 슬롯을 비우고 EmptySlot 을 켠다.
    public void ClearSlot(int index)
    {
        if (!IsValid(index))
            return;

        DestroySpawned(index);

        if (_slots[index].emptySlot != null)
            _slots[index].emptySlot.SetActive(true);
    }

    private void DestroySpawned(int index)
    {
        if (_spawned == null || _spawned[index] == null)
            return;

        if (_slotHandlers != null && _slotHandlers[index] != null)
        {
            _spawned[index].Clicked -= _slotHandlers[index];
            _slotHandlers[index] = null;
        }
        Destroy(_spawned[index].gameObject);
        _spawned[index] = null;
    }

    private void OnSlotClickedInternal(int index, int gearId)
    {
        // 교체 슬롯 선택 모드일 때는 인덱스만 알려주고 상세 팝업은 열지 않는다.
        if (SlotSelectionMode)
        {
            Debug.Log($"[ArtifactEquipBinder] 교체 슬롯 선택 → index {index}");
            OnSlotIndexClicked?.Invoke(index);
            return;
        }

        Debug.Log($"[ArtifactEquipBinder] 장착 슬롯 클릭 → Gear {gearId} 상세 정보 요청");
        OnSlotClicked?.Invoke(gearId);
    }

    // 비어 있는(장착 안 된) 슬롯인지. 컨트롤러가 빈 슬롯 탐색에 사용.
    public bool IsSlotEmpty(int index) => IsValid(index) && (_spawned == null || _spawned[index] == null);

    private bool IsValid(int index) => _slots != null && index >= 0 && index < _slots.Length && _slots[index] != null;

    // 생성된 장착 카드를 슬롯(container) 기준으로 정렬(위치/회전/스케일 초기화).
    private static void ResetRect(Transform t)
    {
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        // 위치만 컨테이너 기준 0 으로(크기/앵커는 프리팹 설정 유지 → 고정 크기 카드가 찌그러지지 않음).
        if (t is RectTransform rt)
            rt.anchoredPosition3D = Vector3.zero;
    }
}
