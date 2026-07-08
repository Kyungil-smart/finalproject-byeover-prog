// 담당자 : 정승우
// 설명   : Firebase 인증 서비스 -- 구글 로그인(Google Sign-In) + 게스트 로그인

// 2차 수정자 : 조규민
// 수정 내용 : 게스트/Firebase 초기화 실패 처리, 중복 로그인 방어, Google 설정 검증, Web Client ID 자동 해석, 로그인 실패 유형 전달, Editor 전용 Google 로그인 흐름 테스트, 테스트 전 기존 세션 로그아웃 옵션, 고정 테스트 유저 키 로그인 옵션, 게임 화면 입력 기반 Email/Password 테스트 로그인 실패 원인 로그 보강, Editor Email/Password 계정 자동 생성 흐름, 기존 Editor Email/Password 계정 로그인 전용 흐름, 기존 익명 세션 재사용 방어 추가, Google 로그인 실패 유형 전달 누락 보정, 자동 로그인 후 로그아웃 시 GoogleSignIn 미설정 인스턴스 생성 방지

#if FIREBASE_ENABLED
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
#endif

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseAuthService : MonoBehaviour
{
    private const float DEFAULT_GOOGLE_SIGN_IN_TIMEOUT_SECONDS = 30f;
    private const float DEFAULT_FIREBASE_AUTH_TIMEOUT_SECONDS = 20f;

    public event Action<string> OnLoginSuccess;
#pragma warning disable CS0067 // 추가: 조규민 - Editor에서 FIREBASE_ENABLED가 꺼진 경우에도 Android 빌드용 실패 이벤트를 유지한다.
    public event Action<string> OnLoginFailed;
    public event Action<AuthLoginFailureType, string> OnLoginFailedWithType;
#pragma warning restore CS0067
    public event Action OnLogout;

    [Header("설정")]
    [Tooltip("Firebase Console의 웹 앱 OAuth 클라이언트 ID입니다. Android 클라이언트 ID가 아니라 client_type 3 값을 넣어야 합니다.")]
    [SerializeField] private string _webClientId;

    [Header("타임아웃")]
    [Tooltip("Google 계정 선택 창 이후 ID Token을 기다릴 최대 시간입니다.")]
    [SerializeField] private float _googleSignInTimeoutSeconds = DEFAULT_GOOGLE_SIGN_IN_TIMEOUT_SECONDS;
    [Tooltip("Google ID Token을 Firebase Credential로 교환할 때 기다릴 최대 시간입니다.")]
    [SerializeField] private float _firebaseAuthTimeoutSeconds = DEFAULT_FIREBASE_AUTH_TIMEOUT_SECONDS;

#if UNITY_EDITOR
    [Header("에디터 테스트")]
    [Tooltip("Unity Editor에서 구글 로그인 이후 흐름을 테스트할 때만 켭니다. 실제 구글 계정 인증은 안드로이드 빌드에서 진행해야 합니다.")]
    [SerializeField] private bool _enableEditorGoogleLoginTest;
    [Tooltip("에디터 구글 로그인 테스트 전에 기존 Firebase 세션을 끊고 새 인증 상태로 시작합니다.")]
    [SerializeField] private bool _signOutBeforeEditorGoogleLoginTest;
    [Tooltip("Firebase Email/Password 테스트 계정으로 로그인해 실제 Auth UID로 Firestore 흐름을 검증합니다.")]
    [SerializeField] private bool _useEmailPasswordEditorGoogleTestUser;
    [Tooltip("Firebase 익명 UID 대신 고정 테스트 UID를 사용해 여러 에디터 테스트 유저를 오갈 때 켭니다.")]
    [SerializeField] private bool _useFixedEditorGoogleTestUser;
    [Tooltip("고정 테스트 UID를 만들 때 사용할 키입니다. 예: google_test_01")]
    [SerializeField] private string _editorGoogleTestUserKey = "google_test_01";
    [Tooltip("에디터 구글 로그인 테스트에서 프로필 생성 흐름에 전달할 표시 이름입니다.")]
    [SerializeField] private string _editorGoogleTestDisplayName = "Editor Google Tester";
#endif

    public string UserUID { get; private set; }
    public string UserEmail { get; private set; }
    public string UserDisplayName { get; private set; }
    public bool LastSignInWasGoogle { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(UserUID);
    public bool IsFirebaseReady { get; private set; }
    public bool IsSigningIn { get; private set; } // 추가: 조규민 - 로그인 중복 요청을 막기 위한 상태값
    public bool RequiresEditorGoogleEmailPasswordInput
    {
        get
        {
#if UNITY_EDITOR
            return _enableEditorGoogleLoginTest && _useEmailPasswordEditorGoogleTestUser;
#else
            return false;
#endif
        }
    }

#if FIREBASE_ENABLED
    private FirebaseAuth _auth;
    private string _resolvedWebClientId;
    private string _configuredGoogleWebClientId;
    private bool _hasGoogleSignInInstance;
#endif

    public IEnumerator InitializeFirebase()
    {
#if FIREBASE_ENABLED
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        // 추가: 조규민 - Firebase 의존성 확인 실패와 예외를 로그인 실패 이벤트로 분리한다.
        if (task.IsFaulted)
        {
            string error = GetExceptionMessage(task.Exception, "Firebase 초기화 중 예외가 발생했습니다.");
            Debug.LogError("[Auth] Firebase 초기화 예외: " + error);
            IsFirebaseReady = false;
            RaiseLoginFailed(error);
            yield break;
        }

        if (task.Result != DependencyStatus.Available)
        {
            string error = "Firebase 초기화 실패: " + task.Result;
            Debug.LogError("[Auth] " + error);
            IsFirebaseReady = false;
            RaiseLoginFailed(error);
            yield break;
        }

        _auth = FirebaseAuth.DefaultInstance;
        IsFirebaseReady = true;

        if (_auth.CurrentUser != null)
        {
            ApplyFirebaseUser(_auth.CurrentUser, HasGoogleProvider(_auth.CurrentUser));
        }
#else
        Debug.Log("[Auth] FIREBASE_ENABLED 꺼져있음. 더미 모드.");
        IsFirebaseReady = false;
        yield return null;
#endif
    }

    public IEnumerator GoogleSignInCoroutine(string editorEmail = null, string editorPassword = null)
    {
#if FIREBASE_ENABLED
        if (IsSigningIn)
        {
            yield break;
        }

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            yield return StartCoroutine(EditorGoogleSignInTestCoroutine(editorEmail, editorPassword));
            yield break;
        }
#endif

        string validationError = GetGoogleSignInValidationError();
        if (!string.IsNullOrEmpty(validationError))
        {
            RaiseLoginFailed(GetValidationFailureType(validationError), validationError);
            yield break;
        }

        IsSigningIn = true;

        Task<GoogleSignInUser> signInTask;
        try
        {
            ConfigureGoogleSignIn();
            signInTask = GoogleSignIn.DefaultInstance.SignIn();
        }
        catch (Exception exception)
        {
            LogExceptionDetails("[Auth][GoogleSignIn] SignIn() call exception", exception);
            CompleteFailedSignIn(GetGoogleSignInFailureType(exception), GetGoogleSignInExceptionMessage(exception));
            yield break;
        }

        bool googleCompleted = false;
        yield return StartCoroutine(WaitForTask(signInTask, _googleSignInTimeoutSeconds, result => googleCompleted = result));
        if (!googleCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Timeout, "Google 로그인 응답 시간이 초과되었습니다. 네트워크 상태와 Google Play Services 상태를 확인해 주세요.");
            yield break;
        }

        if (signInTask.IsCanceled)
        {
            Debug.LogWarning("[Auth][GoogleSignIn] SignIn() task canceled.");
            CompleteFailedSignIn(AuthLoginFailureType.Canceled, "구글 로그인이 취소되었습니다.");
            yield break;
        }

        if (signInTask.IsFaulted)
        {
            LogExceptionDetails("[Auth][GoogleSignIn] SignIn() task faulted", signInTask.Exception);
            CompleteFailedSignIn(GetGoogleSignInFailureType(signInTask.Exception), GetGoogleSignInExceptionMessage(signInTask.Exception));
            yield break;
        }

        if (signInTask.Result == null || string.IsNullOrEmpty(signInTask.Result.IdToken))
        {
            CompleteFailedSignIn(AuthLoginFailureType.Configuration, "Google ID Token을 받지 못했습니다. Web Client ID와 SHA-1/SHA-256 설정을 확인해 주세요.");
            yield break;
        }

        yield return StartCoroutine(SignInFirebaseWithGoogleToken(signInTask.Result.IdToken));
#else
        RaiseLoginFailed(AuthLoginFailureType.General, "실제 Google 로그인은 Android 빌드에서만 사용할 수 있습니다. Android로 Switch Platform 후 기기에서 테스트해 주세요.");
        yield return null;
#endif
    }

    // 추가: 조규민 - 기존 Editor Email/Password 테스트 계정 로그인만 수행하고 신규 계정 생성을 시도하지 않는다.
    public IEnumerator ExistingEditorGoogleAccountSignInCoroutine(string editorEmail, string editorPassword)
    {
#if FIREBASE_ENABLED && UNITY_EDITOR
        if (IsSigningIn)
        {
            yield break;
        }

        if (!Application.isEditor)
        {
            RaiseLoginFailed(AuthLoginFailureType.Configuration, "기존 계정 로그인은 Unity Editor Email/Password 테스트 모드에서만 사용할 수 있습니다.");
            yield break;
        }

        if (!_enableEditorGoogleLoginTest || !_useEmailPasswordEditorGoogleTestUser)
        {
            RaiseLoginFailed(AuthLoginFailureType.Configuration, "FirebaseAuthService의 Editor Email/Password 테스트 옵션을 켜 주세요.");
            yield break;
        }

        if (!CanUseFirebaseAuth())
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "Firebase 인증 서비스가 준비되지 않았습니다.");
            yield break;
        }

        if (_signOutBeforeEditorGoogleLoginTest)
        {
            ClearCurrentFirebaseSessionForEditorTest();
        }

        yield return StartCoroutine(EditorGoogleEmailPasswordSignInCoroutine(editorEmail, editorPassword, false));
