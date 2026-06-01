// 담당자 : 최동훈
// 설명   : Sort 퍼즐 View -- 슬롯 표시 + 드래그 피드백

using UnityEngine;

/// <summary>
/// 퍼즐 테이블과 대기열의 시각적 표시를 담당한다. 로직 없음.
/// </summary>
public class SortTableView : MonoBehaviour, ISortTableView
{
    // ---------- SerializeField ----------
    [Header("데이터 참조")]
    [SerializeField] private SortModel _model;
    [SerializeField] private SortInputHandler _inputHandler;
    [SerializeField] private HintSystem _hintSystem;
    [SerializeField] private LocalizationManager _localization;

    [Header("퍼즐 슬롯")]
    [Tooltip("9테이블 x 3슬롯. 순서대로 드래그")]
    [SerializeField] private Transform[] _puzzleSlots;

    [Header("대기열 슬롯")]
    [Tooltip("4테이블 x 3슬롯. 순서대로 드래그")]
    [SerializeField] private Transform[] _waitingSlots;

    [Header("유닛 스프라이트")]
    [SerializeField] private Sprite[] _unitSprites;

    [Header("드래그 연출용 가짜 유닛")]
    [SerializeField] private SpriteRenderer _dragFeedbackSR;

    // ---------- Private ----------
    private SortTablePresenter _presenter;
    private int _currentDraggingUnitType = -1;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        _presenter = new SortTablePresenter(this, _model, _inputHandler, _hintSystem);

        // 슬롯 위치를 InputHandler에 전달
        SetupSlotPositions();
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }

    private void SetupSlotPositions()
    {
        var positions = new Vector2[SortModel.TABLE_COUNT][];
        int currentIdx = 0;

        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            positions[t] = new Vector2[SortModel.SLOTS_PER_TABLE];
            for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
            {
                if (currentIdx < _puzzleSlots.Length)
                {
                    positions[t][s] = _puzzleSlots[currentIdx].position;
                    currentIdx++;
                }
            }
        }
        _inputHandler.SetSlotPositions(positions);
    }

    // ---------- ISortTableView ----------
    public void PlaceUnit(int tableIdx, int slotIdx, int unitType)
    {
        int idx = tableIdx * SortModel.SLOTS_PER_TABLE + slotIdx;
        if (idx >= _puzzleSlots.Length) return;

        var sr = _puzzleSlots[idx].GetComponent<SpriteRenderer>();
        if (sr != null && unitType >= 0 && unitType < _unitSprites.Length)
        {
            Debug.Log($"[뷰] {tableIdx}, {slotIdx}에 유닛 {unitType} 배치!");
            sr.sprite = _unitSprites[unitType];
            sr.enabled = true;
            sr.color = Color.white;
        }
    }

    public void ClearSlot(int tableIdx, int slotIdx)
    {
        int idx = tableIdx * SortModel.SLOTS_PER_TABLE + slotIdx;
        if (idx >= _puzzleSlots.Length) return;

        var sr = _puzzleSlots[idx].GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"[뷰] {tableIdx}, {slotIdx} 삭제! (sr.enabled = false)");
            sr.sprite = null;
            sr.color = Color.clear;
            sr.enabled = false;
        }
    }

    public void ShowDragFeedback(int fromTable, int fromSlot, Vector2 dragPos)
    {
        int idx = fromTable * SortModel.SLOTS_PER_TABLE + fromSlot;
        if (idx >= _puzzleSlots.Length) return;

        var originalSR = _puzzleSlots[idx].GetComponent<SpriteRenderer>();
        if (originalSR == null || !originalSR.enabled) return;

        originalSR.enabled = false;

        if (_dragFeedbackSR != null)
        {
            _dragFeedbackSR.sprite = originalSR.sprite;
            _dragFeedbackSR.color = originalSR.color;
            _dragFeedbackSR.enabled = true;
        }

        UpdateDragFeedbackPosition(dragPos);
    }

    public void UpdateDragFeedbackPosition(Vector2 dragPos)
    {
        if (_dragFeedbackSR != null && _dragFeedbackSR.enabled)
        {
            Vector3 finalWorldPos = new Vector3(dragPos.x, dragPos.y, 0f);
            _dragFeedbackSR.transform.position = finalWorldPos;
        }
    }

    public void HideDragFeedback() 
    {
        if (_dragFeedbackSR != null)
        {
            _dragFeedbackSR.enabled = false;
            _dragFeedbackSR.sprite = null;
            _dragFeedbackSR.color = Color.clear;
        }
    }

    public void PlayClearAnimation(int tableIdx) { /* 정렬 성공 연출 */ }
    public void LockSlot(int tableIdx, int slotIdx) { /* 거미줄 표시 */ }
    public void UnlockSlot(int tableIdx, int slotIdx) { /* 거미줄 해제 */ }
    public void ShowDeadlockWarning() { /* 데드락 경고 UI */ }
    public void UpdateWaiting(int waitingIdx, WaitingCombo combo)
    {
        int baseSlotIdx = waitingIdx * 3;

        for (int i = 0; i < 3; i++)
        {
            int slotIdx = baseSlotIdx + i;
            if (slotIdx < 0 || slotIdx >= _waitingSlots.Length) continue;
            Debug.Log($"[매칭확인] 모델 대기열 {waitingIdx}번의 {i}번 유닛(타입:{combo.unitTypes[i]}) -> 뷰 슬롯 {slotIdx}번으로 배치");
            var sr = _waitingSlots[slotIdx].GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            int unitType = combo.unitTypes[i];

            if (unitType >= 0 && unitType < _unitSprites.Length)
            {
                sr.sprite = _unitSprites[unitType];
                sr.enabled = true;
            }
            else
            {
                sr.enabled = false;
            }
        }
    }
    public void ResetBoard() { /* 전체 초기화 연출 */ }
    public void ShowHint(int tableIdx, int slotIdx) { /* 유닛 흔들기 */ }
    public void ShowWaitingHint() { /* 대기열 흔들기 */ }
}
