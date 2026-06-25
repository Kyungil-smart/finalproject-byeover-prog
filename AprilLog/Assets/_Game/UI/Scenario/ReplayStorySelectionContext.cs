//담당자: 조규민
//설명: 시나리오 다시보기 팝업에서 선택한 챕터 정보를 _Story 씬으로 전달한다.

/// <summary>
/// 씬 전환 중 유지되어야 하는 시나리오 다시보기 선택 정보를 관리한다.
/// </summary>
public static class ReplayStorySelectionContext
{
    public static bool IsReplayMode { get; private set; }
    public static int ChapterIndex { get; private set; }
    public static string ChapterLabel { get; private set; }
    public static string ChapterName { get; private set; }
    public static string ChapterDescription { get; private set; }
    public static string ReturnSceneName { get; private set; } = "_Lobby";
    public static bool HasReturnLobbyPage { get; private set; }
    public static LobbyPageType ReturnLobbyPage { get; private set; } = LobbyPageType.Main;

    public static void SetReplay(
        int _chapterIndex,
        string _chapterLabel,
        string _chapterName,
        string _chapterDescription,
        string _returnSceneName)
    {
        // 기능: 다시보기로 재생할 챕터 정보와 종료 후 돌아갈 씬 이름을 저장한다.
        IsReplayMode = true;
        ChapterIndex = _chapterIndex;
        ChapterLabel = string.IsNullOrWhiteSpace(_chapterLabel) ? "CHAPTER." + (_chapterIndex + 1) : _chapterLabel;
        ChapterName = string.IsNullOrWhiteSpace(_chapterName) ? "Chapter " + (_chapterIndex + 1) : _chapterName;
        ChapterDescription = _chapterDescription ?? string.Empty;
        ReturnSceneName = string.IsNullOrWhiteSpace(_returnSceneName) ? "_Lobby" : _returnSceneName;
        HasReturnLobbyPage = false;
        ReturnLobbyPage = LobbyPageType.Main;
    }

    public static void SetReplay(
        int _chapterIndex,
        string _chapterLabel,
        string _chapterName,
        string _chapterDescription,
        string _returnSceneName,
        LobbyPageType _returnLobbyPage)
    {
        SetReplay(_chapterIndex, _chapterLabel, _chapterName, _chapterDescription, _returnSceneName);
        HasReturnLobbyPage = true;
        ReturnLobbyPage = _returnLobbyPage;
    }

    public static void Clear()
    {
        // 기능: 다시보기 종료 후 일반 스토리 진입과 섞이지 않도록 선택 정보를 초기화한다.
        IsReplayMode = false;
        ChapterIndex = 0;
        ChapterLabel = string.Empty;
        ChapterName = string.Empty;
        ChapterDescription = string.Empty;
        ReturnSceneName = "_Lobby";
        HasReturnLobbyPage = false;
        ReturnLobbyPage = LobbyPageType.Main;
    }

}
