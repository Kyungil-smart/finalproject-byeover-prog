//담당자: 조규민

using System;
using System.Collections.Generic;

/// <summary>
/// 하우징 배치 UI 상태를 보관합니다.
/// </summary>
public class HousingPlacementModel
{
    private readonly List<HousingPlacementItemData> _items = new();

    private bool _isPlacementMode;
    private HousingPlacementCategory _selectedCategory = HousingPlacementCategory.Decoration;
    private HousingPlacementItemData _selectedItem;

    public event Action<bool> OnPlacementModeChanged;
    public event Action<HousingPlacementCategory> OnCategoryChanged;
    public event Action<HousingPlacementItemData> OnSelectedItemChanged;
    public event Action OnItemsChanged;

    public bool IsPlacementMode => _isPlacementMode;
    public HousingPlacementCategory SelectedCategory => _selectedCategory;
    public HousingPlacementItemData SelectedItem => _selectedItem;

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

    public void SetItems(IEnumerable<HousingPlacementItemData> _newItems)
    {
        _items.Clear();

        if (_newItems != null)
        {
            _items.AddRange(_newItems);
        }

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
}