#else
        RaiseLoginFailed(AuthLoginFailureType.General, "기존 계정 로그인은 Firebase가 활성화된 Unity Editor Email/Password 테스트 모드에서만 사용할 수 있습니다.");
        yield return null;
#endif
    }

#if FIREBASE_ENABLED
#if UNITY_EDITOR
    private IEnumerator EditorGoogleSignInTestCoroutine(string editorEmail, string editorPassword)
    {
        if (!_enableEditorGoogleLoginTest)
        {
            RaiseLoginFailed(AuthLoginFailureType.Configuration, "Editor Google 로그인 테스트가 꺼져 있습니다. FirebaseAuthService의 Editor 테스트 옵션을 켜 주세요.");
            yield break;
        }

        if (!CanUseFirebaseAuth())
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "Firebase 인증 서비스가 준비되지 않았습니다.");
            yield break;
        }

        if (_signOutBeforeEditorGoogleLoginTest)
        {
            ClearCurrentFirebaseSessionForEditorTest();
        }

        if (_useEmailPasswordEditorGoogleTestUser)
        {
            yield return StartCoroutine(EditorGoogleEmailPasswordSignInCoroutine(editorEmail, editorPassword));
            yield break;
        }

        if (_useFixedEditorGoogleTestUser)
        {
            ApplyFixedEditorGoogleTestUser();
            yield break;
        }

        IsSigningIn = true;

        var authTask = _auth.SignInAnonymouslyAsync();
        bool firebaseCompleted = false;
        yield return StartCoroutine(WaitForTask(authTask, _firebaseAuthTimeoutSeconds, result => firebaseCompleted = result));
        if (!firebaseCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Timeout, "Editor Google 로그인 테스트 인증 시간이 초과되었습니다. Firebase 익명 인증 설정과 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (authTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Canceled, "Editor Google 로그인 테스트가 취소되었습니다.");
            yield break;
        }

        if (authTask.IsFaulted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, GetExceptionMessage(authTask.Exception, "Editor Google 로그인 테스트 인증 실패"));
            yield break;
        }

        ApplyFirebaseUser(authTask.Result.User, true);
        UserEmail = null;
        UserDisplayName = string.IsNullOrWhiteSpace(_editorGoogleTestDisplayName) ? null : _editorGoogleTestDisplayName.Trim();
        LastSignInWasGoogle = true;
        IsSigningIn = false;
        Debug.Log("[Auth] Editor Google 로그인 테스트 성공: " + UserUID);
        OnLoginSuccess?.Invoke(UserUID);
    }

    private IEnumerator EditorGoogleEmailPasswordSignInCoroutine(string editorEmail, string editorPassword, bool canCreateMissingUser = true)
    {
        string validationError = GetEditorGoogleEmailPasswordValidationError(editorEmail, editorPassword);
        if (!string.IsNullOrEmpty(validationError))
        {
            RaiseLoginFailed(AuthLoginFailureType.Configuration, validationError);
            yield break;
        }

        IsSigningIn = true;
        string normalizedEditorEmail = editorEmail.Trim();

        var authTask = _auth.SignInWithEmailAndPasswordAsync(normalizedEditorEmail, editorPassword);
        bool firebaseCompleted = false;
        yield return StartCoroutine(WaitForTask(authTask, _firebaseAuthTimeoutSeconds, result => firebaseCompleted = result));
        if (!firebaseCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Timeout, "Editor Email/Password 테스트 로그인 시간이 초과되었습니다. Firebase 인증 설정과 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (authTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Canceled, "Editor Email/Password 테스트 로그인이 취소되었습니다.");
            yield break;
        }

        if (authTask.IsFaulted)
        {
            if (!canCreateMissingUser || !ShouldCreateEditorEmailPasswordUser(authTask.Exception))
            {
                LogExceptionDetails("[Auth][EditorEmailPassword] SignInWithEmailAndPasswordAsync faulted", authTask.Exception);
                CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, GetFirebaseAuthExceptionMessage(authTask.Exception, "Editor Email/Password 테스트 로그인 실패"));
                yield break;
            }

            LogEditorEmailPasswordCreateAttempt(authTask.Exception);
            yield return StartCoroutine(CreateEditorEmailPasswordUserCoroutine(normalizedEditorEmail, editorPassword));
            yield break;
        }

        CompleteEditorGoogleEmailPasswordSignIn(authTask.Result.User, normalizedEditorEmail, "Editor Email/Password Google 테스트 로그인 성공");
    }

    private IEnumerator CreateEditorEmailPasswordUserCoroutine(string editorEmail, string editorPassword)
    {
        var createTask = _auth.CreateUserWithEmailAndPasswordAsync(editorEmail, editorPassword);
        bool firebaseCompleted = false;
        yield return StartCoroutine(WaitForTask(createTask, _firebaseAuthTimeoutSeconds, result => firebaseCompleted = result));
        if (!firebaseCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Timeout, "Editor Email/Password 테스트 계정 생성 시간이 초과되었습니다. Firebase 인증 설정과 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (createTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Canceled, "Editor Email/Password 테스트 계정 생성이 취소되었습니다.");
            yield break;
        }

        if (createTask.IsFaulted)
        {
            LogExceptionDetails("[Auth][EditorEmailPassword] CreateUserWithEmailAndPasswordAsync faulted", createTask.Exception);
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, GetFirebaseAuthExceptionMessage(createTask.Exception, "Editor Email/Password 테스트 계정 생성 실패"));
            yield break;
        }

        CompleteEditorGoogleEmailPasswordSignIn(createTask.Result.User, editorEmail, "Editor Email/Password Google 테스트 계정 생성 후 로그인 성공");
    }

    private void CompleteEditorGoogleEmailPasswordSignIn(FirebaseUser user, string editorEmail, string logMessage)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.UserId))
        {
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, "Editor Email/Password 테스트 로그인 UID를 받지 못했습니다.");
            return;
        }

        ApplyFirebaseUser(user, true);
        UserEmail = string.IsNullOrWhiteSpace(editorEmail) ? UserEmail : editorEmail.Trim();
        UserDisplayName = string.IsNullOrWhiteSpace(_editorGoogleTestDisplayName) ? UserDisplayName : _editorGoogleTestDisplayName.Trim();
        LastSignInWasGoogle = true;
        IsSigningIn = false;
        Debug.Log("[Auth] " + logMessage + ": " + UserUID);
        OnLoginSuccess?.Invoke(UserUID);
    }

    private string GetEditorGoogleEmailPasswordValidationError(string editorEmail, string editorPassword)
    {
        if (string.IsNullOrWhiteSpace(editorEmail))
        {
            return "Editor Email/Password 테스트 이메일이 비어 있습니다.";
        }

        if (string.IsNullOrEmpty(editorPassword))
        {
            return "Editor Email/Password 테스트 비밀번호가 비어 있습니다.";
        }

        return null;
    }

    private bool ShouldCreateEditorEmailPasswordUser(Exception exception)
    {
        var firebaseException = GetInnerException<FirebaseException>(exception);
        if (firebaseException == null)
        {
            return false;
        }

        string authErrorName = GetAuthErrorName(firebaseException);
        if (authErrorName == "UserNotFound" || authErrorName == "InvalidCredential" || authErrorName == "InternalError" || authErrorName == "Failure")
        {
            return true;
        }

        string errorMessage = firebaseException.Message ?? exception.Message;
        return !string.IsNullOrEmpty(errorMessage) && errorMessage.IndexOf("internal error", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void LogEditorEmailPasswordCreateAttempt(Exception exception)
    {
        var firebaseException = GetInnerException<FirebaseException>(exception);
        string authErrorName = firebaseException == null ? "Unknown" : GetAuthErrorName(firebaseException);
        Debug.Log("[Auth][EditorEmailPassword] 기존 계정 로그인 실패로 신규 테스트 계정 생성을 시도합니다. AuthError=" + authErrorName);
    }

    private void ApplyFixedEditorGoogleTestUser()
    {
        // 추가: 조규민 - 에디터에서 같은 테스트 유저 키로 기존/신규 유저 흐름을 반복 확인한다.
        string testUserKey = NormalizeEditorGoogleTestUserKey(_editorGoogleTestUserKey);
        UserUID = "editor_google_" + testUserKey;
        UserEmail = null;
        UserDisplayName = string.IsNullOrWhiteSpace(_editorGoogleTestDisplayName) ? null : _editorGoogleTestDisplayName.Trim();
        LastSignInWasGoogle = true;
        IsSigningIn = false;
        Debug.Log("[Auth] 고정 Editor Google 테스트 유저 로그인: " + UserUID);
        OnLoginSuccess?.Invoke(UserUID);
    }

    private string NormalizeEditorGoogleTestUserKey(string testUserKey)
    {
        if (string.IsNullOrWhiteSpace(testUserKey))
        {
            return "default";
        }

        var builder = new System.Text.StringBuilder();
        foreach (char character in testUserKey.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (character == '_' || character == '-')
            {
                builder.Append('_');
            }
        }

        return builder.Length == 0 ? "default" : builder.ToString();
    }

    private void ClearCurrentFirebaseSessionForEditorTest()
    {
        // 추가: 조규민 - 에디터 테스트에서 다른 테스트 계정을 만들 수 있도록 기존 익명 세션을 끊는다.
        _auth.SignOut();
        UserUID = null;
        UserEmail = null;
        UserDisplayName = null;
        LastSignInWasGoogle = false;
    }
#endif

    private IEnumerator SignInFirebaseWithGoogleToken(string idToken)
    {
        var credential = GoogleAuthProvider.GetCredential(idToken, null);
        var authTask = _auth.SignInWithCredentialAsync(credential);
        bool firebaseCompleted = false;
        yield return StartCoroutine(WaitForTask(authTask, _firebaseAuthTimeoutSeconds, result => firebaseCompleted = result));
        if (!firebaseCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Timeout, "Firebase Google 인증 시간이 초과되었습니다. Firebase Authentication 제공업체와 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (authTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, "Firebase Google 인증이 취소되었습니다.");
            yield break;
        }

        if (authTask.IsFaulted)
        {
            LogExceptionDetails("[Auth][FirebaseAuth] SignInWithCredentialAsync faulted", authTask.Exception);
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, GetExceptionMessage(authTask.Exception, "Firebase 인증 실패"));
            yield break;
        }

        ApplyFirebaseUser(authTask.Result, true);
        IsSigningIn = false;
        OnLoginSuccess?.Invoke(UserUID);
    }
#endif

    public IEnumerator GuestSignInCoroutine()
    {
#if FIREBASE_ENABLED
        // 추가: 조규민 - 게스트 로그인 중복 실행과 Firebase 미초기화 상태를 방어한다.
        if (IsSigningIn)
        {
            yield break;
        }

        if (!CanUseFirebaseAuth())
        {
            RaiseLoginFailed(AuthLoginFailureType.FirebaseAuth, "Firebase 인증 서비스가 준비되지 않았습니다.");
            yield break;
        }

        // 추가: 조규민 - 앱 재실행 후 남아 있는 익명 세션이 있으면 새 게스트 UID를 요청하지 않는다.
        if (_auth.CurrentUser != null && _auth.CurrentUser.IsAnonymous)
        {
            ApplyFirebaseUser(_auth.CurrentUser, false);
            OnLoginSuccess?.Invoke(UserUID);
            yield break;
        }

        IsSigningIn = true;

        var task = _auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCanceled)
        {
            IsSigningIn = false;
            RaiseLoginFailed("게스트 로그인이 취소되었습니다.");
            yield break;
        }

        if (task.IsFaulted)
        {
            IsSigningIn = false;
            RaiseLoginFailed(GetExceptionMessage(task.Exception, "익명 인증 실패"));
            yield break;
        }

        ApplyFirebaseUser(task.Result.User, false);
        IsSigningIn = false;
        OnLoginSuccess?.Invoke(UserUID);
#else
        Debug.Log("[Auth] 게스트 로그인 더미 모드");
        UserUID = "guest_" + UnityEngine.Random.Range(10000, 99999);
        UserEmail = null;
        UserDisplayName = null;
        LastSignInWasGoogle = false;
        OnLoginSuccess?.Invoke(UserUID);
        yield return null;
#endif
    }

    public void SignOut()
    {
#if FIREBASE_ENABLED
        if (_hasGoogleSignInInstance)
        {
            try
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Auth] Google Sign-In 로그아웃 정리 실패: " + exception.Message);
            }
        }

        _auth?.SignOut();
