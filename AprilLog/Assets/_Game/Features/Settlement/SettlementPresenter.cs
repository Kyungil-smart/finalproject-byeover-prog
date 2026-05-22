// 담당자 : 정승우
// 설명   : 정산 Presenter -- 확인 버튼 눌리면 로비로

public class SettlementPresenter
{
    private readonly ISettlementView _view;

    public SettlementPresenter(ISettlementView view)
    {
        _view = view;
        _view.OnConfirmClicked += HandleConfirm;
    }

    public void Dispose()
    {
        _view.OnConfirmClicked -= HandleConfirm;
    }

    private void HandleConfirm()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DeleteLocalSave();
            GameManager.Instance.LoadLobby();
        }

        _view.Hide();
    }
}
