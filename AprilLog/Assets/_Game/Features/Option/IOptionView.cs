// 담당자 : 정승우
// 설명   : 옵션 View 인터페이스

using System;

public interface IOptionView
{
    void Show();
    void Hide();
    void SetBGMVolume(float ratio);
    void SetSFXVolume(float ratio);
    event Action<float> OnBGMChanged;
    event Action<float> OnSFXChanged;
    event Action OnLanguageToggled;
    event Action OnCloseClicked;
}
