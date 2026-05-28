// 담당자 : 조규민
// 구현원리 : UGUI와 TextMeshPro 컴포넌트 입력을 이벤트로 변환하고, Presenter가 요청한 표시 상태만 화면에 반영한다.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인 화면의 순수 UI 표시와 입력 이벤트 전달을 담당한다.
public class LoginView : MonoBehaviour, ILoginView
{
    public event Action OnGuestLoginClicked;
    public event Action OnGoogleLoginClicked;
    public event Action<string, string> OnRegisterClicked;
    public event Action<bool> OnTermsAgreementChanged;
    public event Action OnTermsPopupClicked;
    public event Action OnPopupClosed;

    [Header("버튼")]
    [SerializeField] private Button _guestLoginButton;
    [SerializeField] private Button _googleLoginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _termsPopupButton;
    [SerializeField] private Button _popupCloseButton;

    [Header("약관")]
    [SerializeField] private Toggle _termsToggle;

    [Header("표시")]
    [SerializeField] private GameObject _loadingIndicator;
    [SerializeField] private GameObject _registerPanel;
    [SerializeField] private GameObject _popupPanel;
    [SerializeField] private TMP_Text _popupMessageText;
    [SerializeField] private TMP_Text _registerMessageText;
    [SerializeField] private TMP_Text _appVersionText;
    [SerializeField] private TMP_Text _uidText;

    [Header("회원가입")]
    [SerializeField] private TMP_InputField _playerIdInputField;
    [SerializeField] private TMP_InputField _passwordInputField;

    private LoginPresenter _presenter;
    private LoginModel _model;

    // 런타임 생성 UI가 인스펙터 없이 필요한 참조를 주입할 수 있게 한다.
    public void Configure(Button guestLoginButton, Button googleLoginButton, Button registerButton,
        Button termsPopupButton, Button popupCloseButton, Toggle termsToggle,
        GameObject loadingIndicator, GameObject registerPanel, GameObject popupPanel,
        TMP_InputField playerIdInputField, TMP_InputField passwordInputField,
        TMP_Text popupMessageText, TMP_Text registerMessageText, TMP_Text appVersionText, TMP_Text uidText)
    {
        _guestLoginButton = guestLoginButton;
        _googleLoginButton = googleLoginButton;
        _registerButton = registerButton;
        _termsPopupButton = termsPopupButton;
        _popupCloseButton = popupCloseButton;
        _termsToggle = termsToggle;
        _loadingIndicator = loadingIndicator;
        _registerPanel = registerPanel;
        _popupPanel = popupPanel;
        _playerIdInputField = playerIdInputField;
        _passwordInputField = passwordInputField;
        _popupMessageText = popupMessageText;
        _registerMessageText = registerMessageText;
        _appVersionText = appVersionText;
        _uidText = uidText;

        BindButtons();
        ValidateRequiredReferences();
        HidePopup();
        HideRegisterPanel();
        SetLoading(false);
    }

    // View 생성 시 Model과 버튼 이벤트를 준비한다.
    private void Awake()
    {
        _model = new LoginModel();

        if (_guestLoginButton != null || _googleLoginButton != null || _termsToggle != null || _popupPanel != null)
        {
            BindButtons();
            ValidateRequiredReferences();
            HidePopup();
            HideRegisterPanel();
            SetLoading(false);
        }
    }

    // 모든 Awake 이후 GameManager가 준비된 상태에서 Presenter를 연결한다.
    private void Start()
    {
        _presenter = new LoginPresenter(this, _model);
    }

    // View 제거 시 Presenter 이벤트 구독을 해제한다.
    private void OnDestroy()
    {
        _presenter?.Dispose();
        UnbindButtons();
    }

    // Unity UI 입력을 View 이벤트로 변환한다.
    private void BindButtons()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.AddListener(NotifyGuestLoginClicked);

        if (_googleLoginButton != null)
            _googleLoginButton.onClick.AddListener(NotifyGoogleLoginClicked);

        if (_registerButton != null)
            _registerButton.onClick.AddListener(NotifyRegisterClicked);

        if (_termsPopupButton != null)
            _termsPopupButton.onClick.AddListener(NotifyTermsPopupClicked);

        if (_popupCloseButton != null)
            _popupCloseButton.onClick.AddListener(NotifyPopupClosed);

