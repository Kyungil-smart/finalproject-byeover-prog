// 담당자 : 정승우
// 설명   : Boot 씬 초기화 - 앱 시작 시 전부 여기서 순서대로 세팅

// 1차 수정자 : 김영찬
// 수정 내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정


using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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
        if (!GameManager.Instance.IsOfflineMode)
        {
            if (GameManager.Instance.HasPreviousSession())
            {
                yield return StartCoroutine(GameManager.Instance.AutoSignIn());
                Debug.Log("[Bootstrap] 자동 로그인 완료");
            }
            else
            {
                // 로그인 화면 표시하고 유저가 버튼 누를 때까지 대기
                GameManager.Instance.ShowLoginUI();

                // TODO [2026-05-21 정승우] #후순위
                // 지금은 로그인 UI가 없어서 바로 게스트 로그인으로 넘김
                // 나중에 Login 씬이나 로그인 팝업이 만들어지면 대기 로직으로 바꿔야 함
                GameManager.Instance.StartGuestSignIn();
                yield return new WaitUntil(() => GameManager.Instance.IsLoggedIn);
                Debug.Log("[Bootstrap] 로그인 완료");
            }
        }
        else
        {
            Debug.Log("[Bootstrap] 오프라인 모드 -- 로그인 건너뜀");
        }

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
