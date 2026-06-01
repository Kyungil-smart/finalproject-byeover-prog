//담당자: 조규민

// 로그인 결과 안내 문구를 한 곳에서 관리한다.
public static class LoginMessageProvider
{
    // Google 로그인 결과별 안내 문구를 Presenter가 한 곳에서 가져가도록 관리한다.
    private const string GOOGLE_SUCCESS_MESSAGE = "구글 계정으로 로그인되었습니다.";
    private const string GOOGLE_GENERAL_FAILURE_MESSAGE = "구글 로그인에 실패했습니다. 다시 시도해 주세요.";
    private const string GOOGLE_CANCELED_MESSAGE = "네트워크 연결을 확인한 뒤 다시 시도해 주세요.";
    private const string FIREBASE_AUTH_FAILURE_MESSAGE = "로그인 처리 중 문제가 발생했습니다. 잠시 후 다시 시도해 주세요.";

    // Google 로그인 성공 시 표시할 문구를 반환한다.
    public static string GetGoogleSuccessMessage()
    {
        return GOOGLE_SUCCESS_MESSAGE;
    }

    // 실패 유형에 맞는 Google 로그인 안내 문구를 반환한다.
    public static string GetGoogleFailureMessage(AuthLoginFailureType failureType)
    {
        switch (failureType)
        {
            case AuthLoginFailureType.Canceled:
                return GOOGLE_CANCELED_MESSAGE;
            case AuthLoginFailureType.FirebaseAuth:
                return FIREBASE_AUTH_FAILURE_MESSAGE;
            default:
                return GOOGLE_GENERAL_FAILURE_MESSAGE;
        }
    }
}
