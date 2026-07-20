// 담당자 : 조규민
// 약관 동의·로그인 진행·UID 등 로그인 UI 상태 저장과 변경 이벤트 발행
// 구현원리 : 로그인 화면에서 필요한 약관 동의, 로그인 진행, UID 상태를 순수 C# 모델로 보관한다.

/// <summary>
/// 로그인 화면 상태를 보관한다.
/// </summary>
public class LoginModel
{
    public bool HasAcceptedTerms { get; private set; }
    public bool HasReadTerms { get; private set; }
    public bool HasConfirmedTerms { get; private set; } // 약관 체크 후 확인 버튼까지 누른 상태를 별도로 보관한다.
    public bool IsSigningIn { get; private set; }
    public bool IsGoogleLoginRequested { get; private set; }
    public string UserUID { get; private set; }

    // 약관 동의 토글 변경 값을 모델 상태로 저장한다.
    public void SetTermsAgreement(bool hasAcceptedTerms)
    {
        HasAcceptedTerms = hasAcceptedTerms;
    }

    // 약관 모달 확인 완료 여부를 모델 상태로 저장한다.
    public void SetTermsConfirmed(bool hasConfirmedTerms)
    {
        HasConfirmedTerms = hasConfirmedTerms;
    }

    // 로그인 진행 상태를 저장해 버튼 중복 입력을 막는다.
    public void SetSigningIn(bool isSigningIn)
    {
        IsSigningIn = isSigningIn;
    }

    // 약관 전문을 마지막까지 확인했는지 저장한다.
    public void SetTermsRead(bool hasReadTerms)
    {
        HasReadTerms = hasReadTerms;
    }

    // 추가: Google 로그인 요청 중 발생한 실패만 Google 안내 문구로 보여주기 위해 상태를 보관한다.
    public void SetGoogleLoginRequested(bool isGoogleLoginRequested)
    {
        IsGoogleLoginRequested = isGoogleLoginRequested;
    }

    // 인증 성공 후 Firebase UID를 화면 표시용으로 저장한다.
    public void SetUserUID(string uid)
    {
        UserUID = uid;
    }
}
