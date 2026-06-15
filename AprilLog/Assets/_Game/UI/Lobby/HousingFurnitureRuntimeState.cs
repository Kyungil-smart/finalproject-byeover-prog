//담당자: 조규민
//설명: 하우징 가구의 구매 보유 목록과 슬롯별 착용 상태를 런타임에서 관리한다.

using System.Collections.Generic;

/// <summary>
/// 하우징 가구의 플레이어별 보유/착용 상태를 담는다.
/// </summary>
public class HousingFurnitureRuntimeState
{
    private readonly HashSet<int> _ownedFurnitureIds = new HashSet<int>();
    private readonly Dictionary<int, int> _equippedFurnitureBySlot = new Dictionary<int, int>();

    public IEnumerable<int> OwnedFurnitureIds => _ownedFurnitureIds;
    public IReadOnlyDictionary<int, int> EquippedFurnitureBySlot => _equippedFurnitureBySlot;

    public bool IsOwned(int _furnitureId)
    {
        // 기능: 보유 가구 ID 집합에 대상 가구가 포함되어 있는지 확인한다.
        return _ownedFurnitureIds.Contains(_furnitureId);
    }

    public void AddOwned(int _furnitureId)
    {
        // 기능: 유효한 가구 ID를 보유 목록에 추가한다.
        if (_furnitureId <= 0)
            return;

        _ownedFurnitureIds.Add(_furnitureId);
    }

    public bool TryGetEquipped(int _slotId, out int _furnitureId)
    {
        // 기능: 슬롯 ID에 현재 착용된 가구 ID가 있는지 조회한다.
        return _equippedFurnitureBySlot.TryGetValue(_slotId, out _furnitureId);
    }

    public void SetEquipped(int _slotId, int _furnitureId)
    {
        // 기능: 슬롯별 착용 가구 ID를 설정하거나 교체한다.
        _equippedFurnitureBySlot[_slotId] = _furnitureId;
    }

    public void Clear()
    {
        // 기능: 보유 목록과 착용 상태를 모두 초기화한다.
        _ownedFurnitureIds.Clear();
        _equippedFurnitureBySlot.Clear();
    }
}
