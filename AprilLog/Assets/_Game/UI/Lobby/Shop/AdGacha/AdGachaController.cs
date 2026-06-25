using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 작성자 : 홍정옥
// 설명   : 광고 보상 가챠 컨트롤러
// - 단일 버튼이 상태에 따라 역할 전환: (시청가능)광고 보기 → (시청후)뽑기
// - 뽑기는 ShopGachaPresenter.FreeDrawSingle 로 1회 무료 뽑기(천장/누적 제외)
// - 뽑기 완료 후 24시간 쿨타임 시작, 남은 시간을 카운트다운으로 표시
// - 쿨타임은 절대 시각(UTC)으로 저장하므로 게임을 꺼도 실제 시간이 흐름
// - 상태는 uid 별 로컬 JSON(persistentDataPath/ad_gacha_{uid}.json)에 저장
public class AdGachaController : MonoBehaviour
{
    // 저장 데이터(절대 UTC 시각 기반)
    [Serializable]
    private class AdGachaSave
    {
        public string nextAvailableAtUtc = ""; // 이 시각 이후에 다시 시청 가능 빈 값이면 즉시 가능
        public bool hasPendingFreeDraw; // 광고는 봤지만 아직 안 뽑은 상태(앱 재시작해도 유지)
        public bool firstRewardGranted; // 첫 실행 무료 지급 완료 여부(1회성, 영속)
    }

    private enum Phase { Available, PendingFreeDraw, Cooldown }

    [Header("시스템 참조")]
    [Tooltip("보상형 광고 서비스")]
    [SerializeField] private RewardedAdService _adService;
    [Tooltip("뽑기 실행을 위임할 상점 가챠 프리젠터")]
    [SerializeField] private ShopGachaPresenter _gachaPresenter;
    [Tooltip("네트워크 상태 오프라인일 때 시청 막음")]
    [SerializeField] private NetworkChecker _networkChecker;

    [Header("뽑기 설정")]
    [Tooltip("광고 무료 뽑기에 사용할 가챠 박스 ID")]
    [SerializeField] private int _adGachaId = 3;
    [Tooltip("뽑기 후 다음 시청까지의 쿨타임")]
    [SerializeField] private float _cooldownHours = 24f;

    [Header("UI - 버튼 (시청/뽑기 겸용 단일 버튼)")]
    [Tooltip("광고 시청 + 뽑기 겸용 버튼. 상태에 따라 역할/문구가 바뀐다. OnClick 은 코드에서 자동 연결.")]
    [SerializeField] private Button _actionButton;
    [Tooltip("버튼 안의 문구 텍스트(선택). 상태에 따라 자동으로 시청/뽑기 문구로 바뀐다.")]
    [SerializeField] private TMP_Text _actionLabel;
    [Tooltip("시청 가능 상태의 버튼 문구")]
    [SerializeField] private string _watchLabel = "광고 보고 무료 뽑기";
    [Tooltip("광고 시청 후 뽑기 가능 상태의 버튼 문구")]
    [SerializeField] private string _drawLabel = "뽑기";

    [Header("UI - 표시")]
    [Tooltip("메인 표시 텍스트(Text_Button_Time). 상태에 따라 남은시간/안내문구를 모두 여기 한곳에 표시한다.")]
    [SerializeField] private TMP_Text _countdownText;
    [Tooltip("(선택/레거시) 별도 상태 텍스트. 비워둬도 됨 — 안내는 메인 텍스트에 통합 표시된다.")]
    [SerializeField] private TMP_Text _statusText;

    [Header("UI - 표시 문구")]
    [Tooltip("쿨타임일 때 남은시간 앞에 붙는 문구")]
    [SerializeField] private string _cooldownPrefix = "보상까지 남은시간 : ";
    [Tooltip("광고 시청 가능 상태일 때 표시 문구")]
    [SerializeField] private string _availableMessage = "광고 보고 무료 뽑기!";
    [Tooltip("광고 시청 후 뽑기 가능 상태일 때 표시 문구")]
    [SerializeField] private string _pendingMessage = "지금 뽑기 가능!";
    [Tooltip("오프라인일 때 표시 문구")]
    [SerializeField] private string _offlineMessage = "오프라인 상태입니다.";
    [Tooltip("광고 로딩 중 표시 오브젝트")]
    [SerializeField] private GameObject _loadingIndicator;
    [Tooltip("광고 로드 실패 표시 오브젝트")]
    [SerializeField] private GameObject _loadFailedIndicator;

