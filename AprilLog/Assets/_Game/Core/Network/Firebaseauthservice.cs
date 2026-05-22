// 담당자 : 정승우
// 설명   : Firebase 인증 서비스 -- 구글 로그인(Google Sign-In) + 게스트 로그인

#if FIREBASE_ENABLED
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
#endif

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 구글 로그인(Google Sign-In)과 게스트 로그인(Firebase 익명 인증)을 처리한다.
/// GameManager가 이걸 통해서 인증함.
///
/// 담당자가 해야 할 것:
/// - Firebase Console > Authentication > Google 로그인 활성화
/// - google-signin-unity 플러그인 임포트
/// - Web Client ID를 Inspector에 입력
/// - FIREBASE_ENABLED define 추가
/// </summary>
public class FirebaseAuthService : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnLogout;

    // ---------- SerializeField ----------
    [Header("설정")]
    [Tooltip("google-services.json에서 oauth_client type=3의 client_id")]
    [SerializeField] private string _webClientId;

    // ---------- 상태 ----------
    public string UserUID { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(UserUID);
    public bool IsFirebaseReady { get; private set; }

#if FIREBASE_ENABLED
    private FirebaseAuth _auth;
#endif

    // ---------- Firebase 초기화 ----------
    public IEnumerator InitializeFirebase()
    {
#if FIREBASE_ENABLED
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result != DependencyStatus.Available)
        {
            Debug.LogError("[Auth] Firebase 사용 불가: " + task.Result);
            IsFirebaseReady = false;
            yield break;
        }

        _auth = FirebaseAuth.DefaultInstance;
        IsFirebaseReady = true;

        if (_auth.CurrentUser != null)
        {
            UserUID = _auth.CurrentUser.UserId;
            Debug.Log("[Auth] 이전 세션 복원됨: " + UserUID);
            OnLoginSuccess?.Invoke(UserUID);
        }
#else
        Debug.Log("[Auth] FIREBASE_ENABLED 꺼져있음. 더미 모드.");
        IsFirebaseReady = false;
        yield return null;
#endif
    }

    // ---------- 구글 로그인 ----------
    public IEnumerator GoogleSignInCoroutine()
    {
#if FIREBASE_ENABLED
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            WebClientId = _webClientId
        };

        var signInTask = GoogleSignIn.DefaultInstance.SignIn();
        yield return new WaitUntil(() => signInTask.IsCompleted);

        if (signInTask.IsFaulted || signInTask.IsCanceled)
        {
            string error = signInTask.Exception != null
                ? signInTask.Exception.Message
                : "유저가 취소함";
            Debug.LogWarning("[Auth] 구글 로그인 실패: " + error);
            OnLoginFailed?.Invoke(error);
            yield break;
        }

        string idToken = signInTask.Result.IdToken;

        var credential = GoogleAuthProvider.GetCredential(idToken, null);
        var authTask = _auth.SignInWithCredentialAsync(credential);
        yield return new WaitUntil(() => authTask.IsCompleted);

        if (authTask.IsFaulted)
        {
            string error = authTask.Exception != null
                ? authTask.Exception.Message
                : "Firebase 인증 실패";
            Debug.LogError("[Auth] Firebase 인증 실패: " + error);
            OnLoginFailed?.Invoke(error);
            yield break;
        }

        UserUID = authTask.Result.User.UserId;
        Debug.Log("[Auth] 구글 로그인 성공: " + UserUID);
        OnLoginSuccess?.Invoke(UserUID);
#else
        Debug.Log("[Auth] 구글 로그인 더미 모드");
        UserUID = "google_" + UnityEngine.Random.Range(10000, 99999);
        OnLoginSuccess?.Invoke(UserUID);
        yield return null;
#endif
    }

    // ---------- 게스트 로그인 ----------
    public IEnumerator GuestSignInCoroutine()
    {
#if FIREBASE_ENABLED
        var task = _auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            string error = task.Exception != null
                ? task.Exception.Message
                : "익명 인증 실패";
            Debug.LogError("[Auth] 게스트 로그인 실패: " + error);
            OnLoginFailed?.Invoke(error);
            yield break;
        }

        UserUID = task.Result.User.UserId;
        Debug.Log("[Auth] 게스트 로그인 성공: " + UserUID);
        OnLoginSuccess?.Invoke(UserUID);
#else
        Debug.Log("[Auth] 게스트 로그인 더미 모드");
        UserUID = "guest_" + UnityEngine.Random.Range(10000, 99999);
        OnLoginSuccess?.Invoke(UserUID);
        yield return null;
#endif
    }

    // ---------- 로그아웃 ----------
    public void SignOut()
    {
#if FIREBASE_ENABLED
        _auth?.SignOut();
#endif
        UserUID = null;
        Debug.Log("[Auth] 로그아웃");
        OnLogout?.Invoke();
    }

    // ---------- 유틸 ----------
    public bool HasPreviousSession()
    {
#if FIREBASE_ENABLED
        return _auth != null && _auth.CurrentUser != null;
#else
        return false;
#endif
    }
}