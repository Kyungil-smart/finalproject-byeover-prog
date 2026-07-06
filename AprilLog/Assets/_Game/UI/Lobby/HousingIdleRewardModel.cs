//담당자: 조규민
//팝업의 시간당 생산량과 현재 수령 가능 수량이 섞이지 않도록 생산량 상태를 분리
// 수정 내용 : 보상 지급 성공 전에는 마지막 수령 시각과 누적 상태를 초기화하지 않도록 수령 준비와 확정을 분리,
// 실제 적립된 시간 단위와 UI 진행률이 일치하도록 진행률 계산 기준을 통일

using System;
using UnityEngine;

public class HousingIdleRewardModel
{
    private readonly int _maxChargeSeconds;
    private readonly int _rewardIntervalSeconds;
    private readonly int _maxRewardCount;
    private readonly HousingIdleRewardTable _rewardTable;

    private DateTime _chargeStartUtc;
    private HousingIdleRewardState _currentState;

    public event Action<HousingIdleRewardState> OnStateChanged;

    public HousingIdleRewardState CurrentState => _currentState;

    public HousingIdleRewardModel(int _maxChargeSeconds, int _rewardIntervalSeconds, HousingIdleRewardTable _rewardTable, DateTime _chargeStartUtc)
    {
        this._maxChargeSeconds = Mathf.Max(1, _maxChargeSeconds);
        this._rewardIntervalSeconds = Mathf.Max(1, _rewardIntervalSeconds);
        _maxRewardCount = Mathf.Max(1, Mathf.FloorToInt((float)this._maxChargeSeconds / this._rewardIntervalSeconds));
        this._rewardTable = _rewardTable ?? HousingIdleRewardTable.Empty;
        this._chargeStartUtc = EnsureUtc(_chargeStartUtc);
        _currentState = CalculateState(this._chargeStartUtc);
    }

    public void Refresh()
    {
        HousingIdleRewardState _newState = CalculateState(DateTime.UtcNow);

        if (_currentState.HasSameDisplayValue(_newState))
        {
            return;
        }

        _currentState = _newState;
        OnStateChanged?.Invoke(_currentState);
    }

    public HousingIdleRewardClaimResult CreateClaimRequest()
    {
        DateTime _claimedAtUtc = DateTime.UtcNow;
        HousingIdleRewardState _claimedState = CalculateState(_claimedAtUtc);

        return new HousingIdleRewardClaimResult(_claimedState, _claimedAtUtc);
    }

    public void CommitClaim(HousingIdleRewardClaimResult _claimResult)
    {
        _chargeStartUtc = EnsureUtc(_claimResult.ClaimedAtUtc);
        _currentState = CalculateState(_chargeStartUtc);
        OnStateChanged?.Invoke(_currentState);
    }

    public void ForceNotify()
    {
        _currentState = CalculateState(DateTime.UtcNow);
        OnStateChanged?.Invoke(_currentState);
    }

    private HousingIdleRewardState CalculateState(DateTime _nowUtc)
    {
        double _elapsedSeconds = Math.Max(0d, (_nowUtc - _chargeStartUtc).TotalSeconds);
        double _cappedElapsedSeconds = Math.Min(_elapsedSeconds, _maxChargeSeconds);
        // 1시간이 완전히 지난 횟수만 보상으로 인정하고, 최대 누적 시간 이후에는 더 쌓지 않습니다.
        int _rewardCount = Mathf.Clamp(Mathf.FloorToInt((float)(_cappedElapsedSeconds / _rewardIntervalSeconds)), 0, _maxRewardCount);
        // 보상은 시간 단위로 적립되므로 진행률도 실제 적립된 횟수를 기준으로 단계적으로 표시합니다.
        float _progress = Mathf.Clamp01((float)_rewardCount / _maxRewardCount);
        int _progressPercent = Mathf.Clamp(Mathf.FloorToInt(_progress * 100f), 0, 100);

        return new HousingIdleRewardState(
            _progress,
            _progressPercent,
            CalculateReward(_rewardTable.GoldRewardPerHour, _rewardCount),
            CalculateReward(_rewardTable.ParchmentRewardPerHour, _rewardCount),
            CalculateReward(_rewardTable.DiamondRewardPerHour, _rewardCount),
            _rewardTable.GoldRewardPerHour,
            _rewardTable.ParchmentRewardPerHour,
            _rewardTable.DiamondRewardPerHour,
            _rewardTable.GoldItemId,
            _rewardTable.ParchmentItemId,
            _rewardTable.DiamondItemId);
    }

