// 담당자 : 조규민
// 구현원리 : View 이벤트를 받아 GameManager에 인증 요청을 위임하고, GameManager 인증 이벤트를 다시 View 상태로 반영한다.

using UnityEngine;

// 로그인 화면 입력과 인증 상태 표시를 연결한다.
public class LoginPresenter
{
    private readonly ILoginView _view;
    private readonly LoginModel _model;

    // View/Model을 연결하고 GameManager 인증 이벤트를 구독한다.
    public LoginPresenter(ILoginView view, LoginModel model)
    {
        _view = view;
        _model = model;

        _view.OnGuestLoginClicked += HandleGuestLoginClicked;
        _view.OnGoogleLoginClicked += HandleGoogleLoginClicked;
        _view.OnRegisterClicked += HandleRegisterClicked;
        _view.OnTermsAgreementChanged += HandleTermsAgreementChanged;
        _view.OnTermsConfirmed += HandleTermsConfirmed; // 약관 확인 버튼 입력을 로그인 활성화 조건에 반영한다.
        _view.OnTermsPopupClicked += HandleTermsPopupClicked;
        _view.OnPopupClosed += HandlePopupClosed;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoginStarted += HandleLoginStarted;
            GameManager.Instance.OnLoginSucceeded += HandleLoginSucceeded;
            GameManager.Instance.OnLoginFailed += HandleLoginFailed;
            GameManager.Instance.OnRegistrationRequired += HandleRegistrationRequired;
            GameManager.Instance.OnRegistrationFailed += HandleRegistrationFailed;
        }
        else
        {
            Debug.LogWarning("[LoginPresenter] GameManager가 준비되지 않았습니다.");
        }

        // 로그인 화면 진입 시 약관 모달을 먼저 표시하고 확인 전 로그인 버튼을 막는다.
        _view.ShowTermsAgreementPanel();
        _view.SetTermsConfirmButtonInteractable(false);
        RefreshView();
    }

    // View 파괴 시 이벤트 구독을 해제한다.
    public void Dispose()
    {
        _view.OnGuestLoginClicked -= HandleGuestLoginClicked;
        _view.OnGoogleLoginClicked -= HandleGoogleLoginClicked;
        _view.OnRegisterClicked -= HandleRegisterClicked;
        _view.OnTermsAgreementChanged -= HandleTermsAgreementChanged;
        _view.OnTermsConfirmed -= HandleTermsConfirmed;
        _view.OnTermsPopupClicked -= HandleTermsPopupClicked;
        _view.OnPopupClosed -= HandlePopupClosed;

        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnLoginStarted -= HandleLoginStarted;
        GameManager.Instance.OnLoginSucceeded -= HandleLoginSucceeded;
        GameManager.Instance.OnLoginFailed -= HandleLoginFailed;
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

        GameManager.Instance.StartGuestSignIn();
    }

    // 약관 확인 전에는 Google 로그인을 요청하지 않는다.
    private void HandleGoogleLoginClicked()
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

        GameManager.Instance.StartGoogleSignIn();
    }

    // 회원가입 입력값을 검증하고 GameManager에 등록 요청을 전달한다.
    private void HandleRegisterClicked(string playerId, string password)
    {
        if (_model.IsSigningIn)
        {
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

    // 약관 동의 토글 변경 시 확인 버튼 상태를 즉시 갱신한다.
    private void HandleTermsAgreementChanged(bool hasAcceptedTerms)
    {
        _model.SetTermsAgreement(hasAcceptedTerms);
        _view.SetTermsConfirmButtonInteractable(hasAcceptedTerms);
        RefreshView();
    }

    // 약관 체크 후 확인을 눌러야 실제 로그인 버튼을 활성화한다.
    private void HandleTermsConfirmed()
    {
        if (!_model.HasAcceptedTerms)
        {
            return;
        }

        _model.SetTermsConfirmed(true);
        _view.HideTermsAgreementPanel();
        RefreshView();
    }

    // 약관 상세 팝업은 현재 안내 메시지로 표시한다.
    private void HandleTermsPopupClicked()
    {
        _view.ShowPopup("게스트 로그인 이용을 위해 개인정보 처리 및 서비스 이용 약관 동의가 필요합니다.");
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
        _model.SetUserUID(uid);
        _model.SetSigningIn(false);
        _view.HideRegisterPanel();
        RefreshView();
    }

    // 인증 실패 메시지를 팝업으로 표시하고 다시 입력 가능한 상태로 돌린다.
    private void HandleLoginFailed(string error)
    {
        _model.SetSigningIn(false);
        RefreshView();
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
        _view.SetRegisterButtonInteractable(!_model.IsSigningIn);
        _view.SetAccountInfo(Application.version, _model.UserUID);
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
}
