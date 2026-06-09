// 담당자 : 정승우
// 설명   : 앱 전체 관리 -- 상태, 인증, 세이브, 씬 전환

// 2차 수정자 : 조규민
// 수정 내용 : 로그인 이벤트 전달, 게스트 중복 방어, 실제 씬 이름 전환, Portrait 고정, Google 로그인 실패 유형 전달, Editor Google 테스트 계정 입력 전달, Firestore 회원가입 UID 보정, 기존 Editor Email/Password 계정 로그인 요청 중계 추가

using System;
using System.Collections;
using System.IO;
using System.Text;
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
    public event Action OnLoginStarted; // 추가: 조규민 - LoginPresenter와 Bootstrap이 로그인 진행 상태를 받을 수 있게 한다.
    public event Action<string> OnLoginSucceeded; // 추가: 조규민 - 인증 성공 UID를 UI 흐름에 전달한다.
    public event Action<string> OnLoginFailed; // 추가: 조규민 - 인증 실패 메시지를 UI 흐름에 전달한다.
    public event Action<AuthLoginFailureType, string> OnLoginFailedWithType; // 추가: 조규민 - Google 로그인 실패 안내 문구를 구분할 수 있도록 실패 유형을 전달한다.
    public event Action OnRegistrationRequired;
    public event Action<string> OnRegistrationFailed;

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

    private const float REGISTER_TIMEOUT_SECONDS = 25f;

    public GameState CurrentState => _currentState;
    public string UserUID => _authService != null ? _authService.UserUID : null;
    public bool IsLoggedIn => _authService != null && _authService.IsLoggedIn;
    public bool LastSignInWasGoogle => _authService != null && _authService.LastSignInWasGoogle;
    public bool RequiresEditorGoogleEmailPasswordInput => _authService != null && _authService.RequiresEditorGoogleEmailPasswordInput;
    public bool IsOfflineMode { get; private set; }

    // 클라우드에서 내려온 유저 데이터
    public UserCloudData CloudData { get; private set; }

    // 로비에서 선택한 챕터
    public int SelectedChapterId { get; set; }

    // ---------- 생명주기 ----------
    // 추가: 조규민 - Unity Splash 이전에 Portrait를 먼저 고정해 첫 화면 회전 보정이 늦게 적용되는 상황을 줄인다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void ApplyPortraitOrientationBeforeSplash()
    {
        ApplyPortraitOrientation();
    }

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
        Application.targetFrameRate = 60;   // 60fps (vSync는 0이라 이 값이 그대로 적용됨)
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // (구 Input의 multiTouchEnabled는 New Input System 전용 빌드(activeInputHandler=1)에서 throw하므로 제거함)

        // 추가: 조규민 - 씬 진입 후에도 Portrait 고정을 한 번 더 적용한다.
        ApplyPortraitOrientation();
    }

    // 추가: 조규민 - 모바일 세로형 게임 기준으로 Portrait만 허용한다.
    private static void ApplyPortraitOrientation()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void OnEnable()
    {
        // 네트워크 복구 시 밀린 데이터 동기화
        if (_networkChecker != null)
            _networkChecker.OnOnline += HandleOnlineRestored;

        // 인증 이벤트
        if (_authService != null)
        {
            _authService.OnLoginSuccess += HandleLoginSuccess;
            _authService.OnLoginFailedWithType += HandleLoginFailedWithType;
        }
    }

    private void OnDisable()
    {
        if (_networkChecker != null)
            _networkChecker.OnOnline -= HandleOnlineRestored;

        if (_authService != null)
        {
            _authService.OnLoginSuccess -= HandleLoginSuccess;
            _authService.OnLoginFailedWithType -= HandleLoginFailedWithType;
        }
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
        string resolvedUid = ResolveAuthenticatedUid(uid);
        if (string.IsNullOrWhiteSpace(resolvedUid))
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "로그인 UID를 받지 못했습니다. 다시 로그인해 주세요.");
            return;
        }

        // Firestore 서비스에 uid 전달
        if (_firestoreService != null)
            _firestoreService.Initialize(resolvedUid);

        if (_authService != null && _authService.LastSignInWasGoogle)
        {
            StartCoroutine(CompleteGoogleLoginCoroutine(resolvedUid));
            return;
        }

        // 추가: 조규민 - 인증 성공을 Login UI와 Bootstrap 대기 흐름에 알린다.
        OnLoginSucceeded?.Invoke(resolvedUid);
    }

    private IEnumerator CompleteGoogleLoginCoroutine(string uid)
    {
        if (_firestoreService == null)
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "Firestore 서비스가 연결되지 않았습니다.");
            yield break;
        }

        bool profileExists = false;
        yield return StartCoroutine(_firestoreService.CheckUserProfileExistsCoroutine(exists => profileExists = exists));

        if (profileExists)
        {
            OnLoginSucceeded?.Invoke(uid);
            yield break;
        }

        string autoPlayerId = CreateAutoPlayerId();
        bool registered = false;
        yield return StartCoroutine(TryCreateGoogleProfileCoroutine(autoPlayerId, result => registered = result));

        if (registered)
        {
            OnLoginSucceeded?.Invoke(uid);
            yield break;
        }

        ChangeState(GameState.Login);
        OnRegistrationRequired?.Invoke();
        OnRegistrationFailed?.Invoke(string.IsNullOrEmpty(_firestoreService.LastError)
            ? "자동 회원가입에 실패했습니다. 아이디와 비밀번호를 직접 입력해 주세요."
            : "자동 회원가입 실패: " + _firestoreService.LastError);
    }

    private void HandleLoginFailed(string error)
    {
        RaiseLoginFailed(AuthLoginFailureType.General, error);
    }

    private void HandleLoginFailedWithType(AuthLoginFailureType failureType, string error)
    {
        RaiseLoginFailed(failureType, error);
    }

    // 추가: 조규민 - 실패 유형 이벤트와 기존 문자열 이벤트를 함께 발행해 기존 UI 흐름을 유지한다.
    private void RaiseLoginFailed(AuthLoginFailureType failureType, string error)
    {
        // 추가: 조규민 - 로그인 실패 시 앱 상태를 Login으로 유지하고 UI에 오류를 전달한다.
        ChangeState(GameState.Login);
        OnLoginFailedWithType?.Invoke(failureType, error);
        OnLoginFailed?.Invoke(error);
    }

    private string ResolveAuthenticatedUid(string uid)
    {
        if (!string.IsNullOrWhiteSpace(uid))
        {
            return uid.Trim();
        }

        if (_authService == null || string.IsNullOrWhiteSpace(_authService.UserUID))
        {
            return null;
        }

        return _authService.UserUID.Trim();
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
        StartCoroutine(LoadSceneCoroutine("_Lobby")); // 추가: 조규민 - 실제 씬 파일명과 Build Settings 경로에 맞춘다.
    }

    public void LoadInGame()
    {
        ChangeState(GameState.InGame);
        StartCoroutine(LoadSceneCoroutine("_InGame")); // 추가: 조규민 - 실제 씬 파일명과 Build Settings 경로에 맞춘다.
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
        if (_authService == null || !_authService.IsLoggedIn)
        {
            yield break;
        }

        HandleLoginSuccess(_authService.UserUID);
        yield return null;
    }

    public void StartGoogleSignIn()
    {
        StartGoogleSignIn(null, null);
    }

    public void StartGoogleSignIn(string editorEmail, string editorPassword)
    {
        if (_authService == null)
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "인증 서비스가 연결되지 않았습니다.");
            return;
        }

        if (_authService.IsSigningIn)
        {
            return;
        }

        OnLoginStarted?.Invoke(); // 추가: 조규민 - Google 로그인은 추후 대상이지만 진행 상태 이벤트는 동일하게 사용한다.
        StartCoroutine(_authService.GoogleSignInCoroutine(editorEmail, editorPassword));
    }

    // 추가: 조규민 - UI에서 입력한 기존 Editor Email/Password 계정 로그인을 FirebaseAuthService에 위임한다.
    public void StartExistingEditorGoogleAccountSignIn(string editorEmail, string editorPassword)
    {
        if (_authService == null)
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "인증 서비스가 연결되지 않았습니다.");
            return;
        }

        if (_authService.IsSigningIn)
        {
            return;
        }

        OnLoginStarted?.Invoke();
        StartCoroutine(_authService.ExistingEditorGoogleAccountSignInCoroutine(editorEmail, editorPassword));
    }

    public void StartGuestSignIn()
    {
        if (_authService == null)
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "인증 서비스가 연결되지 않았습니다.");
            return;
        }

        if (_authService.IsSigningIn)
        {
            return;
        }

        OnLoginStarted?.Invoke(); // 추가: 조규민 - Login UI가 로딩 상태로 전환할 수 있게 한다.
        StartCoroutine(_authService.GuestSignInCoroutine());
    }

    public void RegisterGoogleUser(string playerId, string password)
    {
        if (_authService == null || !_authService.IsLoggedIn)
        {
            OnRegistrationFailed?.Invoke("Google 인증이 먼저 필요합니다.");
            return;
        }

        if (_firestoreService == null)
        {
            OnRegistrationFailed?.Invoke("Firestore 서비스가 연결되지 않았습니다.");
            return;
        }

        string resolvedUid = ResolveAuthenticatedUid(_authService.UserUID);
        if (string.IsNullOrWhiteSpace(resolvedUid))
        {
            OnRegistrationFailed?.Invoke("회원가입할 로그인 UID가 없습니다. 다시 로그인해 주세요.");
            return;
        }

        _firestoreService.Initialize(resolvedUid);
        StartCoroutine(RegisterGoogleUserCoroutine(playerId, password));
    }

    private IEnumerator RegisterGoogleUserCoroutine(string playerId, string password)
    {
        OnLoginStarted?.Invoke();

        bool succeeded = false;
        bool completed = false;
        var registerCoroutine = StartCoroutine(_firestoreService.CreateGoogleUserProfileCoroutine(
            playerId,
            _authService.UserEmail,
            _authService.UserDisplayName,
            result =>
            {
                succeeded = result;
                completed = true;
            }));

        float elapsedTime = 0f;
        while (!completed && elapsedTime < REGISTER_TIMEOUT_SECONDS)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!completed)
        {
            StopCoroutine(registerCoroutine);
            OnRegistrationFailed?.Invoke("회원가입 요청 시간이 초과되었습니다. Firestore 설정과 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (!succeeded)
        {
            OnRegistrationFailed?.Invoke(string.IsNullOrEmpty(_firestoreService.LastError)
                ? "회원가입에 실패했습니다."
                : _firestoreService.LastError);
            yield break;
        }

        OnLoginSucceeded?.Invoke(_authService.UserUID);
    }

    private IEnumerator TryCreateGoogleProfileCoroutine(string playerId, Action<bool> onCompleted)
    {
        bool succeeded = false;
        bool completed = false;
        var registerCoroutine = StartCoroutine(_firestoreService.CreateAutomaticGoogleUserProfileCoroutine(
            playerId,
            _authService.UserEmail,
            _authService.UserDisplayName,
            result =>
            {
                succeeded = result;
                completed = true;
            }));

        float elapsedTime = 0f;
        while (!completed && elapsedTime < REGISTER_TIMEOUT_SECONDS)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!completed)
        {
            StopCoroutine(registerCoroutine);
            OnRegistrationFailed?.Invoke("회원가입 요청 시간이 초과되었습니다. Firestore 설정과 네트워크 상태를 확인해 주세요.");
            onCompleted?.Invoke(false);
            yield break;
        }

        onCompleted?.Invoke(succeeded);
    }

    private string CreateAutoPlayerId()
    {
        string source = GetAutoPlayerIdSource();
        var builder = new StringBuilder();

        foreach (char character in source)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        string baseId = builder.Length >= 2 ? builder.ToString() : "player";
        string uid = _authService != null ? _authService.UserUID : string.Empty;
        string suffix = string.IsNullOrEmpty(uid) ? "000000" : uid.Substring(Mathf.Max(0, uid.Length - 6)).ToLowerInvariant();
        int maxBaseLength = Mathf.Max(2, 19 - suffix.Length);

        if (baseId.Length > maxBaseLength)
        {
            baseId = baseId.Substring(0, maxBaseLength);
        }

        return baseId + "_" + suffix;
    }

    private string GetAutoPlayerIdSource()
    {
        if (_authService == null)
        {
            return "player";
        }

        if (!string.IsNullOrWhiteSpace(_authService.UserEmail))
        {
            int atIndex = _authService.UserEmail.IndexOf('@');
            return atIndex > 0 ? _authService.UserEmail.Substring(0, atIndex) : _authService.UserEmail;
        }

        if (!string.IsNullOrWhiteSpace(_authService.UserDisplayName))
        {
            return _authService.UserDisplayName;
        }

        return "player";
    }

    public void ShowLoginUI()
    {
        ChangeState(GameState.Login);
    }

    // ---------- 로그아웃 ----------
    public void Logout()
    {
        if (_authService == null)
        {
            Debug.LogWarning("[GameManager] AuthService 없음. 로그아웃 불가.");
            return;
        }

        _authService.SignOut();
        CloudData = null;
        ChangeState(GameState.Login);
        StartCoroutine(LoadSceneCoroutine("_Boot")); // 로그인 화면으로
    }

    // ---------- 계정 탈퇴 ----------
    public void DeleteAccount()
    {
        StartCoroutine(DeleteAccountCoroutine());
    }

    private IEnumerator DeleteAccountCoroutine()
    {
        // 1. Firestore 데이터 삭제
        if (_firestoreService != null && IsLoggedIn)
        {
            bool deleted = false;
            yield return StartCoroutine(_firestoreService.DeleteUserDataCoroutine(result => deleted = result));

            if (!deleted)
                Debug.LogWarning("[GameManager] Firestore 데이터 삭제 실패. 계속 진행합니다.");
        }

        // 2. Firebase Auth 계정 삭제
        if (_authService != null)
        {
            bool deleted = false;
            yield return StartCoroutine(_authService.DeleteAccountCoroutine(result => deleted = result));

            if (!deleted)
            {
                Debug.LogWarning("[GameManager] Firebase 계정 삭제 실패.");
                // 실패해도 로컬은 초기화 후 로그인 화면으로
            }
        }

        // 3. 로컬 데이터 초기화
        DeleteLocalSave();
        PlayerPrefs.DeleteAll();
        CloudData = null;

        ChangeState(GameState.Login);
        StartCoroutine(LoadSceneCoroutine("_Boot"));
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
        Action<UserCloudData> onDataLoaded = null;
        onDataLoaded = (data) =>
        {
            CloudData = data;
            loaded = true;
            _firestoreService.OnDataLoaded -= onDataLoaded;
        };
        _firestoreService.OnDataLoaded += onDataLoaded;

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

    public void SaveLocalData(Legacy_InGameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetInGameSavePath(), json);
    }

    public bool HasLocalSave()
    {
        return File.Exists(GetInGameSavePath());
    }

    public Legacy_InGameSaveData LoadLocalSaveData()
    {
        string path = GetInGameSavePath();
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<Legacy_InGameSaveData>(json);
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
