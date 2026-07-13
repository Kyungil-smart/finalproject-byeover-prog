using System;
using UnityEngine;
using GoogleMobileAds.Api;

// 작성자 : 홍정옥
// 설명   : Google Mobile Ads 보상형 광고(RewardedAd) 래퍼

// 1차 수정자 : 조규민
// 수정 내용 : 여러 광고 화면의 SDK 중복 초기화와 동시 표시 방지, 예외 발생 시 콜백 상태 복구
public class RewardedAdService : MonoBehaviour
{
    // 구글 보상형 광고 테스트 ID (개발/검수용)
    private const string TestAdUnitAndroid = "ca-app-pub-3940256099942544/5224354917";

    [Header("광고 단위 ID")]
    [Tooltip("체크 시 구글 공식 테스트 ID 를 사용한다. 실제 ID 가 준비되면 해제")]
    [SerializeField] private bool _useTestIds = true;
    [Tooltip("실제 Android 보상형 광고 단위 ID (운영용). _useTestIds 해제 시 사용")]
    [SerializeField] private string _androidAdUnitId = "";

    [Header("설정")]
    [Tooltip("Start 에서 SDK 초기화와 첫 광고 로드를 자동 수행한다.")]
    [SerializeField] private bool _autoInitialize = true;
    [Tooltip("초기화·로드 실패 시 일정 시간 뒤 자동으로 다시 시도한다. 첫 로드가 실패해 광고가 준비되지 않는 것을 방지한다.")]
    [SerializeField] private bool _autoRetryOnFailure = true;
    [Tooltip("자동 재시도 간격(초).")]
    [SerializeField] private float _retryDelaySeconds = 15f;

    // ---------- 이벤트 ----------
    public event Action OnAdLoaded;        // 광고 로드 완료(표시 가능)
    public event Action OnAdLoadFailed;    // 광고 로드 실패

    // ---------- 상태 ----------
    public bool IsInitialized { get; private set; }
    public bool IsAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();
    public bool IsShowingAd => _isShowingAd;

    private RewardedAd _rewardedAd;
    private bool _isLoading;
    private bool _isShowingAd;

    private static bool _isSdkInitializationStarted;
    private static bool _isSdkInitialized;
    private static bool _isAnyAdShowing;
    private static event Action<bool> OnSdkInitializationCompleted;

    private string AdUnitId => _useTestIds ? TestAdUnitAndroid : _androidAdUnitId;

    // ---------- 생명주기 ----------
    private void Start()
    {
        if (_autoInitialize)
            Initialize();
    }

    private void OnDestroy()
    {
        CancelInvoke();
        OnSdkInitializationCompleted -= HandleSdkInitializationCompleted;

        if (_isShowingAd)
        {
            _isShowingAd = false;
            _isAnyAdShowing = false;
        }

        ClearPending();
        DestroyAd();
    }

    // ---------- 초기화 ----------
    public void Initialize()
    {
        if (IsInitialized)
        {
            LoadAd();
            return;
        }

        if (_isSdkInitialized)
        {
            CompleteInitialization();
            return;
        }

        OnSdkInitializationCompleted -= HandleSdkInitializationCompleted;
        OnSdkInitializationCompleted += HandleSdkInitializationCompleted;

        if (_isSdkInitializationStarted)
        {
            return;
        }

        _isSdkInitializationStarted = true;

        MobileAds.Initialize(_initializationStatus =>
        {
            bool _isSucceeded = _initializationStatus != null;
            _isSdkInitialized = _isSucceeded;
            _isSdkInitializationStarted = false;

            Action<bool> _callbacks = OnSdkInitializationCompleted;
            OnSdkInitializationCompleted = null;
            _callbacks?.Invoke(_isSucceeded);
        });
    }

    private void HandleSdkInitializationCompleted(bool _isSucceeded)
    {
        OnSdkInitializationCompleted -= HandleSdkInitializationCompleted;

        if (!_isSucceeded)
        {
            Debug.LogWarning("[RewardedAdService] Mobile Ads SDK 초기화에 실패했습니다.", this);
            OnAdLoadFailed?.Invoke();
            ScheduleRetry(nameof(RetryInitialize));
            return;
        }

        CompleteInitialization();
    }

    private void CompleteInitialization()
    {
        IsInitialized = true;
        Debug.Log("[RewardedAdService] Mobile Ads SDK 초기화 완료");
        LoadAd();
    }

