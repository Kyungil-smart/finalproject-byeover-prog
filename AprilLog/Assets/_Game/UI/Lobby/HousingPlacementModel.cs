//담당자: 조규민
// 수정 내용 : 구매 보유 가구 ID와 장착 가구 ID를 분리하고 구매 확인 대기 상태를 관리

using System;
using System.Collections.Generic;

/// <summary>
/// 하우징 배치 UI 상태를 보관
/// </summary>
public class HousingPlacementModel
{
    private const string _locationTable = "location4";
    private const string _locationSofa = "location5";
    private const string _typeFloor = "floor";
    private const string _typeWall = "wall";
    private const string _typeBed = "bed";
    private const string _typeKitchen = "coffee";
    private const string _typeDesk = "reward";

    private readonly List<HousingPlacementItemData> _items = new();
    private readonly HashSet<int> _equippedFurnitureIds = new();
    private readonly HashSet<int> _ownedFurnitureIds = new();

    private bool _isPlacementMode;
    private bool _isPurchaseProcessing;
    private HousingPlacementCategory _selectedCategory = HousingPlacementCategory.Decoration;
    private HousingPlacementItemData _selectedItem;
    private HousingPlacementItemData _pendingPurchaseItem;

    public event Action<bool> OnPlacementModeChanged;
    public event Action<HousingPlacementCategory> OnCategoryChanged;
    public event Action<HousingPlacementItemData> OnSelectedItemChanged;
    public event Action<HousingPlacementItemData> OnPurchaseConfirmationChanged;
    public event Action<bool> OnPurchaseProcessingChanged;
    public event Action OnItemsChanged;

    public bool IsPlacementMode => _isPlacementMode;
    public HousingPlacementCategory SelectedCategory => _selectedCategory;
    public HousingPlacementItemData SelectedItem => _selectedItem;
    public HousingPlacementItemData PendingPurchaseItem => _pendingPurchaseItem;
    public bool IsPurchaseProcessing => _isPurchaseProcessing;

    public HousingPlacementModel(IEnumerable<HousingPlacementItemData> _initialItems)
    {
        SetItems(_initialItems);
    }

    public void SetPlacementMode(bool _isActive)
    {
        if (_isPlacementMode == _isActive)
        {
            return;
        }

        _isPlacementMode = _isActive;
        OnPlacementModeChanged?.Invoke(_isPlacementMode);
    }

    public void TogglePlacementMode()
    {
        SetPlacementMode(_isPlacementMode == false);
    }

    public void SelectCategory(HousingPlacementCategory _category)
    {
        if (_selectedCategory == _category)
        {
            return;
        }

        _selectedCategory = _category;
        SelectItem(null);
        OnCategoryChanged?.Invoke(_selectedCategory);
    }

    public void SelectItem(HousingPlacementItemData _itemData)
    {
        if (_selectedItem == _itemData)
        {
            return;
        }

        _selectedItem = _itemData;
        OnSelectedItemChanged?.Invoke(_selectedItem);
    }

    public bool RequestPurchaseConfirmation(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _pendingPurchaseItem != null || _isPurchaseProcessing)
        {
            return false;
        }

