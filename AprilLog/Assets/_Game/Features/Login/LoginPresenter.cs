// 담당자 : 조규민
// LoginView 입력 이벤트와 인증 서비스 결과 이벤트 등록·해제
// 약관 확인 후 게스트·Google·기존 계정 로그인과 회원가입 흐름 조정
// Model 상태 기반 버튼 잠금·로딩·오류 메시지 UI 갱신
// 구현원리 : View 이벤트를 받아 GameManager에 인증 요청을 위임하고, 기존 계정 로그인 요청과 GameManager 인증 이벤트를 View 상태로 반영한다.

using UnityEngine;

// 로그인 화면 입력과 인증 상태 표시를 연결한다.
public class LoginPresenter
{
    private readonly ILoginView _view;
    private readonly LoginModel _model;
    private const int TermsPolicyLanguageId = 11000;

    // View/Model을 연결하고 GameManager 인증 이벤트를 구독한다.
    public LoginPresenter(ILoginView view, LoginModel model)
    {
        _view = view;
        _model = model;

        _view.OnGuestLoginClicked += HandleGuestLoginClicked;
        _view.OnGoogleLoginClicked += HandleGoogleLoginClicked;
        _view.OnExistingAccountLoginClicked += HandleExistingAccountLoginClicked;
        _view.OnRegisterClicked += HandleRegisterClicked;
        _view.OnTermsAgreementChanged += HandleTermsAgreementChanged;
        _view.OnTermsReadCompleted += HandleTermsReadCompleted;
        _view.OnTermsConfirmed += HandleTermsConfirmed; // 약관 확인 버튼 입력을 로그인 활성화 조건에 반영한다.
        _view.OnTermsPopupClicked += HandleTermsPopupClicked;
        _view.OnPopupClosed += HandlePopupClosed;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoginStarted += HandleLoginStarted;
            GameManager.Instance.OnLoginSucceeded += HandleLoginSucceeded;
            GameManager.Instance.OnLoginFailedWithType += HandleLoginFailedWithType;
            GameManager.Instance.OnRegistrationRequired += HandleRegistrationRequired;
            GameManager.Instance.OnRegistrationFailed += HandleRegistrationFailed;
        }
        else
        {
            Debug.LogWarning("[LoginPresenter] GameManager가 준비되지 않았습니다.");
        }

