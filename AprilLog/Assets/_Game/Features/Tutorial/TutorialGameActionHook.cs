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
    private StageLoopManager _loop;

    /// <summary>정렬 알림자(SortSystem)를 연결한다. 중복 구독 방지 포함.</summary>
    public void Bind(ISortNotifier notifier)
    {
        Unbind();
        _notifier = notifier;
        if (_notifier != null) _notifier.OnSortCompleted += HandleSortCompleted;
    }

    /// <summary>챕터 종료(승리)를 연결한다. 마지막 단계(gameAction==ChapterClear)가 이 이벤트로 진행/완료된다.
    /// 이 훅이 없으면 마지막 단계에서 튜토리얼이 영원히 안 끝나 매 전투가 튜토 챕터로 반복된다.</summary>
    public void BindChapterEnd(StageLoopManager loop)
    {
        if (_loop != null) _loop.OnChapterEnd -= HandleChapterEnd;
        _loop = loop;
        if (_loop != null) _loop.OnChapterEnd += HandleChapterEnd;
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (_notifier != null) _notifier.OnSortCompleted -= HandleSortCompleted;
        _notifier = null;
        if (_loop != null) _loop.OnChapterEnd -= HandleChapterEnd;
        _loop = null;
    }

    private void HandleChapterEnd(bool isVictory)
    {
        if (!isVictory) return;   // 패배(step3 강제패배 포함)로는 진행하지 않는다

        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;

        TutorialStep step = tm.CurrentStep;
        if (step == null || step.advanceMode != TutorialAdvanceMode.GameAction || step.gameAction != TutorialGameAction.ChapterClear)
            return;

        tm.AdvanceStep();   // 마지막 단계면 TutorialManager가 Complete까지 처리한다
    }

    private int _sortCount;
    private int _countedStepId = -1;

    private void HandleSortCompleted(UnitType _)
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null || !tm.IsRunning) return;

        TutorialStep step = tm.CurrentStep;
        // 정렬로 진행하는 단계(gameAction==Sort)일 때만 진행. 다른 GameAction 단계는 무시.
        if (step == null || step.advanceMode != TutorialAdvanceMode.GameAction || step.gameAction != TutorialGameAction.Sort)
            return;

        // 단계가 바뀌면 카운트 초기화. 같은 단계에서 requiredSortCount 만큼 정렬해야 진행.
        if (_countedStepId != step.stepId) { _countedStepId = step.stepId; _sortCount = 0; }

        _sortCount++;
        if (_sortCount >= Mathf.Max(1, step.requiredSortCount))
        {
            _sortCount = 0;
            tm.AdvanceStep();
        }
    }
}
