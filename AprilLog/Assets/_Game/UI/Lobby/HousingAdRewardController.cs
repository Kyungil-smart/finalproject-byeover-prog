//담당자: 조규민
// 광고·네트워크·보상 서비스 참조 해석과 외부 이벤트 등록·해제
// 광고 로드·재생·보상 지급·실패 상태를 Presenter와 Model에 전달
// 보상 종류에 따른 스태미나·다이아 지급과 팝업 상태 갱신

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 하우징 광고 보기 UI의 MVP 객체를 생성하고 광고 보상 지급을 연결합니다.
/// </summary>
public class HousingAdRewardController : MonoBehaviour
{
    [Header("View 연결")]
    [SerializeField] private HousingAdRewardButtonView _buttonView;
    [SerializeField] private HousingAdRewardPopupView _popupView;

    [Header("팝업 문구")]
    [SerializeField] private string _message = "광고를 보고 보상을 획득하시겠습니까?";
    [SerializeField] private string _rewardTitle = "Reward";
    [SerializeField] private string _confirmText = "광고 보기";
    [SerializeField] private string _cancelText = "취소";

    [Header("Inspector 값 유지")]
    [Tooltip("켜져 있으면 팝업 아이콘을 클릭한 가구 이미지로 자동 교체합니다. 꺼두면 Inspector에 설정한 아이콘을 유지합니다.")]
    [SerializeField] private bool _applyFurnitureIconFromButton = false;

    [Header("광고 서비스 연결")]
    [Tooltip("보상형 광고 서비스입니다. 비워두면 씬에서 자동 탐색합니다.")]
    [SerializeField] private RewardedAdService _adService;
    [Tooltip("네트워크 상태 확인용입니다. 비워두면 씬에서 자동 탐색합니다.")]
    [SerializeField] private NetworkChecker _networkChecker;

    [Header("보상 모델 연결")]
    [Tooltip("행동력 지급 대상입니다. 비워두면 씬에서 자동 탐색합니다.")]
    [SerializeField] private StaminaModel _staminaModel;
    [Tooltip("GameManager가 없는 테스트 환경에서 다이아 지급에 사용할 모델입니다.")]
    [SerializeField] private CurrencyModel _currencyModel;

    [Header("광고 보상")]
    [SerializeField] private int _rewardStamina = 20;
    [SerializeField] private int _rewardDiamond = 200;
    [Tooltip("켜져 있으면 시작 시 광고 초기화/로드를 요청합니다.")]
    [SerializeField] private bool _loadAdOnEnable = true;

    [Header("광고 이벤트")]
    [Tooltip("광고 보기 요청 직전에 호출됩니다. 사운드나 로그 연결용입니다.")]
    [SerializeField] private UnityEvent _onAdWatchRequested;
    [Tooltip("광고 보상 지급 완료 후 호출됩니다.")]
    [SerializeField] private UnityEvent _onRewardGranted;
    [Tooltip("광고 표시 실패 시 호출됩니다.")]
    [SerializeField] private UnityEvent _onAdFailed;

    private HousingAdRewardModel _model;
    private HousingAdRewardPresenter _presenter;
    private bool _isShowingAd;
    private bool _isRewardGrantedForCurrentAd;

    private void Awake()
    {
        ResolveReferences();
        InitializePresenter();
    }

    private void OnEnable()
    {
        SubscribeExternalEvents();
        PrepareAd();
        RefreshAdAvailability();
    }

    private void OnDisable()
    {
        UnsubscribeExternalEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeExternalEvents();
        _presenter?.Release();
    }

    // 광고 로드·보상·종료와 네트워크 상태 이벤트 등록
    private void SubscribeExternalEvents()
    {
        UseReadyAdServiceIfAvailable();
        ResolveAdReferences();

        if (_adService != null)
        {
            _adService.OnAdLoaded -= HandleAdLoaded;
            _adService.OnAdLoaded += HandleAdLoaded;
            _adService.OnAdLoadFailed -= HandleAdLoadFailed;
            _adService.OnAdLoadFailed += HandleAdLoadFailed;
        }

        if (_networkChecker == null)
        {
            return;
        }

        _networkChecker.OnOnline -= HandleNetworkOnline;
        _networkChecker.OnOnline += HandleNetworkOnline;
        _networkChecker.OnOffline -= HandleNetworkOffline;
        _networkChecker.OnOffline += HandleNetworkOffline;
    }

    private void UnsubscribeExternalEvents()
    {
        if (_adService != null)
        {
            _adService.OnAdLoaded -= HandleAdLoaded;
            _adService.OnAdLoadFailed -= HandleAdLoadFailed;
        }

        if (_networkChecker != null)
        {
            _networkChecker.OnOnline -= HandleNetworkOnline;
            _networkChecker.OnOffline -= HandleNetworkOffline;
        }
    }