    // ---------- 로드 ----------
    public void LoadAd()
    {
        if (!IsInitialized || _isLoading) return;
        if (IsAdReady) return; // 이미 준비된 광고가 있으면 재로드하지 않음

        string adUnitId = AdUnitId;
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[RewardedAdService] 광고 단위 ID 가 비어 있습니다. _useTestIds 를 켜거나 ID 를 입력하세요.", this);
            return;
        }

        DestroyAd();
        _isLoading = true;

        var request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (RewardedAd ad, LoadAdError error) =>
        {
            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"[RewardedAdService] 광고 로드 실패: {error}", this);
                OnAdLoadFailed?.Invoke();
                ScheduleRetry(nameof(RetryLoad));
                return;
            }

            Debug.Log("[RewardedAdService] 광고 로드 완료");
            CancelInvoke(nameof(RetryLoad));
            _rewardedAd = ad;
            RegisterAdEvents(ad);
            OnAdLoaded?.Invoke();
        });
    }

    // ---------- 실패 재시도 ----------
    // 첫 초기화·로드가 실패해도 일정 시간 뒤 다시 시도해 광고가 준비되지 않는 상태로 방치되지 않게 한다.
    private void ScheduleRetry(string _methodName)
    {
        if (!_autoRetryOnFailure || !isActiveAndEnabled)
        {
            return;
        }

        CancelInvoke(_methodName);
        Invoke(_methodName, Mathf.Max(1f, _retryDelaySeconds));
    }

    private void RetryLoad()
    {
        if (IsAdReady || _isShowingAd)
        {
            return;
        }

        LoadAd();
    }

    private void RetryInitialize()
    {
        if (IsInitialized)
        {
            LoadAd();
            return;
        }

        Initialize();
    }

    // ---------- 표시 ----------
    // 보상 획득 시 onRewardEarned, 광고 닫힘 시 onClosed, 표시 실패 시 onFailed 호출
    // 호출 후 다음 사용을 위해 새 광고를 자동 로드한다.
    public bool ShowAd(Action onRewardEarned, Action onClosed, Action onFailed)
    {
        if (_isShowingAd || _isAnyAdShowing)
        {
            Debug.LogWarning("[RewardedAdService] 다른 보상형 광고가 이미 재생 중입니다.", this);
            onFailed?.Invoke();
            return false;
        }

        if (!IsAdReady)
        {
            Debug.LogWarning("[RewardedAdService] 준비된 광고가 없습니다.", this);
            onFailed?.Invoke();
            LoadAd();
            return false;
        }

        _pendingReward = onRewardEarned;
        _pendingClosed = onClosed;
        _pendingFailed = onFailed;
        _rewardEarnedThisShow = false;
        _isShowingAd = true;
        _isAnyAdShowing = true;

        try
        {
            _rewardedAd.Show(_ =>
            {
                // 보상 지급 조건 충족(광고를 끝까지 시청)
                if (_rewardEarnedThisShow)
                {
                    return;
                }

                _rewardEarnedThisShow = true;
                _pendingReward?.Invoke();
            });
        }
        catch (Exception _exception)
        {
            Debug.LogWarning($"[RewardedAdService] 광고 표시 요청 중 예외가 발생했습니다: {_exception.Message}", this);
            Action _failedCallback = _pendingFailed;
            ReleaseShowingState();
            ClearPending();
            _failedCallback?.Invoke();
            LoadAd();
            return false;
        }

        return true;
    }

    // 표시 1회 동안의 콜백/중복 방어 상태
    private Action _pendingReward;
    private Action _pendingClosed;
    private Action _pendingFailed;
    private bool _rewardEarnedThisShow;

    // ---------- 광고 인스턴스 이벤트 ----------
    private void RegisterAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            _rewardEarnedThisShow = false;
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Action _closedCallback = _pendingClosed;
            ReleaseShowingState();
            ClearPending();
            _closedCallback?.Invoke();
            LoadAd(); // 다음 시청을 위해 미리 로드
        };

        ad.OnAdFullScreenContentFailed += (AdError adError) =>
        {
            Debug.LogWarning($"[RewardedAdService] 광고 표시 실패: {adError}", this);
            Action _failedCallback = _pendingFailed;
            bool _shouldNotifyFailure = !_rewardEarnedThisShow;
            ReleaseShowingState();

            if (_shouldNotifyFailure)
                _failedCallback?.Invoke();

            ClearPending();
            LoadAd();
        };
    }

    private void ReleaseShowingState()
    {
        _isShowingAd = false;
        _isAnyAdShowing = false;
    }

    private void ClearPending()
    {
        _pendingReward = null;
        _pendingClosed = null;
        _pendingFailed = null;
    }

    private void DestroyAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
    }
}
