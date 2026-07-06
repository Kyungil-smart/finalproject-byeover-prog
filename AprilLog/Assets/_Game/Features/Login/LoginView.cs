// 담당자 : 조규민
// 로그인 버튼과 약관 입력 이벤트를 Presenter에 전달하고 수명 주기에 맞춰 등록·해제
// Presenter 요청에 따른 패널·팝업·로딩·계정 정보 표시 상태 갱신
// 에디터 로그인 입력 필드 생성과 키보드 포커스 처리
// 구현원리 : UGUI와 TextMeshPro 컴포넌트 입력을 이벤트로 변환하고, Presenter가 요청한 표시 상태와 기존 계정 로그인 버튼 자동 생성 및 앱 버전 표시를 화면에 반영

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 로그인 화면의 필수 UI 표시와 입력 이벤트 전달을 담당
public class LoginView : MonoBehaviour, ILoginView, IPointerClickHandler
{
    public event Action OnGuestLoginClicked;
    public event Action<string, string> OnGoogleLoginClicked;
    public event Action<string, string> OnExistingAccountLoginClicked;
    public event Action<string, string> OnRegisterClicked;
    public event Action<bool> OnTermsAgreementChanged;
    public event Action OnTermsConfirmed; // 약관 확인 버튼 입력을 Presenter로 전달
    public event Action OnTermsPopupClicked;
    public event Action OnPopupClosed;

    [Header("버튼")]
    [SerializeField] private Button _guestLoginButton;
    [SerializeField] private Button _googleLoginButton;
    [SerializeField] private Button _existingAccountLoginButton;
    [SerializeField] private Button _registerButton;
    [SerializeField] private Button _termsPopupButton;
    [SerializeField] private Button _popupCloseButton;

    [Header("약관")]
    [SerializeField] private Toggle _termsToggle;
    [SerializeField] private GameObject _termsAgreementPanel; // 로그인 화면 위에 표시할 약관 동의 모달 패널
    [SerializeField] private Button _termsConfirmButton; // 약관 체크 후 로그인 버튼을 열기 위한 확인 버튼
    [SerializeField] private TMP_Text _termsTitleText;
    [SerializeField] private TMP_Text _termsToggleLabelText;
    [SerializeField] private TMP_Text _termsConfirmButtonLabelText;
    [SerializeField] private TMP_Text _guestLoginButtonLabelText;

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
    private bool _isLocalizationSubscribed;

    // View 생성 시 Model과 버튼 이벤트를 준비
    private void Awake()
    {
        _model = new LoginModel();

        if (_guestLoginButton != null || _googleLoginButton != null || _termsToggle != null || _popupPanel != null)
        {
            EnsureExistingAccountLoginButton();
            BindButtons();
            ValidateRequiredReferences();
            PrepareInputFields();
            CacheInputFieldRects();
            HidePopup();
            HideRegisterPanel();
            SetLoading(false);
            SetAppVersionText(Application.version);
        }
    }

    // 화면 활성화 시 버튼과 다국어 변경 이벤트 등록
    private void OnEnable()
    {
        ResolveLocalizationTextReferences();
        SubscribeLocalization();
        UpdateLocalizedTexts();
        SetAppVersionText(Application.version);
    }

    // 화면 비활성화 시 버튼과 다국어 변경 이벤트 해제
    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    // 모든 Awake 이후 GameManager가 준비된 상태에서 Presenter를 연결
    private void Start()
    {
        SubscribeLocalization();
        UpdateLocalizedTexts();
        _presenter = new LoginPresenter(this, _model);
    }

    // View 제거 시 Presenter와 Unity UI 리스너를 정리
    private void OnDestroy()
    {
        UnsubscribeLocalization();
        _presenter?.Dispose();
        UnbindButtons();
    }

    private void ResolveLocalizationTextReferences()
    {
        _termsTitleText ??= FindChildComponentByName<TMP_Text>(_termsAgreementPanel?.transform, "TermsTitleText");
        _termsToggleLabelText ??= FindChildComponentByName<TMP_Text>(_termsToggle?.transform, "Label");
        _termsConfirmButtonLabelText ??= FindChildComponentByName<TMP_Text>(_termsConfirmButton?.transform, "Label");
        _guestLoginButtonLabelText ??= FindChildComponentByName<TMP_Text>(_guestLoginButton?.transform, "Label");
        _termsToggleLabelText ??= _termsToggle?.GetComponentInChildren<TMP_Text>(true);
        _termsConfirmButtonLabelText ??= _termsConfirmButton?.GetComponentInChildren<TMP_Text>(true);
        _guestLoginButtonLabelText ??= _guestLoginButton?.GetComponentInChildren<TMP_Text>(true);
    }

    private void SubscribeLocalization()
    {
        if (_isLocalizationSubscribed || LocalizationManager.Instance == null)
        {
            return;
        }

        LocalizationManager.Instance.OnLanguageChanged += UpdateLocalizedTexts;
        _isLocalizationSubscribed = true;
    }

