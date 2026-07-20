// 담당자 : 홍정옥
// 설명   : [임시] 시나리오(_Story) 종료 시 게임 씬(_InGame)으로 전환 (테스트 빌드용, 나중에 삭제)

// 1차 수정자 : 조규민
// 수정 내용 : 하우징 책장 다시보기 모드에서는 스토리 종료 후 전투가 아니라 로비로 복귀

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TempStoryToGameFlow : MonoBehaviour
{
    [SerializeField] private ScenarioDataDriver _driver;
    [Tooltip("시나리오 종료 후 이동할 씬")]
    [SerializeField] private string _nextScene = "_InGame";

    [Header("튜토리얼 진입 직전 컷")]
    [Tooltip("인게임 진입 전에 전체화면으로 한 번 보여줄 대사 ID(예: 100009 = BG 20116). 0이면 사용 안 함.")]
    [SerializeField] private int _preTutorialCutTalkId = 100009;
    [Tooltip("위 대사가 속한 GroupID.")]
    [SerializeField] private int _preTutorialCutGroupId = 3002;

    private bool _preTutorialCutPlayed;

    private void Awake()
    {
        if (_driver == null)
            _driver = FindFirstObjectByType<ScenarioDataDriver>();
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
            bool hasReturnLobbyPage = ReplayStorySelectionContext.HasReturnLobbyPage;
            LobbyPageType returnLobbyPage = ReplayStorySelectionContext.ReturnLobbyPage;

            // 추가:조규민 기능 설명: 다시보기 진입 출처가 로비 복귀 페이지를 지정한 경우에만 해당 페이지를 예약한다.
            if (returnSceneName == "_Lobby" && hasReturnLobbyPage)
                LobbyReturnContext.RequestPage(returnLobbyPage);

            // 추가:조규민 기능 설명: 복귀 전에 다시보기 상태를 초기화해 다음 일반 스토리 진입과 섞이지 않게 한다.
            ReplayStorySelectionContext.Clear();

            if (GameManager.Instance != null && returnSceneName == "_Lobby")
                GameManager.Instance.LoadLobby();
            else
                SceneManager.LoadScene(returnSceneName);

            return;
        }

        // 인트로가 끝나면 인게임 진입 직전에 시나리오 오프닝 컷(예: 100009 / BG 20116)을 전체화면으로 한 번 보여준다.
        // 같은 드라이버에 주입 재생하므로, 이 컷이 끝나면 OnFinished가 다시 이 함수로 들어와 인게임으로 넘어간다.
        if (!_preTutorialCutPlayed && TryPlayPreTutorialCut())
            return;

        EnterGame();
    }

    private void EnterGame()
    {
        // GameManager가 있으면 정식 흐름(상태 전환 포함), 없으면 직접 로드
        if (GameManager.Instance != null)
            GameManager.Instance.LoadInGame();
        else
            SceneManager.LoadScene(_nextScene);
    }

    private bool TryPlayPreTutorialCut()
    {
        if (_preTutorialCutTalkId <= 0 || _driver == null) return false;

        StoryRepo repo = DataManager.Instance != null ? DataManager.Instance.StoryRepo : null;
        if (repo == null) return false;

        List<Story_TalkData> group = repo.GetTalkGroup(_preTutorialCutGroupId);
        Story_TalkData line = group?.Find(l => l != null && l.ID == _preTutorialCutTalkId);
        if (line == null) return false;

        if (!InjectSingleLine(_driver, line)) return false;

        _preTutorialCutPlayed = true;
        return true;
    }

    // ScenarioDataDriver는 그룹 단위로만 재생(Play(groupId))하므로, 한 줄만 보여주려고
    // 내부 상태를 리플렉션으로 세팅해 단일 라인을 재생한다. 담당 스크립트는 수정하지 않는다.
    private static bool InjectSingleLine(ScenarioDataDriver driver, Story_TalkData line)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = typeof(ScenarioDataDriver);

        FieldInfo linesField = type.GetField("_lines", flags);
        FieldInfo indexField = type.GetField("_index", flags);
        FieldInfo playingField = type.GetField("_isPlaying", flags);
        FieldInfo finishedField = type.GetField("_finished", flags);
        MethodInfo subscribeMethod = type.GetMethod("Subscribe", flags);
        MethodInfo showMethod = type.GetMethod("Show", flags);

        if (linesField == null || indexField == null || playingField == null
            || finishedField == null || subscribeMethod == null || showMethod == null)
        {
            Debug.LogWarning("[TempStoryToGameFlow] ScenarioDataDriver 내부 멤버를 찾지 못해 오프닝 컷을 건너뜁니다.");
            return false;
        }

        try
        {
            subscribeMethod.Invoke(driver, null);
            linesField.SetValue(driver, new List<Story_TalkData> { line });
            indexField.SetValue(driver, 0);
            finishedField.SetValue(driver, false);
            playingField.SetValue(driver, true);
            showMethod.Invoke(driver, null);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TempStoryToGameFlow] 오프닝 컷 재생 준비 실패: {e.Message}");
            return false;
        }
    }
}
