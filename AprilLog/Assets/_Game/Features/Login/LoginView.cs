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
    public event Action OnTermsReadCompleted;
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
    [SerializeField] private TMP_Text _termsPopupButtonLabelText;
    [SerializeField] private ScrollRect _termsPolicyScrollRect;
    [SerializeField] private TMP_Text _termsPolicyContentText;
    [SerializeField] private TMP_Text _guestLoginButtonLabelText;

    [Header("약관 이미지")]
    [SerializeField] private Sprite _termsBoxSprite;
    [SerializeField] private Sprite _termsPolicyScrollViewSprite;
    [SerializeField] private Sprite _termsConfirmButtonSprite;
    [SerializeField] private Sprite _termsCheckBoxSprite;
    [SerializeField] private Sprite _termsCheckmarkSprite;

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
    private TMP_Text _registerTitleText;
    private TMP_Text _registerButtonLabelText;
    private TMP_Text _existingAccountLoginButtonLabelText;
    private TMP_Text _loadingIndicatorText;
    private RectTransform _passwordInputRectTransform;
    private bool _isLocalizationSubscribed;
    private bool _isTermsPolicyContentVisible;
    private bool _hasNotifiedTermsRead;
    private bool _hasCachedTermsToggleLabelColor;
    private bool _hasCachedTermsConfirmLabelColor;
    private Color _termsToggleLabelColor;
    private Color _termsConfirmLabelColor;
    private const int TermsPolicyLanguageId = 11000;
    private const float TermsReadThreshold = 0.01f;
    private const float DisabledLabelAlpha = 0.45f;

    // View 생성 시 Model과 버튼 이벤트를 준비
    private void Awake()
    {
        _model = new LoginModel();

        if (_guestLoginButton != null || _googleLoginButton != null || _termsToggle != null || _popupPanel != null)
        {
            EnsureExistingAccountLoginButton();
            EnsureTermsPolicyView();
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
        _termsPopupButtonLabelText ??= FindChildComponentByName<TMP_Text>(_termsPopupButton?.transform, "Label");
        _guestLoginButtonLabelText ??= FindChildComponentByName<TMP_Text>(_guestLoginButton?.transform, "Label");
        _registerTitleText ??= FindChildComponentByName<TMP_Text>(_registerPanel?.transform, "RegisterTitleText");
        _registerButtonLabelText ??= _registerButton?.GetComponentInChildren<TMP_Text>(true);
        _existingAccountLoginButtonLabelText ??= _existingAccountLoginButton?.GetComponentInChildren<TMP_Text>(true);
        _loadingIndicatorText ??= _loadingIndicator?.GetComponent<TMP_Text>();
        _termsToggleLabelText ??= _termsToggle?.GetComponentInChildren<TMP_Text>(true);
        _termsConfirmButtonLabelText ??= _termsConfirmButton?.GetComponentInChildren<TMP_Text>(true);
        _termsPopupButtonLabelText ??= _termsPopupButton?.GetComponentInChildren<TMP_Text>(true);
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

        bool _isKorean = _localization.CurrentLanguage == "ko";

        SetLocalizedText(_termsTitleText, _isKorean ? "개인정보 처리방침" : "Privacy Policy");
        SetLocalizedText(_termsToggleLabelText, _localization.Get(11001, LocalizingType.UI));
        SetLocalizedText(_termsConfirmButtonLabelText, _localization.Get(11002, LocalizingType.UI));
        SetLocalizedText(_termsPopupButtonLabelText, _isKorean ? "약관보기" : "View Terms");
        SetLocalizedText(_guestLoginButtonLabelText, _localization.Get(11003, LocalizingType.UI));
        SetLocalizedText(_registerTitleText, _isKorean ? "회원가입" : "Sign Up");
        SetLocalizedText(_registerButtonLabelText, _isKorean ? "회원가입" : "Sign Up");
        SetLocalizedText(_existingAccountLoginButtonLabelText, _isKorean ? "로그인" : "Log In");
        SetLocalizedText(_loadingIndicatorText, _isKorean ? "로그인 중..." : "Logging in...");

        if (_playerIdInputField != null)
            SetLocalizedText(_playerIdInputField.placeholder as TMP_Text, _isKorean ? "아이디" : "ID");
        if (_passwordInputField != null)
            SetLocalizedText(_passwordInputField.placeholder as TMP_Text, _isKorean ? "비밀번호" : "Password");

        if (_isTermsPolicyContentVisible)
        {
            SetLocalizedText(_termsPolicyContentText, _localization.Get(TermsPolicyLanguageId, LocalizingType.UI));
        }
    }

    private static void SetLocalizedText(TMP_Text _target, string _value)
    {
        if (_target != null)
        {
            _target.SetText(_value);
        }
    }

    private void EnsureTermsPolicyView()
    {
        if (_termsAgreementPanel == null)
        {
            return;
        }

        Transform _termsBoxTransform = ResolveTermsBoxTransform();
        _termsPopupButton ??= FindChildComponentByName<Button>(_termsBoxTransform, "TermsPopupButton");
        _termsPolicyScrollRect ??= FindChildComponentByName<ScrollRect>(_termsBoxTransform, "TermsPolicyScrollView");
        _termsPolicyContentText ??= FindChildComponentByName<TMP_Text>(_termsBoxTransform, "TermsPolicyContentText");

        if (_termsPopupButton != null)
        {
            _termsPopupButton.gameObject.SetActive(false);
        }

        if (_termsPolicyScrollRect == null || _termsPolicyContentText == null)
        {
            CreateTermsPolicyScrollView(_termsBoxTransform);
        }

        ArrangeTermsAgreementPanel();
        ApplyTermsSprites();
        SetTermsPolicyContentVisible(true);
    }

    private Transform ResolveTermsBoxTransform()
    {
        if (_termsConfirmButton != null)
        {
            return _termsConfirmButton.transform.parent;
        }

        if (_termsToggle != null)
        {
            return _termsToggle.transform.parent;
        }

        return _termsAgreementPanel.transform;
    }

    private Button CreateTermsPopupButton(Transform _parent)
    {
        GameObject _buttonObject = new GameObject("TermsPopupButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        _buttonObject.transform.SetParent(_parent, false);

        RectTransform _buttonRectTransform = _buttonObject.GetComponent<RectTransform>();
        _buttonRectTransform.anchorMin = new Vector2(0.5f, 0.58f);
        _buttonRectTransform.anchorMax = new Vector2(0.5f, 0.58f);
        _buttonRectTransform.anchoredPosition = Vector2.zero;
        _buttonRectTransform.sizeDelta = new Vector2(360f, 72f);

        Image _buttonImage = _buttonObject.GetComponent<Image>();
        _buttonImage.color = new Color(1f, 1f, 1f, 0.01f);
        _buttonImage.raycastTarget = true;

        Button _button = _buttonObject.GetComponent<Button>();
        _button.targetGraphic = _buttonImage;

        GameObject _labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        _labelObject.transform.SetParent(_buttonObject.transform, false);

        RectTransform _labelRectTransform = _labelObject.GetComponent<RectTransform>();
        _labelRectTransform.anchorMin = Vector2.zero;
        _labelRectTransform.anchorMax = Vector2.one;
        _labelRectTransform.offsetMin = Vector2.zero;
        _labelRectTransform.offsetMax = Vector2.zero;

        _termsPopupButtonLabelText = _labelObject.GetComponent<TMP_Text>();
        _termsPopupButtonLabelText.SetText("약관보기");
        _termsPopupButtonLabelText.fontSize = 42f;
        _termsPopupButtonLabelText.alignment = TextAlignmentOptions.Center;
        _termsPopupButtonLabelText.color = new Color(0.06f, 0.16f, 0.32f, 1f);
        _termsPopupButtonLabelText.raycastTarget = false;

        return _button;
    }

    private void CreateTermsPolicyScrollView(Transform _parent)
    {
        GameObject _scrollObject = new GameObject("TermsPolicyScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        _scrollObject.transform.SetParent(_parent, false);

        RectTransform _scrollRectTransform = _scrollObject.GetComponent<RectTransform>();
        _scrollRectTransform.anchorMin = new Vector2(0.5f, 0.52f);
        _scrollRectTransform.anchorMax = new Vector2(0.5f, 0.52f);
        _scrollRectTransform.anchoredPosition = new Vector2(0f, 60f);
        _scrollRectTransform.sizeDelta = new Vector2(980f, 500f);

        Image _scrollImage = _scrollObject.GetComponent<Image>();
        _scrollImage.sprite = _termsPolicyScrollViewSprite;
        _scrollImage.color = new Color(0.97f, 0.91f, 0.8f, 0.94f);

        GameObject _viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        _viewportObject.transform.SetParent(_scrollObject.transform, false);

        RectTransform _viewportRectTransform = _viewportObject.GetComponent<RectTransform>();
        _viewportRectTransform.anchorMin = Vector2.zero;
        _viewportRectTransform.anchorMax = Vector2.one;
        _viewportRectTransform.offsetMin = new Vector2(28f, 28f);
        _viewportRectTransform.offsetMax = new Vector2(-28f, -28f);

        Image _viewportImage = _viewportObject.GetComponent<Image>();
        _viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        Mask _viewportMask = _viewportObject.GetComponent<Mask>();
        _viewportMask.showMaskGraphic = false;

        GameObject _contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        _contentObject.transform.SetParent(_viewportObject.transform, false);

        RectTransform _contentRectTransform = _contentObject.GetComponent<RectTransform>();
        _contentRectTransform.anchorMin = new Vector2(0f, 1f);
        _contentRectTransform.anchorMax = new Vector2(1f, 1f);
        _contentRectTransform.pivot = new Vector2(0.5f, 1f);
        _contentRectTransform.offsetMin = Vector2.zero;
        _contentRectTransform.offsetMax = Vector2.zero;

        VerticalLayoutGroup _layoutGroup = _contentObject.GetComponent<VerticalLayoutGroup>();
        _layoutGroup.childAlignment = TextAnchor.UpperCenter;
        _layoutGroup.childControlWidth = true;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.childForceExpandWidth = true;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.spacing = 28f;
        _layoutGroup.padding = new RectOffset(18, 18, 18, 18);

        ContentSizeFitter _contentSizeFitter = _contentObject.GetComponent<ContentSizeFitter>();
        _contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _termsPolicyScrollRect = _scrollObject.GetComponent<ScrollRect>();
        _termsPolicyScrollRect.viewport = _viewportRectTransform;
        _termsPolicyScrollRect.content = _contentRectTransform;
        _termsPolicyScrollRect.horizontal = false;
        _termsPolicyScrollRect.vertical = true;
        _termsPolicyScrollRect.movementType = ScrollRect.MovementType.Clamped;
        _termsPolicyScrollRect.scrollSensitivity = 48f;

        GameObject _contentTextObject = new GameObject("TermsPolicyContentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        _contentTextObject.transform.SetParent(_contentObject.transform, false);

        LayoutElement _contentLayoutElement = _contentTextObject.GetComponent<LayoutElement>();
        _contentLayoutElement.minHeight = 1800f;
        _contentLayoutElement.flexibleWidth = 1f;

        _termsPolicyContentText = _contentTextObject.GetComponent<TMP_Text>();
        _termsPolicyContentText.SetText(string.Empty);
        _termsPolicyContentText.fontSize = 28f;
        _termsPolicyContentText.alignment = TextAlignmentOptions.TopLeft;
        _termsPolicyContentText.color = Color.black;
        _termsPolicyContentText.textWrappingMode = TextWrappingModes.Normal;
        _termsPolicyContentText.raycastTarget = false;
    }

    private void ArrangeTermsAgreementPanel()
    {
        ArrangeTermsTitle();
        ArrangeTermsPolicyScrollView();
        ArrangeTermsBottomControls();
    }

    private void ArrangeTermsTitle()
    {
        if (_termsTitleText == null)
        {
            return;
        }

        RectTransform _titleRectTransform = _termsTitleText.GetComponent<RectTransform>();
        if (_titleRectTransform != null)
        {
            _titleRectTransform.anchorMin = new Vector2(0.5f, 0.84f);
            _titleRectTransform.anchorMax = new Vector2(0.5f, 0.84f);
            _titleRectTransform.anchoredPosition = new Vector2(0f, 71f);
            _titleRectTransform.sizeDelta = new Vector2(980f, 96f);
        }

        _termsTitleText.fontSize = 56f;
        _termsTitleText.alignment = TextAlignmentOptions.Center;
    }

    private void ArrangeTermsPolicyScrollView()
    {
        if (_termsPolicyScrollRect == null)
        {
            return;
        }

        RectTransform _scrollRectTransform = _termsPolicyScrollRect.GetComponent<RectTransform>();
        if (_scrollRectTransform == null)
        {
            return;
        }

        _scrollRectTransform.anchorMin = new Vector2(0.5f, 0.53f);
        _scrollRectTransform.anchorMax = new Vector2(0.5f, 0.53f);
        _scrollRectTransform.anchoredPosition = new Vector2(0f, 60f);
        _scrollRectTransform.sizeDelta = new Vector2(980f, 500f);
    }

    private void ArrangeTermsBottomControls()
    {
        ArrangeBottomControl(_termsToggle?.GetComponent<RectTransform>(), new Vector2(0.5f, 0.25f), new Vector2(0f, 20f), new Vector2(940f, 96f));
        ArrangeBottomControl(_termsConfirmButton?.GetComponent<RectTransform>(), new Vector2(0.5f, 0.12f), new Vector2(0f, 10f), new Vector2(333.3333f, 114.6667f));
    }

    private void ArrangeBottomControl(RectTransform _rectTransform, Vector2 _anchor, Vector2 _position, Vector2 _size)
    {
        if (_rectTransform == null)
        {
            return;
        }

        Transform _termsBoxTransform = ResolveTermsBoxTransform();
        if (_rectTransform.parent != _termsBoxTransform)
        {
            _rectTransform.SetParent(_termsBoxTransform, false);
        }

        _rectTransform.anchorMin = _anchor;
        _rectTransform.anchorMax = _anchor;
        _rectTransform.anchoredPosition = _position;
        _rectTransform.sizeDelta = _size;
    }

    private void MoveTermsControlsToPolicyContent()
    {
        if (_termsPolicyScrollRect == null || _termsPolicyScrollRect.content == null)
        {
            return;
        }

        MoveControlToPolicyContent(_termsToggle?.transform, 106f);
        MoveControlToPolicyContent(_termsConfirmButton?.transform, 116f);
    }

    private void MoveControlToPolicyContent(Transform _controlTransform, float _preferredHeight)
    {
        if (_controlTransform == null || _termsPolicyScrollRect.content == null)
        {
            return;
        }

        _controlTransform.SetParent(_termsPolicyScrollRect.content, false);

        LayoutElement _layoutElement = _controlTransform.GetComponent<LayoutElement>();
        _layoutElement ??= _controlTransform.gameObject.AddComponent<LayoutElement>();
        _layoutElement.minHeight = _preferredHeight;
        _layoutElement.preferredHeight = _preferredHeight;
        _layoutElement.flexibleWidth = 1f;
    }

    private void SetTermsPolicyContentVisible(bool _isVisible)
    {
        _isTermsPolicyContentVisible = _isVisible;

        if (_termsPolicyScrollRect != null)
        {
            _termsPolicyScrollRect.gameObject.SetActive(_isVisible);
        }

        if (_termsToggle != null)
        {
            _termsToggle.gameObject.SetActive(_isVisible);
        }

        if (_termsConfirmButton != null)
        {
            _termsConfirmButton.gameObject.SetActive(_isVisible);
        }
    }

    private void ApplyTermsSprites()
    {
        Transform _termsBoxTransform = ResolveTermsBoxTransform();
        ApplySprite(_termsBoxTransform?.GetComponent<Image>(), _termsBoxSprite, false);
        ApplySprite(_termsPolicyScrollRect?.GetComponent<Image>(), _termsPolicyScrollViewSprite, false);
        ApplySprite(_termsConfirmButton?.GetComponent<Image>(), _termsConfirmButtonSprite, false);

        Image _checkBoxImage = FindChildComponentByName<Image>(_termsToggle?.transform, "CheckBox");
        Image _checkmarkImage = FindChildComponentByName<Image>(_termsToggle?.transform, "Checkmark");
        ApplySprite(_checkBoxImage, _termsCheckBoxSprite, false);
        ApplySprite(_checkmarkImage, _termsCheckmarkSprite, true);

        if (_termsToggle != null)
        {
            _termsToggle.targetGraphic = _checkBoxImage;
            _termsToggle.graphic = _checkmarkImage;
        }
    }

    private static void ApplySprite(Image _image, Sprite _sprite, bool _preserveAspect)
    {
        if (_image == null || _sprite == null)
        {
            return;
        }

        _image.sprite = _sprite;
        _image.type = Image.Type.Simple;
        _image.preserveAspect = _preserveAspect;
    }

    private void ResetTermsReadingState()
    {
        _hasNotifiedTermsRead = false;

        if (_termsToggle != null)
        {
            _termsToggle.SetIsOnWithoutNotify(false);
        }

        SetTermsToggleInteractable(false);
        SetTermsConfirmButtonInteractable(false);
    }

    private void NotifyTermsScrollChanged(Vector2 _normalizedPosition)
    {
        TryNotifyTermsReadCompleted();
    }

    private void TryNotifyTermsReadCompleted()
    {
        if (_hasNotifiedTermsRead || _termsPolicyScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform _content = _termsPolicyScrollRect.content;
        RectTransform _viewport = _termsPolicyScrollRect.viewport;

        if (_content == null || _viewport == null)
        {
            return;
        }

        bool _doesNotRequireScrolling = _content.rect.height <= _viewport.rect.height + 0.5f;
        bool _hasReachedBottom = _termsPolicyScrollRect.verticalNormalizedPosition <= TermsReadThreshold;

        if (!_doesNotRequireScrolling && !_hasReachedBottom)
        {
            return;
        }

        _hasNotifiedTermsRead = true;
        OnTermsReadCompleted?.Invoke();
    }

    private void ApplyTermsPolicyContentFromLocalization()
    {
        if (LocalizationManager.Instance == null)
        {
            SetLocalizedText(_termsPolicyContentText, "약관 정보를 불러올 수 없습니다.");
            return;
        }

        SetLocalizedText(_termsPolicyContentText, LocalizationManager.Instance.Get(TermsPolicyLanguageId, LocalizingType.UI));
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

        if (_termsPolicyScrollRect != null)
            _termsPolicyScrollRect.onValueChanged.AddListener(NotifyTermsScrollChanged);

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
            _existingAccountLoginButtonLabelText = labelText;
            bool isKorean = LocalizationManager.Instance == null || LocalizationManager.Instance.CurrentLanguage == "ko";
            labelText.SetText(isKorean ? "로그인" : "Log In");
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

        if (_termsPolicyScrollRect != null)
            _termsPolicyScrollRect.onValueChanged.RemoveListener(NotifyTermsScrollChanged);

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
        EnsureTermsPolicyView();
        ApplyTermsPolicyContentFromLocalization();
        ResetTermsReadingState();

        if (_termsAgreementPanel != null)
            _termsAgreementPanel.SetActive(true);

        SetTermsPolicyContentVisible(true);
        Canvas.ForceUpdateCanvases();

        if (_termsPolicyScrollRect != null)
        {
            _termsPolicyScrollRect.verticalNormalizedPosition = 1f;
        }

        TryNotifyTermsReadCompleted();
    }

    // 약관 동의 모달을 숨긴다.
    public void HideTermsAgreementPanel()
    {
        if (_termsAgreementPanel != null)
            _termsAgreementPanel.SetActive(false);
    }

    public void ShowTermsPolicyContent(string content)
    {
        EnsureTermsPolicyView();

        if (_termsAgreementPanel != null)
        {
            _termsAgreementPanel.SetActive(true);
        }

        if (_termsPolicyContentText != null)
        {
            _termsPolicyContentText.SetText(string.IsNullOrEmpty(content) ? "약관 정보를 불러올 수 없습니다." : content);
        }

        SetTermsPolicyContentVisible(true);
        Canvas.ForceUpdateCanvases();

        if (_termsPolicyScrollRect != null)
        {
            _termsPolicyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    // 약관 동의 체크 상태에 따라 확인 버튼 입력 가능 여부를 제어
    public void SetTermsConfirmButtonInteractable(bool isInteractable)
    {
        if (_termsConfirmButton != null)
            _termsConfirmButton.interactable = isInteractable;
    }

    public void SetTermsToggleInteractable(bool isInteractable)
    {
        if (_termsToggle != null)
        {
            _termsToggle.interactable = isInteractable;
        }

        SetTermsLabelInteractable(
            ref _termsToggleLabelText,
            _termsToggle?.transform,
            ref _termsToggleLabelColor,
            ref _hasCachedTermsToggleLabelColor,
            isInteractable);
        SetTermsLabelInteractable(
            ref _termsConfirmButtonLabelText,
            _termsConfirmButton?.transform,
            ref _termsConfirmLabelColor,
            ref _hasCachedTermsConfirmLabelColor,
            isInteractable);
    }

    private void SetTermsLabelInteractable(
        ref TMP_Text _label,
        Transform _root,
        ref Color _activeColor,
        ref bool _hasCachedColor,
        bool _isInteractable)
    {
        _label ??= FindChildComponentByName<TMP_Text>(_root, "Label");

        if (_label == null)
        {
            return;
        }

        if (!_hasCachedColor)
        {
            _activeColor = _label.color;
            _hasCachedColor = true;
        }

        Color _labelColor = _activeColor;
        _labelColor.a = _isInteractable ? _activeColor.a : _activeColor.a * DisabledLabelAlpha;
        _label.color = _labelColor;
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
