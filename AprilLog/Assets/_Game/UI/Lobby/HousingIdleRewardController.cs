//담당자: 조규민

// 기존 지급 API가 성공한 경우에만 Presenter가 방치 보상 수령 상태를 확정하도록 결과를 반환
// Inspector 충전 시간과 수동 보상값이 유효 범위를 벗어나지 않도록 자동 보정

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 하우징 시간 누적 보상 UI와 계정 저장 데이터를 연결합니다.
/// </summary>
// 저장된 충전 시각과 현재 챕터 보상 테이블로 방치 보상 기능 초기화
// 주기적 Model 갱신과 수령 결과에 따른 로컬 재화 지급
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
    [SerializeField] private int _maxChargeSeconds = 43200;
    [Tooltip("재화가 1회 적립되는 시간입니다.")]
    [SerializeField] private int _rewardIntervalSeconds = 3600;
    [Tooltip("Model 상태를 확인하는 주기입니다. UI는 값이 바뀔 때만 갱신됩니다.")]
    [SerializeField] private float _refreshIntervalSeconds = 1f;

    private HousingIdleRewardModel _model;
    private HousingIdleRewardPresenter _presenter;

    private void Awake()
    {
        ResolveReferences();
        InitializePresenter();
    }

    private void OnValidate()
    {
        _defaultClearChapter = Mathf.Max(1, _defaultClearChapter);
        _manualGoldReward = Mathf.Max(0, _manualGoldReward);
        _manualParchmentReward = Mathf.Max(0, _manualParchmentReward);
        _manualDiamondReward = Mathf.Max(0, _manualDiamondReward);
        _maxChargeSeconds = Mathf.Max(1, _maxChargeSeconds);
        _rewardIntervalSeconds = Mathf.Clamp(_rewardIntervalSeconds, 1, _maxChargeSeconds);
        _refreshIntervalSeconds = Mathf.Max(0.25f, _refreshIntervalSeconds);
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

    // 방치 보상 버튼·팝업과 재화 지급 대상 참조 탐색
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
        _model = new HousingIdleRewardModel(_maxChargeSeconds, _rewardIntervalSeconds, _rewardTable, _chargeStartUtc);
        _presenter = new HousingIdleRewardPresenter(_model, _popupView, _rewardButtonView, HandleClaimRequested);
        _presenter.Initialize();
        ApplyFurnitureIcon();
        _popupView?.Hide();
    }

    // 저장 데이터의 충전 시작 시각 로드와 유효하지 않은 값 보정
    private DateTime ResolveSavedChargeStartUtc()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.EnsureHousingAutoCurrencyLastClaimUtc();
        }

        return DateTime.UtcNow;
    }

    // 현재 챕터 데이터 우선 조회 후 Inspector 기본 보상표 대체
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

    // 활성화 상태에서 주기적인 방치 보상 UI 갱신 시작
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

    // 수령 결과 검증 후 재화 지급과 저장 데이터 반영
    private bool HandleClaimRequested(HousingIdleRewardClaimResult _claimResult)
    {
        HousingIdleRewardState _state = _claimResult.State;

        if (GameManager.Instance != null)
        {
            return GameManager.Instance.ClaimHousingIdleReward(
                _state.GoldReward,
                _state.ParchmentReward,
                _state.DiamondReward,
                _claimResult.ClaimedAtUtcText);
        }

        return GrantLocalCurrency(_state);
    }

    private bool GrantLocalCurrency(HousingIdleRewardState _state)
    {
        if (_currencyModel == null)
        {
            Debug.LogWarning($"[HousingIdleRewardController] CurrencyModel 미연결로 시간 누적 보상을 지급하지 못했습니다. Gold: {_state.GoldReward}, Parchment: {_state.ParchmentReward}, Diamond: {_state.DiamondReward}", this);
            return false;
        }

        if (_state.GoldReward > 0)
        {
            _currencyModel.AddGold(_state.GoldReward);
        }

        if (_state.ParchmentReward > 0)
        {
            _currencyModel.AddParchment(_state.ParchmentReward);
        }

        if (_state.DiamondReward > 0)
        {
            _currencyModel.AddDiamond(_state.DiamondReward);
        }

        return true;
    }
}