    // Inspector 누락 참조 자동 탐색과 서비스별 참조 분리
    private void ResolveReferences()
    {
        if (_buttonView == null)
        {
            _buttonView = GetComponentInChildren<HousingAdRewardButtonView>(true);
        }

        if (_popupView == null)
        {
            _popupView = GetComponentInChildren<HousingAdRewardPopupView>(true);
        }

        ResolveAdReferences();
        ResolveRewardReferences();
    }

    private void ResolveAdReferences()
    {
        if (_adService == null)
        {
            _adService = FindFirstObjectByType<RewardedAdService>(FindObjectsInactive.Include);
        }

        if (_networkChecker == null)
        {
            _networkChecker = FindFirstObjectByType<NetworkChecker>(FindObjectsInactive.Include);
        }
    }

    // 상점에서 미리 로드한 광고가 있으면 하우징 전용 광고를 다시 기다리지 않고 재사용한다.
    private void UseReadyAdServiceIfAvailable()
    {
        RewardedAdService[] _services = FindObjectsByType<RewardedAdService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < _services.Length; i++)
        {
            RewardedAdService _service = _services[i];
            if (_service == null || !_service.IsAdReady)
            {
                continue;
            }

            _adService = _service;
            return;
        }
    }

    private void ResolveRewardReferences()
    {
        if (_staminaModel == null)
        {
            _staminaModel = FindFirstObjectByType<StaminaModel>(FindObjectsInactive.Include);
        }

        if (_currencyModel == null)
        {
            _currencyModel = FindFirstObjectByType<CurrencyModel>(FindObjectsInactive.Include);
        }
    }

    private void InitializePresenter()
    {
        _popupView?.SetRewardAmounts(_rewardStamina, _rewardDiamond);
        _model = new HousingAdRewardModel(_message, _rewardTitle, _confirmText, _cancelText);
        _presenter = new HousingAdRewardPresenter(_model, _popupView, _buttonView, HandleAdWatchRequested);
        _presenter.Initialize();

        if (_applyFurnitureIconFromButton)
        {
            ApplyFurnitureIcon();
        }
    }

    // 네트워크와 광고 서비스 준비 상태 확인 후 보상형 광고 로드 요청
    private void PrepareAd()
    {
        if (!_loadAdOnEnable)
        {
            return;
        }

        ResolveAdReferences();

        if (_adService == null)
        {
            SetFailedState("광고 서비스를 찾을 수 없습니다.");
            return;
        }

        if (!_adService.IsInitialized)
        {
            _presenter?.SetAdStatus(HousingAdRewardStatus.Loading, "광고 서비스를 초기화하고 있습니다.", false);
            _adService.Initialize();
            return;
        }

        _presenter?.SetAdStatus(HousingAdRewardStatus.Loading, "광고를 불러오고 있습니다.", false);
        _adService.LoadAd();
    }

    private void RefreshAdAvailability()
    {
        if (_presenter == null || _isShowingAd)
        {
            return;
        }

        // 이미 로드된 보상형 광고는 오프라인이어도 재생 가능하므로 네트워크 상태보다 우선해서 노출한다.
        // (에디터에서는 Application.internetReachability가 실제와 무관하게 오프라인으로 잡히는 경우가 있어 이 우선 판정이 필요함)
        if (_adService != null && _adService.IsAdReady)
        {
            _presenter.SetAdStatus(HousingAdRewardStatus.Ready, _message, true);
            return;
        }

        if (_networkChecker != null && !_networkChecker.IsOnline)
        {
            _presenter.SetAdStatus(HousingAdRewardStatus.Offline, "인터넷 연결을 확인해 주세요.", false);
            return;
        }

        if (_adService == null)
        {
            SetFailedState("광고 서비스를 찾을 수 없습니다.");
            return;
        }

        _presenter.SetAdStatus(HousingAdRewardStatus.Loading, "광고를 불러오고 있습니다.", false);
    }

    private void ApplyFurnitureIcon()
    {
        if (_buttonView == null || _popupView == null)
        {
            return;
        }

        Image _furnitureImage = _buttonView.GetComponent<Image>();

        if (_furnitureImage == null)
        {
            return;
        }

        _popupView.SetFurnitureIcon(_furnitureImage.sprite);
    }

    private bool HandleAdWatchRequested()
    {
        _onAdWatchRequested?.Invoke();
        return TryShowRewardedAd();
    }

    // 광고 준비 상태 검증 후 보상형 광고 표시 요청
    private bool TryShowRewardedAd()
    {
        if (_isShowingAd)
        {
            return false;
        }

        ResolveAdReferences();

        // 준비된 광고가 없을 때만 오프라인으로 막는다. 이미 로드된 광고는 오프라인이어도 재생 가능.
        if (_networkChecker != null && !_networkChecker.IsOnline && (_adService == null || !_adService.IsAdReady))
        {
            Debug.LogWarning("[HousingAdRewardController] 오프라인 상태라 광고를 표시할 수 없습니다.", this);
            _presenter?.ShowAdStatus(HousingAdRewardStatus.Offline, "인터넷 연결을 확인해 주세요.", false);
            return false;
        }

        if (_adService == null)
        {
            Debug.LogWarning("[HousingAdRewardController] RewardedAdService가 연결되지 않았습니다.", this);
            SetFailedState("광고 서비스를 찾을 수 없습니다.", true);
            return false;
        }

        if (!_adService.IsInitialized)
        {
            Debug.Log("[HousingAdRewardController] 광고 SDK 초기화를 요청했습니다. 잠시 후 다시 시도하세요.", this);
            _presenter?.ShowAdStatus(HousingAdRewardStatus.Loading, "광고 서비스를 초기화하고 있습니다.", false);
            _adService.Initialize();
            return false;
        }

        if (!_adService.IsAdReady)
        {
            Debug.Log("[HousingAdRewardController] 광고를 불러오는 중입니다. 잠시 후 다시 시도하세요.", this);
            _presenter?.ShowAdStatus(HousingAdRewardStatus.Loading, "광고를 불러오고 있습니다.", false);
            _adService.LoadAd();
            return false;
        }

        _isShowingAd = true;
        _isRewardGrantedForCurrentAd = false;
        _presenter?.SetAdStatus(HousingAdRewardStatus.Showing, "광고를 재생하고 있습니다.", false);

        bool _didStart = _adService.ShowAd(
            onRewardEarned: HandleRewardEarned,
            onClosed: HandleAdClosed,
            onFailed: HandleAdFailed);

        if (_didStart)
        {
            return true;
        }

        _isShowingAd = false;
        return false;
    }

    private void HandleRewardEarned()
    {
        if (_isRewardGrantedForCurrentAd)
        {
            return;
        }

        _isRewardGrantedForCurrentAd = true;
        GrantReward();
        _onRewardGranted?.Invoke();
    }

    // 선택된 광고 보상 종류에 따른 실제 재화 지급 분기
    private void GrantReward()
    {
        ResolveRewardReferences();
        GrantStaminaReward();
        GrantDiamondReward();
    }

    private void GrantStaminaReward()
    {
        int _safeRewardStamina = Mathf.Max(0, _rewardStamina);

        if (_safeRewardStamina <= 0)
        {
            return;
        }

        if (_staminaModel == null)
        {
            Debug.LogWarning($"[HousingAdRewardController] StaminaModel이 없어 행동력 보상을 지급하지 못했습니다. Amount: {_safeRewardStamina}", this);
            return;
        }

        _staminaModel.Recover(_safeRewardStamina, out int _lossAmount);

        if (_lossAmount > 0)
        {
            Debug.Log($"[HousingAdRewardController] 행동력 최대치로 {_lossAmount}만큼 지급되지 않았습니다.", this);
        }
    }

    private void GrantDiamondReward()
    {
        int _safeRewardDiamond = Mathf.Max(0, _rewardDiamond);

        if (_safeRewardDiamond <= 0)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddDiamond(_safeRewardDiamond, "하우징 광고 보상");
            return;
        }

        if (_currencyModel == null)
        {
            Debug.LogWarning($"[HousingAdRewardController] CurrencyModel이 없어 다이아 보상을 지급하지 못했습니다. Amount: {_safeRewardDiamond}", this);
            return;
        }

        _currencyModel.AddDiamond(_safeRewardDiamond);
    }

    private void HandleAdClosed()
    {
        _isShowingAd = false;
        RefreshAdAvailability();
    }

    private void HandleAdFailed()
    {
        _isShowingAd = false;
        _isRewardGrantedForCurrentAd = false;
        SetFailedState("광고 재생에 실패했습니다. 잠시 후 다시 시도해 주세요.", true);
        _onAdFailed?.Invoke();
    }

    private void HandleAdLoaded()
    {
        if (_isShowingAd)
        {
            return;
        }

        _presenter?.SetAdStatus(HousingAdRewardStatus.Ready, _message, true);
    }

    private void HandleAdLoadFailed()
    {
        if (_isShowingAd)
        {
            return;
        }

        SetFailedState("광고를 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.");
    }

    private void HandleNetworkOnline()
    {
        PrepareAd();
        RefreshAdAvailability();
    }

    private void HandleNetworkOffline()
    {
        if (_isShowingAd)
        {
            return;
        }

        _presenter?.SetAdStatus(HousingAdRewardStatus.Offline, "인터넷 연결을 확인해 주세요.", false);
    }

    // 광고 실패 메시지와 재시도 가능 상태를 Model에 반영
    private void SetFailedState(string _messageText, bool _showPopup = false)
    {
        if (_showPopup)
        {
            _presenter?.ShowAdStatus(HousingAdRewardStatus.Failed, _messageText, true);
            return;
        }

        _presenter?.SetAdStatus(HousingAdRewardStatus.Failed, _messageText, true);
    }
}
