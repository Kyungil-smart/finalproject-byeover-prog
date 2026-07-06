//담당자: 조규민
//가구 배치 팝업 갱신, 가격 버튼 전용 구매 확인, 재화 부족 안내 및 중복 처리 방지
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 가구 배치 UI 입력과 적용 흐름을 중재합니다.
/// </summary>
// 배치 Model과 버튼·목록·구매 확인 View 이벤트 연결
// 아이템 선택·구매·적용 상태 전환과 방 슬롯 반영
public class HousingPlacementPresenter
{
    private readonly HousingPlacementModel _model;
    private readonly HousingPlacementButtonView _buttonView;
    private readonly HousingPlacementPopupView _popupView;
    private readonly HousingPurchaseConfirmView _purchaseConfirmView;
    private readonly HousingFurnitureSlotView _slotView;
    private readonly bool _applyImmediatelyOnItemClick;
    private readonly Action<HousingPlacementItemData> _onFurnitureApplied;
    private readonly Func<HousingPlacementItemData, bool> _onFurniturePurchaseRequested;
    private readonly Func<HousingPlacementItemData, bool> _canAffordFurniturePurchase;

    private bool _isApplyingItem;

    public HousingPlacementPresenter(
        HousingPlacementModel _model,
        HousingPlacementButtonView _buttonView,
        HousingPlacementPopupView _popupView,
        HousingPurchaseConfirmView _purchaseConfirmView,
        HousingFurnitureSlotView _slotView,
        bool _applyImmediatelyOnItemClick,
        Action<HousingPlacementItemData> _onFurnitureApplied = null,
        Func<HousingPlacementItemData, bool> _onFurniturePurchaseRequested = null,
        Func<HousingPlacementItemData, bool> _canAffordFurniturePurchase = null)
    {
        this._model = _model;
        this._buttonView = _buttonView;
        this._popupView = _popupView;
        this._purchaseConfirmView = _purchaseConfirmView;
        this._slotView = _slotView;
        this._applyImmediatelyOnItemClick = _applyImmediatelyOnItemClick;
        this._onFurnitureApplied = _onFurnitureApplied;
        this._onFurniturePurchaseRequested = _onFurniturePurchaseRequested;
        this._canAffordFurniturePurchase = _canAffordFurniturePurchase;
    }

    // Model·배치 버튼·팝업·구매 확인 View 이벤트 연결
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
        _popupView.OnPurchaseClicked += HandlePurchaseClicked;
        _popupView.OnApplyClicked += HandleApplyClicked;

        if (_purchaseConfirmView != null)
        {
            _purchaseConfirmView.OnConfirmClicked += HandlePurchaseConfirmed;
            _purchaseConfirmView.OnCancelClicked += HandlePurchaseCanceled;
        }

        _model.OnPlacementModeChanged += HandlePlacementModeChanged;
        _model.OnCategoryChanged += HandleCategoryChanged;
        _model.OnSelectedItemChanged += HandleSelectedItemChanged;
        _model.OnPurchaseConfirmationChanged += HandlePurchaseConfirmationChanged;
        _model.OnPurchaseProcessingChanged += HandlePurchaseProcessingChanged;
        _model.OnItemsChanged += RefreshItems;
        SubscribeLocalization();

