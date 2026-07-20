//담당자: 조규민
// 광고 보상 Model·버튼·팝업 View 이벤트 연결과 상태 기반 UI 갱신
// 광고 시청 요청 결과에 따른 팝업 표시와 확인·취소 흐름 처리

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

    // Model·버튼·팝업 이벤트 구독과 최초 화면 상태 동기화
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

    // Presenter 종료 시 모든 입력과 상태 이벤트 구독 해제
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

    // 광고 시청 요청 결과에 따른 진행 상태 또는 실패 안내 표시
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

    // Model 상태 기반 버튼 입력과 팝업 UI 갱신
    private void HandleStateChanged(HousingAdRewardState _state)
    {
        _popupView.Refresh(_state);
    }
}
