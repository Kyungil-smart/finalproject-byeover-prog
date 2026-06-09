// 담당자 : 최동훈
// 설명   : Sort 퍼즐 View -- 슬롯 표시 + 드래그 피드백

using DG.Tweening;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

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
    [SerializeField] private Image[] _puzzleSlots;

    [Header("대기열 슬롯")]
    [Tooltip("4테이블 x 3슬롯. 순서대로 드래그")]
    [SerializeField] private Image[] _waitingSlots;

    [Header("유닛 스프라이트")]
    [SerializeField] private Sprite[] _unitSprites;

    [Header("드래그 연출용 가짜 유닛")]
    [SerializeField] private Image _dragFeedbackImg;

    // ---------- Private ----------
    private SortTablePresenter _presenter;
    private int _currentDraggingUnitType = -1;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        _presenter = new SortTablePresenter(this, _model, _inputHandler, _hintSystem);

        StartCoroutine(SetupAfterLayout());
    }

    // 안드로이드 빌드에서 시작 직후 해상도가 늦게 확정되면 처음 계산한 슬롯 좌표가 어긋나
    // 터치 히트 판정이 전부 실패할 수 있다. 화면 크기가 바뀔 때마다 슬롯 좌표를 다시 계산한다.
    private Vector2Int _lastScreenSize;
    private void Update()
    {
        var size = new Vector2Int(Screen.width, Screen.height);
        if (size != _lastScreenSize)
        {
            _lastScreenSize = size;
            SetupSlotPositions();
        }
    }

    private void OnEnable()
    {
        if (_hintSystem != null)
        {
            _hintSystem.OnHintShow += ShowHint;
            _hintSystem.OnHintWaiting += ShowWaitingHint;
        }

        if (_inputHandler != null)
        {
            _inputHandler.OnDragging += UpdateDragFeedbackPosition;

            _inputHandler.OnDragStarted += (tableIdx, slotIdx) =>
            {
                // 빌드(터치) 대응: Mouse.current는 터치 기기에서 null → NRE. Pointer.current(마우스/터치 공용)로 읽는다.
                var pointer = UnityEngine.InputSystem.Pointer.current;
                Vector2 currentPos = pointer != null ? pointer.position.ReadValue() : Vector2.zero;
                ShowDragFeedback(tableIdx, slotIdx, currentPos);
            };

            _inputHandler.OnDragEnded += HideDragFeedback;
            _inputHandler.OnDragCanceled += HideDragFeedback;
        }
    }

    private void OnDisable()
    {
        if (_hintSystem != null)
        {
            _hintSystem.OnHintShow -= ShowHint;
            _hintSystem.OnHintWaiting -= ShowWaitingHint;
        }

        if (_inputHandler != null)
        {
            _inputHandler.OnDragging -= UpdateDragFeedbackPosition;
        }
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
    }
    private System.Collections.IEnumerator SetupAfterLayout()
    {
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();

        SetupSlotPositions();
    }

    private void SetupSlotPositions()
    {
        var positions = new Vector2[SortModel.TABLE_COUNT][];

        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            positions[t] = new Vector2[SortModel.SLOTS_PER_TABLE];
            for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
            {
                int index = t * SortModel.SLOTS_PER_TABLE + s;

                if (index < _puzzleSlots.Length)
                {
                    RectTransform rt = _puzzleSlots[index].GetComponent<RectTransform>();
                    // 빌드 대응: 캔버스가 ScreenSpace-Camera/WorldSpace면 null 카메라로 변환 시 좌표가 어긋나
                    // 터치 히트 판정이 전부 실패한다(에디터 Overlay에선 우연히 맞음). 렌더모드에 맞는 카메라 사용.
                    Canvas canvas = rt.GetComponentInParent<Canvas>();
                    Camera screenCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? canvas.worldCamera : null;
                    positions[t][s] = RectTransformUtility.WorldToScreenPoint(screenCam, rt.position);
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

        var img = _puzzleSlots[idx];
        if (img != null && unitType >= 0 && unitType < _unitSprites.Length)
        {
            // Debug.Log($"[뷰] {tableIdx}, {slotIdx}에 유닛 {unitType} 배치!");
            img.sprite = _unitSprites[unitType];
            img.enabled = true;
            img.color = Color.white;
        }
    }

    public void ClearSlot(int tableIdx, int slotIdx)
    {
        int idx = tableIdx * SortModel.SLOTS_PER_TABLE + slotIdx;
        if (idx >= _puzzleSlots.Length) return;

        var img = _puzzleSlots[idx];
        if (img != null)
        {
            // Debug.Log($"[뷰] {tableIdx}, {slotIdx} 삭제! (sr.enabled = false)");
            img.sprite = null;
            img.color = Color.clear;
            img.enabled = false;
        }
    }

    public void ShowDragFeedback(int fromTable, int fromSlot, Vector2 dragPos)
    {
        int idx = fromTable * SortModel.SLOTS_PER_TABLE + fromSlot;
        if (idx >= _puzzleSlots.Length) return;

        var originalImg = _puzzleSlots[idx].GetComponent<Image>();
        if (originalImg == null || !originalImg.enabled) return;

        originalImg.enabled = false;

        if (_dragFeedbackImg != null)
        {
            _dragFeedbackImg.sprite = originalImg.sprite;
            _dragFeedbackImg.color = originalImg.color;
            _dragFeedbackImg.enabled = true;
        }

        UpdateDragFeedbackPosition(dragPos);
    }

    public void UpdateDragFeedbackPosition(Vector2 dragPos)
    {
        if (_dragFeedbackImg != null && _dragFeedbackImg.enabled)
        {
            _dragFeedbackImg.rectTransform.position = dragPos;
        }
    }

    public void HideDragFeedback()
    {
        if (_dragFeedbackImg != null)
        {
            _dragFeedbackImg.enabled = false;
            _dragFeedbackImg.sprite = null;
            _dragFeedbackImg.color = Color.clear;
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
            //Debug.Log($"[매칭확인] 모델 대기열 {waitingIdx}번의 {i}번 유닛(타입:{combo.unitTypes[i]}) -> 뷰 슬롯 {slotIdx}번으로 배치");
            var img = _waitingSlots[slotIdx];
            if (img == null) continue;

            int unitType = combo.unitTypes[i];

            if (unitType >= 0 && unitType < _unitSprites.Length)
            {
                img.sprite = _unitSprites[unitType];
                img.enabled = true;
            }
            else
            {
                img.enabled = false;
            }
        }
    }
    public void ResetBoard() { /* 전체 초기화 연출 */ }

    public void ShowHint(int tableIdx, int slotIdx)
    {
        int idx = tableIdx * SortModel.SLOTS_PER_TABLE + slotIdx;
        if (idx < 0 || idx >= _puzzleSlots.Length) return;

        RectTransform rt = _puzzleSlots[idx].GetComponent<RectTransform>();
        if (rt == null) return;

        rt.DOKill();

        Vector2 originalAnchorPos = rt.anchoredPosition;

        rt.transform.DOShakePosition(0.5f, 20f, 10, 90f)
            .OnComplete(() =>
            {
                rt.anchoredPosition = originalAnchorPos;
            });
    }

    public void ShowWaitingHint()
    {
        foreach (var slot in _waitingSlots)
        {
            if (slot == null)
            {
                continue;
            }

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.DOKill();

            Vector2 originalPos = rt.anchoredPosition;

            rt.DOShakeAnchorPos(0.3f, 20f, 10, 90f)
                   .OnComplete(() =>
                   {
                       rt.anchoredPosition = originalPos;
                   });
        }
    }
}
