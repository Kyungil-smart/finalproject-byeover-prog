// 담당자 : 정승우
// 설명   : Boot 씬 초기화 - 앱 시작 시 전부 여기서 순서대로 세팅

// 1차 수정자 : 김영찬
// 수정 내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정

// 2차 수정자 : 조규민
// 수정 내용 : 게스트 로그인 자동 실행/무한 대기 제거, 로그인 성공/실패 이벤트 기반 분기 추가


using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Boot 씬에서 모든 매니저와 데이터를 의존성 순서대로 초기화한 뒤 Lobby 씬으로 전환한다.
/// 이 순서를 바꾸면 안 됨. Repository -> Localization -> Pool -> Firebase -> Login -> Lobby.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("시스템")]
    [SerializeField] private DataManager _data;
    [SerializeField] private LocalizationManager _localization;
    [SerializeField] private PoolManager _poolManager;

    [Header("로그인")]
    [Tooltip("로그인 UI 연결 전 개발 확인용 자동 게스트 로그인 여부")]
    [SerializeField] private bool _autoGuestSignInForDevelopment; // 추가: 조규민 - Login UI 연결 전 테스트할 때만 Inspector에서 켠다.

    // ---------- 시작 ----------
    private void Start()
    {
        StartCoroutine(InitializeAll());
    }

    private IEnumerator InitializeAll()
    {
        Debug.Log("[Bootstrap] === 초기화 시작 ===");

        // [1] Repository 초기화
        // SO는 이미 메모리에 올라와있어서 파싱 없이 Dictionary 변환만 함. 빠름.
        _data.InitRepo();
        Debug.Log("[Bootstrap] Repository 초기화 완료");

        // [2] 로컬라이제이션
        _localization.Initialize();
        Debug.Log("[Bootstrap] Localization 초기화 완료");

        // [3] 오브젝트 풀 사전 생성
        // 여기서 Instantiate가 한번에 몰리니까 로딩 화면에서 처리하면 좋음
        _poolManager.WarmUp();
        yield return null;  // 1프레임 양보해서 로딩 화면이 렌더링될 시간 줌
        Debug.Log("[Bootstrap] PoolManager WarmUp 완료");

        // [4] Firebase 초기화
        yield return StartCoroutine(GameManager.Instance.InitializeFirebase());
        Debug.Log("[Bootstrap] Firebase 초기화 완료 (오프라인: " + GameManager.Instance.IsOfflineMode + ")");

        // [5] 로그인 처리
        LoginRuntimeUIFactory.EnsureExists(); // 추가: 조규민 - 씬에 LoginView가 없으면 최소 로그인 UI를 생성한다.
        GameManager.Instance.ShowLoginUI();

        // 추가: 조규민 - Google 이전 세션도 회원가입 프로필 확인을 거친 뒤에만 로비로 이동한다.
        bool loginCompleted = false;
        bool loginSucceeded = false;
        string loginError = null;

        Action<string> onLoginSucceeded = null;
        Action<string> onLoginFailed = null;

        onLoginSucceeded = (uid) =>
        {
            loginSucceeded = true;
            loginCompleted = true;
        };

        onLoginFailed = (error) =>
        {
            loginError = error;
            loginCompleted = true;
        };

        GameManager.Instance.OnLoginSucceeded += onLoginSucceeded;
        GameManager.Instance.OnLoginFailed += onLoginFailed;

        if (!GameManager.Instance.IsOfflineMode && GameManager.Instance.HasPreviousSession())
        {
            yield return StartCoroutine(GameManager.Instance.AutoSignIn());
        }
        else if (_autoGuestSignInForDevelopment)
        {
            GameManager.Instance.StartGuestSignIn();
        }

        yield return new WaitUntil(() => loginCompleted);

        GameManager.Instance.OnLoginSucceeded -= onLoginSucceeded;
        GameManager.Instance.OnLoginFailed -= onLoginFailed;

        if (!loginSucceeded)
        {
            Debug.LogWarning("[Bootstrap] 로그인 실패. 로그인 화면 유지: " + loginError);
            yield break;
        }

        Debug.Log("[Bootstrap] 로그인 완료");

        // [6] 클라우드 데이터 로드
        if (GameManager.Instance.IsLoggedIn)
        {
            yield return StartCoroutine(GameManager.Instance.LoadCloudData());
        }
        else
        {
            GameManager.Instance.LoadLocalProgress();
        }
        Debug.Log("[Bootstrap] 데이터 로드 완료");

        // [7] 로비 진입
        Debug.Log("[Bootstrap] === 초기화 완료. Lobby로 이동 ===");
        GameManager.Instance.LoadLobby();
    }
}
