//담당자: 조규민
// 인챈트 종료 선택에 따른 계속 진행·로비 복귀·챕터 재시작 흐름 분기
// 로비 복귀 전 현재 진행 상태 저장 및 화면 전환 요청
//포기하기 확인 시 인게임을 재시작하지 않고 진행 세이브를 삭제한 뒤 로비로 복귀하도록 변경

using UnityEngine;

/// <summary>
/// 인챈트 링크 버튼의 실제 기능 흐름을 처리한다.
/// </summary>
public class EnchantLinkButtonBoundaryPresenter
{
    private const string _returnLobbyMessage = "로비로 돌아가시겠습니까?\n게임은 자동 저장됩니다.";
    private const string _restartChapterMessage = "게임을 포기하고 로비로 돌아가시겠습니까?\n진행 중인 게임은 저장되지 않습니다.";

    private readonly EnchantLinkButtonBoundaryView _view;
    private readonly ScreenNavigator _screenNavigator;
    private readonly InGameConfirmPopupPresenter _confirmPopupPresenter;

    public EnchantLinkButtonBoundaryPresenter(
        EnchantLinkButtonBoundaryView view,
        ScreenNavigator screenNavigator,
        InGameConfirmPopupPresenter confirmPopupPresenter)
    {
        _view = view;
        _screenNavigator = screenNavigator;
        _confirmPopupPresenter = confirmPopupPresenter;

        _view.OnContinueClicked += HandleContinueClicked;
        _view.OnReturnLobbyClicked += HandleReturnLobbyClicked;
        _view.OnRestartChapterClicked += HandleRestartChapterClicked;
    }

    public void Dispose()
    {
        _view.OnContinueClicked -= HandleContinueClicked;
        _view.OnReturnLobbyClicked -= HandleReturnLobbyClicked;
        _view.OnRestartChapterClicked -= HandleRestartChapterClicked;
    }

    // 계속 진행 선택 시 버튼 선택 상태 표시와 확인 팝업 요청
    private void HandleContinueClicked()
    {
        _view.ShowSelectedButton(EnchantLinkButtonType.Continue);

        if (_screenNavigator != null)
        {
            _screenNavigator.OnCloseButtonClick();
            return;
        }

        Time.timeScale = 1f;
    }

    private void HandleReturnLobbyClicked()
    {
        _view.ShowSelectedButton(EnchantLinkButtonType.ReturnLobby);

        if (_confirmPopupPresenter == null)
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] 확인 팝업이 없어 로비 복귀를 처리하지 않았습니다.");
            return;
        }

        _confirmPopupPresenter.Open(_returnLobbyMessage, ReturnToLobby);
    }

    private void HandleRestartChapterClicked()
    {
        _view.ShowSelectedButton(EnchantLinkButtonType.RestartChapter);

        if (_confirmPopupPresenter == null)
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] 확인 팝업이 없어 포기하기를 처리하지 않았습니다.");
            return;
        }

        _confirmPopupPresenter.Open(_restartChapterMessage, RestartChapter);
    }

    // 현재 진행 정보 저장 후 로비 씬 이동
    private void ReturnToLobby()
    {
        SaveCurrentProgressForResume();

        if (_screenNavigator != null)
        {
            _screenNavigator.ToLobbyAction();
            return;
        }

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLobby();
        }
    }

    // 현재 챕터 재시작을 위한 진행 상태 초기화와 인게임 씬 재로드
    private void RestartChapter()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeleteLocalSave();
        }
        else
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] GameManager가 없어 포기하기 세이브 삭제를 처리하지 못했습니다.");
        }

        if (_screenNavigator != null)
        {
            _screenNavigator.ToLobbyAction();
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLobby();
        }
    }

    private void SaveCurrentProgressForResume()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] GameManager가 없어 로비 복귀 저장을 처리하지 않았습니다.");
            return;
        }

        StageLoopManager loopManager = Object.FindFirstObjectByType<StageLoopManager>();
        if (loopManager == null)
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] StageLoopManager가 없어 현재 스테이지 저장을 처리하지 않았습니다.");
            return;
        }

        GameManager.Instance.SaveLocal();
    }

}
