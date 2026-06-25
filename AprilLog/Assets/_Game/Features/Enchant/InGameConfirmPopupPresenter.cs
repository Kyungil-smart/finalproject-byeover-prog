//담당자: 조규민

using System;

/// <summary>
/// 인게임 확인 팝업의 확인/취소 흐름을 처리한다.
/// </summary>
public class InGameConfirmPopupPresenter
{
    private readonly InGameConfirmPopupModel _model;
    private readonly InGameConfirmPopupView _view;
    private Action _confirmAction;

    public InGameConfirmPopupPresenter(InGameConfirmPopupModel model, InGameConfirmPopupView view)
    {
        _model = model;
        _view = view;

        _model.OnMessageChanged += _view.SetMessage;
        _model.OnVisibleChanged += _view.SetVisible;
        _view.OnYesClicked += HandleYesClicked;
        _view.OnNoClicked += HandleCancelClicked;
        _view.OnCloseClicked += HandleCancelClicked;
    }

    public void Dispose()
    {
        _model.OnMessageChanged -= _view.SetMessage;
        _model.OnVisibleChanged -= _view.SetVisible;
        _view.OnYesClicked -= HandleYesClicked;
        _view.OnNoClicked -= HandleCancelClicked;
        _view.OnCloseClicked -= HandleCancelClicked;
    }

    public void Open(string message, Action confirmAction)
    {
        _confirmAction = confirmAction;
        _model.Open(message);
    }

    private void HandleYesClicked()
    {
        Action _action = _confirmAction;
        _confirmAction = null;
        _model.Close();
        _action?.Invoke();
    }

    private void HandleCancelClicked()
    {
        _confirmAction = null;
        _model.Close();
    }
}
