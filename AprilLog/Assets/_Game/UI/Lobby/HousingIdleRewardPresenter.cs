//담당자: 조규민

// 수정 내용 : 보상 지급 성공 후에만 Model의 마지막 수령 시각을 확정하고 중복 수령 입력을 차단

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
    private readonly Func<HousingIdleRewardClaimResult, bool> _onClaimRequested;

    private bool _isInitialized;
    private bool _isClaiming;

    public HousingIdleRewardPresenter(
        HousingIdleRewardModel _model,
        HousingIdleRewardPopupView _popupView,
        HousingIdleRewardButtonView _rewardButtonView,
        Func<HousingIdleRewardClaimResult, bool> _onClaimRequested)
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
        if (_isClaiming || !_model.CurrentState.HasClaimableReward)
        {
            return;
        }

        if (_onClaimRequested == null)
        {
            Debug.LogWarning("[HousingIdleRewardPresenter] 시간 누적 보상 지급 처리기가 연결되지 않았습니다.");
            return;
        }

        _isClaiming = true;

        try
        {
            HousingIdleRewardClaimResult _claimResult = _model.CreateClaimRequest();

            if (!_onClaimRequested.Invoke(_claimResult))
            {
                Debug.LogWarning("[HousingIdleRewardPresenter] 시간 누적 보상 지급에 실패했습니다.");
                return;
            }

            _model.CommitClaim(_claimResult);
            _popupView.Hide();
        }
        finally
        {
            _isClaiming = false;
        }
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