#endif
        UserUID = null;
        UserEmail = null;
        UserDisplayName = null;
        LastSignInWasGoogle = false;
        IsSigningIn = false; // 추가: 조규민 - 로그아웃 후 로그인 진행 상태를 초기화한다.
        OnLogout?.Invoke();
    }

    public bool HasPreviousSession()
    {
#if FIREBASE_ENABLED
        return _auth != null && _auth.CurrentUser != null;
#else
        return false;
#endif
    }

    /// <summary>Firebase Auth 계정을 삭제한다.</summary>
    public IEnumerator DeleteAccountCoroutine(System.Action<bool> onResult)
    {
#if FIREBASE_ENABLED
        if (_auth?.CurrentUser == null)
        {
            Debug.LogWarning("[Auth] 삭제할 계정 없음.");
            onResult?.Invoke(false);
            yield break;
        }

        bool completed = false;
        bool succeeded = false;

        _auth.CurrentUser.DeleteAsync().ContinueWith(task =>
        {
            succeeded = !task.IsFaulted && !task.IsCanceled;
            if (task.IsFaulted)
                Debug.LogWarning("[Auth] 계정 삭제 실패: " + task.Exception?.Message);
            completed = true;
        });

        yield return new WaitUntil(() => completed);

        if (succeeded)
        {
            UserUID = null;
            UserEmail = null;
            UserDisplayName = null;
            LastSignInWasGoogle = false;
            IsSigningIn = false;
            OnLogout?.Invoke();
        }

        onResult?.Invoke(succeeded);
#else
        // Firebase 미설정 환경에서는 바로 성공 처리
        UserUID = null;
        UserEmail = null;
        UserDisplayName = null;
        LastSignInWasGoogle = false;
        IsSigningIn = false;
        OnLogout?.Invoke();
        onResult?.Invoke(true);
        yield break;
#endif
    }

    // 추가: 조규민 - 로그인 실패 이벤트 발행 지점을 공통화해 Editor/Firebase define 차이로 생기는 경고를 줄인다.
    private void RaiseLoginFailed(string message)
    {
        RaiseLoginFailed(AuthLoginFailureType.General, message);
    }

    // 추가: 조규민 - UI에서 Google 실패 안내 문구를 구분할 수 있도록 실패 유형을 함께 전달한다.
    private void RaiseLoginFailed(AuthLoginFailureType failureType, string message)
    {
        OnLoginFailedWithType?.Invoke(failureType, message);
        OnLoginFailed?.Invoke(message);
    }

