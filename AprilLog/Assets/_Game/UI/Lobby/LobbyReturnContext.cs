//담당자: 조규민
//설명: 씬 전환 후 로비에서 열어야 할 페이지 예약 상태를 관리한다.

/// <summary>
/// 다른 씬에서 로비로 복귀할 때 초기 표시 페이지를 한 번만 전달한다.
/// </summary>
// 다른 씬에서 요청한 로비 복귀 페이지를 한 번만 전달하고 사용 후 해제
public static class LobbyReturnContext
{
    public static bool HasPendingPage { get; private set; }
    public static LobbyPageType PendingPage { get; private set; } = LobbyPageType.Main;

    // 로비 재진입 시 열 페이지 요청 저장
    public static void RequestPage(LobbyPageType _pageType)
    {
        // 기능: 다음 _Lobby 씬 진입 시 열 페이지를 예약한다.
        HasPendingPage = true;
        PendingPage = _pageType;
    }

    // 저장된 페이지 요청 반환과 일회성 상태 해제
    public static bool TryConsumePage(out LobbyPageType _pageType)
    {
        // 기능: LobbyPageController가 예약된 페이지를 한 번만 소비하게 한다.
        _pageType = PendingPage;

        if (!HasPendingPage)
            return false;

        HasPendingPage = false;
        PendingPage = LobbyPageType.Main;
        return true;
    }
}
