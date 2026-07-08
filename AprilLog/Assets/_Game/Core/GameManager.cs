// 담당자 : 정승우
// 설명   : 앱 전체 관리 -- 상태, 인증, 세이브, 씬 전환

// 2차 수정자 : 조규민
// 수정 내용 : 로그인 이벤트 전달, 게스트 중복 방어, 실제 씬 이름 전환, Portrait 고정, Google 로그인 실패 유형 전달, Editor Google 테스트 계정 입력 전달, Firestore 회원가입 UID 보정, 기존 Editor Email/Password 계정 로그인 요청 중계 추가

// 추가: 조규민 - 로그인 계정의 아웃게임 모델과 정산 결과를 UserCloudData로 저장하는 공용 진입점 추가

// 3차 수정자 : 김영찬
// 수정 내용 : 세이브 개선
//
// 4차 수정자 : 조규민
// 수정 내용 : 계정별 최초 진입 상태 마이그레이션과 최초 스토리 시작·튜토리얼 완료 저장 API 추가

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 앱 수명주기, 인증, 로컬/클라우드 저장, 씬 전환을 담당한다.
/// 실제 Firebase 통신은 FirebaseAuthService, FirestoreService에 위임.
/// </summary>
// 2차 수정자 : 조규민
// 수정 내용 : 하우징 자동재화 수령 시간 저장과 골드/양피지 지급을 한 번에 처리하는 계정 저장 API 추가
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
    public event Action OnCloudDataReady; // 추가 - 김영찬 : 클라우드 데이터가 스크립트 내부 로직보다 늦게 로딩 되는 경우 대비하여 이벤트 발송

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
    
    // 재생 해야 될 시나리오 그룹
    public int SelectedScenarioGroupId { get; set; }

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
        Application.targetFrameRate = 60;   // 60fps (vSync=0이라 그대로 적용됨)
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // (구 Input의 multiTouchEnabled는 New Input System 전용 빌드(activeInputHandler=1)에서 throw하므로 제거)

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

    // 모바일: 전투 중 백그라운드 전환/종료 = '포기'(인게임 세이브 삭제) → 재진입 시 새 판.
    // (기획 #300: 종료로 죽음/손해를 회피하는 세이브스컴 차단. 의도적 이어하기는 '로비로' 버튼으로만.)
    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && _currentState == GameState.InGame)
            DeleteLocalSave();
    }

    private void OnApplicationQuit()
    {
        if (_currentState == GameState.InGame)
            DeleteLocalSave();
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
        AudioManager.Bgm(SfxId.LobbyBgm);   // SFX 가이드 1.0: 로그인/로딩/로비 공용 BGM
        StartCoroutine(LoadSceneCoroutine("_Lobby")); // 추가: 조규민 - 실제 씬 파일명과 Build Settings 경로에 맞춘다.
    }

    public void LoadInGame()
    {
        ChangeState(GameState.InGame);
        StartCoroutine(LoadSceneCoroutine("_InGame")); // 추가: 조규민 - 실제 씬 파일명과 Build Settings 경로에 맞춘다.
    }

    public void LoadInGame(int chapterId)
    {
        SelectedChapterId = chapterId;
        ChangeState(GameState.InGame);
        StartCoroutine(LoadSceneCoroutine("_InGame"));
    }

    // 추가: 정승우 - 최초 실행 튜토리얼 인트로 시나리오 씬으로 진입.
    // 시나리오 종료 시 그 씬의 흐름(TempStoryToGameFlow 등)이 인게임으로 넘긴다.
    public void LoadScenarioIntro()
    {
        SelectedScenarioGroupId = 3001; // 인트로 그룹 ID
        StartCoroutine(LoadSceneCoroutine("_Story")); // 시나리오 재생 씬(_Boot/_Lobby/_InGame/_Story 규칙)
    }

    public void LoadScenarioByGroupId(int groupId)
    {
        SelectedScenarioGroupId = groupId;
        StartCoroutine(LoadSceneCoroutine("_Story"));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        SceneTransition.Load(sceneName); // 전환 오버레이가 내부에서 비동기 로드까지 처리
        yield break;
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
                // 익명 세션은 recent-login 만료로 삭제가 거부될 수 있다.
                // 이때 세션을 끊지 않으면 _Boot 재진입 시 같은 계정으로 자동 재로그인되어
                // 무한 로딩에 빠지므로, 삭제 실패 시 로컬 세션을 강제로 정리한다.
                _authService.SignOut();
            }
        }

        // 3. 로컬 데이터 초기화
        DeleteLocalSave();
        _firestoreService?.DeleteLocalBackup();   // 게스트 로컬 백업(cloud_backup*.json)까지 지워야 재로그인 시 옛 데이터가 안 돌아온다.
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

        OnCloudDataReady?.Invoke();
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

    // 추가: 조규민 - 상태 필드가 없던 기존 계정은 최초 콘텐츠를 이미 경험한 계정으로 마이그레이션한다.
    public bool ShouldStartInitialStory()
    {
        if (CloudData == null)
        {
            Debug.LogWarning("[GameManager] 최초 진입 상태를 확인할 계정 데이터가 없습니다.");
            return false;
        }

        MigrateInitialFlowStateIfNeeded();
        return !CloudData._initialStoryStarted;
    }

    public bool IsTutorialCompleted()
    {
        if (CloudData == null)
        {
            return PlayerPrefs.GetInt("Tutorial_Completed", 0) == 1;
        }

        MigrateInitialFlowStateIfNeeded();
        return CloudData._tutorialCompleted;
    }

    public void MarkInitialStoryStarted()
    {
        if (CloudData == null)
        {
            Debug.LogWarning("[GameManager] 최초 스토리 시작 상태를 저장할 계정 데이터가 없습니다.");
            return;
        }

        MigrateInitialFlowStateIfNeeded();
        if (CloudData._initialStoryStarted)
        {
            return;
        }

        CloudData._initialStoryStarted = true;
        SyncToCloud(CloudData);
    }

    public void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt("Tutorial_Completed", 1);
        PlayerPrefs.Save();

        if (CloudData == null)
        {
            return;
        }

        MigrateInitialFlowStateIfNeeded();
        if (CloudData._tutorialCompleted)
        {
            return;
        }

        CloudData._tutorialCompleted = true;
        SyncToCloud(CloudData);
    }

    private void MigrateInitialFlowStateIfNeeded()
    {
        if (CloudData == null || CloudData._hasInitialFlowState)
        {
            return;
        }

        // 새 스키마 필드가 없는 문서는 업데이트 이전부터 존재한 계정이므로 최초 콘텐츠를 재노출하지 않는다.
        CloudData._hasInitialFlowState = true;
        CloudData._initialStoryStarted = true;
        CloudData._tutorialCompleted = true;
        SyncToCloud(CloudData);
        Debug.Log("[GameManager] 기존 계정의 최초 진입 상태 마이그레이션을 완료했습니다.");
    }

    public void SyncToCloud(UserCloudData data)
    {
        if (_firestoreService == null) return;

        EnsureCloudIdentity(data);
        CloudData = data;

        // 튜토리얼 진행 중에는 실제 저장을 보류
        // 미러는 계속 갱신하되 디스크/클라우드 쓰기는 완료 시점에 한 번에 확정
        // 완료 시 IsRunning=false가 되어 이 지점을 통과한다.
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsRunning)
            return;

        if (IsLoggedIn)
            StartCoroutine(_firestoreService.SaveCoroutine(data));   // 클라우드(+내부에서 로컬백업도)
        else
            _firestoreService.SaveLocalBackup(data);                 // 단계④: 오프라인/비로그인도 로컬 즉시저장(재화 유실 방지)
    }

    public void ApplyCloudDataToOutGameModels(PlayerProgressModel progressModel, CurrencyModel currencyModel)
    {
        CloudData ??= UserCloudData.CreateDefault();

        EnsureCloudIdentity(CloudData);

        if (progressModel != null)
        {
            progressModel.Initialize(
                CloudData.characterLevel,
                CloudData.currentChapter,
                CloudData.currentStage,
                CloneUnlockedStages(CloudData.unlockedStages));
        }

        if (currencyModel != null)
        {
            currencyModel.Initialize(CloudData.gold, CloudData.parchment, CloudData.diamond);
        }
        
        if (DataManager.Instance != null && DataManager.Instance.ResourceRepo != null)
        {
            DataManager.Instance.ResourceRepo.LoadResourceData();
        }
    }

    public void ApplyCloudDataToArtifactManager(ArtifactManager manager)
    {
        if (manager == null)
        {
            return;
        }

        CloudData ??= UserCloudData.CreateDefault();

        EnsureCloudIdentity(CloudData);

        // CloudData에 저장된 아티팩트를 매니저로 복원한다.
        // (manager.LoadData()를 다시 부르면 LoadData ↔ Apply 무한 재귀로 StackOverflow가 난다.)
        manager.MyArtifacts = CloneArtifactList(CloudData.myArtifacts);
    }

    // 튜토리얼 시작 시 아웃게임 상태(아티팩트/캐릭터 레벨)를 기본값으로 되돌린다.
    // 이전에 저장된 진행이 남아 있어도 튜토리얼은 항상 같은 시작 상태에서 진행되어야 한다.
    // (예: 이미 max로 저장된 아티팩트가 남으면 강화 단계가 진행되지 않는다.)
    // SyncToCloud 게이트로 실제 저장은 보류되고, 튜토리얼 완료 시점에 최종 상태가 한 번에 확정 저장된다.
    public void ResetOutGameStateForTutorial(PlayerProgressModel progressModel, ArtifactManager artifactManager)
    {
        if (CloudData != null)
        {
            CloudData.myArtifacts = new List<ArtifactInstance>();
            CloudData.characterLevel = PlayerProgressModel.StartLevel;
        }

        if (artifactManager != null)
            artifactManager.MyArtifacts = new List<ArtifactInstance>();

        if (progressModel != null)
            progressModel.SetCharacterLevel(PlayerProgressModel.StartLevel);
    }

    public void SaveOutGameProgress(PlayerProgressModel progressModel)
    {
        if (!IsLoggedIn)
        {
            return;
        }

        var data = CloudData ?? UserCloudData.CreateDefault();
        EnsureCloudIdentity(data);

        if (progressModel != null)
        {
            data.characterLevel = progressModel.CharacterLevel;
            data.currentChapter = progressModel.CurrentChapter;
            data.currentStage = progressModel.CurrentStage;
            data.unlockedStages = CloneUnlockedStages(progressModel.UnlockedStages);
        }

        // 단계③: 재화는 GameManager 단일 API(AddCurrency/TrySpendCurrency/SetCurrency)가 CloudData에 직접 반영·저장한다.
        // 여기서 currencyModel(사본) 값으로 덮어쓰지 않는다 — stale 사본 덮어쓰기로 인한 재화 유실 버그 제거.

        SyncToCloud(data);
    }

    public void SaveChapterResult(bool isVictory, int chapterId, int completedStageCount, int rewardGold, int rewardParchment, int rewardDiamond)
    {
        var data = CloudData ?? UserCloudData.CreateDefault();
        EnsureCloudIdentity(data);

        // 판이 정산으로 끝났으므로 인챈트 리세마라 방지 스냅샷을 비운다(다음 판은 새 뽑기).
        // 포기/강제종료 경로는 정산을 안 타므로 스냅샷이 남아 재진입 시 같은 카드가 복원된다 - 그게 방지 목적.
        ClearEnchantDrawSnapshots(data);

        AddCurrency(Mathf.Max(0, rewardGold), Mathf.Max(0, rewardParchment));
        AddDiamond(Mathf.Max(0, rewardDiamond));

        if (isVictory)
        {
            int safeChapterId = Mathf.Max(1, chapterId);
            int safeCompletedStageCount = Mathf.Max(1, completedStageCount);
            // 튜토리얼/0챕터(98xx/99xx)는 본편 진행도가 아니므로 currentChapter를 오염시키지 않는다.
            // currentChapter는 Max 갱신이라 9801이 한 번 박히면 본편(101~)을 아무리 깨도 안 내려가고,
            // 이 값을 읽는 하우징 방치보상 티어(GetRewardAtOrBelow)가 최고 단계로 고착된다.
            if (safeChapterId < 9000)
            {
                data.currentChapter = Mathf.Max(data.currentChapter, safeChapterId);
                data.currentStage = Mathf.Max(data.currentStage, safeCompletedStageCount);
                AddUnlockedStage(data, BuildStageId(safeChapterId, safeCompletedStageCount));
                AddNextStageIfExists(data, safeChapterId, safeCompletedStageCount + 1);
            }
        }

        SyncToCloud(data);
        RaiseCurrencyChanged();   // 단계②: 전투 보상이 View(로비 등)에 전파되도록 단일 이벤트 발행
    }

    // ---------- 인챈트 리세마라 방지 스냅샷 ----------
    // 인챈트 팝업의 카드 구성을 '뜬 순간'과 '리롤한 순간'에 저장해, 강제종료 후 재진입해도
    // 같은 뽑기 순번에서 같은 카드가 복원되게 한다. 소거는 정산(SaveChapterResult)에서만.

    /// <summary>해당 뽑기 순번의 저장된 스냅샷. 없으면 null.</summary>
    public EnchantDrawSnapshot GetEnchantDrawSnapshot(int drawIndex)
    {
        var draws = CloudData?.pendingEnchantDraws;
        if (draws == null) return null;

        for (int i = 0; i < draws.Count; i++)
            if (draws[i] != null && draws[i].drawIndex == drawIndex) return draws[i];
        return null;
    }

    /// <summary>스냅샷 저장(같은 drawIndex는 교체) + 즉시 영속. 카드가 화면에 보이기 전에 호출해야
    /// '표시 후 저장 전 강제종료' 틈으로 리롤이 성립하지 않는다.</summary>
    public void SaveEnchantDrawSnapshot(EnchantDrawSnapshot snapshot)
    {
        if (snapshot == null) return;
        var data = CloudData ?? UserCloudData.CreateDefault();
        EnsureCloudIdentity(data);

        if (data.pendingEnchantDraws == null) data.pendingEnchantDraws = new List<EnchantDrawSnapshot>();
        for (int i = data.pendingEnchantDraws.Count - 1; i >= 0; i--)
            if (data.pendingEnchantDraws[i] == null || data.pendingEnchantDraws[i].drawIndex == snapshot.drawIndex)
                data.pendingEnchantDraws.RemoveAt(i);

        data.pendingEnchantDraws.Add(snapshot);
        SyncToCloud(data);
    }

    private static void ClearEnchantDrawSnapshots(UserCloudData data)
    {
        if (data?.pendingEnchantDraws == null || data.pendingEnchantDraws.Count == 0) return;
        data.pendingEnchantDraws.Clear();
    }

    // ---------- 시나리오 저장/조회 ----------
    public void SaveFirstReadScenario(int groupId)
    {
        var data = CloudData ?? UserCloudData.CreateDefault();
        EnsureCloudIdentity(data);

        if (!IsFirstReadScenario(groupId))
        {
            CloudData.firstReadScenarios.Add(groupId);
        }
        
        SyncToCloud(data);
    }

    public bool IsFirstReadScenario(int groupId)
    {
        if (CloudData != null) return CloudData.firstReadScenarios.Contains(groupId);
        
        Debug.LogWarning("[GameManager] 클라우드 데이터를 찾을 수 없음");
        return false;
    }

    // ---------- 최초 클리어 보상 (1회성, 영속) ----------
    // 팀 공용 canonical API. 중복방지 + 지급 + 영속만 책임진다. 보상 수치는 호출부(기획 데이터)가 정한다.
    // 반복 클리어로 매번 주는 '변동 보상'은 이 API가 아니라 AddCurrency로 처리할 것(그건 1회성 아님).

    /// <summary>해당 스테이지의 최초 클리어 보상을 이미 지급했는지. 키는 데이터의 실제 Stage_ID(1000~).</summary>
    private bool IsStageFirstClearRewarded(int stageId)
    {
        return CloudData != null
            && CloudData.firstClearRewardedStages != null
            && CloudData.firstClearRewardedStages.Contains(stageId);
    }
    
    private bool IsChapterFirstClearRewarded(int chapterId)
    {
        return CloudData != null
               && CloudData.firstClearRewardedChapters != null
               && CloudData.firstClearRewardedChapters.Contains(chapterId);
    }

    /// <summary>스테이지 최초 클리어 보상을 1회만 지급한다. 이미 지급됐거나 CloudData 없으면 아무것도 안 하고 false.
    /// stageId는 데이터의 실제 Stage_ID(StageData.Stage_ID / StageRepo.GetStageId)를 넘길 것(BuildStageId 금지).</summary>
    public bool TryGrantFirstClearStageReward(int stageId, List<ItemSaveEntry> rewardList)
    {
        if (stageId <= 0 || CloudData == null) return false;
        if (IsStageFirstClearRewarded(stageId)) return false;   // 이미 최초보상 지급됨 → 중복 차단
        if (rewardList == null || rewardList.Count == 0) return false;

        if (CloudData.firstClearRewardedStages == null) CloudData.firstClearRewardedStages = new List<int>();
        CloudData.firstClearRewardedStages.Add(stageId);        // 먼저 마킹 → 아래 지급 영속 시 함께 저장

        StringBuilder debugLog = new StringBuilder();
        debugLog.Append($"[GameManager] 최초 클리어 보상 지급: Stage {stageId} (");

        foreach (var data in rewardList)
        {
            AddResource(data.itemId, data.amount);
            
            debugLog.Append($"+ID({data.itemId}): {data.amount} ");
        }
        
        debugLog.Append(")");
        Debug.Log(debugLog.ToString());
        return true;
    }
    
    public bool TryGrantFirstClearChapterReward(int chapterId, List<ItemSaveEntry> rewardList)
    {
        if (chapterId <= 0 || CloudData == null) return false;
        if (IsChapterFirstClearRewarded(chapterId)) return false;   // 이미 최초보상 지급됨 → 중복 차단
        if (rewardList == null || rewardList.Count == 0) return false;

        if (CloudData.firstClearRewardedChapters == null) CloudData.firstClearRewardedChapters = new List<int>();
        CloudData.firstClearRewardedChapters.Add(chapterId);        // 먼저 마킹 → 아래 지급 영속 시 함께 저장
        
        StringBuilder debugLog = new StringBuilder();
        debugLog.Append($"[GameManager] 최초 클리어 보상 지급: Chapter {chapterId} (");

        foreach (var data in rewardList)
        {
            AddResource(data.itemId, data.amount);
            
            debugLog.Append($"+ID({data.itemId}): {data.amount} ");
        }
        
        debugLog.Append(")");
        Debug.Log(debugLog.ToString());
        return true;
    }

    public void SaveArtifact(List<ArtifactInstance> myArtifacts)
    {
        var data = CloudData ?? UserCloudData.CreateDefault();
        EnsureCloudIdentity(data);

        data.myArtifacts = CloneArtifactList(myArtifacts);

        SyncToCloud(data);
    }

    // 아티팩트 목록의 저장/로드 복사 경계. CloudData와 런타임(ArtifactManager.MyArtifacts)이 같은 인스턴스를
    // 공유하면 저장 호출 없이도 변경이 CloudData에 스며들어(우연한 영속) 저장 누락 버그를 은닉한다.
    // 계약: 아티팩트 영속은 SaveArtifact 호출로만 일어난다. 양방향 모두 깊은 복사로 공유를 끊는다.
    private static List<ArtifactInstance> CloneArtifactList(List<ArtifactInstance> source)
    {
        var result = new List<ArtifactInstance>();
        if (source == null) return result;

        foreach (var inst in source)
            if (inst != null) result.Add(inst.Clone());

        return result;
    }

    // ===== 재화 단일 API (모든 획득/소비의 유일한 출입구) =====
    // 계약: 게임플레이/UI 코드는 ResourceRepo를 직접 만지지 않고 이 지갑 API만 쓴다.
    //   지급 = AddResource(itemId, amount, reason) / 차감 = UseResource / 다중 비용 = TrySpendResources(원자적).
    //   Set 계열(SetCurrency, repo.SetItemCount)은 로드·디버그 전용. CurrencyModel류 씬 모델은 표시 View(쓰기 경로 아님).
    // 런타임 원장 = ResourceRepo 아이템 컨테이너(골드 70001, 양피지 70002, 다이아 70003, 강화석 70004, 조각 70005, 티켓 70006).
    // 영속 = SyncAndSaveResourceCloudData(원장 → CloudData 미러 → Firestore+로컬백업). 변이당 1회.
    public event Action<int, int> OnCurrencyChanged;   // (gold, parchment) — 변경 시 전역 발행
    public event Action<int, int> OnItemChanged;       // (itemId, 변경 후 수량) — 모든 재화/아이템 공통. 신규 UI는 이것 하나만 구독하면 된다.

    public int Gold => CloudData != null ? CloudData.gold : 0;
    public int Parchment => CloudData != null ? CloudData.parchment : 0;
    public int Diamond => CloudData != null ? CloudData.diamond : 0;
    
    private const int GoldId = 70001;
    private const int ParchmentId = 70002;
    private const int DiamondId = 70003;

    /// <summary>아이템/재화 보유량 조회(런타임 원장 기준). 골드류도 같은 아이템ID로 조회된다.</summary>
    public int GetResourceCount(int itemId)
    {
        var repo = DataManager.Instance?.ResourceRepo;
        return repo != null ? repo.GetItemCount(itemId) : 0;
    }

    private void RaiseItemChanged(int itemId)
        => OnItemChanged?.Invoke(itemId, GetResourceCount(itemId));

    // 기존의 파편화된 함수 호출과 하드코딩된 분기를 이 안으로 완전히 격리함
    public void AddResource(int itemId, int amount, string reason = null)
    {
        if (itemId == GoldId)
        {
            AddCurrency(amount, 0, reason ?? "보상");
        }
        else if (itemId == ParchmentId)
        {
            AddCurrency(0, amount, reason ?? "보상");
        }
        else if (itemId == DiamondId)
        {
            AddDiamond(amount, reason);
        }
        else
        {
            if (amount <= 0) return;
            DataManager.Instance.ResourceRepo.AddItem(itemId, amount);
            Debug.Log($"[재화] +아이템 {itemId} x{amount} ({reason}) → 보유 {GetResourceCount(itemId)}");
            RaiseItemChanged(itemId);
            SyncAndSaveResourceCloudData();
        }
    }

    public bool UseResource(int itemId, int amount)
    {
        if (itemId == GoldId)
        {
            return TrySpendCurrency(amount, 0);
        }
        if (itemId == ParchmentId)
        {
            return TrySpendCurrency(0, amount);
        }
        if (itemId == DiamondId)
        {
            return TrySpendDiamond(amount);
        }

        var result = DataManager.Instance.ResourceRepo.UseItem(itemId, amount);
        if(!result) return false;

        RaiseItemChanged(itemId);
        SyncAndSaveResourceCloudData();
        return true;
    }

    /// <summary>다중 비용 원자 차감 — 전부 검사한 뒤 전부 차감하고 영속/이벤트는 1회만 처리한다.
    /// 아티팩트 강화(골드+강화석)처럼 비용이 여러 개일 때 수동 롤백 없이 이걸 쓴다. 하나라도 부족하면 false(변경 없음).</summary>
    public bool TrySpendResources(string reason, params (int itemId, int amount)[] costs)
    {
        var repo = DataManager.Instance?.ResourceRepo;
        if (repo == null || costs == null) return false;

        // 같은 아이템이 중복으로 들어와도 합산해서 검사한다.
        var merged = new Dictionary<int, int>();
        foreach (var (itemId, amount) in costs)
        {
            if (amount <= 0) continue;
            merged[itemId] = merged.TryGetValue(itemId, out int prev) ? prev + amount : amount;
        }
        if (merged.Count == 0) return true;

        foreach (var cost in merged)
            if (repo.GetItemCount(cost.Key) < cost.Value) return false;

        string detail = "";
        foreach (var cost in merged)
        {
            if (!repo.UseItem(cost.Key, cost.Value))
            {
                // 사전 검사를 통과했는데 차감이 거부되면 원장 구현 불일치다. 이미 차감된 앞 항목을 되돌리고 실패 처리.
                Debug.LogError($"[GameManager] TrySpendResources 차감 불일치: 아이템 {cost.Key} x{cost.Value} ({reason})");
                foreach (var refund in merged)
                {
                    if (refund.Key == cost.Key) break;
                    repo.AddItem(refund.Key, refund.Value);
                }
                return false;
            }
            detail += $"{cost.Key} x{cost.Value} ";
        }

        Debug.Log($"[재화] 지출 ({reason}): {detail}");
        foreach (var cost in merged) RaiseItemChanged(cost.Key);
        SyncAndSaveResourceCloudData();
        return true;
    }
    
    public bool CanAffordCurrency(int gold, int parchment)
    {
        return CloudData != null
            && CloudData.gold >= Mathf.Max(0, gold)
            && CloudData.parchment >= Mathf.Max(0, parchment);
    }

    /// <summary>재화 가산 — 전투 보상/업적/로그인 보상 공통 진입점. reason은 로그·추적용.</summary>
    public void AddCurrency(int gold, int parchment, string reason = null)
    {
        gold = Mathf.Max(0, gold);
        parchment = Mathf.Max(0, parchment);
        if (gold == 0 && parchment == 0) return;

        var repo = DataManager.Instance?.ResourceRepo;
        if (repo != null)
        {
            if (gold > 0) repo.AddItem(GoldId, gold);
            if (parchment > 0) repo.AddItem(ParchmentId, parchment);
        }

        Debug.Log($"[재화] +골드 {gold} +양피지 {parchment} ({reason}) → 골드 {Gold} / 양피지 {Parchment}");
        if (gold > 0) RaiseItemChanged(GoldId);
        if (parchment > 0) RaiseItemChanged(ParchmentId);
        SyncAndSaveResourceCloudData();
    }

    /// <summary>재화 차감 시도 — 상점/레벨업 등 소비 공통 진입점. 부족하면 false(변경 없음).</summary>
    public bool TrySpendCurrency(int gold, int parchment)
    {
        if (!CanAffordCurrency(gold, parchment)) return false;

        var repo = DataManager.Instance?.ResourceRepo;
        if (repo != null)
        {
            bool goldSuccess = (gold <= 0) || repo.UseItem(GoldId, gold);
            bool parchmentSuccess = (parchment <= 0) || repo.UseItem(ParchmentId, parchment);
            
            if (goldSuccess && !parchmentSuccess && gold > 0) 
                repo.AddItem(GoldId, gold);
            
            if (!goldSuccess && parchmentSuccess && parchment > 0) 
                repo.AddItem(ParchmentId, parchment);
            
            if (!goldSuccess || !parchmentSuccess)
            {
                Debug.LogError($"[GameManager] 재화 차감 실패! (CanAfford는 통과했으나 UseItem에서 거부됨) - Gold:{goldSuccess}, Parchment:{parchmentSuccess}");
                return false;
            }
        }

        if (gold > 0) RaiseItemChanged(GoldId);
        if (parchment > 0) RaiseItemChanged(ParchmentId);
        SyncAndSaveResourceCloudData();
        return true;
    }

    /// <summary>재화를 지정 값으로 설정 — 하이드레이션/리셋·테스트용(가산 아님). 값 동일하면 무시.</summary>
    public void SetCurrency(int gold, int parchment)
    {
        var repo = DataManager.Instance?.ResourceRepo;
        if (repo != null)
        {
            repo.SetItemCount(GoldId, Mathf.Max(0, gold));
            repo.SetItemCount(ParchmentId, Mathf.Max(0, parchment));
        }
        SyncAndSaveResourceCloudData();
    }

    // ===== 다이아 API (gold/parchment와 동일 패턴. 영속 원본 = CloudData.diamond) =====
    public bool CanAffordDiamond(int diamond)
        => CloudData != null && CloudData.diamond >= Mathf.Max(0, diamond);

    /// <summary>다이아 가산. reason은 로그·추적용.</summary>
    public void AddDiamond(int diamond, string reason = null)
    {
        diamond = Mathf.Max(0, diamond);
        if (diamond == 0) return;

        DataManager.Instance?.ResourceRepo?.AddItem(DiamondId, diamond);
        Debug.Log($"[재화] +다이아 {diamond} ({reason}) → 다이아 {Diamond}");
        RaiseItemChanged(DiamondId);
        SyncAndSaveResourceCloudData();
    }

    /// <summary>다이아 차감 시도. 부족하면 false(변경 없음).</summary>
    public bool TrySpendDiamond(int diamond)
    {
        if (!CanAffordDiamond(diamond)) return false;
        
        var repo = DataManager.Instance?.ResourceRepo;
        if (repo != null)
        {
            bool success = (diamond <= 0) || repo.UseItem(DiamondId, diamond);
            if (!success)
            {
                Debug.LogError("[GameManager] 다이아 차감 실패! (UseItem에서 거부됨)");
                return false;
            }
        }

        if (diamond > 0) RaiseItemChanged(DiamondId);
        SyncAndSaveResourceCloudData();
        return true;
    }

    /// <summary>다이아를 지정 값으로 설정 — 하이드레이션/리셋·테스트용. 값 동일하면 무시.</summary>
    public void SetDiamond(int diamond)
    {
        DataManager.Instance?.ResourceRepo?.SetItemCount(DiamondId, Mathf.Max(0, diamond));
        SyncAndSaveResourceCloudData();
    }
    
    // ResourceRepo의 최신 상태를 CloudData로 복사 및 저장
    public void SyncAndSaveResourceCloudData()
    {
        EnsureCurrencyData();
        var repo = DataManager.Instance?.ResourceRepo;
        if (repo != null)
        {
            CloudData.gold = repo.GetItemCount(GoldId);
            CloudData.parchment = repo.GetItemCount(ParchmentId);
            CloudData.diamond = repo.GetItemCount(DiamondId);
            CloudData.inventory = repo.ExportInventory();
            CloudData.staminaData = repo.ExportStaminaData();
        }
        
        RaiseCurrencyChanged();
        
        // 유저 데이터를 실제 서버/로컬에 굽는 기존 함수 호출
        PersistCurrency(); 
    }
    
    // 추가: 조규민 - 하우징 가구 구매는 재화 차감과 보유 등록을 한 번의 영속 데이터 변경으로 처리한다.
    public bool TryPurchaseHousingFurniture(int _furnitureId, int _price, HousingPlacementPriceCurrency _currency)
    {
        if (_furnitureId <= 0)
        {
            Debug.LogWarning($"[하우징 구매] 유효하지 않은 가구 ID입니다. Furniture: {_furnitureId}");
            return false;
        }

        EnsureCurrencyData();
        EnsureHousingOwnedFurnitureData();

        if (IsHousingFurnitureOwned(_furnitureId))
        {
            return true;
        }

        int _safePrice = Mathf.Max(0, _price);

        if (!CanAffordHousingPurchase(_safePrice, _currency))
        {
            Debug.LogWarning($"[하우징 구매] 재화가 부족합니다. Furniture: {_furnitureId}, Price: {_safePrice}, Currency: {_currency}");
            return false;
        }

        if (!TrySpendHousingPurchaseCurrency(_safePrice, _currency))
        {
            Debug.LogError($"[하우징 구매] 재화 차감에 실패했습니다. Furniture: {_furnitureId}, Price: {_safePrice}, Currency: {_currency}");
            return false;
        }

        CloudData.housingOwnedFurnitureIds.Add(_furnitureId);
        Debug.Log($"[하우징 구매] 가구 구매 완료. Furniture: {_furnitureId}, Price: {_safePrice}, Currency: {_currency}");

        SyncAndSaveResourceCloudData();
        return true;
    }

    public bool IsHousingFurnitureOwned(int _furnitureId)
    {
        if (_furnitureId <= 0 || CloudData == null)
        {
            return false;
        }

        EnsureHousingOwnedFurnitureData();
        return CloudData.housingOwnedFurnitureIds.Contains(_furnitureId);
    }

    private bool CanAffordHousingPurchase(int _price, HousingPlacementPriceCurrency _currency)
    {
        if (_price <= 0)
        {
            return true;
        }

        var _repo = DataManager.Instance?.ResourceRepo;

        if (_repo != null)
        {
            int _itemId = _currency == HousingPlacementPriceCurrency.Diamond ? DiamondId : GoldId;
            return _repo.GetItemCount(_itemId) >= _price;
        }

        switch (_currency)
        {
            case HousingPlacementPriceCurrency.Diamond:
                return CloudData.diamond >= _price;
            default:
                return CloudData.gold >= _price;
        }
    }

    private bool TrySpendHousingPurchaseCurrency(int _price, HousingPlacementPriceCurrency _currency)
    {
        if (_price <= 0)
        {
            return true;
        }

        // 수정 : 김영찬 -> 재화는 CloudData.diamond/gold를 직접 증/차감 하면 안되서 수정함 (팀장님 지시사항)
        // 2차 수정 : 재화는 전용 획득/소모 함수를 쓰지 않으면 획득/소모 내역이 증발하니 그것을 방지하기 위해 통합 획득/소모용 함수 생성 및 적용

        int _itemId = _currency == HousingPlacementPriceCurrency.Diamond ? DiamondId : GoldId;
        return UseResource(_itemId, _price);
    }

    private void EnsureHousingOwnedFurnitureData()
    {
        if (CloudData == null)
        {
            return;
        }

        if (CloudData.housingOwnedFurnitureIds == null)
        {
            CloudData.housingOwnedFurnitureIds = new List<int>();
        }
    }

    public DateTime EnsureHousingAutoCurrencyLastClaimUtc()
    {
        EnsureCurrencyData();

        if (TryParseUtc(CloudData.housingAutoCurrencyLastClaimAt, out DateTime savedUtc))
        {
            return savedUtc;
        }

        DateTime nowUtc = DateTime.UtcNow;
        CloudData.housingAutoCurrencyLastClaimAt = nowUtc.ToString("o");
        SyncToCloud(CloudData);
        return nowUtc;
    }

    public void ClaimHousingAutoCurrency(int gold, int parchment, string lastClaimAtUtc)
    {
        gold = Mathf.Max(0, gold);
        parchment = Mathf.Max(0, parchment);
        EnsureCurrencyData();

        CloudData.gold = Mathf.Max(0, CloudData.gold + gold);
        CloudData.parchment = Mathf.Max(0, CloudData.parchment + parchment);
        CloudData.housingAutoCurrencyLastClaimAt = string.IsNullOrWhiteSpace(lastClaimAtUtc)
            ? DateTime.UtcNow.ToString("o")
            : lastClaimAtUtc;

        Debug.Log($"[하우징 자동재화] +골드 {gold} +양피지 {parchment} / 마지막 수령 {CloudData.housingAutoCurrencyLastClaimAt}");
        RaiseCurrencyChanged();
        PersistCurrency();
    }

    // 추가: 조규민 - 하우징 방치 보상의 모든 재화를 한 번의 저장 흐름으로 지급한다.
    // 수정 : 클라우드데이터의 재화를 직접 수정하는것은 팀장님께서 금지 시켰으며, 골드/양피지/다이아몬드는 팀장님이 전용 획득/소모 함수를 사용하라고 했기 때문에 해당 부분에 대해 수정함
    public bool ClaimHousingIdleReward(int _gold, int _parchment, int _diamond, string _lastClaimAtUtc)
    {
        _gold = Mathf.Max(0, _gold);
        _parchment = Mathf.Max(0, _parchment);
        _diamond = Mathf.Max(0, _diamond);

        if (_gold == 0 && _parchment == 0 && _diamond == 0)
        {
            return false;
        }

        EnsureCurrencyData();

        AddResource(GoldId, _gold);
        AddResource(ParchmentId, _parchment);
        AddResource(DiamondId, _diamond);

        CloudData.housingAutoCurrencyLastClaimAt = string.IsNullOrWhiteSpace(_lastClaimAtUtc)
            ? DateTime.UtcNow.ToString("o")
            : _lastClaimAtUtc;

        Debug.Log($"[하우징 자동 재화] +골드 {_gold} +양피지 {_parchment} +다이아 {_diamond} / 마지막 수령 {CloudData.housingAutoCurrencyLastClaimAt}");
        SyncAndSaveResourceCloudData();
        return true;
    }

    private static bool TryParseUtc(string value, out DateTime utcTime)
    {
        utcTime = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedTime))
        {
            return false;
        }

        utcTime = parsedTime.Kind == DateTimeKind.Utc ? parsedTime : parsedTime.ToUniversalTime();
        return true;
    }

    private void EnsureCurrencyData()
    {
        if (CloudData == null) CloudData = UserCloudData.CreateDefault();
        EnsureCloudIdentity(CloudData);
    }

    private void RaiseCurrencyChanged() => OnCurrencyChanged?.Invoke(Gold, Parchment);

    // 재화 변경 영속화. SyncToCloud가 로그인=클라우드+로컬 / 오프라인=로컬백업을 알아서 처리(단계④).
    private void PersistCurrency()
    {
        if (CloudData != null) SyncToCloud(CloudData);
    }

    private void EnsureCloudIdentity(UserCloudData data)
    {
        if (data == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(data.uid))
        {
            data.uid = UserUID;
        }

        if (string.IsNullOrWhiteSpace(data.provider))
        {
            data.provider = LastSignInWasGoogle ? "google" : "guest";
        }
    }

    private static List<int> CloneUnlockedStages(List<int> unlockedStages)
    {
        return unlockedStages == null ? new List<int>() : new List<int>(unlockedStages);
    }

    private void AddNextStageIfExists(UserCloudData data, int chapterId, int nextStageNumber)
    {
        int nextStageId = BuildStageId(chapterId, nextStageNumber);
        if (HasStage(nextStageId))
        {
            AddUnlockedStage(data, nextStageId);
            return;
        }

        int nextChapterFirstStageId = BuildStageId(chapterId + 1, 1);
        if (HasStage(nextChapterFirstStageId))
        {
            AddUnlockedStage(data, nextChapterFirstStageId);
            data.currentChapter = Mathf.Max(data.currentChapter, chapterId + 1);
            data.currentStage = 1;
            return;
        }

        // 테마 경계: Chapter_ID는 테마*100+순서(101~105, 201~205)라 105 다음은 106이 아니라 201이다.
        int nextThemeFirstChapterId = (chapterId / 100 + 1) * 100 + 1;
        int nextThemeFirstStageId = BuildStageId(nextThemeFirstChapterId, 1);
        if (HasStage(nextThemeFirstStageId))
        {
            AddUnlockedStage(data, nextThemeFirstStageId);
            data.currentChapter = Mathf.Max(data.currentChapter, nextThemeFirstChapterId);
            data.currentStage = 1;
        }
    }

    private bool HasStage(int stageId)
    {
        return DataManager.Instance != null
            && DataManager.Instance.StageRepo != null
            && DataManager.Instance.StageRepo.GetStage(stageId) != null;
    }

    // Stage_ID는 StageRepo (챕터,순서) 역조회로만 구한다. 못 찾으면 -1(HasStage/AddUnlockedStage가 무시).
    // 형태상 Chapter_ID*100+순서(10101 등)지만 테마 경계/특수 챕터(98xx/99xx)가 있어 산술 조합은 금지.
    private static int BuildStageId(int chapterId, int stageNumber)
    {
        var repo = DataManager.Instance != null ? DataManager.Instance.StageRepo : null;
        return repo != null ? repo.GetStageId(chapterId, Mathf.Max(1, stageNumber)) : -1;
    }

    private static void AddUnlockedStage(UserCloudData data, int stageId)
    {
        if (stageId <= 0) return;   // BuildStageId가 못 찾으면 -1 → 잘못된 ID로 unlockedStages 오염 방지

        if (data.unlockedStages == null)
        {
            data.unlockedStages = new List<int>();
        }

        if (!data.unlockedStages.Contains(stageId))
        {
            data.unlockedStages.Add(stageId);
        }
    }

    // ---------- 로컬 세이브 (인게임) ----------
    public void SaveLocal()
    {
        // 인게임 진행 상태를 모아 로컬 세이브에 기록한다. (백그라운드 전환/종료/스테이지 클리어 체크포인트 공용)
        // 옛 구현은 로그만 찍는 빈 스텁이라 OnApplicationPause/Quit, StageLoopManager.ClearStage의 자동 저장이 전부 무동작이었다.
        // 구성은 EnchantLinkButtonBoundaryPresenter.CreateCurrentProgressSaveData와 동일 — 추후 단일 빌더로 통합 권장.
        var loop = FindFirstObjectByType<StageLoopManager>();
        if (loop == null)
        {
            Debug.LogWarning("[GameManager] SaveLocal: StageLoopManager가 없어 인게임 세이브를 건너뜀(인게임 상태 아님?).");
            return;
        }

        var playerModel = FindFirstObjectByType<PlayerModel>();
        var growthSystem = FindFirstObjectByType<InGameGrowthSystem>();
        var enchantModel = FindFirstObjectByType<EnchantModel>();
        var comboModel = FindFirstObjectByType<ComboModel>();
        var sortModel = FindFirstObjectByType<SortModel>();
        var sortSystem = FindFirstObjectByType<SortSystem>();
        var jokerSystem = FindFirstObjectByType<JokerSystem>();
        var rewardManager = FindFirstObjectByType<InGameRewardManager>();

        var data = new InGameSaveData
        {
            // 스테이지
            chapterId = Mathf.Max(1, loop.CurrentChapterId),
            clearedStage = Mathf.Max(0, loop.CompletedStageCount),
            
            // 플레이어
            playerHP = playerModel != null ? Mathf.Max(1, playerModel.CurrentHP) : 1,
            currentEXP = growthSystem != null ? Mathf.Max(0, growthSystem.CurrentEXP) : 0,
            inGameLevel = growthSystem != null ? Mathf.Max(1, growthSystem.CurrentLevel) : 1,
            
            // 퍼즐
            puzzleSlots = sortModel != null ? sortModel.ExportPuzzleSlots() : Array.Empty<int>(),
            waitingSlots = sortModel != null ? sortModel.ExportWaitingSlots() : Array.Empty<int>(),
            jokerCount = jokerSystem != null ? jokerSystem.GetJokerCount() : 2,
            jokerRemainingCooldown = jokerSystem != null ? jokerSystem.GetRemainingCooldown() : 0f,
            nextStageSeed = sortSystem != null ? sortSystem.GetCurrentSeedForSave() : UnityEngine.Random.Range(0, int.MaxValue),
            
            // 전투 보상
            accumulatedRewards = rewardManager != null ? rewardManager.ExportRewardData() : new List<ItemSaveEntry>(),
            
            // 인첸트
            acquiredEnchants = enchantModel != null ? enchantModel.ToSaveData() : new List<AcquiredEnchantSaveData>(),
            
            // 기록
            totalDamage = RunStats.HighestDamage,
            highestDamage =  RunStats.HighestDamage,
            MaxBySkill = RunStats.ExportMaxBySkill(),
            maxCombo = comboModel != null ? comboModel.MaxComboThisRun : 0
        };

        SaveLocalData(data);
        Debug.Log($"[GameManager] 인게임 로컬 세이브 완료 (챕터{data.chapterId} 클리어{data.clearedStage} HP{data.playerHP})");
    }

    private void SaveLocalData(InGameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetInGameSavePath(), json);
        Debug.Log($"[세이브 경로] {Application.persistentDataPath}");
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
