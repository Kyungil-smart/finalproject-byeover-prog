// 담당자 : 정승우
// 설명   : 튜토리얼 View 계약(매니저 <-> 겉모습 사이 약속).
//          이 인터페이스만 지키면 홍정옥(UI)과 정승우(두뇌)가 따로 작업해도 맞물린다.

using System;

public interface ITutorialView
{
    /// <summary>매니저 -> 뷰 : 이 단계를 화면에 그려라(어두운 막 + step.highlightTargetId 강조).</summary>
    void ShowStep(TutorialStep step);

    /// <summary>매니저 -> 뷰 : 오버레이를 숨겨라(이 씬엔 보여줄 단계 없음 / 튜토리얼 종료).</summary>
    void Hide();

    /// <summary>뷰 -> 매니저 : 유저가 이 단계 동작을 완료했다(주로 강조 영역 탭). 매니저가 다음 단계로 넘긴다.</summary>
    event Action OnStepActionCompleted;
}
