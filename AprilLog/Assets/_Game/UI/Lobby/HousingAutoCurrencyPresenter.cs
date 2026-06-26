//담당자: 조규민

using System;
using UnityEngine;

/// <summary>
/// 자동재화 가구 입력, 팝업 갱신, 수령 요청 흐름을 중재합니다.
/// </summary>
public class HousingAutoCurrencyPresenter
{
    private readonly HousingAutoCurrencyModel _model;
    private readonly HousingAutoCurrencyPopupView _popupView;
    private readonly HousingAutoCurrencyFurnitureView _furnitureView;
    private readonly Action<HousingAutoCurrencyClaimResult> _onClaimRequested;

    private bool _isInitialized;

    public HousingAutoCurrencyPresenter(
        HousingAutoCurrencyModel _model,
        HousingAutoCurrencyPopupView _popupView,
        HousingAutoCurrencyFurnitureView _furnitureView,
        Action<HousingAutoCurrencyClaimResult> _onClaimRequested)
    {
        this._model = _model;
        this._popupView = _popupView;
        this._furnitureView = _furnitureView;
        this._onClaimRequested = _onClaimRequested;
    }

    public void Initialize()
    {
        if (_model == null || _popupView == null || _furnitureView == null)
        {
            Debug.LogWarning("[HousingAutoCurrencyPresenter] 자동재화 MVP 연결이 부족합니다.");
            return;
        }

        _furnitureView.OnClicked += HandleFurnitureClicked;
        _popupView.OnConfirmClicked += HandleConfirmClicked;
        _popupView.OnCancelClicked += HandleCancelClicked;
        _model.OnStateChanged += HandleStateChanged;
        _popupView.Refresh(_model.CurrentState);
        _isInitialized = true;
    }

    public void Release()
    {
        if (_furnitureView != null)
        {
            _furnitureView.OnClicked -= HandleFurnitureClicked;
        }

        if (_popupView != null)
        {
            _popupView.OnConfirmClicked -= HandleConfirmClicked;
            _popupView.OnCancelClicked -= HandleCancelClicked;
        }

        if (_model == null)
        {
            return;
        }

        _model.OnStateChanged -= HandleStateChanged;
    }

    public void Refresh()
    {
        if (!_isInitialized)
        {
            return;
        }

        _model.Refresh();
    }

    private void HandleFurnitureClicked()
    {
        _model.ForceNotify();
        _popupView.Show();
    }

    private void HandleConfirmClicked()
    {
        HousingAutoCurrencyClaimResult _claimResult = _model.Claim();
        _onClaimRequested?.Invoke(_claimResult);
        _popupView.Hide();
    }

    private void HandleCancelClicked()
    {
        _popupView.Hide();
    }

    private void HandleStateChanged(HousingAutoCurrencyState _state)
    {
        _popupView.Refresh(_state);
    }
}
