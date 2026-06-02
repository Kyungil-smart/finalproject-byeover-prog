// 담당자 : 정승우
// 설명   : 옵션 View 인터페이스

using System;

public interface IOptionView
{
    // ---------- 표시 ----------
    void Show();
    void Hide();

    // ---------- 볼륨 ----------
    void SetMasterVolume(float ratio);
    void SetBGMVolume(float ratio);
    void SetSFXVolume(float ratio);

    // ---------- 언어 ----------
    void SetLanguageIndicator(string langCode);   // "ko" / "en"

    // ---------- 이벤트 : 볼륨 ----------
    event Action<float> OnMasterVolumeChanged;
    event Action<float> OnBGMChanged;
    event Action<float> OnSFXChanged;

    // ---------- 이벤트 : 언어 ----------
    event Action OnKoreanSelected;
    event Action OnEnglishSelected;

    // ---------- 이벤트 : 계정 ----------
    event Action OnLogoutClicked;
    event Action OnDeleteAccountConfirmed;  // 탈퇴 확인 팝업에서 "예" 클릭

    // ---------- 이벤트 : 닫기 ----------
    event Action OnCloseClicked;
}
