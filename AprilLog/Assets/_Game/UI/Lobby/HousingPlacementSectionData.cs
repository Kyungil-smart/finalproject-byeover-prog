//담당자: 조규민

using System.Collections.Generic;

/// <summary>
/// 하우징 배치 팝업에 표시할 가구 섹션 데이터입니다.
/// </summary>
public class HousingPlacementSectionData
{
    private readonly List<HousingPlacementItemData> _items;

    public string Title { get; }
    public IReadOnlyList<HousingPlacementItemData> Items => _items;

    public HousingPlacementSectionData(string _title, IEnumerable<HousingPlacementItemData> _items)
    {
        Title = _title;
        this._items = _items == null
            ? new List<HousingPlacementItemData>()
            : new List<HousingPlacementItemData>(_items);
    }
}
