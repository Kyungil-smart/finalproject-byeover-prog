//담당자: 조규민

// 로그인 실패 상황을 UI 문구로 변환할 수 있도록 구분한다.
public enum AuthLoginFailureType
{
    // Google 로그인 단계에서 발생한 일반 실패다.
    General,
    // 사용자가 Google 로그인 창을 닫거나 취소한 상태다.
    Canceled,
    // Google 로그인 이후 Firebase 인증 처리에서 실패한 상태다.
    FirebaseAuth
}
