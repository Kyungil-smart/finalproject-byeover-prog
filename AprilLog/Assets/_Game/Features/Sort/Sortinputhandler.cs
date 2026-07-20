// 담당자 : 최동훈
// 설명   : Sort 유닛 터치 드래그 입력만 처리
// 수정 사항 : (구)인풋시스템 뉴인풋시스템으로 변경
// 최종 변경 일자 : 26.05.26

using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public event Action OnDragEnded;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private SortModel _model;

    [Header("설정")]
    [Tooltip("드래그로 인식하는 최소 이동 거리(screen px). 인접 슬롯 간격(1080p ~86px)보다 작아야 옆칸 드래그가 탭으로 안 버려진다.")]
    [SerializeField] private float _dragThreshold = 30f;

    [Tooltip("슬롯 히트 영역 반경(screen px). 손가락 크기 보정용")]
    [SerializeField] private float _touchRadius = 100f;

    // ---------- Private ----------
    private int _selectedTable = -1;
    private int _selectedSlot = -1;
    private bool _isDragging;
    private Vector2 _touchStartScreen;

    // 슬롯 위치 캐싱. View가 초기화할 때 넣어줌.
    private Vector2[][] _slotPositions;

    // View가 슬롯 오브젝트 위치를 기반으로 채워줌
    public void SetSlotPositions(Vector2[][] positions)
    {
        _slotPositions = positions;
    }

    // ---------- Update ----------
    private void Update()
    {
        if (!enabled) return;
        if (Time.timeScale == 0f && !TutorialInGameDirector.AllowsPausedSortInput) return;
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
        var ts = Touchscreen.current;
        if (ts == null) return;

        // [빌드 버그 수정] phase enum을 Update당 1회 polling하면 Began이 코얼레스/누락되거나(픽업 실패),
        // 손가락 뗄 때 Ended를 놓쳐(드롭 실패) 빌드에서 퍼즐을 못 맞췄다.
        // press의 엣지(wasPressedThisFrame/wasReleasedThisFrame)로 마우스 경로와 동일하게 이산 처리한다.
        var touch = ts.primaryTouch;
        Vector2 pos = touch.position.ReadValue();

        if (touch.press.wasPressedThisFrame)
            BeginInput(pos);
        else if (touch.press.isPressed)
            MoveInput(pos);
        else if (touch.press.wasReleasedThisFrame)
            EndInput(pos);
    }

    // ---------- 마우스 (에디터 테스트용) ----------
    private void HandleMouse()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            BeginInput(Mouse.current.position.ReadValue());
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            MoveInput(Mouse.current.position.ReadValue());
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndInput(Mouse.current.position.ReadValue());
        }
    }

    // ---------- 공통 입력 처리 ----------
    private void BeginInput(Vector2 screenPos)
    {
        Debug.Log($"[인풋 검증] 현재 인식된 총 테이블 개수: {_slotPositions.Length}");

        _touchStartScreen = screenPos;
        _isDragging = false;

        FindSlot(screenPos, out _selectedTable, out _selectedSlot);

        if (_selectedTable >= 0)
        {
            int modelUnitType = _model.GetUnit(_selectedTable, _selectedSlot);
            Debug.Log($"[클릭 검증] 검출된 테이블: {_selectedTable}, 슬롯: {_selectedSlot} | 모델 내부 데이터 값: {modelUnitType}");
        }

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
                AudioManager.Play(SfxId.UnitSelect);   // SFX 가이드 15: 유닛 선택(드래그 시작)
                OnDragStarted?.Invoke(_selectedTable, _selectedSlot);
            }
        }
        else
        {
            OnDragging?.Invoke(screenPos);
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

        FindSlot(screenPos, out int toTable, out int toSlot);

        OnUnitDropped?.Invoke(_selectedTable, _selectedSlot, toTable, toSlot);
        OnDragEnded?.Invoke();

        _selectedTable = -1;
        _selectedSlot = -1;
        _isDragging = false;
    }

    // 월드 좌표에서 가장 가까운 슬롯 찾기
    private void FindSlot(Vector2 screenPos, out int tableIdx, out int slotIdx)
    {
        tableIdx = -1;
        slotIdx = -1;
        float minDistance = float.MaxValue;

        for (int t = 0; t < _slotPositions.Length; t++)
        {
            for (int s = 0; s < _slotPositions[t].Length; s++)
            {
                float dist = Vector2.Distance(screenPos, _slotPositions[t][s]);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    tableIdx = t;
                    slotIdx = s;
                }
            }
        }

        if (minDistance > _touchRadius)
        {
            Debug.Log($"[인풋] 선택 실패: minDistance({minDistance}) > touchRadius({_touchRadius}) | 위치: {screenPos}");
            tableIdx = -1;
            slotIdx = -1;
        }

        else
        {
            Debug.Log($"[인풋] 선택 성공! 거리: {minDistance}, 위치: {screenPos}");
        }
    }
}