    private AdGachaSave _save = new AdGachaSave();
    private string _loadedUid;
    private bool _isShowingAd;
    private bool _isDrawing;
    private float _uiTickTimer;

    private const float UiTickInterval = 1f; // 카운트다운 갱신 주기(초)

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if (_adService == null)
            _adService = FindFirstObjectByType<RewardedAdService>(FindObjectsInactive.Include);
        if (_gachaPresenter == null)
            _gachaPresenter = FindFirstObjectByType<ShopGachaPresenter>(FindObjectsInactive.Include);
        // NetworkChecker 는 보통 Boot 씬의 GameManager(DontDestroyOnLoad)에 붙어 로비까지 유지되므로
        // 인스펙터에서 직접 못 꽂는 경우가 많다. 비어 있으면 런타임에 자동 탐색한다.
        if (_networkChecker == null)
            _networkChecker = FindFirstObjectByType<NetworkChecker>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (_actionButton != null) _actionButton.onClick.AddListener(OnClickAction);

        if (_adService != null)
        {
            _adService.OnAdLoaded += HandleAdLoaded;
            _adService.OnAdLoadFailed += HandleAdLoadFailed;
        }

        Load();
        GrantFirstRunRewardIfNeeded();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (_actionButton != null) _actionButton.onClick.RemoveListener(OnClickAction);

