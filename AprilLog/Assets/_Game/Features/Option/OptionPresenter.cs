// 담당자 : 정승우
// 설명   : 옵션 Presenter
//          - 볼륨 조절 (전체 / BGM / SFX)
//          - 언어 전환 (한국어 / 영어)
//          - 계정 로그아웃 / 탈퇴
// 수정자 : 홍정옥
// 수정내용 : 전체볼륨·언어 버튼·로그아웃·탈퇴 핸들러 추가

using UnityEngine;

public class OptionPresenter
{
    private readonly IOptionView _view;
    private readonly LocalizationManager _localization;

    public OptionPresenter(IOptionView view, LocalizationManager localization)
    {
        if (view == null)
        {
            Debug.LogWarning("[OptionPresenter] View가 없습니다.");
            return;
        }

        _view         = view;
        _localization = localization;

        _view.OnMasterVolumeChanged   += HandleMasterVolume;
        _view.OnBGMChanged            += HandleBGM;
        _view.OnSFXChanged            += HandleSFX;
        _view.OnKoreanSelected        += HandleKorean;
        _view.OnEnglishSelected       += HandleEnglish;
        _view.OnLogoutClicked         += HandleLogout;
        _view.OnDeleteAccountConfirmed += HandleDeleteAccount;
        _view.OnCloseClicked          += HandleClose;
    }

    public void Dispose()
    {
        if (_view == null) return;

        _view.OnMasterVolumeChanged   -= HandleMasterVolume;
        _view.OnBGMChanged            -= HandleBGM;
        _view.OnSFXChanged            -= HandleSFX;
        _view.OnKoreanSelected        -= HandleKorean;
        _view.OnEnglishSelected       -= HandleEnglish;
        _view.OnLogoutClicked         -= HandleLogout;
        _view.OnDeleteAccountConfirmed -= HandleDeleteAccount;
        _view.OnCloseClicked          -= HandleClose;
    }

    // ---------- 볼륨 ----------
    private void HandleMasterVolume(float v)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.MasterVolume = v;
    }

    private void HandleBGM(float v)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = v;
    }

    private void HandleSFX(float v)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = v;
    }

    // ---------- 언어 ----------
    private void HandleKorean()
    {
        _localization?.SetLanguage("ko");
        _view?.SetLanguageIndicator("ko");
    }

    private void HandleEnglish()
    {
        _localization?.SetLanguage("en");
        _view?.SetLanguageIndicator("en");
    }

    // ---------- 계정 ----------
    private void HandleLogout()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[OptionPresenter] GameManager 없음. 로그아웃 불가.");
            return;
        }

        _view?.Hide();
        GameManager.Instance.Logout();
    }

    private void HandleDeleteAccount()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[OptionPresenter] GameManager 없음. 계정 탈퇴 불가.");
            return;
        }

        _view?.Hide();
        GameManager.Instance.DeleteAccount();
    }

    // ---------- 닫기 ----------
    private void HandleClose() => _view?.Hide();
}
