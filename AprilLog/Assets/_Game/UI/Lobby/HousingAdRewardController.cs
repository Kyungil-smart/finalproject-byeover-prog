//담당자: 조규민

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
        PrepareAd();
    }

    private void OnDestroy()
    {
        _presenter?.Release();
    }

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
        _model = new HousingAdRewardModel(_message, _rewardTitle, _confirmText, _cancelText);
        _presenter = new HousingAdRewardPresenter(_model, _popupView, _buttonView, HandleAdWatchRequested);
        _presenter.Initialize();

        if (_applyFurnitureIconFromButton)
        {
            ApplyFurnitureIcon();
        }
    }

    private void PrepareAd()
    {
        if (!_loadAdOnEnable)
        {
            return;
        }

        ResolveAdReferences();

        if (_adService == null)
        {
            return;
        }

        if (!_adService.IsInitialized)
        {
            _adService.Initialize();
            return;
        }

        _adService.LoadAd();
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

    private bool TryShowRewardedAd()
    {
        if (_isShowingAd)
        {
            return false;
        }

        ResolveAdReferences();

        if (_networkChecker != null && !_networkChecker.IsOnline)
        {
            Debug.LogWarning("[HousingAdRewardController] 오프라인 상태라 광고를 표시할 수 없습니다.", this);
            return false;
        }

        if (_adService == null)
        {
            Debug.LogWarning("[HousingAdRewardController] RewardedAdService가 연결되지 않았습니다.", this);
            return false;
        }

        if (!_adService.IsInitialized)
        {
            Debug.Log("[HousingAdRewardController] 광고 SDK 초기화를 요청했습니다. 잠시 후 다시 시도하세요.", this);
            _adService.Initialize();
            return false;
        }

        if (!_adService.IsAdReady)
        {
            Debug.Log("[HousingAdRewardController] 광고를 불러오는 중입니다. 잠시 후 다시 시도하세요.", this);
            _adService.LoadAd();
            return false;
        }

        _isShowingAd = true;
        _isRewardGrantedForCurrentAd = false;

        _adService.ShowAd(
            onRewardEarned: HandleRewardEarned,
            onClosed: HandleAdClosed,
            onFailed: HandleAdFailed);

        return true;
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

        _staminaModel.Recover(_safeRewardStamina, out var lossamount);
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
    }

    private void HandleAdFailed()
    {
        _isShowingAd = false;
        _isRewardGrantedForCurrentAd = false;
        _onAdFailed?.Invoke();
    }
}
