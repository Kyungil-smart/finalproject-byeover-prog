//담당자: 조규민

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하우징 자동재화 MVP 객체를 Inspector 참조와 계정 저장 데이터로 초기화합니다.
/// </summary>
public class HousingAutoCurrencyController : MonoBehaviour
{
    [Header("View 연결")]
    [SerializeField] private HousingAutoCurrencyFurnitureView _furnitureView;
    [SerializeField] private HousingAutoCurrencyPopupView _popupView;
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

    private HousingAutoCurrencyModel _model;
    private HousingAutoCurrencyPresenter _presenter;

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
        if (_furnitureView == null)
        {
            _furnitureView = GetComponentInChildren<HousingAutoCurrencyFurnitureView>(true);
        }

        if (_popupView == null)
        {
            _popupView = GetComponentInChildren<HousingAutoCurrencyPopupView>(true);
        }

        if (_currencyModel == null)
        {
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        }
    }

    private void InitializePresenter()
    {
        HousingAutoCurrencyRewardTable _rewardTable = ResolveRewardTable();
        DateTime _chargeStartUtc = ResolveSavedChargeStartUtc();
        _model = new HousingAutoCurrencyModel(_maxChargeSeconds, _rewardTable, _chargeStartUtc);
        _presenter = new HousingAutoCurrencyPresenter(_model, _popupView, _furnitureView, HandleClaimRequested);
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

    private HousingAutoCurrencyRewardTable ResolveRewardTable()
    {
        if (!_useHousingRewardTable)
        {
            return CreateManualRewardTable();
        }

        HousingRewardData _rewardData = FindHousingRewardData();

        if (_rewardData == null)
        {
            Debug.LogWarning("[HousingAutoCurrencyController] HousingRewardData를 찾지 못해 Inspector 수동 보상값을 사용합니다.", this);
            return CreateManualRewardTable();
        }

        return new HousingAutoCurrencyRewardTable(
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
        HousingRewardData _currentReward = _housingRepo.GetReward(_currentChapter);

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

    private HousingAutoCurrencyRewardTable CreateManualRewardTable()
    {
        return new HousingAutoCurrencyRewardTable(
            70001,
            _manualGoldReward,
            70002,
            _manualParchmentReward,
            70003,
            _manualDiamondReward);
    }

    private void ApplyFurnitureIcon()
    {
        if (_popupView == null || _furnitureView == null)
        {
            return;
        }

        Image _furnitureImage = _furnitureView.GetComponent<Image>();

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

    private void HandleClaimRequested(HousingAutoCurrencyClaimResult _claimResult)
    {
        HousingAutoCurrencyState _state = _claimResult.State;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClaimHousingAutoCurrency(_state.GoldReward, _state.ParchmentReward, _claimResult.ClaimedAtUtcText);
            LogDiamondReward(_state.DiamondReward);
            return;
        }

        GrantLocalCurrency(_state);
        LogDiamondReward(_state.DiamondReward);
    }

    private void GrantLocalCurrency(HousingAutoCurrencyState _state)
    {
        if (_currencyModel == null)
        {
            Debug.Log($"[HousingAutoCurrencyController] CurrencyModel 미연결로 자동재화 수령을 로그 처리합니다. Gold: {_state.GoldReward}, Parchment: {_state.ParchmentReward}", this);
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

    private void LogDiamondReward(int _diamondReward)
    {
        if (_diamondReward <= 0)
        {
            return;
        }

        // TODO [2026-06-26 조규민] 다이아 재화 저장 API 확정 후 실제 지급 연동 필요
        Debug.Log($"[HousingAutoCurrencyController] 다이아 보상은 저장 API 미확정으로 임시 처리합니다. Amount: {_diamondReward}", this);
    }
}
