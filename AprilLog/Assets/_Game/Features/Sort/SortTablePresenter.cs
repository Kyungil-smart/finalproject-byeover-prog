// 담당자 : 최동훈
// 설명   : Sort 퍼즐 Presenter -- Model/System 이벤트 구독해서 View 갱신
// 수정 사항 : 보드 최초 랜덤 배치(ShuffleBoard) 초기화 로직 연동
// 최종 변경 일자 : 26.05.27

/// <summary>
/// SortModel 이벤트를 구독해서 SortTableView를 갱신한다.
/// </summary>
public class SortTablePresenter
{
    private readonly ISortTableView _view;
    private readonly SortModel _model;
    private readonly SortInputHandler _input;
    private readonly HintSystem _hint;

    public SortTablePresenter(ISortTableView view, SortModel model, SortInputHandler input, HintSystem hint)
    {
        _view = view;
        _model = model;
        _input = input;
        _hint = hint;

        _model.Initialize();
        _model.OnSlotChanged += HandleSlotChanged;
        _model.OnTableCleared += HandleTableCleared;
        _model.OnWaitingUpdated += HandleWaitingUpdated;
        _model.OnBoardReset += HandleBoardReset;
        _input.OnDragStarted += HandleDragStarted;
        _input.OnDragging += HandleDragging;
        _input.OnDragCanceled += HandleDragCanceled;
        _hint.OnHintShow += HandleHint;
        _hint.OnHintWaiting += HandleHintWaiting;
        _model.ShuffleBoard();

    }

    public void Dispose()
    {
        _model.OnSlotChanged -= HandleSlotChanged;
        _model.OnTableCleared -= HandleTableCleared;
        _model.OnWaitingUpdated -= HandleWaitingUpdated;
        _model.OnBoardReset -= HandleBoardReset;
        _input.OnDragStarted -= HandleDragStarted;
        _input.OnDragging -= HandleDragging;
        _input.OnDragCanceled -= HandleDragCanceled;
        _hint.OnHintShow -= HandleHint;
        _hint.OnHintWaiting -= HandleHintWaiting;
    }

    private void HandleSlotChanged(int t, int s, int unit)
    {
        if (unit >= 0)
            _view.PlaceUnit(t, s, unit);
        else
            _view.ClearSlot(t, s);
    }

    private void HandleTableCleared(int t) => _view.PlayClearAnimation(t);
    private void HandleWaitingUpdated(int idx, WaitingCombo c) => _view.UpdateWaiting(idx, c);
    private void HandleBoardReset() => _view.ResetBoard();
    private void HandleDragStarted(int t, int s) => _view.ShowDragFeedback(t, s, UnityEngine.Vector2.zero);
    private void HandleDragging(UnityEngine.Vector2 pos) => _view.ShowDragFeedback(0, 0, pos);
    private void HandleDragCanceled() => _view.HideDragFeedback();
    private void HandleHint(int t, int s) => _view.ShowHint(t, s);
    private void HandleHintWaiting() => _view.ShowWaitingHint();
}