    private int CalculateReward(int _rewardPerHour, int _rewardCount)
    {
        if (_rewardPerHour <= 0 || _rewardCount <= 0)
        {
            return 0;
        }

        return _rewardPerHour * _rewardCount;
    }

    private static DateTime EnsureUtc(DateTime _dateTime)
    {
        if (_dateTime.Kind == DateTimeKind.Utc)
        {
            return _dateTime;
        }

        return _dateTime.ToUniversalTime();
    }
}

public readonly struct HousingIdleRewardClaimResult
{
    public HousingIdleRewardState State { get; }
    public DateTime ClaimedAtUtc { get; }
    public string ClaimedAtUtcText => ClaimedAtUtc.ToString("o");

    public HousingIdleRewardClaimResult(HousingIdleRewardState _state, DateTime _claimedAtUtc)
    {
        State = _state;
        ClaimedAtUtc = _claimedAtUtc.Kind == DateTimeKind.Utc ? _claimedAtUtc : _claimedAtUtc.ToUniversalTime();
    }
}

public class HousingIdleRewardTable
{
    public static readonly HousingIdleRewardTable Empty = new HousingIdleRewardTable(70001, 0, 70002, 0, 70003, 0);

    public int GoldItemId { get; }
    public int GoldRewardPerHour { get; }
    public int ParchmentItemId { get; }
    public int ParchmentRewardPerHour { get; }
    public int DiamondItemId { get; }
    public int DiamondRewardPerHour { get; }

    public HousingIdleRewardTable(
        int _goldItemId,
        int _goldRewardPerHour,
        int _parchmentItemId,
        int _parchmentRewardPerHour,
        int _diamondItemId,
        int _diamondRewardPerHour)
    {
        GoldItemId = _goldItemId;
        GoldRewardPerHour = Mathf.Max(0, _goldRewardPerHour);
        ParchmentItemId = _parchmentItemId;
        ParchmentRewardPerHour = Mathf.Max(0, _parchmentRewardPerHour);
        DiamondItemId = _diamondItemId;
        DiamondRewardPerHour = Mathf.Max(0, _diamondRewardPerHour);
    }
}

public readonly struct HousingIdleRewardState
{
    public float Progress { get; }
    public int ProgressPercent { get; }
    public int GoldReward { get; }
    public int ParchmentReward { get; }
    public int DiamondReward { get; }
    public int GoldPerHour { get; }
    public int ParchmentPerHour { get; }
    public int DiamondPerHour { get; }
    public int GoldItemId { get; }
    public int ParchmentItemId { get; }
    public int DiamondItemId { get; }
    public bool IsFull => ProgressPercent >= 100;
    public bool HasClaimableReward => GoldReward > 0 || ParchmentReward > 0 || DiamondReward > 0;

    public HousingIdleRewardState(
        float _progress,
        int _progressPercent,
        int _goldReward,
        int _parchmentReward,
        int _diamondReward,
        int _goldPerHour,
        int _parchmentPerHour,
        int _diamondPerHour,
        int _goldItemId,
        int _parchmentItemId,
        int _diamondItemId)
    {
        Progress = Mathf.Clamp01(_progress);
        ProgressPercent = Mathf.Clamp(_progressPercent, 0, 100);
        GoldReward = Mathf.Max(0, _goldReward);
        ParchmentReward = Mathf.Max(0, _parchmentReward);
        DiamondReward = Mathf.Max(0, _diamondReward);
        GoldPerHour = Mathf.Max(0, _goldPerHour);
        ParchmentPerHour = Mathf.Max(0, _parchmentPerHour);
        DiamondPerHour = Mathf.Max(0, _diamondPerHour);
        GoldItemId = _goldItemId;
        ParchmentItemId = _parchmentItemId;
        DiamondItemId = _diamondItemId;
    }

    public bool HasSameDisplayValue(HousingIdleRewardState _other)
    {
        return ProgressPercent == _other.ProgressPercent
            && GoldReward == _other.GoldReward
            && ParchmentReward == _other.ParchmentReward
            && DiamondReward == _other.DiamondReward
            && GoldPerHour == _other.GoldPerHour
            && ParchmentPerHour == _other.ParchmentPerHour
            && DiamondPerHour == _other.DiamondPerHour;
    }
}