        if (_adService != null)
        {
            _adService.OnAdLoaded -= HandleAdLoaded;
            _adService.OnAdLoadFailed -= HandleAdLoadFailed;
        }
    }

    private void Update()
    {
        // 쿨타임 중에만 1초 간격으로 카운트다운 갱신(매 프레임 갱신 방지).
        if (GetPhase() != Phase.Cooldown) return;

        _uiTickTimer += Time.unscaledDeltaTime;
        if (_uiTickTimer < UiTickInterval) return;

        _uiTickTimer = 0f;
        RefreshUI();
    }

    // 백그라운드 복귀 시 실제 경과 시간을 반영해 상태를 재평가한다.
    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused)
        {
            Load();      // uid 가 늦게 확정되는 경우 대비해 다시 로드
            RefreshUI();
        }
    }

    // ---------- 버튼 핸들러 ----------
    // 단일 겸용 버튼의 OnClick. 현재 상태에 따라 시청 또는 뽑기로 분기한다.
    public void OnClickAction()
    {
        switch (GetPhase())
        {
            case Phase.Available: OnClickWatchAd(); break;
            case Phase.PendingFreeDraw: OnClickDraw(); break;
            // Cooldown: 버튼이 비활성이지만, 혹시 호출돼도 무시.
        }
    }

    public void OnClickWatchAd()
    {
        if (_isShowingAd) return;
        if (GetPhase() != Phase.Available) return;

        if (_networkChecker != null && !_networkChecker.IsOnline)
        {
            SetStatus("오프라인 상태입니다. 네트워크 연결 후 시청해 주세요.");
            return;
        }

        if (_adService == null)
        {
            Debug.LogWarning("[AdGachaController] RewardedAdService 가 연결되지 않았습니다.", this);
            return;
        }

        if (!_adService.IsAdReady)
        {
            SetStatus("광고를 불러오는 중입니다. 잠시 후 다시 시도해 주세요.");
            _adService.LoadAd();
            return;
        }

        _isShowingAd = true;
        _adService.ShowAd(
            onRewardEarned: HandleRewardEarned,
            onClosed: () => { _isShowingAd = false; RefreshUI(); },
            onFailed: () => { _isShowingAd = false; SetStatus("광고 재생에 실패했습니다."); RefreshUI(); });
    }

    public void OnClickDraw()
    {
        if (_isDrawing) return;
        if (GetPhase() != Phase.PendingFreeDraw) return;

        if (_gachaPresenter == null)
        {
            Debug.LogWarning("[AdGachaController] ShopGachaPresenter 가 연결되지 않았습니다.", this);
            return;
        }

        _isDrawing = true;
        bool drawn = _gachaPresenter.FreeDrawSingle(_adGachaId);
        _isDrawing = false;

        if (!drawn)
        {
            // 추첨 실패(데이터 문제 등) — 권한 유지, 다시 시도 가능하게 둔다.
            SetStatus("뽑기에 실패했습니다. 다시 시도해 주세요.");
            RefreshUI();
            return;
        }

        // 뽑기 성공 → 권한 소모 + 24시간 쿨타임 시작
        _save.hasPendingFreeDraw = false;
        _save.nextAvailableAtUtc = DateTime.UtcNow.AddHours(_cooldownHours).ToString("o");
        Save();
        RefreshUI();
    }

    // 첫 실행(uid별 최초 1회)에는 광고 없이 무료 뽑기를 즉시 지급한다.
    // firstRewardGranted 영속 플래그로 평생 1회만 동작한다.
    private void GrantFirstRunRewardIfNeeded()
    {
        if (_save.firstRewardGranted) return;

        _save.firstRewardGranted = true;

        // 쿨타임 중이거나 이미 지급(미사용) 상태가 아니면 즉시 뽑기 가능 상태로 만든다.
        if (!_save.hasPendingFreeDraw && !IsCooldownActive())
            _save.hasPendingFreeDraw = true;

        Save();
    }

    // ---------- 광고 이벤트 ----------
    private void HandleRewardEarned()
    {
        // 광고를 끝까지 시청 → 무료뽑기 권한 지급(쿨타임 종료 + 미지급 상태일 때만).
        if (GetPhase() != Phase.Available) return;

        _save.hasPendingFreeDraw = true;
        Save();
        // UI 갱신은 onClosed 콜백에서 _isShowingAd 해제 후 수행된다.
    }

    private void HandleAdLoaded() => RefreshUI();
    private void HandleAdLoadFailed() => RefreshUI();

    // ---------- 상태 판정 ----------
    private Phase GetPhase()
    {
        if (_save.hasPendingFreeDraw) return Phase.PendingFreeDraw;
        return IsCooldownActive() ? Phase.Cooldown : Phase.Available;
    }

    private bool IsCooldownActive()
    {
        return RemainingCooldown() > TimeSpan.Zero;
    }

    private TimeSpan RemainingCooldown()
    {
        if (string.IsNullOrEmpty(_save.nextAvailableAtUtc))
            return TimeSpan.Zero;

        if (!DateTime.TryParse(_save.nextAvailableAtUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime next))
            return TimeSpan.Zero;

        TimeSpan remain = next.ToUniversalTime() - DateTime.UtcNow;
        return remain > TimeSpan.Zero ? remain : TimeSpan.Zero;
    }

    // ---------- UI ----------
    private void RefreshUI()
    {
        Phase phase = GetPhase();

        bool adReady = _adService != null && _adService.IsAdReady;
        bool online = _networkChecker == null || _networkChecker.IsOnline;

        // 단일 겸용 버튼 활성/비활성 + 문구
        bool canWatch = phase == Phase.Available && adReady && online && !_isShowingAd;
        bool canDraw = phase == Phase.PendingFreeDraw && !_isDrawing;
        if (_actionButton != null)
            _actionButton.interactable = canWatch || canDraw;
        if (_actionLabel != null)
            _actionLabel.text = phase == Phase.PendingFreeDraw ? _drawLabel : _watchLabel;

        // 로딩/실패 인디케이터
        bool loading = phase == Phase.Available && _adService != null && !adReady;
        if (_loadingIndicator != null) _loadingIndicator.SetActive(loading && online);
        if (_loadFailedIndicator != null) _loadFailedIndicator.SetActive(loading && !online);

        // 메인 텍스트 한곳에 상태별 표시(남은시간/안내문구 통합). 항상 표시 — 빈칸 안 생김.
        SetMainText(BuildMainText(phase, online));
    }

    // 현재 상태에 맞는 메인 표시 문구를 만든다.
    private string BuildMainText(Phase phase, bool online)
    {
        switch (phase)
        {
            case Phase.Cooldown:
                TimeSpan t = RemainingCooldown();
                return _cooldownPrefix + string.Format("{0:00} : {1:00} : {2:00}",
                    (int)t.TotalHours, t.Minutes, t.Seconds);
            case Phase.PendingFreeDraw:
                return _pendingMessage;
            default: // Available
                return online ? _availableMessage : _offlineMessage;
        }
    }

    // 메인 텍스트(Text_Button_Time) 갱신. 레거시 _statusText 가 있으면 함께 표시.
    private void SetMainText(string msg)
    {
        if (_countdownText != null) _countdownText.text = msg;
        if (_statusText != null) _statusText.text = msg;
    }

    // 일시적 안내(로딩/실패 등). 메인 텍스트에 잠깐 표시되며 다음 RefreshUI 때 갱신된다.
    private void SetStatus(string msg)
    {
        SetMainText(msg);
    }

    // ---------- 저장/로드 ----------
    private string CurrentUid =>
        GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.UserUID)
            ? GameManager.Instance.UserUID
            : "guest";

    private string SavePath => Path.Combine(Application.persistentDataPath, $"ad_gacha_{CurrentUid}.json");

    private void Load()
    {
        string uid = CurrentUid;
        if (_loadedUid == uid && _save != null) return; // 같은 uid 면 재로드 불필요

        _loadedUid = uid;
        try
        {
            string path = SavePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _save = JsonUtility.FromJson<AdGachaSave>(json) ?? new AdGachaSave();
            }
            else
            {
                _save = new AdGachaSave();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdGachaController] 저장 데이터 로드 실패: {e.Message}", this);
            _save = new AdGachaSave();
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_save));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdGachaController] 저장 실패: {e.Message}", this);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용 : 쿨타임 즉시 해제
    [ContextMenu("DEBUG/쿨타임 초기화")]
    private void DebugResetCooldown()
    {
        _save.nextAvailableAtUtc = "";
        _save.hasPendingFreeDraw = false;
        Save();
        RefreshUI();
    }

    // 테스트용 : 광고 없이 뽑기 권한 부여
    [ContextMenu("DEBUG/무료뽑기 권한 지급")]
    private void DebugGrantFreeDraw()
    {
        _save.hasPendingFreeDraw = true;
        Save();
        RefreshUI();
    }

    // 테스트용 : 상태(쿨타임/지급) 무시하고 광고를 즉시 표시한다(에디터 placeholder 확인용).
    [ContextMenu("DEBUG/광고 즉시 시청")]
    private void DebugShowAdNow()
    {
        if (_adService == null)
        {
            Debug.LogWarning("[AdGachaController] RewardedAdService 미연결.", this);
            return;
        }

        if (!_adService.IsAdReady)
        {
            Debug.Log("[AdGachaController] 광고가 아직 준비 안 됨 → 로드 요청. 잠시 후 다시 시도하세요.", this);
            _adService.LoadAd();
            return;
        }

        _adService.ShowAd(
            onRewardEarned: () => Debug.Log("[AdGachaController] (DEBUG) 보상 획득 콜백"),
            onClosed: () => Debug.Log("[AdGachaController] (DEBUG) 광고 닫힘"),
            onFailed: () => Debug.Log("[AdGachaController] (DEBUG) 광고 실패"));
    }

    // 테스트용 : 첫 실행 상태로 완전 초기화(첫 무료 지급 다시 받기)
    [ContextMenu("DEBUG/첫 실행 상태로 초기화")]
    private void DebugResetToFirstRun()
    {
        _save.nextAvailableAtUtc = "";
        _save.hasPendingFreeDraw = false;
        _save.firstRewardGranted = false;
        GrantFirstRunRewardIfNeeded();
        RefreshUI();
    }
#endif
}
