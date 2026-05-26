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

public class FirebaseAuthService : MonoBehaviour
{
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnLogout;

    [Header("설정")]
    [SerializeField] private string _webClientId;

    public string UserUID { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(UserUID);
    public bool IsFirebaseReady { get; private set; }

#if FIREBASE_ENABLED
    private FirebaseAuth _auth;
#endif

    public IEnumerator InitializeFirebase()
    {
#if FIREBASE_ENABLED
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.Result != DependencyStatus.Available)
        {
            Debug.LogError("[Auth] Firebase 사용 불가: " + task.Result);
            IsFirebaseReady = false;
            OnLoginFailed?.Invoke("Firebase 초기화 실패");
            yield break;
        }
        _auth = FirebaseAuth.DefaultInstance;
        IsFirebaseReady = true;
        if (_auth.CurrentUser != null)
        {
            UserUID = _auth.CurrentUser.UserId;
            OnLoginSuccess?.Invoke(UserUID);
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
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            WebClientId = _webClientId
        };
        var signInTask = GoogleSignIn.DefaultInstance.SignIn();
        yield return new WaitUntil(() => signInTask.IsCompleted);
        if (signInTask.IsFaulted || signInTask.IsCanceled)
        {
            OnLoginFailed?.Invoke(signInTask.Exception?.Message ?? "유저가 취소함");
            yield break;
        }
        var credential = GoogleAuthProvider.GetCredential(signInTask.Result.IdToken, null);
        var authTask = _auth.SignInWithCredentialAsync(credential);
        yield return new WaitUntil(() => authTask.IsCompleted);
        if (authTask.IsFaulted)
        {
            OnLoginFailed?.Invoke(authTask.Exception?.Message ?? "Firebase 인증 실패");
            yield break;
        }
        UserUID = authTask.Result.User.UserId;
        OnLoginSuccess?.Invoke(UserUID);
#else
        Debug.Log("[Auth] 구글 로그인 더미 모드");
        UserUID = "google_" + UnityEngine.Random.Range(10000, 99999);
        OnLoginSuccess?.Invoke(UserUID);
        yield return null;
#endif
    }

    public IEnumerator GuestSignInCoroutine()
    {
#if FIREBASE_ENABLED
        var task = _auth.SignInAnonymouslyAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.IsFaulted)
        {
            OnLoginFailed?.Invoke(task.Exception?.Message ?? "익명 인증 실패");
            yield break;
        }
        UserUID = task.Result.User.UserId;
        OnLoginSuccess?.Invoke(UserUID);
#else
        Debug.Log("[Auth] 게스트 로그인 더미 모드");
        UserUID = "guest_" + UnityEngine.Random.Range(10000, 99999);
        OnLoginSuccess?.Invoke(UserUID);
        yield return null;
#endif
    }

    public void SignOut()
    {
#if FIREBASE_ENABLED
        _auth?.SignOut();
#endif
        UserUID = null;
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
}