        _buttonView.SetPlacementMode(_model.IsPlacementMode);
        _popupView.SetSelectedItem(_model.SelectedItem);
        _purchaseConfirmView?.HideAll();
        RefreshItems();
    }

    public void Release()
    {
        if (_buttonView != null)
        {
            _buttonView.OnPlacementButtonClicked -= HandlePlacementButtonClicked;
            _buttonView.OnCloseButtonClicked -= HandleCloseButtonClicked;
        }

        if (_purchaseConfirmView != null)
        {
            _purchaseConfirmView.OnConfirmClicked -= HandlePurchaseConfirmed;
            _purchaseConfirmView.OnCancelClicked -= HandlePurchaseCanceled;
            _purchaseConfirmView.HideAll();
        }

        if (_popupView != null)
        {
            _popupView.OnCategoryClicked -= HandleCategoryClicked;
            _popupView.OnItemClicked -= HandleItemClicked;
            _popupView.OnPurchaseClicked -= HandlePurchaseClicked;
            _popupView.OnApplyClicked -= HandleApplyClicked;
        }

        if (_model == null)
        {
            return;
        }

        _model.OnPlacementModeChanged -= HandlePlacementModeChanged;
        _model.OnCategoryChanged -= HandleCategoryChanged;
        _model.OnSelectedItemChanged -= HandleSelectedItemChanged;
        _model.OnPurchaseConfirmationChanged -= HandlePurchaseConfirmationChanged;
        _model.OnPurchaseProcessingChanged -= HandlePurchaseProcessingChanged;
        _model.OnItemsChanged -= RefreshItems;
        UnsubscribeLocalization();
    }

    private void HandlePlacementButtonClicked()
    {
        _model.TogglePlacementMode();
    }

    private void SubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
    }

    private void UnsubscribeLocalization()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void HandleLanguageChanged()
    {
        RefreshItems();

        if (_model.PendingPurchaseItem != null)
        {
            _purchaseConfirmView?.ShowConfirmation(BuildPurchaseConfirmationMessage(_model.PendingPurchaseItem));
        }
    }

    private void HandleCloseButtonClicked()
    {
        _model.CancelPurchaseConfirmation();
        _model.SetPlacementMode(false);
    }

    private void HandleCategoryClicked(HousingPlacementCategory _category)
    {
        _model.CancelPurchaseConfirmation();
        _model.SelectCategory(_category);
    }

    // 선택 아이템 변경과 슬롯 목록 선택 상태 갱신
    private void HandleItemClicked(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            return;
        }

        _model.SelectItem(_itemData);

        if (ResolveItemState(_itemData) == HousingPlacementItemState.Price)
        {
            return;
        }

        if (_applyImmediatelyOnItemClick)
        {
            ApplySelectedItem();
        }
    }

    private void HandlePurchaseClicked(HousingPlacementItemData _itemData)
    {
        if (_itemData == null || ResolveItemState(_itemData) != HousingPlacementItemState.Price)
        {
            return;
        }

        if (_purchaseConfirmView == null)
        {
            Debug.LogWarning("[HousingPlacementPresenter] 구매 확인 View가 연결되지 않아 구매 요청을 중단합니다.");
            return;
        }

        _model.SelectItem(_itemData);
        _model.RequestPurchaseConfirmation(_itemData);
    }

    // 재화 검증·차감·보유 처리 후 구매 팝업 닫음
    private void HandlePurchaseConfirmed()
    {
        HousingPlacementItemData _itemData = _model.PendingPurchaseItem;

        if (_itemData == null || ResolveItemState(_itemData) != HousingPlacementItemState.Price)
        {
            _model.CancelPurchaseConfirmation();
            return;
        }

        if (!_model.BeginPurchase())
        {
            return;
        }

        if (!CanAffordPurchase(_itemData))
        {
            _model.CompletePurchase();
            _purchaseConfirmView?.ShowInsufficientCurrency();
            return;
        }

        TryPurchaseSelectedItem(_itemData);
        _model.CompletePurchase();
    }

    private void HandlePurchaseCanceled()
    {
        _model.CancelPurchaseConfirmation();
    }

    private void HandlePurchaseConfirmationChanged(HousingPlacementItemData _itemData)
    {
        if (_itemData == null)
        {
            _purchaseConfirmView?.HideConfirmation();
            return;
        }

        _purchaseConfirmView?.ShowConfirmation(BuildPurchaseConfirmationMessage(_itemData));
    }

    private void HandlePurchaseProcessingChanged(bool _isProcessing)
    {
        _purchaseConfirmView?.SetButtonsInteractable(!_isProcessing);
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

    // 선택 카테고리와 아이템 상태 기반 구역·슬롯 UI 갱신
    private void RefreshItems()
    {
        List<HousingPlacementSectionData> _sections = _model.GetSections(_model.SelectedCategory);
        _popupView.RefreshSections(_sections, _model.SelectedCategory, ResolveItemState);
    }

    // 선택 아이템의 보유·해금 여부 검증 후 방 슬롯 적용
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

    private bool TryPurchaseSelectedItem(HousingPlacementItemData _itemData)
    {
        if (_onFurniturePurchaseRequested == null)
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 하우징 구매 처리기가 연결되지 않았습니다. Furniture: {_itemData.FurnitureId}");
            return false;
        }

        if (!_onFurniturePurchaseRequested.Invoke(_itemData))
        {
            Debug.LogWarning($"[HousingPlacementPresenter] 가구 구매에 실패했습니다. Furniture: {_itemData.FurnitureId}");
            return false;
        }

        _model.OwnItem(_itemData);
        _popupView.SetSelectedItem(_itemData);
        Debug.Log($"[HousingPlacementPresenter] 가구 구매 완료: {_itemData.DisplayName} / Furniture: {_itemData.FurnitureId}");
        return true;
    }

    private bool CanAffordPurchase(HousingPlacementItemData _itemData)
    {
        return _canAffordFurniturePurchase == null || _canAffordFurniturePurchase.Invoke(_itemData);
    }

    private static string BuildPurchaseConfirmationMessage(HousingPlacementItemData _itemData)
    {
        string _currencyName = _itemData.PriceCurrency == HousingPlacementPriceCurrency.Diamond
            ? "Diamond"
            : "Gold";
        string _displayName = ResolveLocalizedItemName(_itemData);
        int _price = Mathf.Max(0, _itemData.Price);

        if (LocalizationManager.Instance == null)
        {
            return $"{_currencyName} {_price:N0} / {_displayName}";
        }

        string _template = LocalizationManager.Instance.Get(13019, LocalizingType.UI);
        return _template
            .Replace("{0}", _price.ToString("N0"))
            .Replace("{Currency}", _currencyName)
            .Replace("{Furniturename}", _displayName);
    }

    private static string ResolveLocalizedItemName(HousingPlacementItemData _itemData)
    {
        if (_itemData.NameId > 0 && LocalizationManager.Instance != null)
        {
            string _localizedName = LocalizationManager.Instance.Get(_itemData.NameId, LocalizingType.Housing);

            if (!string.IsNullOrWhiteSpace(_localizedName) && !_localizedName.StartsWith("["))
            {
                return _localizedName;
            }
        }

        return string.IsNullOrWhiteSpace(_itemData.DisplayName)
            ? _itemData.ItemId
            : _itemData.DisplayName;
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
