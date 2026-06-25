// 담당자 : 정승우
// 설명   : Sort 퍼즐 View 인터페이스

public interface ISortTableView
{
    void PlaceUnit(int tableIdx, int slotIdx, int unitType);
    void ClearSlot(int tableIdx, int slotIdx);
    void ShowDragFeedback(int fromTable, int fromSlot, UnityEngine.Vector2 dragPos);
    void UpdateDragFeedbackPosition(UnityEngine.Vector2 dragPos);
    void HideDragFeedback();
    void PlayClearAnimation(int tableIdx);
    void LockSlot(int tableIdx, int slotIdx);
    void UnlockSlot(int tableIdx, int slotIdx);
    void ShowDeadlockWarning();
    void UpdateWaiting(int waitingIdx, WaitingCombo combo);
    void ResetBoard();
    void ShowHint(int tableIdx, int slotIdx);
    void ShowWaitingHint();
}
