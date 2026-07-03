//담당자: 조규민

using System;
using UnityEngine;

/// <summary>
/// 광고 보상 가구 입력과 팝업 표시 흐름을 중재합니다.
/// </summary>
public class HousingAdRewardPresenter
{
    private readonly HousingAdRewardModel _model;
    private readonly HousingAdRewardPopupView _popupView;
    private readonly HousingAdRewardButtonView _buttonView;
    private readonly Func<bool> _onAdWatchRequested;

    private bool _isInitialized;

    public HousingAdRewardPresenter(
        HousingAdRewardModel _model,
        HousingAdRewardPopupView _popupView,
        HousingAdRewardButtonView _buttonView,
        Func<bool> _onAdWatchRequested)
    {
        this._model = _model;
        this._popupView = _popupView;
        this._buttonView = _buttonView;
        this._onAdWatchRequested = _onAdWatchRequested;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_model == null || _popupView == null || _buttonView == null)
        {
            Debug.LogWarning("[HousingAdRewardPresenter] 광고 보기 MVP 연결이 부족합니다.");
            return;
        }

        _buttonView.OnClicked += HandleButtonClicked;
        _popupView.OnConfirmClicked += HandleConfirmClicked;
        _popupView.OnCancelClicked += HandleCancelClicked;
        _model.OnStateChanged += HandleStateChanged;
        _popupView.Refresh(_model.CurrentState);
        _isInitialized = true;
    }

    public void Release()
    {
        if (_buttonView != null)
        {
            _buttonView.OnClicked -= HandleButtonClicked;
        }

        if (_popupView != null)
        {
            _popupView.OnConfirmClicked -= HandleConfirmClicked;
            _popupView.OnCancelClicked -= HandleCancelClicked;
        }

        if (_model != null)
        {
            _model.OnStateChanged -= HandleStateChanged;
        }

        _isInitialized = false;
    }

    public void SetAdStatus(HousingAdRewardStatus _status, string _message, bool _canConfirm)
    {
        _model?.SetAdStatus(_status, _message, _canConfirm);
        _buttonView?.SetInteractable(_status != HousingAdRewardStatus.Showing);
    }

    public void ShowAdStatus(HousingAdRewardStatus _status, string _message, bool _canConfirm)
    {
        _model?.ShowStatus(_status, _message, _canConfirm);
        _buttonView?.SetInteractable(_status != HousingAdRewardStatus.Showing);
    }

    private void HandleButtonClicked()
    {
        _model.Show();
    }

    private void HandleConfirmClicked()
    {
        if (!_model.CurrentState.CanConfirm)
        {
            return;
        }

        bool _isAdStarted = _onAdWatchRequested?.Invoke() == true;

        if (!_isAdStarted)
        {
            return;
        }

        _model.Hide();
    }

    private void HandleCancelClicked()
    {
        _model.Hide();
    }

    private void HandleStateChanged(HousingAdRewardState _state)
    {
        _popupView.Refresh(_state);
    }
}
