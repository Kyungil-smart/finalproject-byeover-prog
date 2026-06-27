//담당자: 조규민

using System;
using UnityEngine;

/// <summary>
/// 시간 누적 보상 가구 입력, 팝업 갱신, 수령 요청 흐름을 중재합니다.
/// </summary>
public class HousingIdleRewardPresenter
{
    private readonly HousingIdleRewardModel _model;
    private readonly HousingIdleRewardPopupView _popupView;
    private readonly HousingIdleRewardButtonView _rewardButtonView;
    private readonly Action<HousingIdleRewardClaimResult> _onClaimRequested;

    private bool _isInitialized;

    public HousingIdleRewardPresenter(
        HousingIdleRewardModel _model,
        HousingIdleRewardPopupView _popupView,
        HousingIdleRewardButtonView _rewardButtonView,
        Action<HousingIdleRewardClaimResult> _onClaimRequested)
    {
        this._model = _model;
        this._popupView = _popupView;
        this._rewardButtonView = _rewardButtonView;
        this._onClaimRequested = _onClaimRequested;
    }

    public void Initialize()
    {
        if (_model == null || _popupView == null || _rewardButtonView == null)
        {
            Debug.LogWarning("[HousingIdleRewardPresenter] 시간 누적 보상 MVP 연결이 부족합니다.");
            return;
        }

        _rewardButtonView.OnClicked += HandleRewardButtonClicked;
        _popupView.OnConfirmClicked += HandleConfirmClicked;
        _popupView.OnCancelClicked += HandleCancelClicked;
        _model.OnStateChanged += HandleStateChanged;
        _popupView.Refresh(_model.CurrentState);
        _isInitialized = true;
    }

    public void Release()
    {
        if (_rewardButtonView != null)
        {
            _rewardButtonView.OnClicked -= HandleRewardButtonClicked;
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

    private void HandleRewardButtonClicked()
    {
        _model.ForceNotify();
        _popupView.Show();
    }

    private void HandleConfirmClicked()
    {
        HousingIdleRewardClaimResult _claimResult = _model.Claim();
        _onClaimRequested?.Invoke(_claimResult);
        _popupView.Hide();
    }

    private void HandleCancelClicked()
    {
        _popupView.Hide();
    }

    private void HandleStateChanged(HousingIdleRewardState _state)
    {
        _popupView.Refresh(_state);
    }
}
