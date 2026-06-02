// 담당자 : 조규민
// 구현원리 : UGUI와 TextMeshPro 컴포넌트 입력을 이벤트로 변환하고, Presenter가 요청한 표시 상태만 화면에 반영한다.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 로그인 화면의 필수 UI 표시와 입력 이벤트 전달을 담당한다.
public class LoginView : MonoBehaviour, ILoginView, IPointerClickHandler
{
    public event Action OnGuestLoginClicked;
    public event Action<string, string> OnGoogleLoginClicked;
    public event Action<string, string> OnRegisterClicked;
    public event Action<bool> OnTermsAgreementChanged;
    public event Action OnTermsConfirmed; // 약관 확인 버튼 입력을 Presenter로 전달한다.
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
    [SerializeField] private GameObject _termsAgreementPanel; // 로그인 화면 위에 표시할 약관 동의 모달 패널이다.
    [SerializeField] private Button _termsConfirmButton; // 약관 체크 후 로그인 버튼을 열기 위한 확인 버튼이다.

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
    private RectTransform _passwordInputRectTransform;

    // View 생성 시 Model과 버튼 이벤트를 준비한다.
    private void Awake()
    {
        _model = new LoginModel();

        if (_guestLoginButton != null || _googleLoginButton != null || _termsToggle != null || _popupPanel != null)
        {
            BindButtons();
            ValidateRequiredReferences();
            PrepareInputFields();
            CacheInputFieldRects();
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

    // View 제거 시 Presenter와 Unity UI 리스너를 정리한다.
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

        // 약관 모달 확인 버튼 클릭을 Presenter로 전달한다.
        if (_termsConfirmButton != null)
            _termsConfirmButton.onClick.AddListener(NotifyTermsConfirmed);

        if (_passwordInputField != null)
            _passwordInputField.onSelect.AddListener(ActivatePasswordInputField);
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

        // 약관 확인 버튼 리스너를 정리한다.
        if (_termsConfirmButton != null)
            _termsConfirmButton.onClick.RemoveListener(NotifyTermsConfirmed);

        if (_passwordInputField != null)
            _passwordInputField.onSelect.RemoveListener(ActivatePasswordInputField);
    }

    private void PrepareInputFields()
    {
        PrepareInputField(_playerIdInputField);
        PrepareInputField(_passwordInputField);
    }

    private void PrepareInputField(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.interactable = true;
        inputField.readOnly = false;
        inputField.shouldHideMobileInput = false;
        inputField.shouldHideSoftKeyboard = false;

        if (inputField.textComponent != null)
        {
            inputField.textComponent.gameObject.SetActive(true);
        }
    }

    private void ActivatePasswordInputField(string _)
    {
        ActivatePasswordInputField();
    }

    private void ActivatePasswordInputField()
    {
        if (_passwordInputField == null || !_passwordInputField.interactable)
        {
            return;
        }

        _passwordInputField.Select();
        _passwordInputField.ActivateInputField();
    }

    private void CacheInputFieldRects()
    {
        if (_passwordInputField != null)
        {
            _passwordInputRectTransform = _passwordInputField.GetComponent<RectTransform>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanFocusPasswordInputFromPointer())
        {
            return;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(_passwordInputRectTransform, eventData.position, eventData.pressEventCamera))
        {
            return;
        }

        ActivatePasswordInputField();
    }

    private bool CanFocusPasswordInputFromPointer()
    {
        if (_passwordInputRectTransform == null || _passwordInputField == null)
        {
            return false;
        }

        if (_registerPanel == null || !_registerPanel.activeInHierarchy)
        {
            return false;
        }

        if (_termsAgreementPanel != null && _termsAgreementPanel.activeInHierarchy)
        {
            return false;
        }

        return _popupPanel == null || !_popupPanel.activeInHierarchy;
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

        // 약관 모달 구성 참조 누락을 Inspector에서 바로 확인할 수 있게 한다.
        if (_termsAgreementPanel == null)
            Debug.LogWarning("[LoginView] 약관 동의 패널 참조가 없습니다.", this);

        if (_termsConfirmButton == null)
            Debug.LogWarning("[LoginView] 약관 확인 버튼 참조가 없습니다.", this);

        if (_popupPanel == null)
            Debug.LogWarning("[LoginView] 팝업 패널 참조가 없습니다.", this);
    }

    // 게스트 로그인 버튼 입력을 Presenter로 전달한다.
    private void NotifyGuestLoginClicked()
    {
        OnGuestLoginClicked?.Invoke();
    }

    // Google 로그인 버튼 입력을 Presenter로 전달한다.
    private void NotifyGoogleLoginClicked()
    {
        string playerId = _playerIdInputField != null ? _playerIdInputField.text : string.Empty;
        string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
        OnGoogleLoginClicked?.Invoke(playerId, password);
    }

    // 회원가입 입력 필드 값을 Presenter로 전달한다.
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

    // 약관 확인 버튼 입력을 Presenter로 전달한다.
    private void NotifyTermsConfirmed()
    {
        OnTermsConfirmed?.Invoke();
    }

    // 게스트 로그인 버튼 입력 가능 여부를 제어한다.
    public void SetGuestButtonInteractable(bool isInteractable)
    {
        if (_guestLoginButton != null)
            _guestLoginButton.interactable = isInteractable;
    }

    // Google 로그인 버튼 입력 가능 여부를 제어한다.
    public void SetGoogleButtonInteractable(bool isInteractable)
    {
        if (_googleLoginButton != null)
            _googleLoginButton.interactable = isInteractable;
    }

    // 회원가입 버튼 입력 가능 여부를 제어한다.
    public void SetRegisterButtonInteractable(bool isInteractable)
    {
        if (_registerButton != null)
            _registerButton.interactable = isInteractable;
    }

    // 회원가입 패널을 표시한다.
    public void ShowRegisterPanel()
    {
        PrepareInputFields();

        if (_registerPanel != null)
            _registerPanel.SetActive(true);
    }

    // 회원가입 패널을 숨긴다.
    public void HideRegisterPanel()
    {
        if (_registerPanel != null)
            _registerPanel.SetActive(false);
    }

    // 회원가입 안내 문구를 표시한다.
    public void SetRegisterMessage(string message)
    {
        if (_registerMessageText != null)
            _registerMessageText.SetText(message);
    }

    // 약관 동의 모달을 표시한다.
    public void ShowTermsAgreementPanel()
    {
        if (_termsAgreementPanel != null)
            _termsAgreementPanel.SetActive(true);
    }

    // 약관 동의 모달을 숨긴다.
    public void HideTermsAgreementPanel()
    {
        if (_termsAgreementPanel != null)
            _termsAgreementPanel.SetActive(false);
    }

    // 약관 동의 체크 상태에 따라 확인 버튼 입력 가능 여부를 제어한다.
    public void SetTermsConfirmButtonInteractable(bool isInteractable)
    {
        if (_termsConfirmButton != null)
            _termsConfirmButton.interactable = isInteractable;
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
