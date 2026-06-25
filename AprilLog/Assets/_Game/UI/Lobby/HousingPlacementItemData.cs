//담당자: 조규민

using System;
using UnityEngine;

/// <summary>
/// 하우징 배치 UI에 표시할 가구 항목 데이터입니다.
/// </summary>
[Serializable]
public class HousingPlacementItemData
{
    [Header("기본 정보")]
    [SerializeField] private int _furnitureId;
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField] private HousingPlacementCategory _category;
    [SerializeField] private Sprite _icon;
    [SerializeField] private string _iconResourceKey;

    [Header("상태")]
    [SerializeField] private bool _isOwned;
    [SerializeField] private bool _isUnlocked = true;

    [Header("구매 정보")]
    [SerializeField] private int _itemTableId;
    [SerializeField] private int _price;

    [Header("DB 분류")]
    [SerializeField] private int _nameId;
    [SerializeField] private string _location;
    [SerializeField] private string _sourceCategory;
    [SerializeField] private string _sourceType;
    [SerializeField] private string _resourceKey;

    public int FurnitureId => _furnitureId;
    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public HousingPlacementCategory Category => _category;
    public Sprite Icon => _icon;
    public string IconResourceKey => _iconResourceKey;
    public bool IsOwned => _isOwned;
    public bool IsUnlocked => _isUnlocked;
    public int ItemTableId => _itemTableId;
    public int Price => _price;
    public int NameId => _nameId;
    public string Location => _location;
    public string SourceCategory => _sourceCategory;
    public string SourceType => _sourceType;
    public string ResourceKey => _resourceKey;

    public HousingPlacementItemData(
        string _itemId,
        string _displayName,
        HousingPlacementCategory _category,
        Sprite _icon,
        bool _isOwned,
        bool _isUnlocked,
        int _price)
    {
        this._furnitureId = 0;
        this._itemId = _itemId;
        this._displayName = _displayName;
        this._category = _category;
        this._icon = _icon;
        this._iconResourceKey = null;
        this._isOwned = _isOwned;
        this._isUnlocked = _isUnlocked;
        this._itemTableId = 0;
        this._price = _price;
        this._nameId = 0;
        this._location = null;
        this._sourceCategory = null;
        this._sourceType = null;
        this._resourceKey = null;
    }

    public HousingPlacementItemData(
        int _furnitureId,
        int _nameId,
        string _itemId,
        string _displayName,
        HousingPlacementCategory _category,
        Sprite _icon,
        string _iconResourceKey,
        bool _isOwned,
        bool _isUnlocked,
        int _itemTableId,
        int _price,
        string _location,
        string _sourceCategory,
        string _sourceType,
        string _resourceKey)
    {
        this._furnitureId = _furnitureId;
        this._nameId = _nameId;
        this._itemId = _itemId;
        this._displayName = _displayName;
        this._category = _category;
        this._icon = _icon;
        this._iconResourceKey = _iconResourceKey;
        this._isOwned = _isOwned;
        this._isUnlocked = _isUnlocked;
        this._itemTableId = _itemTableId;
        this._price = _price;
        this._location = _location;
        this._sourceCategory = _sourceCategory;
        this._sourceType = _sourceType;
        this._resourceKey = _resourceKey;
    }
}
