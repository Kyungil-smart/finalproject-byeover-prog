// 담당자 : 정승우
// 설명   : Boot 씬 초기화 - 앱 시작 시 전부 여기서 순서대로 세팅

// 1차 수정자 : 김영찬
// 수정 내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정

// 2차 수정자 : 조규민
// 수정 내용 : 게스트 로그인 자동 실행/무한 대기 제거, 로그인 성공/실패 이벤트 기반 분기 추가
// 3차 수정자 : 조규민
// 수정 내용 : Inspector 연결 기반으로 기본 화면 터치 후 로그인 화면이 표시되도록 진입 흐름 분리
// 4차 수정자 : 조규민
// 수정 내용 : 로비 씬 진입 전 Inspector에 연결한 로딩 애니메이션 재생 흐름 추가
// 5차 수정자 : 조규민
// 수정 내용 : 이전 세션 자동 로그인으로 시작 터치 화면을 건너뛰지 않도록 Inspector 옵션 분리

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
    [Tooltip("켜면 Firebase 이전 세션이 있을 때 시작 터치/로그인 화면을 건너뛰고 자동으로 로비에 진입합니다.")]
    [SerializeField] private bool _usePreviousSessionAutoSignIn; // 추가: 조규민 - 기본 실행에서는 시작 화면을 보여주기 위해 이전 세션 자동 로그인을 옵션화한다.
    [SerializeField] private LoginView _loginView; // 추가: 조규민 - Boot 씬에 직접 배치한 LoginCanvas의 LoginView를 Inspector에서 연결한다.
    [Tooltip("기본 화면에서 첫 터치를 받는 View입니다. StartTouchCanvas에 연결된 StartTouchView를 지정합니다.")]
    [SerializeField] private StartTouchView _startTouchView; // 추가: 조규민 - Boot 씬에 직접 배치한 StartTouchCanvas의 StartTouchView를 Inspector에서 연결한다.

    [Header("로딩 애니메이션")]
    [Tooltip("로비 씬 진입 전에 재생할 로딩 GIF 프레임 View입니다. LoadingVideoCanvas의 BootLoadingVideoView를 연결합니다.")]
    [SerializeField] private BootLoadingVideoView _loadingVideoView; // 추가: 조규민 - 로비 진입 전 GIF 프레임 애니메이션 View를 Inspector에서 연결한다.

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
        if (_loginView != null)
        {
            _loginView.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[Bootstrap] LoginView 참조가 없습니다. Boot Inspector 연결을 확인해 주세요.", this);
        }

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

        if (_usePreviousSessionAutoSignIn && !GameManager.Instance.IsOfflineMode && GameManager.Instance.HasPreviousSession())
        {
            yield return StartCoroutine(GameManager.Instance.AutoSignIn());
        }
        else if (_autoGuestSignInForDevelopment)
        {
            GameManager.Instance.StartGuestSignIn();
        }
        else
        {
            if (_loginView == null || _startTouchView == null)
            {
                Debug.LogWarning("[Bootstrap] 기본 화면 터치 흐름에 필요한 View 참조가 없습니다. Boot Inspector 연결을 확인해 주세요.", this);
                yield break;
            }

            yield return StartCoroutine(WaitForStartTouch());
            ShowConnectedLoginView();
            GameManager.Instance.ShowLoginUI();
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

        // [7] 로비 진입 전 로딩 애니메이션
        yield return StartCoroutine(PlayLobbyLoadingVideo());

        // [8] 로비 진입
        Debug.Log("[Bootstrap] === 초기화 완료. Lobby로 이동 ===");
        GameManager.Instance.LoadLobby();
    }

    private IEnumerator WaitForStartTouch()
    {
        bool hasTouched = false;

        // 추가: 조규민 - StartTouchView 터치 이벤트를 코루틴 대기 조건으로 변환한다.
        void HandleStartTouched()
        {
            hasTouched = true;
        }

        // 추가: 조규민 - 기본 화면 위의 전체 터치 Canvas를 켜고 첫 터치가 들어올 때까지 대기한다.
        _startTouchView.OnStartTouched += HandleStartTouched;
        _startTouchView.gameObject.SetActive(true);
        yield return new WaitUntil(() => hasTouched);
        _startTouchView.OnStartTouched -= HandleStartTouched;
    }

    // 추가: 조규민 - Inspector에 연결된 LoginCanvas를 활성화해 약관 동의 모달이 포함된 로그인 화면을 표시한다.
    private void ShowConnectedLoginView()
    {
        if (_loginView == null)
        {
            Debug.LogWarning("[Bootstrap] LoginView 참조가 없어 로그인 화면을 표시할 수 없습니다.", this);
            return;
        }

        _loginView.gameObject.SetActive(true);
    }

    // 추가: 조규민 - 로비 씬으로 넘어가기 전에 Inspector에 연결된 로딩 GIF 프레임 애니메이션을 재생한다.
    private IEnumerator PlayLobbyLoadingVideo()
    {
        if (_loadingVideoView == null)
        {
            yield break;
        }

        if (_loginView != null)
        {
            _loginView.gameObject.SetActive(false);
        }

        yield return StartCoroutine(_loadingVideoView.Play());
    }
}
