// 담당자 : 정승우
// 설명   : 인게임 HUD Presenter -- Model 구독해서 View 갱신

using UnityEngine;

public class InGameHUDPresenter
{
    private readonly IInGameHUDView _view;
    private readonly PlayerModel _player;
    private readonly ComboModel _combo;

    public InGameHUDPresenter(IInGameHUDView view, PlayerModel player, ComboModel combo)
    {
        _view = view;
        _player = player;
        _combo = combo;

        _player.OnHPChanged += HandleHP;
        _combo.OnComboChanged += HandleCombo;
        _combo.OnComboTimerChanged += HandleTimer;
    }

    public void Dispose()
    {
        _player.OnHPChanged -= HandleHP;
        _combo.OnComboChanged -= HandleCombo;
        _combo.OnComboTimerChanged -= HandleTimer;
    }

    private void HandleHP(int cur, int max) => _view.UpdateHP((float)cur / Mathf.Max(1, max));
    private void HandleCombo(int count) => _view.UpdateCombo(count);
    private void HandleTimer(float ratio) => _view.UpdateComboTimer(ratio);
}