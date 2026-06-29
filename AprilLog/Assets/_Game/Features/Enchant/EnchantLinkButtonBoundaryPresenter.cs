//담당자: 조규민

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인챈트 링크 버튼의 실제 기능 흐름을 처리한다.
/// </summary>
public class EnchantLinkButtonBoundaryPresenter
{
    private const string _returnLobbyMessage = "로비로 돌아가시겠습니까?\n게임은 자동 저장됩니다.";
    private const string _restartChapterMessage = "게임을 포기하시겠습니까?\n진행 중인 게임은 저장되지 않습니다.";

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

    private void RestartChapter()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EnchantLinkButtonBoundaryPresenter] GameManager가 없어 포기하기를 처리하지 않았습니다.");
            return;
        }

        KeepCurrentChapterForRestart();
        GameManager.Instance.DeleteLocalSave();
        GameManager.Instance.LoadInGame();
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

    private void KeepCurrentChapterForRestart()
    {
        StageLoopManager loopManager = Object.FindFirstObjectByType<StageLoopManager>();
        if (loopManager == null)
        {
            return;
        }

        GameManager.Instance.SelectedChapterId = Mathf.Max(1, loopManager.CurrentChapterId);
    }
}
