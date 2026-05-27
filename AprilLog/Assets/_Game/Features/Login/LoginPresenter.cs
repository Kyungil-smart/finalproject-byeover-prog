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
        _view.OnTermsAgreementChanged += HandleTermsAgreementChanged;
        _view.OnTermsPopupClicked += HandleTermsPopupClicked;
        _view.OnPopupClosed += HandlePopupClosed;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoginStarted += HandleLoginStarted;
            GameManager.Instance.OnLoginSucceeded += HandleLoginSucceeded;
            GameManager.Instance.OnLoginFailed += HandleLoginFailed;
        }
        else
        {
            Debug.LogWarning("[LoginPresenter] GameManager가 준비되지 않았습니다.");
        }

        RefreshView();
    }

    // View 파괴 시 이벤트 구독을 해제한다.
    public void Dispose()
    {
        _view.OnGuestLoginClicked -= HandleGuestLoginClicked;
        _view.OnTermsAgreementChanged -= HandleTermsAgreementChanged;
        _view.OnTermsPopupClicked -= HandleTermsPopupClicked;
        _view.OnPopupClosed -= HandlePopupClosed;

        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.OnLoginStarted -= HandleLoginStarted;
        GameManager.Instance.OnLoginSucceeded -= HandleLoginSucceeded;
        GameManager.Instance.OnLoginFailed -= HandleLoginFailed;
    }

    // 약관 동의 전에는 게스트 로그인을 요청하지 않는다.
    private void HandleGuestLoginClicked()
    {
        if (_model.IsSigningIn)
        {
            return;
        }

        if (!_model.HasAcceptedTerms)
        {
            _view.ShowPopup("약관에 동의한 뒤 게스트 로그인을 진행할 수 있습니다.");
            return;
        }

        if (GameManager.Instance == null)
        {
            _view.ShowPopup("게임 매니저가 준비되지 않았습니다.");
            return;
        }

        GameManager.Instance.StartGuestSignIn();
    }

    // 약관 동의 토글 변경 시 버튼 상태를 즉시 갱신한다.
    private void HandleTermsAgreementChanged(bool hasAcceptedTerms)
    {
        _model.SetTermsAgreement(hasAcceptedTerms);
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
        RefreshView();
    }

    // 인증 실패 메시지를 팝업으로 표시하고 재시도 가능 상태로 돌린다.
    private void HandleLoginFailed(string error)
    {
        _model.SetSigningIn(false);
        RefreshView();
        _view.ShowPopup(string.IsNullOrEmpty(error) ? "로그인에 실패했습니다. 다시 시도해 주세요." : error);
    }

    // 현재 모델 상태를 View에 반영한다.
    private void RefreshView()
    {
        _view.SetLoading(_model.IsSigningIn);
        _view.SetGuestButtonInteractable(_model.HasAcceptedTerms && !_model.IsSigningIn);
        _view.SetAccountInfo(Application.version, _model.UserUID);
    }
}
