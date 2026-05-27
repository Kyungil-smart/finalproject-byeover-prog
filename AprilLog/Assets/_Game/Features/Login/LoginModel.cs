// 담당자 : 조규민
// 구현원리 : 로그인 화면에서 필요한 약관 동의, 로그인 진행, UID 상태를 순수 C# 모델로 보관한다.

/// <summary>
/// 로그인 화면 상태를 보관한다.
/// </summary>
public class LoginModel
{
    public bool HasAcceptedTerms { get; private set; }
    public bool IsSigningIn { get; private set; }
    public string UserUID { get; private set; }

    // 약관 토글 값 변경을 모델 상태로 저장한다.
    public void SetTermsAgreement(bool hasAcceptedTerms)
    {
        HasAcceptedTerms = hasAcceptedTerms;
    }

    // 로그인 진행 상태를 저장해 버튼 중복 입력을 막는다.
    public void SetSigningIn(bool isSigningIn)
    {
        IsSigningIn = isSigningIn;
    }

    // 인증 성공 후 Firebase UID를 화면 표시용으로 저장한다.
    public void SetUserUID(string uid)
    {
        UserUID = uid;
    }
}
