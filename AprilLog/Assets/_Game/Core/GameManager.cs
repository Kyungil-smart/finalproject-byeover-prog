// 담당자 : 정승우
// 설명   : 앱 전체 관리 -- 상태, 인증, 세이브, 씬 전환

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 앱 수명주기, 인증, 로컬/클라우드 저장, 씬 전환을 담당한다.
/// 실제 Firebase 통신은 FirebaseAuthService, FirestoreService에 위임.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---------- 이벤트 ----------
    public event Action<GameState> OnGameStateChanged;

    // ---------- SerializeField ----------
    [Header("네트워크 서비스")]
    [SerializeField] private FirebaseAuthService _authService;
    [SerializeField] private FirestoreService _firestoreService;
    [SerializeField] private NetworkChecker _networkChecker;

    [Header("설정")]
    [Tooltip("오프라인 모드 허용 여부")]
    [SerializeField] private bool _allowOfflineMode = true;

    // ---------- 상태 ----------
    [Header("디버그")]
    [Tooltip("현재 앱 상태 (읽기 전용)")]
    [SerializeField] private GameState _currentState = GameState.Boot;

    public GameState CurrentState => _currentState;
    public string UserUID => _authService != null ? _authService.UserUID : null;
    public bool IsLoggedIn => _authService != null && _authService.IsLoggedIn;
    public bool IsOfflineMode { get; private set; }

    // 클라우드에서 내려온 유저 데이터
    public UserCloudData CloudData { get; private set; }

    // 로비에서 선택한 챕터
    public int SelectedChapterId { get; set; }

    // ---------- 생명주기 ----------
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 모바일 필수 설정
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = false;

        // 세로 화면 고정
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }

    private void OnEnable()
    {
        // 네트워크 복구 시 밀린 데이터 동기화
        if (_networkChecker != null)
            _networkChecker.OnOnline += HandleOnlineRestored;

        // 인증 이벤트
        if (_authService != null)
            _authService.OnLoginSuccess += HandleLoginSuccess;
    }

    private void OnDisable()
    {
        if (_networkChecker != null)
            _networkChecker.OnOnline -= HandleOnlineRestored;

        if (_authService != null)
            _authService.OnLoginSuccess -= HandleLoginSuccess;
    }

    // 모바일: 백그라운드 전환 시 즉시 세이브
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && _currentState == GameState.InGame)
            SaveLocal();
    }

    private void OnApplicationQuit()
    {
        if (_currentState == GameState.InGame)
            SaveLocal();
    }

    // ---------- 이벤트 핸들러 ----------
    private void HandleLoginSuccess(string uid)
    {
        // Firestore 서비스에 uid 전달
        if (_firestoreService != null)
            _firestoreService.Initialize(uid);
    }

    private void HandleOnlineRestored()
    {
        // 오프라인에서 온라인으로 복구됐을 때
        if (_firestoreService != null && IsLoggedIn)
            StartCoroutine(_firestoreService.SyncLocalToCloud());
    }

    // ---------- 상태 전환 ----------
    public void ChangeState(GameState newState)
    {
        _currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    // ---------- 씬 전환 ----------
    public void LoadLobby()
    {
        ChangeState(GameState.Lobby);
        StartCoroutine(LoadSceneCoroutine("Lobby"));
    }

    public void LoadInGame()
    {
        ChangeState(GameState.InGame);
        StartCoroutine(LoadSceneCoroutine("InGame"));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
    }

    // ---------- Firebase 초기화 ----------
    public IEnumerator InitializeFirebase()
    {
        if (_authService == null)
        {
            Debug.LogWarning("[GameManager] AuthService 연결 안 됨. 오프라인 모드.");
            IsOfflineMode = true;
            yield break;
        }

        yield return StartCoroutine(_authService.InitializeFirebase());

        IsOfflineMode = !_authService.IsFirebaseReady;

        if (IsOfflineMode && !_allowOfflineMode)
        {
            Debug.LogError("[GameManager] Firebase 필수인데 사용 불가");
        }
    }

    // ---------- 로그인 ----------
    public bool HasPreviousSession()
    {
        return _authService != null && _authService.HasPreviousSession();
    }

    public IEnumerator AutoSignIn()
    {
        // 이전 세션이 있으면 InitializeFirebase에서 이미 복원됨
        yield return null;
    }

    public void StartGoogleSignIn()
    {
        if (_authService != null)
            StartCoroutine(_authService.GoogleSignInCoroutine());
    }

    public void StartGuestSignIn()
    {
        if (_authService != null)
            StartCoroutine(_authService.GuestSignInCoroutine());
    }

    public void ShowLoginUI()
    {
        ChangeState(GameState.Login);
    }

    // ---------- 클라우드 데이터 ----------
    public IEnumerator LoadCloudData()
    {
        if (_firestoreService == null)
        {
            LoadLocalProgress();
            yield break;
        }

        // 로드 완료 이벤트 구독
        bool loaded = false;
        _firestoreService.OnDataLoaded += (data) =>
        {
            CloudData = data;
            loaded = true;
        };

        yield return StartCoroutine(_firestoreService.LoadCoroutine());

        // 이벤트가 안 왔으면 대기
        if (!loaded)
            yield return new WaitUntil(() => loaded);
    }

    public void LoadLocalProgress()
    {
        if (_firestoreService != null && _firestoreService.HasLocalBackup())
        {
            // Firestore 로컬 백업에서 로드
            StartCoroutine(_firestoreService.LoadCoroutine());
        }
        else
        {
            CloudData = UserCloudData.CreateDefault();
        }
    }

    public void SyncToCloud(UserCloudData data)
    {
        if (!IsLoggedIn) return;
        if (_firestoreService == null) return;

        CloudData = data;
        StartCoroutine(_firestoreService.SaveCoroutine(data));
    }

    // ---------- 로컬 세이브 (인게임) ----------
    public void SaveLocal()
    {
        Debug.Log("[GameManager] 로컬 세이브 실행");
    }

    public void SaveLocalData(InGameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetInGameSavePath(), json);
    }

    public bool HasLocalSave()
    {
        return File.Exists(GetInGameSavePath());
    }

    public InGameSaveData LoadLocalSaveData()
    {
        string path = GetInGameSavePath();
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<InGameSaveData>(json);
    }

    public void DeleteLocalSave()
    {
        string path = GetInGameSavePath();
        if (File.Exists(path))
            File.Delete(path);
    }

    // ---------- 경로 ----------
    private string GetInGameSavePath()
    {
        return Path.Combine(Application.persistentDataPath, "ingame_save.json");
    }
}