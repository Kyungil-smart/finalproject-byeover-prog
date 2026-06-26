//담당자: 조규민
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 가구 배치 UI 입력과 적용 흐름을 중재합니다.
/// </summary>
public class HousingPlacementPresenter
{
    private readonly HousingPlacementModel _model;
    private readonly HousingPlacementButtonView _buttonView;
    private readonly HousingPlacementPopupView _popupView;
    private readonly HousingFurniturePlacementView _placementView;
    private readonly bool _applyImmediatelyOnItemClick;

    public HousingPlacementPresenter(
        HousingPlacementModel _model,
        HousingPlacementButtonView _buttonView,
        HousingPlacementPopupView _popupView,
        HousingFurniturePlacementView _placementView,
        bool _applyImmediatelyOnItemClick)
    {
        this._model = _model;
        this._buttonView = _buttonView;
        this._popupView = _popupView;
        this._placementView = _placementView;
        this._applyImmediatelyOnItemClick = _applyImmediatelyOnItemClick;
    }

    public void Initialize()
    {
        if (_model == null || _buttonView == null || _popupView == null)
        {
            Debug.LogWarning("[HousingPlacementPresenter] 배치 UI 참조가 부족합니다.");
            return;
        }

        _buttonView.OnPlacementButtonClicked += HandlePlacementButtonClicked;
        _buttonView.OnCloseButtonClicked += HandleCloseButtonClicked;
        _popupView.OnCategoryClicked += HandleCategoryClicked;
        _popupView.OnItemClicked += HandleItemClicked;
        _popupView.OnApplyClicked += HandleApplyClicked;

        _model.OnPlacementModeChanged += HandlePlacementModeChanged;
        _model.OnCategoryChanged += HandleCategoryChanged;
        _model.OnSelectedItemChanged += HandleSelectedItemChanged;
        _model.OnItemsChanged += RefreshItems;

        _buttonView.SetPlacementMode(_model.IsPlacementMode);
        _popupView.SetSelectedItem(_model.SelectedItem);
        RefreshItems();
    }

    public void Release()
    {
        if (_buttonView != null)
        {
            _buttonView.OnPlacementButtonClicked -= HandlePlacementButtonClicked;
            _buttonView.OnCloseButtonClicked -= HandleCloseButtonClicked;
        }

        if (_popupView != null)
        {
            _popupView.OnCategoryClicked -= HandleCategoryClicked;
            _popupView.OnItemClicked -= HandleItemClicked;
            _popupView.OnApplyClicked -= HandleApplyClicked;
        }

        if (_model == null)
        {
            return;
        }

        _model.OnPlacementModeChanged -= HandlePlacementModeChanged;
        _model.OnCategoryChanged -= HandleCategoryChanged;
        _model.OnSelectedItemChanged -= HandleSelectedItemChanged;
        _model.OnItemsChanged -= RefreshItems;
    }

    private void HandlePlacementButtonClicked()
    {
        _model.TogglePlacementMode();
    }

    private void HandleCloseButtonClicked()
    {
        _model.SetPlacementMode(false);
    }

    private void HandleCategoryClicked(HousingPlacementCategory _category)
    {
        _model.SelectCategory(_category);
    }

    private void HandleItemClicked(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return;
        }

        _model.SelectItem(_itemData);

        if (_applyImmediatelyOnItemClick)
        {
            ApplySelectedItem();
        }
    }

    private void HandlePlacementModeChanged(bool _isActive)
    {
        _buttonView.SetPlacementMode(_isActive);

        if (_isActive)
        {
            RefreshItems();
        }
    }

    private void HandleCategoryChanged(HousingPlacementCategory _category)
    {
        RefreshItems();
    }

    private void HandleSelectedItemChanged(HousingPlacementItemData _itemData)
    {
        _popupView.SetSelectedItem(_itemData);
    }

    private void HandleApplyClicked()
    {
        ApplySelectedItem();
    }

    private void RefreshItems()
    {
        List<HousingPlacementItemData> _items = _model.GetItems(_model.SelectedCategory);
        _popupView.RefreshItems(_items, _model.SelectedCategory);
    }

    private void ApplySelectedItem()
    {
        HousingPlacementItemData _selectedItem = _model.SelectedItem;

        if (_selectedItem == null)
        {
            return;
        }

        if (!_selectedItem.IsUnlocked)
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 해금되지 않은 가구입니다. Furniture: {_selectedItem.FurnitureId}");
            return;
        }

        if (ApplyItemToFurnitureRoot(_selectedItem))
        {
            Debug.Log($"[HousingPlacementPresenter] 가구 적용 완료: {_selectedItem.DisplayName} / 위치: {_selectedItem.Location}");
        }
    }

    private bool ApplyItemToFurnitureRoot(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _placementView == null)
        {
            return false;
        }

        if (!_placementView.ApplyFurniture(_itemData))
        {
            return false;
        }

        return true;
    }
}
