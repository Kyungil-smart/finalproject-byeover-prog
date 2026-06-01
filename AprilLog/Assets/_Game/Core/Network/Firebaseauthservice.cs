// 담당자 : 정승우
// 설명   : Firebase 인증 서비스 -- 구글 로그인(Google Sign-In) + 게스트 로그인

// 2차 수정자 : 조규민
// 수정 내용 : 게스트/Firebase 초기화 실패 처리, 중복 로그인 방어, Google 설정 검증, Web Client ID 자동 해석, 로그인 실패 유형 전달 추가

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

    public string UserUID { get; private set; }
    public string UserEmail { get; private set; }
    public string UserDisplayName { get; private set; }
    public bool LastSignInWasGoogle { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(UserUID);
    public bool IsFirebaseReady { get; private set; }
    public bool IsSigningIn { get; private set; } // 추가: 조규민 - 로그인 중복 요청을 막기 위한 상태값

#if FIREBASE_ENABLED
    private FirebaseAuth _auth;
    private string _resolvedWebClientId;
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

    public IEnumerator GoogleSignInCoroutine()
    {
#if FIREBASE_ENABLED
        if (IsSigningIn)
        {
            yield break;
        }

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
            CompleteFailedSignIn(GetGoogleSignInFailureType(exception), GetGoogleSignInExceptionMessage(exception));
            yield break;
        }

        bool googleCompleted = false;
        yield return StartCoroutine(WaitForTask(signInTask, _googleSignInTimeoutSeconds, result => googleCompleted = result));
        if (!googleCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.General, "Google 로그인 응답 시간이 초과되었습니다. 네트워크 상태와 Google Play Services 상태를 확인해 주세요.");
            yield break;
        }

        if (signInTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.Canceled, "구글 로그인이 취소되었습니다.");
            yield break;
        }

        if (signInTask.IsFaulted)
        {
            CompleteFailedSignIn(GetGoogleSignInFailureType(signInTask.Exception), GetGoogleSignInExceptionMessage(signInTask.Exception));
            yield break;
        }

        if (signInTask.Result == null || string.IsNullOrEmpty(signInTask.Result.IdToken))
        {
            CompleteFailedSignIn(AuthLoginFailureType.General, "Google ID Token을 받지 못했습니다. Web Client ID와 SHA-1/SHA-256 설정을 확인해 주세요.");
            yield break;
        }

        yield return StartCoroutine(SignInFirebaseWithGoogleToken(signInTask.Result.IdToken));
#else
        RaiseLoginFailed(AuthLoginFailureType.General, "실제 Google 로그인은 Android 빌드에서만 사용할 수 있습니다. Android로 Switch Platform 후 기기에서 테스트해 주세요.");
        yield return null;
#endif
    }

#if FIREBASE_ENABLED
    private IEnumerator SignInFirebaseWithGoogleToken(string idToken)
    {
        var credential = GoogleAuthProvider.GetCredential(idToken, null);
        var authTask = _auth.SignInWithCredentialAsync(credential);
        bool firebaseCompleted = false;
        yield return StartCoroutine(WaitForTask(authTask, _firebaseAuthTimeoutSeconds, result => firebaseCompleted = result));
        if (!firebaseCompleted)
        {
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, "Firebase Google 인증 시간이 초과되었습니다. Firebase Authentication 제공업체와 네트워크 상태를 확인해 주세요.");
            yield break;
        }

        if (authTask.IsCanceled)
        {
            CompleteFailedSignIn(AuthLoginFailureType.FirebaseAuth, "Firebase Google 인증이 취소되었습니다.");
            yield break;
        }

        if (authTask.IsFaulted)
        {
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
        try
        {
            GoogleSignIn.DefaultInstance.SignOut();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Auth] Google Sign-In 로그아웃 정리 실패: " + exception.Message);
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
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            ForceTokenRefresh = true,
            RequestEmail = true,
            RequestProfile = true,
            RequestIdToken = true,
            WebClientId = _resolvedWebClientId
        };

        GoogleSignIn.DefaultInstance.EnableDebugLogging(Debug.isDebugBuild);
    }

    private string ResolveWebClientId()
    {
        if (!string.IsNullOrWhiteSpace(_webClientId) && _webClientId != "FIREBASE_ENABLED")
        {
            return _webClientId.Trim();
        }

        string androidResourceClientId = TryGetAndroidResourceString("default_web_client_id");
        if (!string.IsNullOrWhiteSpace(androidResourceClientId))
        {
            return androidResourceClientId.Trim();
        }

        string generatedXmlClientId = TryGetGeneratedGoogleServicesValue("default_web_client_id");
        if (!string.IsNullOrWhiteSpace(generatedXmlClientId))
        {
            return generatedXmlClientId.Trim();
        }

        return null;
    }

    private string TryGetAndroidResourceString(string resourceName)
    {
        if (Application.platform != RuntimePlatform.Android)
        {
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
                return resourceId == 0 ? null : resources.Call<string>("getString", resourceId);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Auth] Android 리소스에서 Google Web Client ID를 읽지 못했습니다: " + exception.Message);
            return null;
        }
    }

    private string TryGetGeneratedGoogleServicesValue(string resourceName)
    {
        string path = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml");
        if (!File.Exists(path))
        {
            return null;
        }

        string xml = File.ReadAllText(path);
        string marker = "name=\"" + resourceName + "\"";
        int markerIndex = xml.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        int valueStart = xml.IndexOf('>', markerIndex);
        int valueEnd = valueStart < 0 ? -1 : xml.IndexOf("</string>", valueStart, StringComparison.Ordinal);
        if (valueStart < 0 || valueEnd < 0 || valueEnd <= valueStart)
        {
            return null;
        }

        return xml.Substring(valueStart + 1, valueEnd - valueStart - 1);
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

        return signInException.Status == GoogleSignInStatusCode.Canceled
            ? AuthLoginFailureType.Canceled
            : AuthLoginFailureType.General;
    }

    private AuthLoginFailureType GetValidationFailureType(string validationError)
    {
        return validationError.Contains("Firebase")
            ? AuthLoginFailureType.FirebaseAuth
            : AuthLoginFailureType.General;
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