    private void UnsubscribeLocalization()
    {
        if (!_isLocalizationSubscribed || LocalizationManager.Instance == null)
        {
            return;
        }

        LocalizationManager.Instance.OnLanguageChanged -= UpdateLocalizedTexts;
        _isLocalizationSubscribed = false;
    }

    // 현재 언어 기준 로그인·약관·회원가입 문구 갱신
    private void UpdateLocalizedTexts()
    {
        LocalizationManager _localization = LocalizationManager.Instance;

        if (_localization == null)
        {
            return;
        }

        SetLocalizedText(_termsTitleText, _localization.Get(11000, LocalizingType.UI));
        SetLocalizedText(_termsToggleLabelText, _localization.Get(11001, LocalizingType.UI));
        SetLocalizedText(_termsConfirmButtonLabelText, _localization.Get(11002, LocalizingType.UI));
        SetLocalizedText(_guestLoginButtonLabelText, _localization.Get(11003, LocalizingType.UI));
    }

    private static void SetLocalizedText(TMP_Text _target, string _value)
    {
        if (_target != null)
        {
            _target.SetText(_value);
        }
    }

    private static T FindChildComponentByName<T>(Transform _root, string _objectName) where T : Component
    {
        if (_root == null)
        {
            return null;
        }

        if (_root.name == _objectName)
        {
            return _root.GetComponent<T>();
        }

        for (int _index = 0; _index < _root.childCount; _index++)
        {
            T _found = FindChildComponentByName<T>(_root.GetChild(_index), _objectName);

            if (_found != null)
            {
                return _found;
            }
        }

        return null;
    }

    // Unity UI 입력을 View 이벤트로 변환
    private void BindButtons()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.AddListener(NotifyGuestLoginClicked);

        if (_googleLoginButton != null)
            _googleLoginButton.onClick.AddListener(NotifyGoogleLoginClicked);

        if (_existingAccountLoginButton != null)
            _existingAccountLoginButton.onClick.AddListener(NotifyExistingAccountLoginClicked);

        if (_registerButton != null)
            _registerButton.onClick.AddListener(NotifyRegisterClicked);

        if (_termsPopupButton != null)
            _termsPopupButton.onClick.AddListener(NotifyTermsPopupClicked);

        if (_popupCloseButton != null)
            _popupCloseButton.onClick.AddListener(NotifyPopupClosed);

        if (_termsToggle != null)
            _termsToggle.onValueChanged.AddListener(NotifyTermsAgreementChanged);

        // 약관 모달 확인 버튼 클릭을 Presenter로 전달
        if (_termsConfirmButton != null)
            _termsConfirmButton.onClick.AddListener(NotifyTermsConfirmed);

