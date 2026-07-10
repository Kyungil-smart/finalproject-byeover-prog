//담당자: 조규민

/// <summary>
/// 정산 팝업에 표시할 재화 보상 항목입니다.
/// </summary>
public readonly struct ResultRewardEntry
{
    public readonly int _itemId;
    public readonly long _amount;
    public readonly string _label;

    public ResultRewardEntry(int _itemId, long _amount, string _label)
    {
        this._itemId = _itemId;
        this._amount = _amount;
        this._label = _label;
    }
}
