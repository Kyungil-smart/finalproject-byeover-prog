// 담당자 : 정승우
// 설명   : 튜토리얼 GameAction 단계 훅.
//          정렬 완성(ISortNotifier.OnSortCompleted)을 받아, 현재 튜토리얼 단계가 GameAction이면
//          TutorialManager.AdvanceStep()으로 다음 단계로 넘긴다.
//          (TapHighlight 단계는 TutorialView의 탭이 처리. 이건 '실제 게임 동작'으로 넘어가는 단계 전용.)
//          InGameBootstrap이 SortSystem 준비 직후 AddComponent + Bind 해준다(씬 배선 불필요).

using UnityEngine;

public class TutorialGameActionHook : MonoBehaviour
{
    private ISortNotifier _notifier;

    /// <summary>정렬 알림자(SortSystem)를 연결한다. 중복 구독 방지 포함.</summary>
    public void Bind(ISortNotifier notifier)
    {
        Unbind();
        _notifier = notifier;
        if (_notifier != null) _notifier.OnSortCompleted += HandleSortCompleted;
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (_notifier != null) _notifier.OnSortCompleted -= HandleSortCompleted;
        _notifier = null;
    }

    private void HandleSortCompleted(UnitType _)
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;

        TutorialStep step = tm.CurrentStep;
        // 정렬로 진행하는 단계(gameAction==Sort)일 때만 진행. 다른 GameAction 단계는 무시.
        if (step != null && step.advanceMode == TutorialAdvanceMode.GameAction && step.gameAction == TutorialGameAction.Sort)
            tm.AdvanceStep();
    }
}