        if (_passwordInputField != null)
            _passwordInputField.onSelect.AddListener(ActivatePasswordInputField);
    }

    // 기존 계정 로그인 버튼이 씬에 없으면 회원가입 버튼을 복제해 런타임에 생성
    private void EnsureExistingAccountLoginButton()
    {
        if (_existingAccountLoginButton != null || _registerButton == null)
        {
            return;
        }

        Button clonedButton = Instantiate(_registerButton, _registerButton.transform.parent);
        clonedButton.name = "ExistingAccountLoginButton";
        clonedButton.onClick.RemoveAllListeners();
        _existingAccountLoginButton = clonedButton;

        RectTransform clonedRectTransform = clonedButton.GetComponent<RectTransform>();
        if (clonedRectTransform != null)
        {
            clonedRectTransform.anchorMin = new Vector2(0.5f, 0.29f);
            clonedRectTransform.anchorMax = new Vector2(0.5f, 0.29f);
            clonedRectTransform.anchoredPosition = Vector2.zero;
            clonedRectTransform.sizeDelta = new Vector2(933.3333f, 106f);
        }

        Image buttonImage = clonedButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.18f, 0.38f, 0.72f, 1f);
        }

        TMP_Text labelText = clonedButton.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.SetText("로그인");
        }

        clonedButton.gameObject.SetActive(true);
    }

    // View 파괴 시 Unity UI 리스너를 제거
    private void UnbindButtons()
    {
        if (_guestLoginButton != null)
            _guestLoginButton.onClick.RemoveListener(NotifyGuestLoginClicked);

        if (_googleLoginButton != null)
            _googleLoginButton.onClick.RemoveListener(NotifyGoogleLoginClicked);

        if (_existingAccountLoginButton != null)
            _existingAccountLoginButton.onClick.RemoveListener(NotifyExistingAccountLoginClicked);

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

    // 계정 입력 필드의 키보드 이동과 비밀번호 포커스 연결
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

    // 핵심 UI 참조 누락을 한 번만 경고
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

        // 약관 모달 구성 참조 누락을 Inspector에서 바로 확인할 수 있게 함
        if (_termsAgreementPanel == null)
            Debug.LogWarning("[LoginView] 약관 동의 패널 참조가 없습니다.", this);

        if (_termsConfirmButton == null)
            Debug.LogWarning("[LoginView] 약관 확인 버튼 참조가 없습니다.", this);

        if (_popupPanel == null)
            Debug.LogWarning("[LoginView] 팝업 패널 참조가 없습니다.", this);
    }

    // 게스트 로그인 버튼 입력을 Presenter로 전달
    private void NotifyGuestLoginClicked()
    {
        OnGuestLoginClicked?.Invoke();
    }

    // Google 로그인 버튼 입력을 Presenter로 전달
    private void NotifyGoogleLoginClicked()
    {
        string playerId = _playerIdInputField != null ? _playerIdInputField.text : string.Empty;
        string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
        OnGoogleLoginClicked?.Invoke(playerId, password);
    }

    // 기존 계정 로그인 입력 필드 값을 Presenter로 전달
    private void NotifyExistingAccountLoginClicked()
    {
        string playerId = _playerIdInputField != null ? _playerIdInputField.text : string.Empty;
        string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
        OnExistingAccountLoginClicked?.Invoke(playerId, password);
    }

    // 회원가입 입력 필드 값을 Presenter로 전달
    private void NotifyRegisterClicked()
    {
        string playerId = _playerIdInputField != null ? _playerIdInputField.text : string.Empty;
        string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
        OnRegisterClicked?.Invoke(playerId, password);
    }

    // 약관 보기 버튼 입력을 Presenter로 전달
    private void NotifyTermsPopupClicked()
    {
        OnTermsPopupClicked?.Invoke();
    }

    // 팝업 닫기 버튼 입력을 Presenter로 전달
    private void NotifyPopupClosed()
    {
        OnPopupClosed?.Invoke();
    }

    // 약관 동의 토글 입력을 Presenter로 전달
    private void NotifyTermsAgreementChanged(bool hasAcceptedTerms)
    {
        OnTermsAgreementChanged?.Invoke(hasAcceptedTerms);
    }

    // 약관 확인 버튼 입력을 Presenter로 전달
    private void NotifyTermsConfirmed()
    {
        OnTermsConfirmed?.Invoke();
    }

    // 게스트 로그인 버튼 입력 가능 여부를 제어
    public void SetGuestButtonInteractable(bool isInteractable)
    {
        if (_guestLoginButton != null)
            _guestLoginButton.interactable = isInteractable;
    }

    // Google 로그인 버튼 입력 가능 여부를 제어
    public void SetGoogleButtonInteractable(bool isInteractable)
    {
        if (_googleLoginButton != null)
            _googleLoginButton.interactable = isInteractable;
    }

    public void SetExistingAccountLoginButtonInteractable(bool isInteractable)
    {
        if (_existingAccountLoginButton != null)
            _existingAccountLoginButton.interactable = isInteractable;
    }

    // 회원가입 버튼 입력 가능 여부를 제어
    public void SetRegisterButtonInteractable(bool isInteractable)
    {
        if (_registerButton != null)
            _registerButton.interactable = isInteractable;
    }

    // 회원가입 패널을 표시
    public void ShowRegisterPanel()
    {
        PrepareInputFields();

        if (_registerPanel != null)
            _registerPanel.SetActive(true);
    }

    // 회원가입 패널을 숨김
    public void HideRegisterPanel()
    {
        if (_registerPanel != null)
            _registerPanel.SetActive(false);
    }

    // 회원가입 안내 문구를 표시
    public void SetRegisterMessage(string message)
    {
        if (_registerMessageText != null)
            _registerMessageText.SetText(message);
    }

    // 약관 동의 모달을 표시
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

    // 약관 동의 체크 상태에 따라 확인 버튼 입력 가능 여부를 제어
    public void SetTermsConfirmButtonInteractable(bool isInteractable)
    {
        if (_termsConfirmButton != null)
            _termsConfirmButton.interactable = isInteractable;
    }

    // 로그인 중 로딩 인디케이터를 표시하고 버튼 입력을 잠금
    // 로그인 진행 상태에 따른 로딩 표시와 입력 버튼 잠금
    public void SetLoading(bool isLoading)
    {
        if (_loadingIndicator != null)
            _loadingIndicator.SetActive(isLoading);
    }

    // 앱 버전과 인증 성공 UID를 화면에 표시
    public void SetAccountInfo(string appVersion, string uid)
    {
        SetAppVersionText(appVersion);

        if (_uidText != null)
            _uidText.SetText(string.IsNullOrEmpty(uid) ? string.Empty : "UID: " + uid);
    }

    private void SetAppVersionText(string appVersion)
    {
        if (_appVersionText == null)
        {
            return;
        }

        _appVersionText.SetText("v" + appVersion);
    }

    // 실패/안내 팝업 메시지를 표시
    public void ShowPopup(string message)
    {
        if (_popupMessageText != null)
            _popupMessageText.SetText(message);

        if (_popupPanel != null)
            _popupPanel.SetActive(true);
    }

    // 팝업 패널을 닫음
    public void HidePopup()
    {
        if (_popupPanel != null)
            _popupPanel.SetActive(false);
    }
}
