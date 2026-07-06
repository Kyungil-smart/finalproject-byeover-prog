//담당자: 조규민
// 팝업 Model과 View 연결 및 확인·취소 입력에 따른 콜백 실행과 닫음 처리

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

    // 확인 동작 보관 후 Model 팝업 열기 요청
    public void Open(string message, Action confirmAction)
    {
        _confirmAction = confirmAction;
        _model.Open(message);
    }

    // 팝업 닫음 후 보관된 확인 동작 일회 실행
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
