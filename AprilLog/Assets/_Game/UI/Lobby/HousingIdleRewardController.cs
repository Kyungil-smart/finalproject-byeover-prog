//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 하우징 시간 누적 보상 UI와 계정 저장 데이터를 연결합니다.
/// </summary>
public class HousingIdleRewardController : MonoBehaviour
{
    [Header("View 연결")]
    [FormerlySerializedAs("_furnitureView")]
    [SerializeField] private HousingIdleRewardButtonView _rewardButtonView;
    [SerializeField] private HousingIdleRewardPopupView _popupView;
    [SerializeField] private CurrencyModel _currencyModel;

    [Header("보상 데이터")]
    [SerializeField] private bool _useHousingRewardTable = true;
    [Tooltip("현재 챕터 보상 데이터가 없을 때 사용할 ClearChapter 값입니다.")]
    [SerializeField] private int _defaultClearChapter = 1;
    [SerializeField] private int _manualGoldReward = 1000;
    [SerializeField] private int _manualParchmentReward = 10;
    [SerializeField] private int _manualDiamondReward = 1;

    [Header("충전 설정")]
    [Tooltip("게이지가 100%까지 충전되는 시간입니다.")]
    [SerializeField] private int _maxChargeSeconds = 3600;
    [Tooltip("Model 상태를 확인하는 주기입니다. UI는 값이 바뀔 때만 갱신됩니다.")]
    [SerializeField] private float _refreshIntervalSeconds = 1f;

    private HousingIdleRewardModel _model;
    private HousingIdleRewardPresenter _presenter;

    private void Awake()
    {
        ResolveReferences();
        InitializePresenter();
    }

    private void OnEnable()
    {
        StartRefreshLoop();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RefreshPresenter));
    }

    private void OnDestroy()
    {
        _presenter?.Release();
    }

    private void ResolveReferences()
    {
        if (_rewardButtonView == null)
        {
            _rewardButtonView = GetComponentInChildren<HousingIdleRewardButtonView>(true);
        }

        if (_popupView == null)
        {
            _popupView = GetComponentInChildren<HousingIdleRewardPopupView>(true);
        }

        if (_currencyModel == null)
        {
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        }
    }

    private void InitializePresenter()
    {
        HousingIdleRewardTable _rewardTable = ResolveRewardTable();
        DateTime _chargeStartUtc = ResolveSavedChargeStartUtc();
        _model = new HousingIdleRewardModel(_maxChargeSeconds, _rewardTable, _chargeStartUtc);
        _presenter = new HousingIdleRewardPresenter(_model, _popupView, _rewardButtonView, HandleClaimRequested);
        _presenter.Initialize();
        ApplyFurnitureIcon();
        _popupView?.Hide();
    }

    private DateTime ResolveSavedChargeStartUtc()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.EnsureHousingAutoCurrencyLastClaimUtc();
        }

        return DateTime.UtcNow;
    }

    private HousingIdleRewardTable ResolveRewardTable()
    {
        if (!_useHousingRewardTable)
        {
            return CreateManualRewardTable();
        }

        HousingRewardData _rewardData = FindHousingRewardData();

        if (_rewardData == null)
        {
            Debug.LogWarning("[HousingIdleRewardController] HousingRewardData를 찾지 못해 Inspector 수동 보상값을 사용합니다.", this);
            return CreateManualRewardTable();
        }

        return new HousingIdleRewardTable(
            _rewardData.Item_1,
            _rewardData.Amount_1,
            _rewardData.Item_2,
            _rewardData.Amount_2,
            _rewardData.Item_3,
            _rewardData.Amount_3);
    }

    private HousingRewardData FindHousingRewardData()
    {
        HousingRepo _housingRepo = DataManager.Instance != null ? DataManager.Instance.HousingRepo : null;

        if (_housingRepo == null)
        {
            return null;
        }

        int _currentChapter = GetCurrentChapter();
        HousingRewardData _currentReward = _housingRepo.GetRewardAtOrBelow(_currentChapter);

        if (_currentReward != null)
        {
            return _currentReward;
        }

        if (_defaultClearChapter == _currentChapter)
        {
            return null;
        }

        return _housingRepo.GetReward(_defaultClearChapter);
    }

    private int GetCurrentChapter()
    {
        if (GameManager.Instance?.CloudData == null)
        {
            return Mathf.Max(1, _defaultClearChapter);
        }

        return Mathf.Max(1, GameManager.Instance.CloudData.currentChapter);
    }

    private HousingIdleRewardTable CreateManualRewardTable()
    {
        return new HousingIdleRewardTable(
            70001,
            _manualGoldReward,
            70002,
            _manualParchmentReward,
            70003,
            _manualDiamondReward);
    }

    private void ApplyFurnitureIcon()
    {
        if (_popupView == null || _rewardButtonView == null)
        {
            return;
        }

        Image _furnitureImage = _rewardButtonView.GetComponent<Image>();

        if (_furnitureImage == null)
        {
            return;
        }

        _popupView.SetFurnitureIcon(_furnitureImage.sprite);
    }

    private void StartRefreshLoop()
    {
        CancelInvoke(nameof(RefreshPresenter));
        float _interval = Mathf.Max(0.25f, _refreshIntervalSeconds);
        InvokeRepeating(nameof(RefreshPresenter), _interval, _interval);
    }

    private void RefreshPresenter()
    {
        _presenter?.Refresh();
    }

    private void HandleClaimRequested(HousingIdleRewardClaimResult _claimResult)
    {
        HousingIdleRewardState _state = _claimResult.State;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClaimHousingIdleReward(
                _state.GoldReward,
                _state.ParchmentReward,
                _state.DiamondReward,
                _claimResult.ClaimedAtUtcText);
            return;
        }

        GrantLocalCurrency(_state);
        GrantDiamondReward(_state.DiamondReward);
    }

    private void GrantLocalCurrency(HousingIdleRewardState _state)
    {
        if (_currencyModel == null)
        {
            Debug.Log($"[HousingIdleRewardController] CurrencyModel 미연결로 시간 누적 보상 수령을 로그 처리합니다. Gold: {_state.GoldReward}, Parchment: {_state.ParchmentReward}", this);
            return;
        }

        if (_state.GoldReward > 0)
        {
            _currencyModel.AddGold(_state.GoldReward);
        }

        if (_state.ParchmentReward > 0)
        {
            _currencyModel.AddParchment(_state.ParchmentReward);
        }
    }

    // 추가: 조규민 - 시간 누적 보상 수령 시 다이아 보상을 기존 재화 저장 흐름으로 지급한다.
    private void GrantDiamondReward(int _diamondReward)
    {
        if (_diamondReward <= 0)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[HousingIdleRewardController] GameManager가 없어 다이아 보상을 지급하지 못했습니다. Amount: {_diamondReward}", this);
            return;
        }

        GameManager.Instance.AddDiamond(_diamondReward, "하우징 시간 누적 보상");
    }
}
