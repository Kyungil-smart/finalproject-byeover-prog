// 담당자 : 정승우
// 설명   : 옵션 Presenter -- 볼륨 조절 + 언어 전환

using UnityEngine;

public class OptionPresenter
{
    private readonly IOptionView _view;
    private readonly ScreenNavigator _navigator;
    private readonly LocalizationManager _localization;

    public OptionPresenter(IOptionView view, ScreenNavigator navigator, LocalizationManager localization)
    {
        if (view == null)
        {
            Debug.LogWarning("[OptionPresenter] View is missing. Presenter initialization skipped.");
            return;
        }

        _view = view;
        _navigator = navigator;
        _localization = localization;

        _view.OnBGMChanged += HandleBGM;
        _view.OnSFXChanged += HandleSFX;
        _view.OnLanguageToggled += HandleLanguage;
        _view.OnCloseClicked += HandleClose;
    }

    public void Dispose()
    {
        if (_view == null)
            return;

        _view.OnBGMChanged -= HandleBGM;
        _view.OnSFXChanged -= HandleSFX;
        _view.OnLanguageToggled -= HandleLanguage;
        _view.OnCloseClicked -= HandleClose;
    }

    private void HandleBGM(float v) { if (AudioManager.Instance != null) AudioManager.Instance.BGMVolume = v; }
    private void HandleSFX(float v) { if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = v; }
    private void HandleLanguage() => _localization?.ToggleLanguage();
    private void HandleClose() => _navigator?.HideOption();
}
