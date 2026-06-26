//담당자: 조규민

using System;
using UnityEngine;

/// <summary>
/// 하우징 자동재화의 충전 시간과 수령 가능 보상을 계산합니다.
/// </summary>
public class HousingAutoCurrencyModel
{
    private readonly int _maxChargeSeconds;
    private readonly HousingAutoCurrencyRewardTable _rewardTable;

    private DateTime _chargeStartUtc;
    private HousingAutoCurrencyState _currentState;

    public event Action<HousingAutoCurrencyState> OnStateChanged;

    public HousingAutoCurrencyState CurrentState => _currentState;

    public HousingAutoCurrencyModel(int _maxChargeSeconds, HousingAutoCurrencyRewardTable _rewardTable, DateTime _chargeStartUtc)
    {
        this._maxChargeSeconds = Mathf.Max(1, _maxChargeSeconds);
        this._rewardTable = _rewardTable ?? HousingAutoCurrencyRewardTable.Empty;
        this._chargeStartUtc = EnsureUtc(_chargeStartUtc);
        _currentState = CalculateState(this._chargeStartUtc);
    }

    public void Refresh()
    {
        HousingAutoCurrencyState _newState = CalculateState(DateTime.UtcNow);

        if (_currentState.HasSameDisplayValue(_newState))
        {
            return;
        }

        _currentState = _newState;
        OnStateChanged?.Invoke(_currentState);
    }

    public HousingAutoCurrencyClaimResult Claim()
    {
        DateTime _claimedAtUtc = DateTime.UtcNow;
        HousingAutoCurrencyState _claimedState = CalculateState(_claimedAtUtc);
        _chargeStartUtc = _claimedAtUtc;
        _currentState = CalculateState(_chargeStartUtc);
        OnStateChanged?.Invoke(_currentState);
        return new HousingAutoCurrencyClaimResult(_claimedState, _claimedAtUtc);
    }

    public void ForceNotify()
    {
        _currentState = CalculateState(DateTime.UtcNow);
        OnStateChanged?.Invoke(_currentState);
    }

    private HousingAutoCurrencyState CalculateState(DateTime _nowUtc)
    {
        double _elapsedSeconds = Math.Max(0d, (_nowUtc - _chargeStartUtc).TotalSeconds);
        float _progress = Mathf.Clamp01((float)(_elapsedSeconds / _maxChargeSeconds));
        int _progressPercent = Mathf.Clamp(Mathf.FloorToInt(_progress * 100f), 0, 100);

        return new HousingAutoCurrencyState(
            _progress,
            _progressPercent,
            Mathf.FloorToInt(_rewardTable.MaxGoldReward * _progress),
            Mathf.FloorToInt(_rewardTable.MaxParchmentReward * _progress),
            Mathf.FloorToInt(_rewardTable.MaxDiamondReward * _progress),
            _rewardTable.GoldItemId,
            _rewardTable.ParchmentItemId,
            _rewardTable.DiamondItemId);
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

public readonly struct HousingAutoCurrencyClaimResult
{
    public HousingAutoCurrencyState State { get; }
    public DateTime ClaimedAtUtc { get; }
    public string ClaimedAtUtcText => ClaimedAtUtc.ToString("o");

    public HousingAutoCurrencyClaimResult(HousingAutoCurrencyState _state, DateTime _claimedAtUtc)
    {
        State = _state;
        ClaimedAtUtc = _claimedAtUtc.Kind == DateTimeKind.Utc ? _claimedAtUtc : _claimedAtUtc.ToUniversalTime();
    }
}

public class HousingAutoCurrencyRewardTable
{
    public static readonly HousingAutoCurrencyRewardTable Empty = new HousingAutoCurrencyRewardTable(70001, 0, 70002, 0, 70003, 0);

    public int GoldItemId { get; }
    public int MaxGoldReward { get; }
    public int ParchmentItemId { get; }
    public int MaxParchmentReward { get; }
    public int DiamondItemId { get; }
    public int MaxDiamondReward { get; }

    public HousingAutoCurrencyRewardTable(
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

public readonly struct HousingAutoCurrencyState
{
    public float Progress { get; }
    public int ProgressPercent { get; }
    public int GoldReward { get; }
    public int ParchmentReward { get; }
    public int DiamondReward { get; }
    public int GoldItemId { get; }
    public int ParchmentItemId { get; }
    public int DiamondItemId { get; }
    public bool IsFull => ProgressPercent >= 100;

    public HousingAutoCurrencyState(
        float _progress,
        int _progressPercent,
        int _goldReward,
        int _parchmentReward,
        int _diamondReward,
        int _goldItemId,
        int _parchmentItemId,
        int _diamondItemId)
    {
        Progress = Mathf.Clamp01(_progress);
        ProgressPercent = Mathf.Clamp(_progressPercent, 0, 100);
        GoldReward = Mathf.Max(0, _goldReward);
        ParchmentReward = Mathf.Max(0, _parchmentReward);
        DiamondReward = Mathf.Max(0, _diamondReward);
        GoldItemId = _goldItemId;
        ParchmentItemId = _parchmentItemId;
        DiamondItemId = _diamondItemId;
    }

    public bool HasSameDisplayValue(HousingAutoCurrencyState _other)
    {
        return ProgressPercent == _other.ProgressPercent
            && GoldReward == _other.GoldReward
            && ParchmentReward == _other.ParchmentReward
            && DiamondReward == _other.DiamondReward;
    }
}
