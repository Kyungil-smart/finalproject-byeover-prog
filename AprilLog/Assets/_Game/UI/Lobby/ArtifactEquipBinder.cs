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
    }

    [Header("장착 슬롯 (FittedArtifacts 하위, 3개)")]
    [SerializeField] private EquipSlot[] _slots = new EquipSlot[3];

    [Tooltip("장착 시 생성할 프리팹 (Slot_Aritfact)")]
    [SerializeField] private ArtifactEquipSlotView _equipPrefab;

    private ArtifactEquipSlotView[] _spawned;

    // 슬롯 단일 클릭 → 상세 정보 팝업용 (인자 = Gear_ID)
    public event Action<int> OnSlotClicked;

    private void Awake()
    {
        _spawned = new ArtifactEquipSlotView[_slots.Length];
    }

    private void Start()
    {
        // 기본 : 전부 비어 있는 상태(EmptySlot 켜짐)로 시작.
        ClearAll();
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
        ArtifactEquipSlotView view = Instantiate(_equipPrefab, slot.container);
        view.SetData(data, grade);
        view.SetOwnedState(level, ascensionStage, isMaxLevel);
        view.Clicked += HandleSlotClicked;
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

        _spawned[index].Clicked -= HandleSlotClicked;
        Destroy(_spawned[index].gameObject);
        _spawned[index] = null;
    }

    private void HandleSlotClicked(int gearId)
    {
        Debug.Log($"[ArtifactEquipBinder] 장착 슬롯 클릭 → Gear {gearId} 상세 정보 요청");
        OnSlotClicked?.Invoke(gearId);
    }

    private bool IsValid(int index) => _slots != null && index >= 0 && index < _slots.Length && _slots[index] != null;
}
