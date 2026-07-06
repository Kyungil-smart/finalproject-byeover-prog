//담당자: 조규민
// 인증 서비스 오류를 로그인 UI 메시지로 구분하기 위한 실패 유형 정의

// 로그인 실패 상황을 UI 문구로 변환할 수 있도록 구분한다.
public enum AuthLoginFailureType
{
    // Google 로그인 단계에서 발생한 일반 실패다.
    General,
    // 사용자가 Google 로그인 창을 닫거나 취소한 상태다.
    Canceled,
    // 네트워크 연결 또는 Google Play Services 응답 문제로 실패한 상태다.
    Network,
    // Web Client ID, SHA 인증서, 패키지명 같은 Google 로그인 설정 문제로 실패한 상태다.
    Configuration,
    // Google 또는 Firebase 인증 응답 시간이 초과된 상태다.
    Timeout,
    // Google 로그인 이후 Firebase 인증 처리에서 실패한 상태다.
    FirebaseAuth
}
