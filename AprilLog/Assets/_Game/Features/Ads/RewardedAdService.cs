using System;
using UnityEngine;
using GoogleMobileAds.Api;

// 작성자 : 홍정옥
// 설명   : Google Mobile Ads 보상형 광고(RewardedAd) 래퍼
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

    // ---------- 이벤트 ----------
    public event Action OnAdLoaded;        // 광고 로드 완료(표시 가능)
    public event Action OnAdLoadFailed;    // 광고 로드 실패

    // ---------- 상태 ----------
    public bool IsInitialized { get; private set; }
    public bool IsAdReady => _rewardedAd != null && _rewardedAd.CanShowAd();

    private RewardedAd _rewardedAd;
    private bool _isLoading;

    private string AdUnitId => _useTestIds ? TestAdUnitAndroid : _androidAdUnitId;

    // ---------- 생명주기 ----------
    private void Start()
    {
        if (_autoInitialize)
            Initialize();
    }

    private void OnDestroy()
    {
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

        MobileAds.Initialize(_ =>
        {
            // 콜백은 메인스레드에서 호출됨(SDK가 보장).
            IsInitialized = true;
            Debug.Log("[RewardedAdService] Mobile Ads SDK 초기화 완료");
            LoadAd();
        });
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
                return;
            }

            Debug.Log("[RewardedAdService] 광고 로드 완료");
            _rewardedAd = ad;
            RegisterAdEvents(ad);
            OnAdLoaded?.Invoke();
        });
    }

    // ---------- 표시 ----------
    // 보상 획득 시 onRewardEarned, 광고 닫힘 시 onClosed, 표시 실패 시 onFailed 호출
    // 호출 후 다음 사용을 위해 새 광고를 자동 로드한다.
    public void ShowAd(Action onRewardEarned, Action onClosed, Action onFailed)
    {
        if (!IsAdReady)
        {
            Debug.LogWarning("[RewardedAdService] 준비된 광고가 없습니다.", this);
            onFailed?.Invoke();
            LoadAd();
            return;
        }

        _pendingReward = onRewardEarned;
        _pendingClosed = onClosed;
        _pendingFailed = onFailed;

        _rewardedAd.Show(_ =>
        {
            // 보상 지급 조건 충족(광고를 끝까지 시청)
            _rewardEarnedThisShow = true;
            _pendingReward?.Invoke();
        });
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
            _pendingClosed?.Invoke();
            ClearPending();
            LoadAd(); // 다음 시청을 위해 미리 로드
        };

        ad.OnAdFullScreenContentFailed += (AdError adError) =>
        {
            Debug.LogWarning($"[RewardedAdService] 광고 표시 실패: {adError}", this);
            if (!_rewardEarnedThisShow)
                _pendingFailed?.Invoke();
            ClearPending();
            LoadAd();
        };
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
