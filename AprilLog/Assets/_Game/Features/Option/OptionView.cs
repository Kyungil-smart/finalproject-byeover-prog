// 담당자 : 정승우
// 설명   : 옵션 View
//          - 전체/BGM/SFX 볼륨 슬라이더
//          - 언어 선택 (한국어 / 영어)
//          - 계정 로그아웃 / 탈퇴
//          - 닫기 버튼
// 수정자 : 홍정옥
// 수정내용 : 전체볼륨·언어 버튼·로그아웃·탈퇴 기능 추가 및 탈퇴 확인 패널 추가

using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionView : MonoBehaviour, IOptionView
{
    // ---------- 이벤트 ----------
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBGMChanged;
    public event Action<float> OnSFXChanged;
    public event Action OnKoreanSelected;
    public event Action OnEnglishSelected;
    public event Action OnLogoutClicked;
    public event Action OnDeleteAccountConfirmed;
    public event Action OnCloseClicked;

    // ---------- 의존성 ----------
    [Header("의존성")]
    [SerializeField] private LocalizationManager _localization;

    // ---------- 볼륨 슬라이더 ----------
    [Header("볼륨")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    // ---------- 언어 버튼 ----------
    [Header("언어 선택")]
    [SerializeField] private Button _korButton;
    [SerializeField] private Button _engButton;
    [Tooltip("현재 선택된 언어 버튼에 적용할 색")]
    [SerializeField] private Color _selectedLangColor   = new Color(1f, 0.85f, 0.3f, 1f);
    [Tooltip("선택되지 않은 언어 버튼 색")]
    [SerializeField] private Color _deselectedLangColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    // ---------- 계정 버튼 ----------
    [Header("계정")]
    [SerializeField] private Button _logoutButton;
    [SerializeField] private Button _deleteAccountButton;

    // ---------- 탈퇴 확인 패널 ----------
    [Header("탈퇴 확인 패널")]
    [Tooltip("탈퇴 확인 전용 패널 (기본 비활성)")]
    [SerializeField] private GameObject _confirmDeletePanel;
    [Tooltip("탈퇴 확인 패널의 '예' 버튼")]
    [SerializeField] private Button _confirmDeleteYesButton;
    [Tooltip("탈퇴 확인 패널의 '아니오' 버튼")]
    [SerializeField] private Button _confirmDeleteNoButton;

    // ---------- 닫기 ----------
    [Header("닫기")]
    [SerializeField] private Button _closeButton;

    // ---------- 상태 ----------
    private OptionPresenter _presenter;
    private bool _isInitialized;

    // ---------- Unity ----------
    private void OnEnable()
    {
        ResolveLocalization();

        if (!_isInitialized)
        {
            _isInitialized = true;
            _presenter = new OptionPresenter(this, _localization);
            BindUI();
        }

        RefreshLanguageIndicator();

        if (_confirmDeletePanel != null)
            _confirmDeletePanel.SetActive(false);
    }

    private void OnDestroy() => _presenter?.Dispose();

    // ---------- 바인딩 ----------
    private void BindUI()
    {
        // 볼륨
        if (_masterSlider != null)
            _masterSlider.onValueChanged.AddListener(v => OnMasterVolumeChanged?.Invoke(v));
        else
            Debug.LogWarning("[OptionView] Master slider가 연결되지 않았습니다.", this);

        if (_bgmSlider != null)
            _bgmSlider.onValueChanged.AddListener(v => OnBGMChanged?.Invoke(v));
        else
            Debug.LogWarning("[OptionView] BGM slider가 연결되지 않았습니다.", this);

        if (_sfxSlider != null)
            _sfxSlider.onValueChanged.AddListener(v => OnSFXChanged?.Invoke(v));
        else
            Debug.LogWarning("[OptionView] SFX slider가 연결되지 않았습니다.", this);

        // 언어
        EnsureButtonCanvasRaycaster(_korButton);
        EnsureButtonCanvasRaycaster(_engButton);

        if (_korButton != null)
            _korButton.onClick.AddListener(() => OnKoreanSelected?.Invoke());
        else
            Debug.LogWarning("[OptionView] 한국어 버튼이 연결되지 않았습니다.", this);

        if (_engButton != null)
            _engButton.onClick.AddListener(() => OnEnglishSelected?.Invoke());
        else
            Debug.LogWarning("[OptionView] 영어 버튼이 연결되지 않았습니다.", this);

        // 계정
        if (_logoutButton != null)
            _logoutButton.onClick.AddListener(() => OnLogoutClicked?.Invoke());
        else
            Debug.LogWarning("[OptionView] 로그아웃 버튼이 연결되지 않았습니다.", this);

        if (_deleteAccountButton != null)
            _deleteAccountButton.onClick.AddListener(OpenConfirmDeletePanel);
        else
            Debug.LogWarning("[OptionView] 계정 탈퇴 버튼이 연결되지 않았습니다.", this);

        // 탈퇴 확인 패널
        if (_confirmDeleteYesButton != null)
            _confirmDeleteYesButton.onClick.AddListener(HandleDeleteConfirmed);

        if (_confirmDeleteNoButton != null)
            _confirmDeleteNoButton.onClick.AddListener(CloseConfirmDeletePanel);

        // 닫기
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
        else
            Debug.LogWarning("[OptionView] 닫기 버튼이 연결되지 않았습니다.", this);
    }

    // ---------- IOptionView 구현 ----------
    public void Show()
    {
        gameObject.SetActive(true);
        // 패널이 비활성 상태였다가 켜질 때 슬라이더 핸들이
        // 잘못된 위치로 튀는 현상을 방지한다.
        Canvas.ForceUpdateCanvases();
        RefreshVolumeSliders();
    }

    public void Hide() => gameObject.SetActive(false);

    public void SetMasterVolume(float ratio)
    {
        if (_masterSlider != null)
            _masterSlider.SetValueWithoutNotify(ratio);
    }

    public void SetBGMVolume(float ratio)
    {
        if (_bgmSlider != null)
            _bgmSlider.SetValueWithoutNotify(ratio);
    }

    public void SetSFXVolume(float ratio)
    {
        if (_sfxSlider != null)
            _sfxSlider.SetValueWithoutNotify(ratio);
    }

    public void SetLanguageIndicator(string langCode)
    {
        bool isKor = langCode == "ko";

        SetButtonColor(_korButton, isKor   ? _selectedLangColor : _deselectedLangColor);
        SetButtonColor(_engButton, !isKor  ? _selectedLangColor : _deselectedLangColor);
    }

    // ---------- 내부 ----------
    private void RefreshVolumeSliders()
    {
        if (AudioManager.Instance == null) return;

        SetMasterVolume(AudioManager.Instance.MasterVolume);
        SetBGMVolume(AudioManager.Instance.BGMVolume);
        SetSFXVolume(AudioManager.Instance.SFXVolume);
    }

    private void RefreshLanguageIndicator()
    {
        ResolveLocalization();

        if (_localization != null)
            SetLanguageIndicator(_localization.CurrentLanguage);
    }

    private void ResolveLocalization()
    {
        if (_localization == null)
            _localization = LocalizationManager.Instance;
    }

    private void OpenConfirmDeletePanel()
    {
        if (_confirmDeletePanel != null)
            _confirmDeletePanel.SetActive(true);
    }

    private void CloseConfirmDeletePanel()
    {
        if (_confirmDeletePanel != null)
            _confirmDeletePanel.SetActive(false);
    }

    private void HandleDeleteConfirmed()
    {
        CloseConfirmDeletePanel();
        OnDeleteAccountConfirmed?.Invoke();
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static void EnsureButtonCanvasRaycaster(Button button)
    {
        if (button == null) return;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }
}
