// 담당자 : 정승우
// 설명   : 인게임 HUD Presenter -- Model 구독해서 View 갱신

using UnityEngine;

/// <summary>
/// PlayerModel, ComboModel 변경을 구독해서 HUD View를 갱신한다.
/// </summary>
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
        _player.OnShieldChanged += HandleShield;
        _combo.OnComboChanged += HandleCombo;
        _combo.OnComboTimerChanged += HandleTimer;
    }

    public void Dispose()
    {
        _player.OnHPChanged -= HandleHP;
        _player.OnShieldChanged -= HandleShield;
        _combo.OnComboChanged -= HandleCombo;
        _combo.OnComboTimerChanged -= HandleTimer;
    }

    private void HandleHP(int cur, int max) => _view.UpdateHP((float)cur / Mathf.Max(1, max));
    private void HandleShield(int cur, int max) => _view.UpdateShield(max > 0 ? (float)cur / max : 0f);
    private void HandleCombo(int count) => _view.UpdateCombo(count);
    private void HandleTimer(float ratio) => _view.UpdateComboTimer(ratio);
}
