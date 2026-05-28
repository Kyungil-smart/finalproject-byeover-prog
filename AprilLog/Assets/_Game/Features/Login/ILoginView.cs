// 담당자 : 조규민
// 구현원리 : LoginView가 Unity UI 입력만 이벤트로 전달하고, Presenter가 인증 흐름을 제어할 수 있도록 View 계약을 정의한다.

using System;

// 로그인 화면 View가 Presenter에 제공해야 하는 UI 표시와 입력 이벤트 계약이다.
public interface ILoginView
{
    event Action OnGuestLoginClicked;
    event Action OnGoogleLoginClicked;
    event Action<string, string> OnRegisterClicked;
    event Action<bool> OnTermsAgreementChanged;
    event Action OnTermsPopupClicked;
    event Action OnPopupClosed;

    // 약관 동의 상태에 따라 게스트 로그인 버튼 활성화를 제어한다.
    void SetGuestButtonInteractable(bool isInteractable);
    void SetGoogleButtonInteractable(bool isInteractable);
    void SetRegisterButtonInteractable(bool isInteractable);
    void ShowRegisterPanel();
    void HideRegisterPanel();
    void SetRegisterMessage(string message);

    // Firebase 인증 진행 중 로딩 표시와 입력 잠금을 제어한다.
    void SetLoading(bool isLoading);

    // 앱 버전과 UID 같은 보조 정보를 표시한다.
    void SetAccountInfo(string appVersion, string uid);

    // 로그인 실패나 약관 안내 메시지를 팝업으로 표시한다.
    void ShowPopup(string message);

    // 팝업 확인 버튼 입력 시 팝업을 닫는다.
    void HidePopup();
}
