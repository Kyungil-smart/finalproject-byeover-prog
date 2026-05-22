// 담당자 : 정승우
// 설명   : Sort 유닛 터치 드래그 입력만 처리

using System;
using UnityEngine;

/// <summary>
/// 터치 드래그를 받아서 어디서 어디로 드롭했는지 이벤트로 알린다.
/// 입력만 처리하고 로직은 SortSystem이 함.
/// </summary>
public class SortInputHandler : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int, int, int> OnUnitDropped;  // fromTable, fromSlot, toTable, toSlot
    public event Action<int, int> OnDragStarted;             // tableIdx, slotIdx
    public event Action<Vector2> OnDragging;                 // 현재 드래그 위치 (world)
    public event Action OnDragCanceled;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private SortModel _model;

    [Header("설정")]
    [Tooltip("드래그로 인식하는 최소 이동 거리(px)")]
    [SerializeField] private float _dragThreshold = 10f;

    [Tooltip("슬롯 히트 영역 반경(world). 손가락 크기 보정용")]
    [SerializeField] private float _touchRadius = 0.5f;

    // ---------- Private ----------
    private Camera _cam;
    private int _selectedTable = -1;
    private int _selectedSlot = -1;
    private bool _isDragging;
    private Vector2 _touchStartScreen;

    // 슬롯 위치 캐싱. View가 초기화할 때 넣어줌.
    private Vector2[][] _slotPositions;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        _cam = Camera.main;
    }

    // View가 슬롯 오브젝트 위치를 기반으로 채워줌
    public void SetSlotPositions(Vector2[][] positions)
    {
        _slotPositions = positions;
    }

    // ---------- Update ----------
    private void Update()
    {
        if (_slotPositions == null) return;

#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    // ---------- 터치 ----------
    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginInput(touch.position);
                break;

            case TouchPhase.Moved:
                MoveInput(touch.position);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                EndInput(touch.position);
                break;
        }
    }

    // ---------- 마우스 (에디터 테스트용) ----------
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
            BeginInput(Input.mousePosition);
        else if (Input.GetMouseButton(0))
            MoveInput(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            EndInput(Input.mousePosition);
    }

    // ---------- 공통 입력 처리 ----------
    private void BeginInput(Vector2 screenPos)
    {
        _touchStartScreen = screenPos;
        _isDragging = false;

        Vector2 worldPos = _cam.ScreenToWorldPoint(screenPos);
        FindSlot(worldPos, out _selectedTable, out _selectedSlot);

        // 빈 슬롯이면 무시
        if (_selectedTable >= 0 && _model.GetUnit(_selectedTable, _selectedSlot) < 0)
        {
            _selectedTable = -1;
            _selectedSlot = -1;
        }
    }

    private void MoveInput(Vector2 screenPos)
    {
        if (_selectedTable < 0) return;

        if (!_isDragging)
        {
            if (Vector2.Distance(screenPos, _touchStartScreen) > _dragThreshold)
            {
                _isDragging = true;
                OnDragStarted?.Invoke(_selectedTable, _selectedSlot);
            }
        }
        else
        {
            Vector2 worldPos = _cam.ScreenToWorldPoint(screenPos);
            OnDragging?.Invoke(worldPos);
        }
    }

    private void EndInput(Vector2 screenPos)
    {
        if (_selectedTable < 0 || !_isDragging)
        {
            OnDragCanceled?.Invoke();
            _selectedTable = -1;
            return;
        }

        Vector2 worldPos = _cam.ScreenToWorldPoint(screenPos);
        FindSlot(worldPos, out int toTable, out int toSlot);

        if (toTable >= 0)
            OnUnitDropped?.Invoke(_selectedTable, _selectedSlot, toTable, toSlot);
        else
            OnDragCanceled?.Invoke();

        _selectedTable = -1;
        _selectedSlot = -1;
        _isDragging = false;
    }

    // 월드 좌표에서 가장 가까운 슬롯 찾기
    private void FindSlot(Vector2 worldPos, out int tableIdx, out int slotIdx)
    {
        tableIdx = -1;
        slotIdx = -1;
        float closest = _touchRadius;

        for (int t = 0; t < _slotPositions.Length; t++)
        {
            for (int s = 0; s < _slotPositions[t].Length; s++)
            {
                float dist = Vector2.Distance(worldPos, _slotPositions[t][s]);
                if (dist < closest)
                {
                    closest = dist;
                    tableIdx = t;
                    slotIdx = s;
                }
            }
        }
    }
}