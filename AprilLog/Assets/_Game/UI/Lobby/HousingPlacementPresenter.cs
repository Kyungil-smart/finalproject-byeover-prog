//담당자: 조규민

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하우징 배치 UI 입력과 상태 갱신을 중재합니다.
/// </summary>
public class HousingPlacementPresenter
{
    private readonly HousingPlacementModel _model;
    private readonly HousingPlacementButtonView _buttonView;
    private readonly HousingPlacementPopupView _popupView;

    public HousingPlacementPresenter(
        HousingPlacementModel _model,
        HousingPlacementButtonView _buttonView,
        HousingPlacementPopupView _popupView)
    {
        this._model = _model;
        this._buttonView = _buttonView;
        this._popupView = _popupView;
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

        _model.OnPlacementModeChanged += HandlePlacementModeChanged;
        _model.OnCategoryChanged += HandleCategoryChanged;
        _model.OnItemsChanged += RefreshItems;

        _buttonView.SetPlacementMode(_model.IsPlacementMode);
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
        }

        if (_model == null)
        {
            return;
        }

        _model.OnPlacementModeChanged -= HandlePlacementModeChanged;
        _model.OnCategoryChanged -= HandleCategoryChanged;
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

        Debug.Log($"[HousingPlacementPresenter] 선택한 가구: {_itemData.DisplayName} / 보유: {_itemData.IsOwned}");
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

    private void RefreshItems()
    {
        List<HousingPlacementItemData> _items = _model.GetItems(_model.SelectedCategory);
        _popupView.RefreshItems(_items, _model.SelectedCategory);
    }
}