#if FIREBASE_ENABLED
    private string GetGoogleSignInValidationError()
    {
        if (!CanUseFirebaseAuth())
        {
            return "Firebase 인증 서비스가 준비되지 않았습니다.";
        }

        _resolvedWebClientId = ResolveWebClientId();
        if (string.IsNullOrWhiteSpace(_resolvedWebClientId) || _resolvedWebClientId == "FIREBASE_ENABLED")
        {
            return "Google Web Client ID가 설정되지 않았습니다. FirebaseAuthService 또는 google-services.xml의 default_web_client_id를 확인해 주세요.";
        }

        if (Application.isEditor)
        {
            return "Google Sign-In 플러그인은 Unity Editor 로그인을 지원하지 않습니다. Android 빌드 후 실기기에서 테스트해 주세요.";
        }

        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            return "실제 Google 로그인은 Android/iOS 기기 빌드에서만 사용할 수 있습니다.";
        }

        return null;
    }

    private void ConfigureGoogleSignIn()
    {
        _resolvedWebClientId = ResolveWebClientId();
        Debug.Log("[Auth][GoogleSignIn] ConfigureGoogleSignIn WebClientId=" + MaskWebClientId(_resolvedWebClientId));

        if (_hasGoogleSignInInstance)
        {
            if (!string.Equals(_configuredGoogleWebClientId, _resolvedWebClientId, StringComparison.Ordinal))
            {
                Debug.LogWarning("[Auth][GoogleSignIn] 이미 생성된 GoogleSignIn 인스턴스가 있어 기존 WebClientId를 유지합니다. Existing=" + MaskWebClientId(_configuredGoogleWebClientId) + ", Requested=" + MaskWebClientId(_resolvedWebClientId));
            }

            GoogleSignIn.DefaultInstance.EnableDebugLogging(Debug.isDebugBuild);
            return;
        }

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            ForceTokenRefresh = true,
            RequestEmail = true,
            RequestProfile = true,
            RequestIdToken = true,
            WebClientId = _resolvedWebClientId
        };

        GoogleSignIn.DefaultInstance.EnableDebugLogging(Debug.isDebugBuild);
        _configuredGoogleWebClientId = _resolvedWebClientId;
        _hasGoogleSignInInstance = true;
    }

    private string ResolveWebClientId()
    {
        if (!string.IsNullOrWhiteSpace(_webClientId) && _webClientId != "FIREBASE_ENABLED")
        {
            Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=Inspector/manual field value=" + MaskWebClientId(_webClientId));
            return _webClientId.Trim();
        }
        Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=Inspector/manual field empty");

        string androidResourceClientId = TryGetAndroidResourceString("default_web_client_id");
        if (!string.IsNullOrWhiteSpace(androidResourceClientId))
        {
            Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=Android resource default_web_client_id value=" + MaskWebClientId(androidResourceClientId));
            return androidResourceClientId.Trim();
        }
        Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=Android resource default_web_client_id empty");

        string generatedXmlClientId = TryGetGeneratedGoogleServicesValue("default_web_client_id");
        if (!string.IsNullOrWhiteSpace(generatedXmlClientId))
        {
            Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=generated google-services.xml value=" + MaskWebClientId(generatedXmlClientId));
            return generatedXmlClientId.Trim();
        }

        string googleSignInJsonClientId = TryGetGoogleServicesJsonWebClientId();
        if (!string.IsNullOrWhiteSpace(googleSignInJsonClientId))
        {
            Debug.Log("[Auth][GoogleSignIn] ResolveWebClientId source=Assets/GoogleSignIn/google-services.json value=" + MaskWebClientId(googleSignInJsonClientId));
            return googleSignInJsonClientId.Trim();
        }

        Debug.LogWarning("[Auth][GoogleSignIn] ResolveWebClientId failed: default_web_client_id is null/empty in all sources.");

        return null;
    }

    private string TryGetAndroidResourceString(string resourceName)
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.Log("[Auth][GoogleSignIn] Android resource lookup skipped. platform=" + Application.platform);
            return null;
        }

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            {
                string packageName = activity.Call<string>("getPackageName");
                int resourceId = resources.Call<int>("getIdentifier", resourceName, "string", packageName);
                Debug.Log("[Auth][GoogleSignIn] Android resource lookup name=" + resourceName + ", package=" + packageName + ", resourceId=" + resourceId);
                return resourceId == 0 ? null : resources.Call<string>("getString", resourceId);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Auth][GoogleSignIn] Android resource lookup failed: " + exception);
            Debug.LogWarning("[Auth] Android 리소스에서 Google Web Client ID를 읽지 못했습니다: " + exception.Message);
            return null;
        }
    }

    private string TryGetGeneratedGoogleServicesValue(string resourceName)
    {
        string path = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml");
        if (!File.Exists(path))
        {
            Debug.Log("[Auth][GoogleSignIn] generated google-services.xml not found. path=" + path);
            return null;
        }

        Debug.Log("[Auth][GoogleSignIn] generated google-services.xml lookup path=" + path + ", name=" + resourceName);
        string xml = File.ReadAllText(path);
        string marker = "name=\"" + resourceName + "\"";
        int markerIndex = xml.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            Debug.Log("[Auth][GoogleSignIn] generated google-services.xml missing string name=" + resourceName);
            return null;
        }

        int valueStart = xml.IndexOf('>', markerIndex);
        int valueEnd = valueStart < 0 ? -1 : xml.IndexOf("</string>", valueStart, StringComparison.Ordinal);
        if (valueStart < 0 || valueEnd < 0 || valueEnd <= valueStart)
        {
            Debug.LogWarning("[Auth][GoogleSignIn] generated google-services.xml invalid string format. name=" + resourceName);
            return null;
        }

        return xml.Substring(valueStart + 1, valueEnd - valueStart - 1);
    }

    // 추가: 조규민 - Assets/GoogleSignIn/google-services.json을 사용하는 프로젝트 기준에 맞춰 Web Client ID 보조 해석 경로를 둔다.
    private string TryGetGoogleServicesJsonWebClientId()
    {
        string path = Path.Combine(Application.dataPath, "GoogleSignIn/google-services.json");
        if (!File.Exists(path))
        {
            Debug.Log("[Auth][GoogleSignIn] google-services.json fallback not found. path=" + path);
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            string clientTypeMarker = "\"client_type\": 3";
            int clientTypeIndex = json.IndexOf(clientTypeMarker, StringComparison.Ordinal);
            if (clientTypeIndex < 0)
            {
                Debug.LogWarning("[Auth][GoogleSignIn] google-services.json fallback missing client_type 3 web client.");
                return null;
            }

            int clientIdKeyIndex = json.LastIndexOf("\"client_id\"", clientTypeIndex, StringComparison.Ordinal);
            if (clientIdKeyIndex < 0)
            {
                Debug.LogWarning("[Auth][GoogleSignIn] google-services.json fallback missing client_id before client_type 3.");
                return null;
            }

            return ReadJsonStringValue(json, clientIdKeyIndex);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Auth][GoogleSignIn] google-services.json fallback read failed: " + exception.Message);
            return null;
        }
    }

    private string ReadJsonStringValue(string json, int keyIndex)
    {
        int colonIndex = json.IndexOf(':', keyIndex);
        int valueStart = colonIndex < 0 ? -1 : json.IndexOf('"', colonIndex + 1);
        int valueEnd = valueStart < 0 ? -1 : json.IndexOf('"', valueStart + 1);
        if (valueStart < 0 || valueEnd < 0 || valueEnd <= valueStart)
        {
            return null;
        }

        return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
    }

    private IEnumerator WaitForTask(Task task, float timeoutSeconds, Action<bool> onCompleted)
    {
        float elapsedTime = 0f;
        while (!task.IsCompleted && elapsedTime < timeoutSeconds)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        onCompleted?.Invoke(task.IsCompleted);
    }

    private void CompleteFailedSignIn(AuthLoginFailureType failureType, string message)
    {
        IsSigningIn = false;
        RaiseLoginFailed(failureType, message);
    }

    private void CompleteFailedSignIn(string message)
    {
        CompleteFailedSignIn(AuthLoginFailureType.General, message);
    }

    // 추가: 조규민 - FirebaseAuth 사용 가능 여부를 한 곳에서 확인한다.
    private bool CanUseFirebaseAuth()
    {
        return IsFirebaseReady && _auth != null;
    }

    private void ApplyFirebaseUser(FirebaseUser user, bool isGoogleUser)
    {
        if (user == null)
        {
            return;
        }

        UserUID = user.UserId;
        UserEmail = user.Email;
        UserDisplayName = user.DisplayName;
        LastSignInWasGoogle = isGoogleUser;
    }

    private bool HasGoogleProvider(FirebaseUser user)
    {
        if (user == null)
        {
            return false;
        }

        foreach (var providerData in user.ProviderData)
        {
            if (providerData != null && providerData.ProviderId == "google.com")
            {
                return true;
            }
        }

        return false;
    }

    // 추가: 조규민 - AggregateException 메시지를 UI에 전달 가능한 문자열로 정리한다.
    private string GetExceptionMessage(Exception exception, string fallbackMessage)
    {
        if (exception == null)
        {
            return fallbackMessage;
        }

        if (exception.InnerException != null)
        {
            return exception.InnerException.Message;
        }

        return exception.Message;
    }

    private string GetFirebaseAuthExceptionMessage(Exception exception, string fallbackMessage)
    {
        var firebaseException = GetInnerException<FirebaseException>(exception);
        if (firebaseException == null)
        {
            return GetExceptionMessage(exception, fallbackMessage);
        }

        string authErrorName = GetAuthErrorName(firebaseException);
        switch (authErrorName)
        {
            case "InvalidEmail":
                return "Editor Email/Password 테스트 이메일 형식이 올바르지 않습니다.";
            case "WrongPassword":
                return "Editor Email/Password 테스트 비밀번호가 기존 계정과 일치하지 않습니다.";
            case "InvalidCredential":
                return "Editor Email/Password 테스트 계정 정보가 올바르지 않습니다. 기존 계정이면 비밀번호를 확인해 주세요.";
            case "InternalError":
                return "Firebase Auth 내부 오류가 발생했습니다. Email/Password 제공업체, Firebase 프로젝트 연결, 네트워크 상태를 확인해 주세요.";
            case "Failure":
                return "Firebase Auth 요청이 실패했습니다. Email/Password 제공업체, Firebase 프로젝트 연결, 네트워크 상태를 확인해 주세요.";
            case "EmailAlreadyInUse":
                return "Editor Email/Password 테스트 계정이 이미 있습니다. 기존 계정 비밀번호로 다시 로그인해 주세요.";
            case "WeakPassword":
                return "Editor Email/Password 테스트 비밀번호가 너무 약합니다. 6자 이상으로 다시 입력해 주세요.";
            case "OperationNotAllowed":
                return "Firebase Console에서 Email/Password 로그인 제공업체가 꺼져 있습니다.";
            case "NetworkRequestFailed":
                return "Firebase Auth 네트워크 요청에 실패했습니다. 인터넷 연결을 확인해 주세요.";
            case "TooManyRequests":
                return "Firebase Auth 요청이 너무 많습니다. 잠시 후 다시 시도해 주세요.";
            default:
                return fallbackMessage + " (" + authErrorName + ": " + firebaseException.Message + ")";
        }
    }

    private string GetAuthErrorName(FirebaseException firebaseException)
    {
        if (firebaseException == null)
        {
            return string.Empty;
        }

        return ((AuthError)firebaseException.ErrorCode).ToString();
    }

    private string GetGoogleSignInExceptionMessage(Exception exception)
    {
        var signInException = GetInnerException<GoogleSignIn.SignInException>(exception);
        if (signInException == null)
        {
            return GetExceptionMessage(exception, "구글 로그인에 실패했습니다.");
        }

        switch (signInException.Status)
        {
            case GoogleSignInStatusCode.Canceled:
                return "구글 로그인이 취소되었습니다.";
            case GoogleSignInStatusCode.NetworkError:
                return "구글 로그인 네트워크 오류가 발생했습니다. 연결 상태를 확인해 주세요.";
            case GoogleSignInStatusCode.DeveloperError:
                return "구글 로그인 설정 오류입니다. 패키지명, SHA-1/SHA-256, Web Client ID를 확인해 주세요.";
            case GoogleSignInStatusCode.Timeout:
                return "구글 로그인 응답 시간이 초과되었습니다.";
            default:
                return "구글 로그인 실패: " + signInException.Status;
        }
    }

    private AuthLoginFailureType GetGoogleSignInFailureType(Exception exception)
    {
        var signInException = GetInnerException<GoogleSignIn.SignInException>(exception);
        if (signInException == null)
        {
            return AuthLoginFailureType.General;
        }

        switch (signInException.Status)
        {
            case GoogleSignInStatusCode.Canceled:
                return AuthLoginFailureType.Canceled;
            case GoogleSignInStatusCode.NetworkError:
                return AuthLoginFailureType.Network;
            case GoogleSignInStatusCode.DeveloperError:
                return AuthLoginFailureType.Configuration;
            case GoogleSignInStatusCode.Timeout:
                return AuthLoginFailureType.Timeout;
            default:
                return AuthLoginFailureType.General;
        }
    }

    private AuthLoginFailureType GetValidationFailureType(string validationError)
    {
        if (validationError.Contains("Firebase"))
        {
            return AuthLoginFailureType.FirebaseAuth;
        }

        if (validationError.Contains("Web Client ID") || validationError.Contains("설정"))
        {
            return AuthLoginFailureType.Configuration;
        }

        return AuthLoginFailureType.General;
    }

    private void LogExceptionDetails(string title, Exception exception)
    {
        if (exception == null)
        {
            Debug.LogError(title + ": exception=null");
            return;
        }

        Debug.LogError(title + ": " + exception);

        var signInException = GetInnerException<GoogleSignIn.SignInException>(exception);
        if (signInException != null)
        {
            Debug.LogError("[Auth][GoogleSignIn] Status=" + signInException.Status + " (" + (int)signInException.Status + ")");
            if (signInException.Status == GoogleSignInStatusCode.DeveloperError)
            {
                Debug.LogError("[Auth][GoogleSignIn] DeveloperError detected. Check package name, SHA-1/SHA-256, OAuth client, and WebClientId=" + MaskWebClientId(_resolvedWebClientId));
            }
        }

        var firebaseException = GetInnerException<FirebaseException>(exception);
        if (firebaseException != null)
        {
            Debug.LogError("[Auth][Firebase] ErrorCode=" + firebaseException.ErrorCode + ", AuthError=" + GetAuthErrorName(firebaseException));
        }

        var aggregateException = exception as AggregateException;
        if (aggregateException == null)
        {
            return;
        }

        int index = 0;
        foreach (var innerException in aggregateException.Flatten().InnerExceptions)
        {
            Debug.LogError("[Auth] AggregateException.InnerExceptions[" + index + "]: " + innerException);

            var innerSignInException = innerException as GoogleSignIn.SignInException;
            if (innerSignInException != null)
            {
                Debug.LogError("[Auth][GoogleSignIn] Inner status=" + innerSignInException.Status + " (" + (int)innerSignInException.Status + ")");
            }

            index++;
        }
    }

    private string MaskWebClientId(string webClientId)
    {
        if (string.IsNullOrWhiteSpace(webClientId))
        {
            return "<null-or-empty>";
        }

        string trimmedClientId = webClientId.Trim();
        string googleDomain = ".apps.googleusercontent.com";
        int domainIndex = trimmedClientId.IndexOf(googleDomain, StringComparison.Ordinal);
        string suffix = domainIndex >= 0 ? trimmedClientId.Substring(domainIndex) : trimmedClientId.Substring(Math.Max(0, trimmedClientId.Length - 20));
        string prefix = trimmedClientId.Substring(0, Math.Min(10, trimmedClientId.Length));
        return prefix + "..." + suffix;
    }

    private T GetInnerException<T>(Exception exception) where T : Exception
    {
        if (exception == null)
        {
            return null;
        }

        if (exception is T matchedException)
        {
            return matchedException;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                var matchedAggregateException = GetInnerException<T>(innerException);
                if (matchedAggregateException != null)
                {
                    return matchedAggregateException;
                }
            }
        }

        return GetInnerException<T>(exception.InnerException);
    }
#endif
}
