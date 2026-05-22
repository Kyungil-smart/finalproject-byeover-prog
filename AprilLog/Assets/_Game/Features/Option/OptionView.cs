// 담당자 : 정승우
// 설명   : 옵션 View -- 사운드 조절, 언어 변경

using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionView : MonoBehaviour, IOptionView
{
    public event Action<float> OnBGMChanged;
    public event Action<float> OnSFXChanged;
    public event Action OnLanguageToggled;
    public event Action OnCloseClicked;

    [Header("참조")]
    [SerializeField] private ScreenNavigator _navigator;
    [SerializeField] private LocalizationManager _localization;

    [Header("UI")]
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Button _langButton;
    [SerializeField] private Button _closeButton;

    private OptionPresenter _presenter;
    private bool _isInitialized;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
            _presenter = new OptionPresenter(this, _navigator, _localization);

            _bgmSlider.onValueChanged.AddListener(v => OnBGMChanged?.Invoke(v));
            _sfxSlider.onValueChanged.AddListener(v => OnSFXChanged?.Invoke(v));
            _langButton.onClick.AddListener(() => OnLanguageToggled?.Invoke());
            _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        }

        // 현재 값으로 슬라이더 세팅
        if (AudioManager.Instance != null)
        {
            _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
            _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
        }
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void SetBGMVolume(float ratio) => _bgmSlider.SetValueWithoutNotify(ratio);
    public void SetSFXVolume(float ratio) => _sfxSlider.SetValueWithoutNotify(ratio);
}
