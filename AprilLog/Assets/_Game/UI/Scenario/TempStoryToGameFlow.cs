// 담당자 : 홍정옥
// 설명   : [임시] 시나리오(_Story) 종료 시 게임 씬(_InGame)으로 전환 (테스트 빌드용, 나중에 삭제)

// 1차 수정자 : 조규민
// 수정 내용 : 하우징 책장 다시보기 모드에서는 스토리 종료 후 전투가 아니라 로비로 복귀

using UnityEngine;
using UnityEngine.SceneManagement;

public class TempStoryToGameFlow : MonoBehaviour
{
    [SerializeField] private ScenarioDummyDriver _driver;
    [Tooltip("시나리오 종료 후 이동할 씬")]
    [SerializeField] private string _nextScene = "_InGame";

    private void Awake()
    {
        if (_driver == null)
            _driver = FindFirstObjectByType<ScenarioDummyDriver>();
    }

    private void OnEnable()
    {
        if (_driver != null)
            _driver.OnFinished += GoToGame;
    }

    private void OnDisable()
    {
        if (_driver != null)
            _driver.OnFinished -= GoToGame;
    }

    private void GoToGame()
    {
        // 추가:조규민 기능 설명: 하우징 책장 다시보기 모드에서는 스토리 종료 후 전투가 아니라 저장된 복귀 씬으로 이동한다.
        if (ReplayStorySelectionContext.IsReplayMode)
        {
            string returnSceneName = ReplayStorySelectionContext.ReturnSceneName;
            // 추가:조규민 기능 설명: 하우징 책장에서 시작한 다시보기는 로비 씬 복귀 후 하우징 페이지를 열도록 예약한다.
            if (returnSceneName == "_Lobby")
                LobbyReturnContext.RequestPage(LobbyPageType.Housing);

            // 추가:조규민 기능 설명: 복귀 전에 다시보기 상태를 초기화해 다음 일반 스토리 진입과 섞이지 않게 한다.
            ReplayStorySelectionContext.Clear();

            if (GameManager.Instance != null && returnSceneName == "_Lobby")
                GameManager.Instance.LoadLobby();
            else
                SceneManager.LoadScene(returnSceneName);

            return;
        }

        // GameManager가 있으면 정식 흐름(상태 전환 포함), 없으면 직접 로드
        if (GameManager.Instance != null)
            GameManager.Instance.LoadInGame();
        else
            SceneManager.LoadScene(_nextScene);
    }
}