        if (_termsToggle != null)
            _termsToggle.onValueChanged.AddListener(NotifyTermsAgreementChanged);
    }

    // View 파괴 시 Unity UI 리스너를 제거한다.
    private void UnbindButtons()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.RemoveListener(NotifyGuestLoginClicked);

        if (_googleLoginButton != null)
            _googleLoginButton.onClick.RemoveListener(NotifyGoogleLoginClicked);

        if (_registerButton != null)
            _registerButton.onClick.RemoveListener(NotifyRegisterClicked);

        if (_termsPopupButton != null)
            _termsPopupButton.onClick.RemoveListener(NotifyTermsPopupClicked);

        if (_popupCloseButton != null)
            _popupCloseButton.onClick.RemoveListener(NotifyPopupClosed);

        if (_termsToggle != null)
            _termsToggle.onValueChanged.RemoveListener(NotifyTermsAgreementChanged);
    }

    // 핵심 UI 참조 누락을 한 번만 경고한다.
    private void ValidateRequiredReferences()
    {
        if (_guestLoginButton == null)
            Debug.LogWarning("[LoginView] 게스트 로그인 버튼 참조가 없습니다.", this);

        if (_googleLoginButton == null)
            Debug.LogWarning("[LoginView] Google 로그인 버튼 참조가 없습니다.", this);

        if (_registerPanel == null)
            Debug.LogWarning("[LoginView] 회원가입 패널 참조가 없습니다.", this);

        if (_termsToggle == null)
            Debug.LogWarning("[LoginView] 약관 동의 토글 참조가 없습니다.", this);

        if (_popupPanel == null)
            Debug.LogWarning("[LoginView] 팝업 패널 참조가 없습니다.", this);
    }

    // 게스트 로그인 버튼 입력을 Presenter로 전달한다.
    private void NotifyGuestLoginClicked()
    {
        OnGuestLoginClicked?.Invoke();
    }

    private void NotifyGoogleLoginClicked()
    {
        OnGoogleLoginClicked?.Invoke();
    }

    private void NotifyRegisterClicked()
    {
        string playerId = _playerIdInputField != null ? _playerIdInputField.text : string.Empty;
        string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
        OnRegisterClicked?.Invoke(playerId, password);
    }

    // 약관 보기 버튼 입력을 Presenter로 전달한다.
    private void NotifyTermsPopupClicked()
    {
        OnTermsPopupClicked?.Invoke();
    }

    // 팝업 닫기 버튼 입력을 Presenter로 전달한다.
    private void NotifyPopupClosed()
    {
        OnPopupClosed?.Invoke();
    }

    // 약관 동의 토글 입력을 Presenter로 전달한다.
    private void NotifyTermsAgreementChanged(bool hasAcceptedTerms)
    {
        OnTermsAgreementChanged?.Invoke(hasAcceptedTerms);
    }

    // 약관 동의와 로그인 진행 상태에 따라 게스트 버튼을 제어한다.
    public void SetGuestButtonInteractable(bool isInteractable)
    {
        if (_guestLoginButton != null)
            _guestLoginButton.interactable = isInteractable;
    }

    public void SetGoogleButtonInteractable(bool isInteractable)
    {
        if (_googleLoginButton != null)
            _googleLoginButton.interactable = isInteractable;
    }

    public void SetRegisterButtonInteractable(bool isInteractable)
    {
        if (_registerButton != null)
            _registerButton.interactable = isInteractable;
    }

    public void ShowRegisterPanel()
    {
        if (_registerPanel != null)
            _registerPanel.SetActive(true);
    }

    public void HideRegisterPanel()
    {
        if (_registerPanel != null)
            _registerPanel.SetActive(false);
    }

    public void SetRegisterMessage(string message)
    {
        if (_registerMessageText != null)
            _registerMessageText.SetText(message);
    }

    // 로그인 중 로딩 인디케이터를 표시하고 버튼 입력을 잠근다.
    public void SetLoading(bool isLoading)
    {
        if (_loadingIndicator != null)
            _loadingIndicator.SetActive(isLoading);
    }

    // 앱 버전과 인증 성공 UID를 화면에 표시한다.
    public void SetAccountInfo(string appVersion, string uid)
    {
        if (_appVersionText != null)
            _appVersionText.text = "v" + appVersion;

        if (_uidText != null)
            _uidText.SetText(string.IsNullOrEmpty(uid) ? string.Empty : "UID: " + uid);
    }

    // 실패/안내 팝업 메시지를 표시한다.
    public void ShowPopup(string message)
    {
        if (_popupMessageText != null)
            _popupMessageText.SetText(message);

        if (_popupPanel != null)
            _popupPanel.SetActive(true);
    }

    // 팝업 패널을 닫는다.
    public void HidePopup()
    {
        if (_popupPanel != null)
            _popupPanel.SetActive(false);
    }
}