        // 로그인 화면 진입 시 약관 모달을 먼저 표시하고 확인 전 로그인 버튼을 막는다.
        _view.ShowTermsAgreementPanel();
        RefreshTermsAgreementControls();
        RefreshView();
    }

    // View 파괴 시 이벤트 구독을 해제한다.
    public void Dispose()
    {
        _view.OnGuestLoginClicked -= HandleGuestLoginClicked;
        _view.OnGoogleLoginClicked -= HandleGoogleLoginClicked;
        _view.OnExistingAccountLoginClicked -= HandleExistingAccountLoginClicked;
        _view.OnRegisterClicked -= HandleRegisterClicked;
        _view.OnTermsAgreementChanged -= HandleTermsAgreementChanged;
        _view.OnTermsReadCompleted -= HandleTermsReadCompleted;
        _view.OnTermsConfirmed -= HandleTermsConfirmed;
        _view.OnTermsPopupClicked -= HandleTermsPopupClicked;
        _view.OnPopupClosed -= HandlePopupClosed;

        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnLoginStarted -= HandleLoginStarted;
        GameManager.Instance.OnLoginSucceeded -= HandleLoginSucceeded;
        GameManager.Instance.OnLoginFailedWithType -= HandleLoginFailedWithType;
        GameManager.Instance.OnRegistrationRequired -= HandleRegistrationRequired;
        GameManager.Instance.OnRegistrationFailed -= HandleRegistrationFailed;
    }

    // 약관 확인 전에는 게스트 로그인을 요청하지 않는다.
    private void HandleGuestLoginClicked()
    {
        if (_model.IsSigningIn)
        {
            return;
        }

        if (!_model.HasConfirmedTerms)
        {
            _view.ShowTermsAgreementPanel();
            return;
        }

        if (GameManager.Instance == null)
        {
            _view.ShowPopup("게임 매니저가 준비되지 않았습니다.");
            return;
        }

        _model.SetGoogleLoginRequested(false);
        GameManager.Instance.StartGuestSignIn();
    }

    // 약관 확인 전에는 Google 로그인을 요청하지 않는다.
    private void HandleGoogleLoginClicked(string editorEmail, string editorPassword)
    {
        if (_model.IsSigningIn)
        {
            return;
        }

        if (!_model.HasConfirmedTerms)
        {
            _view.ShowTermsAgreementPanel();
            return;
        }

        if (GameManager.Instance == null)
        {
            _view.ShowPopup("게임 매니저가 준비되지 않았습니다.");
            return;
        }

        if (GameManager.Instance.RequiresEditorGoogleEmailPasswordInput)
        {
            string validationError = GetEditorGoogleLoginValidationError(editorEmail, editorPassword);
            if (!string.IsNullOrEmpty(validationError))
            {
                _view.ShowPopup(validationError);
                return;
            }
        }

        _model.SetGoogleLoginRequested(true);
        GameManager.Instance.StartGoogleSignIn(editorEmail, editorPassword);
    }

    // 기존 Editor Email/Password 테스트 계정으로 로그인만 시도한다.
    private void HandleExistingAccountLoginClicked(string editorEmail, string editorPassword)
    {
        if (_model.IsSigningIn)
        {
            return;
        }

        if (!_model.HasConfirmedTerms)
        {
            _view.ShowTermsAgreementPanel();
            return;
        }

        if (GameManager.Instance == null)
        {
            _view.SetRegisterMessage("게임 매니저가 준비되지 않았습니다.");
            return;
        }

        string validationError = GetEditorGoogleLoginValidationError(editorEmail, editorPassword);
        if (!string.IsNullOrEmpty(validationError))
        {
            _view.SetRegisterMessage(validationError);
            return;
        }

        _model.SetGoogleLoginRequested(true);
        _view.SetRegisterMessage("기존 계정으로 로그인 중입니다.");
        GameManager.Instance.StartExistingEditorGoogleAccountSignIn(editorEmail, editorPassword);
    }

    // 회원가입 입력값을 검증하고 GameManager에 등록 요청을 전달한다.
    private void HandleRegisterClicked(string playerId, string password)
    {
        if (_model.IsSigningIn)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.RequiresEditorGoogleEmailPasswordInput)
        {
            StartEditorGoogleLoginFromInput(playerId, password);
            return;
        }

        string validationError = GetRegistrationValidationError(playerId, password);
        if (!string.IsNullOrEmpty(validationError))
        {
            _view.SetRegisterMessage(validationError);
            return;
        }

        if (GameManager.Instance == null)
        {
            _view.SetRegisterMessage("게임 매니저가 준비되지 않았습니다.");
            return;
        }

        _view.SetRegisterMessage("회원가입 처리 중입니다.");
        GameManager.Instance.RegisterGoogleUser(playerId.Trim(), password);
    }

    // 약관 전문을 끝까지 읽은 뒤에만 동의 토글 입력을 허용한다.
    private void HandleTermsReadCompleted()
    {
        if (_model.HasReadTerms)
        {
            return;
        }

        _model.SetTermsRead(true);
        _view.SetTermsToggleInteractable(true);
        RefreshTermsAgreementControls();
    }

    // 약관 동의 토글 변경 시 완독 상태와 함께 확인 버튼 상태를 갱신한다.
    private void HandleTermsAgreementChanged(bool hasAcceptedTerms)
    {
        if (!_model.HasReadTerms)
        {
            _model.SetTermsAgreement(false);
            RefreshTermsAgreementControls();
            return;
        }

        _model.SetTermsAgreement(hasAcceptedTerms);
        RefreshTermsAgreementControls();
        RefreshView();
    }

    // 약관 체크 후 확인을 눌러야 실제 로그인 버튼을 활성화한다.
    private void HandleTermsConfirmed()
    {
        if (!_model.HasReadTerms || !_model.HasAcceptedTerms)
        {
            return;
        }

        _model.SetTermsConfirmed(true);
        _view.HideTermsAgreementPanel();
        ShowEditorGoogleLoginInputIfNeeded();
        RefreshView();
    }

    private void RefreshTermsAgreementControls()
    {
        _view.SetTermsToggleInteractable(_model.HasReadTerms);
        _view.SetTermsConfirmButtonInteractable(_model.HasReadTerms && _model.HasAcceptedTerms);
    }

    // 약관보기 입력 시 현재 언어의 약관 전문을 표시한다.
    private void HandleTermsPopupClicked()
    {
        if (LocalizationManager.Instance == null)
        {
            _view.ShowTermsPolicyContent(string.Empty);
            return;
        }

        string termsPolicyContent = LocalizationManager.Instance.Get(TermsPolicyLanguageId, LocalizingType.UI);
        _view.ShowTermsPolicyContent(termsPolicyContent);
    }

    // 팝업 닫기 이벤트를 View 표시 함수로 위임한다.
    private void HandlePopupClosed()
    {
        _view.HidePopup();
    }

    // 인증 시작 이벤트를 로딩 상태로 반영한다.
    private void HandleLoginStarted()
    {
        _model.SetSigningIn(true);
        RefreshView();
    }

    // 인증 성공 UID를 표시하고 로딩 상태를 해제한다.
    private void HandleLoginSucceeded(string uid)
    {
        bool wasGoogleLogin = GameManager.Instance != null && GameManager.Instance.LastSignInWasGoogle;

        _model.SetUserUID(uid);
        _model.SetSigningIn(false);
        _view.HideRegisterPanel();
        RefreshView();

        if (wasGoogleLogin)
        {
            _view.ShowPopup(LoginMessageProvider.GetGoogleSuccessMessage());
        }

        _model.SetGoogleLoginRequested(false);
    }

    // 인증 실패 메시지를 팝업으로 표시하고 다시 입력 가능한 상태로 돌린다.
    private void HandleLoginFailed(string error)
    {
        _model.SetSigningIn(false);
        RefreshView();
        _view.ShowPopup(string.IsNullOrEmpty(error) ? "로그인에 실패했습니다. 다시 시도해 주세요." : error);
    }

    // 인증 실패 유형을 사용자 안내 문구로 변환하고 로그인 상태 해제
    private void HandleLoginFailedWithType(AuthLoginFailureType failureType, string error)
    {
        _model.SetSigningIn(false);
        RefreshView();

        // 추가: Google 로그인 시도에서만 지정된 Google 안내 문구를 사용한다.
        if (_model.IsGoogleLoginRequested)
        {
            if (failureType == AuthLoginFailureType.Configuration && !string.IsNullOrWhiteSpace(error))
            {
                _view.ShowPopup(error);
                _model.SetGoogleLoginRequested(false);
                return;
            }

            _view.ShowPopup(LoginMessageProvider.GetGoogleFailureMessage(failureType));
            _model.SetGoogleLoginRequested(false);
            return;
        }

        _view.ShowPopup(string.IsNullOrEmpty(error) ? "로그인에 실패했습니다. 다시 시도해 주세요." : error);
    }

    // Google 신규 사용자가 추가 프로필 등록을 해야 하는 상태를 표시한다.
    private void HandleRegistrationRequired()
    {
        _model.SetSigningIn(false);
        RefreshView();
        _view.ShowRegisterPanel();
        _view.SetRegisterMessage("아이디와 비밀번호를 입력해 회원가입을 완료해 주세요.");
    }

    // 회원가입 실패 메시지를 회원가입 패널에 표시한다.
    private void HandleRegistrationFailed(string error)
    {
        _model.SetSigningIn(false);
        RefreshView();
        _view.ShowRegisterPanel();
        _view.SetRegisterMessage(string.IsNullOrEmpty(error) ? "회원가입에 실패했습니다." : error);
    }

    // 현재 모델 상태를 View에 반영한다.
    private void RefreshView()
    {
        bool canLogin = _model.HasConfirmedTerms && !_model.IsSigningIn;

        _view.SetLoading(_model.IsSigningIn);
        _view.SetGuestButtonInteractable(canLogin);
        _view.SetGoogleButtonInteractable(canLogin);
        _view.SetExistingAccountLoginButtonInteractable(!_model.IsSigningIn);
        _view.SetRegisterButtonInteractable(!_model.IsSigningIn);
        _view.SetAccountInfo(Application.version, _model.UserUID);
    }

    // 에디터 환경에서 Google 대체 로그인 입력 패널 표시
    private void ShowEditorGoogleLoginInputIfNeeded()
    {
        if (GameManager.Instance == null || !GameManager.Instance.RequiresEditorGoogleEmailPasswordInput)
        {
            return;
        }

        _view.ShowRegisterPanel();
        _view.SetRegisterMessage("Editor 테스트 계정 이메일과 비밀번호를 입력한 뒤 Google 로그인을 눌러 주세요.");
    }

    private void StartEditorGoogleLoginFromInput(string email, string password)
    {
        string validationError = GetEditorGoogleLoginValidationError(email, password);
        if (!string.IsNullOrEmpty(validationError))
        {
            _view.SetRegisterMessage(validationError);
            return;
        }

        _model.SetGoogleLoginRequested(true);
        _view.SetRegisterMessage("Editor 테스트 계정으로 로그인 중입니다.");
        GameManager.Instance.StartGoogleSignIn(email, password);
    }

    // 회원가입 입력값 오류 메시지를 반환한다.
    private string GetRegistrationValidationError(string playerId, string password)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return "아이디를 입력해 주세요.";
        }

        if (playerId.Trim().Length < 2 || playerId.Trim().Length > 20)
        {
            return "아이디는 2~20자로 입력해 주세요.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "비밀번호를 입력해 주세요.";
        }

        if (password.Length < 4)
        {
            return "테스트 비밀번호는 4자 이상 입력해 주세요.";
        }

        return null;
    }

    // 에디터 Google 대체 로그인 이메일과 비밀번호 입력값 검증
    private string GetEditorGoogleLoginValidationError(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Editor 테스트 이메일을 입력해 주세요.";
        }

        if (!email.Contains("@"))
        {
            return "Editor 테스트 이메일 형식이 올바르지 않습니다.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Editor 테스트 비밀번호를 입력해 주세요.";
        }

        return null;
    }
}
