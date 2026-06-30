//담당자: 조규민

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
        _currentState = new HousingAdRewardState(false, _message, _rewardTitle, _confirmText, _cancelText);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

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

public readonly struct HousingAdRewardState
{
    public bool IsVisible { get; }
    public string Message { get; }
    public string RewardTitle { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }

    public HousingAdRewardState(
        bool _isVisible,
        string _message,
        string _rewardTitle,
        string _confirmText,
        string _cancelText)
    {
        IsVisible = _isVisible;
        Message = string.IsNullOrWhiteSpace(_message) ? "광고를 보고 보상을 획득하시겠습니까?" : _message;
        RewardTitle = string.IsNullOrWhiteSpace(_rewardTitle) ? "Reward" : _rewardTitle;
        ConfirmText = string.IsNullOrWhiteSpace(_confirmText) ? "광고 보기" : _confirmText;
        CancelText = string.IsNullOrWhiteSpace(_cancelText) ? "취소" : _cancelText;
    }

    public HousingAdRewardState WithVisible(bool _isVisible)
    {
        return new HousingAdRewardState(_isVisible, Message, RewardTitle, ConfirmText, CancelText);
    }
}
