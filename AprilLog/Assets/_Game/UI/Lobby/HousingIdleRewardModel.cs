//담당자: 조규민
//팝업의 시간당 생산량과 현재 수령 가능 수량이 섞이지 않도록 생산량 상태를 분리

using System;
using UnityEngine;

public class HousingIdleRewardModel
{
    private readonly int _maxChargeSeconds;
    private readonly HousingIdleRewardTable _rewardTable;

    private DateTime _chargeStartUtc;
    private HousingIdleRewardState _currentState;

    public event Action<HousingIdleRewardState> OnStateChanged;

    public HousingIdleRewardState CurrentState => _currentState;

    public HousingIdleRewardModel(int _maxChargeSeconds, HousingIdleRewardTable _rewardTable, DateTime _chargeStartUtc)
    {
        this._maxChargeSeconds = Mathf.Max(1, _maxChargeSeconds);
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

    public HousingIdleRewardClaimResult Claim()
    {
        DateTime _claimedAtUtc = DateTime.UtcNow;
        HousingIdleRewardState _claimedState = CalculateState(_claimedAtUtc);
        _chargeStartUtc = _claimedAtUtc;
        _currentState = CalculateState(_chargeStartUtc);
        OnStateChanged?.Invoke(_currentState);
        return new HousingIdleRewardClaimResult(_claimedState, _claimedAtUtc);
    }

    public void ForceNotify()
    {
        _currentState = CalculateState(DateTime.UtcNow);
        OnStateChanged?.Invoke(_currentState);
    }

    private HousingIdleRewardState CalculateState(DateTime _nowUtc)
    {
        double _elapsedSeconds = Math.Max(0d, (_nowUtc - _chargeStartUtc).TotalSeconds);
        float _progress = Mathf.Clamp01((float)(_elapsedSeconds / _maxChargeSeconds));
        int _progressPercent = Mathf.Clamp(Mathf.FloorToInt(_progress * 100f), 0, 100);

        return new HousingIdleRewardState(
            _progress,
            _progressPercent,
            Mathf.FloorToInt(_rewardTable.MaxGoldReward * _progress),
            Mathf.FloorToInt(_rewardTable.MaxParchmentReward * _progress),
            Mathf.FloorToInt(_rewardTable.MaxDiamondReward * _progress),
            CalculateRewardPerHour(_rewardTable.MaxGoldReward),
            CalculateRewardPerHour(_rewardTable.MaxParchmentReward),
            CalculateRewardPerHour(_rewardTable.MaxDiamondReward),
            _rewardTable.GoldItemId,
            _rewardTable.ParchmentItemId,
            _rewardTable.DiamondItemId);
    }

    private int CalculateRewardPerHour(int _maxReward)
    {
        if (_maxReward <= 0)
        {
            return 0;
        }

        return Mathf.FloorToInt(_maxReward * 3600f / _maxChargeSeconds);
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
    public int MaxGoldReward { get; }
    public int ParchmentItemId { get; }
    public int MaxParchmentReward { get; }
    public int DiamondItemId { get; }
    public int MaxDiamondReward { get; }

    public HousingIdleRewardTable(
        int _goldItemId,
        int _maxGoldReward,
        int _parchmentItemId,
        int _maxParchmentReward,
        int _diamondItemId,
        int _maxDiamondReward)
    {
        GoldItemId = _goldItemId;
        MaxGoldReward = Mathf.Max(0, _maxGoldReward);
        ParchmentItemId = _parchmentItemId;
        MaxParchmentReward = Mathf.Max(0, _maxParchmentReward);
        DiamondItemId = _diamondItemId;
        MaxDiamondReward = Mathf.Max(0, _maxDiamondReward);
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
