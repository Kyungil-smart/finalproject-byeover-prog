// 담당자 : 정승우
// 설명   : 로비 Presenter

using UnityEngine;

public class LobbyPresenter
{
    private readonly ILobbyView _view;
    private readonly PlayerProgressModel _progress;
    private readonly CurrencyModel _currency;

    public LobbyPresenter(ILobbyView view, PlayerProgressModel progress, CurrencyModel currency)
    {
        _view = view;
        _progress = progress;
        _currency = currency;

        _progress.OnCharacterLevelChanged += HandleLevelChanged;
        _progress.OnProgressUpdated += HandleProgressUpdated;
        _currency.OnCurrencyChanged += HandleCurrencyChanged;
        _view.OnStageSelected += HandleStageSelected;

        // 초기값 표시
        _view.SetPlayerInfo("", progress.CharacterLevel);
        _view.SetCurrency(currency.Gold, currency.Parchment);
    }

    public void Dispose()
    {
        _progress.OnCharacterLevelChanged -= HandleLevelChanged;
        _progress.OnProgressUpdated -= HandleProgressUpdated;
        _currency.OnCurrencyChanged -= HandleCurrencyChanged;
        _view.OnStageSelected -= HandleStageSelected;
    }

    private void HandleLevelChanged(int lv) => _view.SetPlayerInfo("", lv);
    private void HandleCurrencyChanged(int g, int p) => _view.SetCurrency(g, p);
    private void HandleProgressUpdated() { /* 스테이지 버튼 갱신 */ }

    private void HandleStageSelected(int stageId)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectedChapterId = stageId;
            GameManager.Instance.LoadInGame();
        }
    }
}
