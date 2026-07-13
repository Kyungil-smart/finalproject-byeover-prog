//담당자: 조규민
// Google 로그인 결과와 실패 유형에 대응하는 사용자 안내 문구 제공

// 로그인 결과 안내 문구를 한 곳에서 관리한다.
public static class LoginMessageProvider
{
    // Google 로그인 결과별 안내 문구를 Presenter가 한 곳에서 가져가도록 관리한다.
    private const string GOOGLE_SUCCESS_MESSAGE = "구글 계정으로 로그인되었습니다.";
    private const string GOOGLE_GENERAL_FAILURE_MESSAGE = "구글 로그인에 실패했습니다. 다시 시도해 주세요.";
    private const string GOOGLE_CANCELED_MESSAGE = "구글 로그인이 취소되었습니다.";
    private const string GOOGLE_NETWORK_FAILURE_MESSAGE = "네트워크 연결을 확인한 뒤 다시 시도해 주세요.";
    private const string GOOGLE_CONFIGURATION_FAILURE_MESSAGE = "Google 로그인 설정을 확인해 주세요.";
    private const string GOOGLE_TIMEOUT_FAILURE_MESSAGE = "로그인 응답 시간이 초과되었습니다. 다시 시도해 주세요.";
    private const string FIREBASE_AUTH_FAILURE_MESSAGE = "로그인 처리 중 문제가 발생했습니다. 잠시 후 다시 시도해 주세요.";
    private const string GOOGLE_SUCCESS_MESSAGE_EN = "Signed in with your Google account.";
    private const string GOOGLE_GENERAL_FAILURE_MESSAGE_EN = "Google sign-in failed. Please try again.";
    private const string GOOGLE_CANCELED_MESSAGE_EN = "Google sign-in was canceled.";
    private const string GOOGLE_NETWORK_FAILURE_MESSAGE_EN = "Check your network connection and try again.";
    private const string GOOGLE_CONFIGURATION_FAILURE_MESSAGE_EN = "Check the Google sign-in configuration.";
    private const string GOOGLE_TIMEOUT_FAILURE_MESSAGE_EN = "The login request timed out. Please try again.";
    private const string FIREBASE_AUTH_FAILURE_MESSAGE_EN = "There was a problem signing in. Please try again shortly.";

    // Google 로그인 성공 시 표시할 문구를 반환한다.
    public static string GetGoogleSuccessMessage()
    {
        return Localized(GOOGLE_SUCCESS_MESSAGE, GOOGLE_SUCCESS_MESSAGE_EN);
    }

    // 실패 유형에 맞는 Google 로그인 안내 문구를 반환한다.
    public static string GetGoogleFailureMessage(AuthLoginFailureType failureType)
    {
        switch (failureType)
        {
            case AuthLoginFailureType.Canceled:
                return Localized(GOOGLE_CANCELED_MESSAGE, GOOGLE_CANCELED_MESSAGE_EN);
            case AuthLoginFailureType.Network:
                return Localized(GOOGLE_NETWORK_FAILURE_MESSAGE, GOOGLE_NETWORK_FAILURE_MESSAGE_EN);
            case AuthLoginFailureType.Configuration:
                return Localized(GOOGLE_CONFIGURATION_FAILURE_MESSAGE, GOOGLE_CONFIGURATION_FAILURE_MESSAGE_EN);
            case AuthLoginFailureType.Timeout:
                return Localized(GOOGLE_TIMEOUT_FAILURE_MESSAGE, GOOGLE_TIMEOUT_FAILURE_MESSAGE_EN);
            case AuthLoginFailureType.FirebaseAuth:
                return Localized(FIREBASE_AUTH_FAILURE_MESSAGE, FIREBASE_AUTH_FAILURE_MESSAGE_EN);
            default:
                return Localized(GOOGLE_GENERAL_FAILURE_MESSAGE, GOOGLE_GENERAL_FAILURE_MESSAGE_EN);
        }
    }

    private static string Localized(string korean, string english)
    {
        return LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == "en"
            ? english
            : korean;
    }
}
