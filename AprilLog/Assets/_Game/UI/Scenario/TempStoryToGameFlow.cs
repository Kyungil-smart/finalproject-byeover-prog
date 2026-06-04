// 담당자 : 홍정옥
// 설명   : [임시] 시나리오(_Story) 종료 시 게임 씬(_InGame)으로 전환 (테스트 빌드용, 나중에 삭제)

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
        // GameManager가 있으면 정식 흐름(상태 전환 포함), 없으면 직접 로드
        if (GameManager.Instance != null)
            GameManager.Instance.LoadInGame();
        else
            SceneManager.LoadScene(_nextScene);
    }
}
