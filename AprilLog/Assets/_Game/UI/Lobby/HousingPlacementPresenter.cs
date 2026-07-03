//담당자: 조규민
// 수정 내용 : 가구 배치 팝업을 하위 카테고리 섹션 단위로 갱신, 미보유 가구 구매 흐름 및 중복 적용 방지 추가
using System;
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
    private readonly HousingFurnitureSlotView _slotView;
    private readonly bool _applyImmediatelyOnItemClick;
    private readonly Action<HousingPlacementItemData> _onFurnitureApplied;
    private readonly Func<HousingPlacementItemData, bool> _onFurniturePurchaseRequested;

    private bool _isApplyingItem;

    public HousingPlacementPresenter(
        HousingPlacementModel _model,
        HousingPlacementButtonView _buttonView,
        HousingPlacementPopupView _popupView,
        HousingFurnitureSlotView _slotView,
        bool _applyImmediatelyOnItemClick,
        Action<HousingPlacementItemData> _onFurnitureApplied = null,
        Func<HousingPlacementItemData, bool> _onFurniturePurchaseRequested = null)
    {
        this._model = _model;
        this._buttonView = _buttonView;
        this._popupView = _popupView;
        this._slotView = _slotView;
        this._applyImmediatelyOnItemClick = _applyImmediatelyOnItemClick;
        this._onFurnitureApplied = _onFurnitureApplied;
        this._onFurniturePurchaseRequested = _onFurniturePurchaseRequested;
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
        List<HousingPlacementSectionData> _sections = _model.GetSections(_model.SelectedCategory);
        _popupView.RefreshSections(_sections, _model.SelectedCategory, ResolveItemState);
    }

    private void ApplySelectedItem()
    {
        if (_isApplyingItem)
        {
            return;
        }

        HousingPlacementItemData _selectedItem = _model.SelectedItem;

        if (_selectedItem == null)
        {
            return;
        }

        HousingPlacementItemState _state = ResolveItemState(_selectedItem);

        if (_state == HousingPlacementItemState.Locked)
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 해금되지 않은 가구입니다. Furniture: {_selectedItem.FurnitureId}");
            return;
        }

        if (_state == HousingPlacementItemState.Equipped)
        {
            return;
        }

        _isApplyingItem = true;

        try
        {
            ApplySelectedItem(_selectedItem, _state);
        }
        finally
        {
            _isApplyingItem = false;
        }
    }

    private void ApplySelectedItem(HousingPlacementItemData _selectedItem, HousingPlacementItemState _state)
    {
        if (_state == HousingPlacementItemState.Price)
        {
            TryPurchaseSelectedItem(_selectedItem);
            return;
        }

        if (ApplyItemToRoomSlot(_selectedItem))
        {
            _model.EquipItem(_selectedItem);
            _onFurnitureApplied?.Invoke(_selectedItem);
            Debug.Log($"[HousingPlacementPresenter] 가구 적용 완료: {_selectedItem.DisplayName} / 위치: {_selectedItem.Location}");
        }
    }

    private HousingPlacementItemState ResolveItemState(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return HousingPlacementItemState.Locked;
        }

        if (_model.IsEquipped(_itemData.FurnitureId))
        {
            return HousingPlacementItemState.Equipped;
        }

        if (_model.IsOwned(_itemData))
        {
            return HousingPlacementItemState.Owned;
        }

        return _itemData.IsUnlocked
            ? HousingPlacementItemState.Price
            : HousingPlacementItemState.Locked;
    }

    private void TryPurchaseSelectedItem(HousingPlacementItemData _itemData)
    {
        if (_onFurniturePurchaseRequested == null)
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 하우징 구매 처리기가 연결되지 않았습니다. Furniture: {_itemData.FurnitureId}");
            return;
        }

        if (!_onFurniturePurchaseRequested.Invoke(_itemData))
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 가구 구매에 실패했습니다. Furniture: {_itemData.FurnitureId}");
            return;
        }

        _model.OwnItem(_itemData);
        _popupView.SetSelectedItem(_itemData);
        Debug.Log($"[HousingPlacementPresenter] 가구 구매 완료: {_itemData.DisplayName} / Furniture: {_itemData.FurnitureId}");
    }

    private bool ApplyItemToRoomSlot(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || _slotView == null)
        {
            return false;
        }

        if (!_slotView.ApplyFurniture(_itemData))
        {
            return false;
        }

        return true;
    }
}
