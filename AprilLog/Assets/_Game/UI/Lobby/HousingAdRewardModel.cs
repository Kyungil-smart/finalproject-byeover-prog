//담당자: 조규민
// 광고 보상 팝업 표시·광고 상태·확인 가능 여부 저장과 상태 변경 알림

using System;

/// <summary>
/// 하우징 광고 보기 팝업의 표시 상태를 보관합니다.
/// </summary>
public class HousingAdRewardModel
{
    private HousingAdRewardState _currentState;

    public event Action<HousingAdRewardState> OnStateChanged;

    public HousingAdRewardState CurrentState => _currentState;

    public HousingAdRewardModel(string _message, string _rewardTitle, string _confirmText, string _cancelText)
    {
        _currentState = new HousingAdRewardState(
            false,
            HousingAdRewardStatus.Loading,
            _message,
            _rewardTitle,
            _confirmText,
            _cancelText,
            "광고를 준비하고 있습니다.",
            false);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    // 광고 상태·안내 문구·확인 가능 여부를 새 상태로 반영
    public void SetAdStatus(HousingAdRewardStatus _status, string _statusMessage, bool _canConfirm)
    {
        _currentState = _currentState.WithAdStatus(_status, _statusMessage, _canConfirm);
        OnStateChanged?.Invoke(_currentState);
    }

    public void ShowStatus(HousingAdRewardStatus _status, string _statusMessage, bool _canConfirm)
    {
        _currentState = _currentState
            .WithAdStatus(_status, _statusMessage, _canConfirm)
            .WithVisible(true);
        OnStateChanged?.Invoke(_currentState);
    }

    // 표시 여부 변경 시 불변 상태 복사와 변경 이벤트 발행
    private void SetVisible(bool _isVisible)
    {
        if (_currentState.IsVisible == _isVisible)
        {
            return;
        }

        _currentState = _currentState.WithVisible(_isVisible);
        OnStateChanged?.Invoke(_currentState);
    }
}

public enum HousingAdRewardStatus
{
    Loading,
    Ready,
    Showing,
    Offline,
    Failed
}

public readonly struct HousingAdRewardState
{
    public bool IsVisible { get; }
    public HousingAdRewardStatus Status { get; }
    public string Message { get; }
    public string RewardTitle { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public string StatusMessage { get; }
    public bool CanConfirm { get; }

    public HousingAdRewardState(
        bool _isVisible,
        HousingAdRewardStatus _status,
        string _message,
        string _rewardTitle,
        string _confirmText,
        string _cancelText,
        string _statusMessage,
        bool _canConfirm)
    {
        IsVisible = _isVisible;
        Status = _status;
        Message = string.IsNullOrWhiteSpace(_message) ? "광고를 보고 보상을 획득하시겠습니까?" : _message;
        RewardTitle = string.IsNullOrWhiteSpace(_rewardTitle) ? "Reward" : _rewardTitle;
        ConfirmText = string.IsNullOrWhiteSpace(_confirmText) ? "광고 보기" : _confirmText;
        CancelText = string.IsNullOrWhiteSpace(_cancelText) ? "취소" : _cancelText;
        StatusMessage = _statusMessage;
        CanConfirm = _canConfirm;
    }

    public HousingAdRewardState WithVisible(bool _isVisible)
    {
        return new HousingAdRewardState(
            _isVisible,
            Status,
            Message,
            RewardTitle,
            ConfirmText,
            CancelText,
            StatusMessage,
            CanConfirm);
    }

    public HousingAdRewardState WithAdStatus(
        HousingAdRewardStatus _status,
        string _statusMessage,
        bool _canConfirm)
    {
        return new HousingAdRewardState(
            IsVisible,
            _status,
            Message,
            RewardTitle,
            ConfirmText,
            CancelText,
            _statusMessage,
            _canConfirm);
    }
}
