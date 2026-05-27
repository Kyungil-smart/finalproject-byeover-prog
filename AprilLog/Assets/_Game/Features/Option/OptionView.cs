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
            if (_navigator == null || _localization == null)
                Debug.LogWarning("[OptionView] Optional dependency is missing. Some option features will be disabled.");

            _presenter = new OptionPresenter(this, _navigator, _localization);

            if (_bgmSlider != null)
                _bgmSlider.onValueChanged.AddListener(v => OnBGMChanged?.Invoke(v));
            else
                Debug.LogWarning("[OptionView] BGM slider is not assigned.");

            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(v => OnSFXChanged?.Invoke(v));
            else
                Debug.LogWarning("[OptionView] SFX slider is not assigned.");

            if (_langButton != null)
                _langButton.onClick.AddListener(() => OnLanguageToggled?.Invoke());
            else
                Debug.LogWarning("[OptionView] Language button is not assigned.");

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
            else
                Debug.LogWarning("[OptionView] Close button is not assigned.");
        }

        // 현재 값으로 슬라이더 세팅
        if (AudioManager.Instance != null)
        {
            if (_bgmSlider != null)
                _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);

            if (_sfxSlider != null)
                _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
        }
    }

    private void OnDestroy() => _presenter?.Dispose();

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
    public void SetBGMVolume(float ratio)
    {
        if (_bgmSlider == null)
        {
            Debug.LogWarning("[OptionView] BGM slider is not assigned.");
            return;
        }

        _bgmSlider.SetValueWithoutNotify(ratio);
    }

    public void SetSFXVolume(float ratio)
    {
        if (_sfxSlider == null)
        {
            Debug.LogWarning("[OptionView] SFX slider is not assigned.");
            return;
        }

        _sfxSlider.SetValueWithoutNotify(ratio);
    }
}