        _pendingPurchaseItem = _itemData;
        OnPurchaseConfirmationChanged?.Invoke(_pendingPurchaseItem);
        return true;
    }

    public bool BeginPurchase()
    {
        if (_pendingPurchaseItem == null || _isPurchaseProcessing)
        {
            return false;
        }

        _isPurchaseProcessing = true;
        OnPurchaseProcessingChanged?.Invoke(true);
        return true;
    }

    public void CompletePurchase()
    {
        SetPurchaseProcessing(false);
        CancelPurchaseConfirmation();
    }

    public void CancelPurchaseConfirmation()
    {
        if (_isPurchaseProcessing)
        {
            return;
        }

        if (_pendingPurchaseItem == null)
        {
            return;
        }

        _pendingPurchaseItem = null;
        OnPurchaseConfirmationChanged?.Invoke(null);
    }

    private void SetPurchaseProcessing(bool _isProcessing)
    {
        if (_isPurchaseProcessing == _isProcessing)
        {
            return;
        }

        _isPurchaseProcessing = _isProcessing;
        OnPurchaseProcessingChanged?.Invoke(_isPurchaseProcessing);
    }

    public void SetItems(IEnumerable<HousingPlacementItemData> _newItems)
    {
        _items.Clear();

        if (_newItems != null)
        {
            _items.AddRange(_newItems);
        }

        OnItemsChanged?.Invoke();
    }

    public void SetEquippedFurnitureIds(IEnumerable<int> _furnitureIds)
    {
        _equippedFurnitureIds.Clear();

        if (_furnitureIds != null)
        {
            foreach (int _furnitureId in _furnitureIds)
            {
                if (_furnitureId > 0)
                {
                    _equippedFurnitureIds.Add(_furnitureId);
                }
            }
        }

        OnItemsChanged?.Invoke();
    }

    public bool IsEquipped(int _furnitureId)
    {
        return _furnitureId > 0 && _equippedFurnitureIds.Contains(_furnitureId);
    }

    public void SetOwnedFurnitureIds(IEnumerable<int> _furnitureIds)
    {
        _ownedFurnitureIds.Clear();

        if (_furnitureIds != null)
        {
            foreach (int _furnitureId in _furnitureIds)
            {
                if (_furnitureId > 0)
                {
                    _ownedFurnitureIds.Add(_furnitureId);
                }
            }
        }

        OnItemsChanged?.Invoke();
    }

    public bool IsOwned(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return false;
        }

        return _itemData.IsOwned || _ownedFurnitureIds.Contains(_itemData.FurnitureId);
    }

    public void OwnItem(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _itemData.FurnitureId <= 0)
        {
            return;
        }

        if (_ownedFurnitureIds.Add(_itemData.FurnitureId))
        {
            OnItemsChanged?.Invoke();
        }
    }

    public void EquipItem(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _itemData.FurnitureId <= 0)
        {
            return;
        }

        RemoveEquippedItemAtLocation(_itemData.Location);
        _equippedFurnitureIds.Add(_itemData.FurnitureId);
        OnItemsChanged?.Invoke();
    }

    public List<HousingPlacementItemData> GetItems(HousingPlacementCategory _category)
    {
        List<HousingPlacementItemData> _filteredItems = new();

        for (int _index = 0; _index < _items.Count; _index++)
        {
            HousingPlacementItemData _item = _items[_index];

            if (_item == null)
            {
                continue;
            }

            if (_item.Category != _category)
            {
                continue;
            }

            _filteredItems.Add(_item);
        }

        return _filteredItems;
    }

    public List<HousingPlacementSectionData> GetSections(HousingPlacementCategory _category)
    {
        List<HousingPlacementSectionData> _sections = new();

        switch (_category)
        {
            case HousingPlacementCategory.Decoration:
                AddSectionByLocation(_sections, _category, "\uD14C\uC774\uBE14", _locationTable);
                AddSectionByLocation(_sections, _category, "\uC18C\uD30C", _locationSofa);
                break;
            case HousingPlacementCategory.Background:
                AddSectionByType(_sections, _category, "\uBC14\uB2E5", _typeFloor);
                AddSectionByType(_sections, _category, "\uBC30\uACBD", _typeWall);
                break;
            case HousingPlacementCategory.Function:
                AddSectionByType(_sections, _category, "\uCE68\uB300", _typeBed);
                AddSectionByType(_sections, _category, "\uC8FC\uBC29", _typeKitchen);
                AddSectionByType(_sections, _category, "\uCC45\uC0C1", _typeDesk);
                break;
        }

        return _sections;
    }

    private void AddSectionByLocation(
        List<HousingPlacementSectionData> _sections,
        HousingPlacementCategory _category,
        string _title,
        string _location)
    {
        AddSection(_sections, _title, FindItems(_category, _location, null));
    }

    private void AddSectionByType(
        List<HousingPlacementSectionData> _sections,
        HousingPlacementCategory _category,
        string _title,
        string _sourceType)
    {
        AddSection(_sections, _title, FindItems(_category, null, _sourceType));
    }

    private static void AddSection(
        List<HousingPlacementSectionData> _sections,
        string _title,
        List<HousingPlacementItemData> _items)
    {
        if (_items.Count <= 0)
        {
            return;
        }

        _sections.Add(new HousingPlacementSectionData(_title, _items));
    }

    private List<HousingPlacementItemData> FindItems(
        HousingPlacementCategory _category,
        string _location,
        string _sourceType)
    {
        List<HousingPlacementItemData> _sectionItems = new();

        for (int _index = 0; _index < _items.Count; _index++)
        {
            HousingPlacementItemData _item = _items[_index];

            if (!IsMatchingSectionItem(_item, _category, _location, _sourceType))
            {
                continue;
            }

            _sectionItems.Add(_item);
        }

        return _sectionItems;
    }

    private static bool IsMatchingSectionItem(
        HousingPlacementItemData _item,
        HousingPlacementCategory _category,
        string _location,
        string _sourceType)
    {
        if (_item == null || _item.Category != _category)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_location))
        {
            return NormalizeKey(_item.Location) == _location;
        }

        if (!string.IsNullOrWhiteSpace(_sourceType))
        {
            return NormalizeKey(_item.SourceType) == _sourceType;
        }

        return true;
    }

    private static string NormalizeKey(string _value)
    {
        return string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim().ToLowerInvariant();
    }

    private void RemoveEquippedItemAtLocation(string _location)
    {
        string _normalizedLocation = NormalizeKey(_location);

        for (int _index = 0; _index < _items.Count; _index++)
        {
            HousingPlacementItemData _item = _items[_index];

            if (_item == null || NormalizeKey(_item.Location) != _normalizedLocation)
            {
                continue;
            }

            _equippedFurnitureIds.Remove(_item.FurnitureId);
        }
    }
}
