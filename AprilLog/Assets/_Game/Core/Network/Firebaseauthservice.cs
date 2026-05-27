// 담당자 : 정승우
// 설명   : Firebase 인증 서비스 -- 구글 로그인(Google Sign-In) + 게스트 로그인
// 2차 수정자 : 조규민
// 수정 내용 : 게스트 로그인 실패 처리, Firebase 초기화 실패 이벤트, 중복 로그인 요청 방어 추가

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
#pragma warning disable CS0067 // 추가: 조규민 - Editor에서 FIREBASE_ENABLED가 꺼진 경우에도 Android 빌드용 실패 이벤트를 유지한다.
    public event Action<string> OnLoginFailed;
#pragma warning restore CS0067
    public event Action OnLogout;

    [Header("설정")]
    [SerializeField] private string _webClientId;

    public string UserUID { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(UserUID);
    public bool IsFirebaseReady { get; private set; }
    public bool IsSigningIn { get; private set; } // 추가: 조규민 - 로그인 중복 요청을 막기 위한 상태값

#if FIREBASE_ENABLED
    private FirebaseAuth _auth;
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
        // 추가: 조규민 - Google 로그인은 추후 구현 대상이지만 중복 실행 방어는 동일하게 둔다.
        if (IsSigningIn)
        {
            yield break;
        }

        if (!CanUseFirebaseAuth())
        {
            RaiseLoginFailed("Firebase 인증 서비스가 준비되지 않았습니다.");
            yield break;
        }

        IsSigningIn = true;

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            WebClientId = _webClientId
        };
        var signInTask = GoogleSignIn.DefaultInstance.SignIn();
        yield return new WaitUntil(() => signInTask.IsCompleted);
        if (signInTask.IsFaulted || signInTask.IsCanceled)
        {
            IsSigningIn = false;
            RaiseLoginFailed(GetExceptionMessage(signInTask.Exception, "구글 로그인이 취소되었습니다."));
            yield break;
        }

        var credential = GoogleAuthProvider.GetCredential(signInTask.Result.IdToken, null);
        var authTask = _auth.SignInWithCredentialAsync(credential);
        yield return new WaitUntil(() => authTask.IsCompleted);
        if (authTask.IsFaulted)
        {
            IsSigningIn = false;
            RaiseLoginFailed(GetExceptionMessage(authTask.Exception, "Firebase 인증 실패"));
            yield break;
        }

        UserUID = authTask.Result.User.UserId;
        IsSigningIn = false;
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
        // 추가: 조규민 - 게스트 로그인 중복 실행과 Firebase 미초기화 상태를 방어한다.
        if (IsSigningIn)
        {
            yield break;
        }

        if (!CanUseFirebaseAuth())
        {
            RaiseLoginFailed("Firebase 인증 서비스가 준비되지 않았습니다.");
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

        UserUID = task.Result.User.UserId;
        IsSigningIn = false;
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
        OnLoginFailed?.Invoke(message);
    }

#if FIREBASE_ENABLED
    // 추가: 조규민 - FirebaseAuth 사용 가능 여부를 한 곳에서 확인한다.
    private bool CanUseFirebaseAuth()
    {
        return IsFirebaseReady && _auth != null;
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
#endif
}
