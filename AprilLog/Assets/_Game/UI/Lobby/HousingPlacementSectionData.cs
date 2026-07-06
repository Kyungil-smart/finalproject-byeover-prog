//담당자: 조규민

using System.Collections.Generic;

/// <summary>
/// 하우징 배치 팝업에 표시할 가구 섹션 데이터입니다.
/// </summary>
// 배치 팝업의 카테고리 구역 제목과 소속 아이템 목록 전달 데이터
public class HousingPlacementSectionData
{
    private readonly List<HousingPlacementItemData> _items;

    public string Title { get; }
    public int TitleLanguageId { get; }
    public IReadOnlyList<HousingPlacementItemData> Items => _items;

    public HousingPlacementSectionData(string _title, IEnumerable<HousingPlacementItemData> _items, int _titleLanguageId = 0)
    {
        Title = _title;
        TitleLanguageId = _titleLanguageId;
        this._items = _items == null
            ? new List<HousingPlacementItemData>()
            : new List<HousingPlacementItemData>(_items);
    }
}
