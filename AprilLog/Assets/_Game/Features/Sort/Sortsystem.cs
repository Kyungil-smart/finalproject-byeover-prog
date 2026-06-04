// 담당자 : 정승우
// 설명   : Sort 퍼즐 핵심 로직 -- 배치, 매칭, 대기열 난이도 알고리즘

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 유닛 이동, 정렬 성공 판정, 대기열 조합 생성을 담당한다.
/// 정렬 성공하면 OnSortCompleted 이벤트를 쏜다.
/// </summary>
public class SortSystem : MonoBehaviour, ISortNotifier
{
    // ---------- ISortNotifier ----------
    public event Action<UnitType> OnSortCompleted;
    public event Action OnDeadlockDetected;

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private SortModel _model;
    [SerializeField] private SortInputHandler _inputHandler;
    [SerializeField] private DeadlockDetector _deadlockDetector;
    [SerializeField] private HintSystem _hintSystem;
    [SerializeField] private PlayerModel _playerModel;

    [Header("연출")]
    [Tooltip("정렬 성공 후 클리어 연출 시간(초)")]
    [SerializeField] private float _clearDuration = 0.3f;

    // ---------- Private ----------
    private bool _isProcessing;
    private System.Random _rng;
    // FillEmptyTablesFromQueue() 안에 if (!_isAutoFillEnabled) return;이랑 후에 삭제
    private bool _isAutoFillEnabled = true;

    // 대기열 난이도 확률. 기본값은 기획서 기준.
    private float _lowProb = 0.40f;
    private float _warningProb = 0.35f;
    private float _highProb = 0.25f;

    // ---------- 초기화 ----------
    public void Initialize(int seed)
    {
        _rng = new System.Random(seed);
        _model.Initialize();

        // 대기열 4개 채우기
        for (int i = 0; i < SortModel.WAITING_COUNT; i++)
        {
            _model.SetWaiting(i, GenerateWaitingCombo());
        }
        // 처음 3개 테이블에 대기열에서 넣기
        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            FillTableFromQueue(t);
        }
    }

    // ---------- 생명주기 ----------
    private void OnEnable()
    {
        _inputHandler.OnUnitDropped += HandleUnitDropped;
    }

    private void OnDisable()
    {
        _inputHandler.OnUnitDropped -= HandleUnitDropped;
    }

    // ---------- 유닛 이동 처리 ----------
    private void HandleUnitDropped(int fromTable, int fromSlot, int toTable, int toSlot)
    {        
        if (_isProcessing) return;
   
        if (toTable == -1) // 이동 불가
        {
            RestoreUnit(fromTable, fromSlot);
            return;
        }

        int actualSlot = ResolveDropSlot(toTable, toSlot);
        if (actualSlot == -1) // 꽉찬 슬롯
        {
            Debug.Log("이동 불가! 꽉 찬 테이블입니다.");
            RestoreUnit(fromTable, fromSlot);
            return;  
        }

        if (fromTable == toTable && fromSlot == actualSlot) // 같은 자리
        {
            RestoreUnit(fromTable, fromSlot);
            return; 
        }

        // 유닛 이동
        int unit = _model.GetUnit(fromTable, fromSlot);
        _model.ClearSlot(fromTable, fromSlot);
        _model.PlaceUnit(toTable, actualSlot, unit);

        _hintSystem.ResetTimer();

        if (_model.IsTableMatched(toTable))
        {
            StartCoroutine(ProcessMatch(toTable));
            return;
        }

        // 이동으로 빈 테이블(3슬롯 모두 공백)이 생기면 대기열로 채워 빈 공간을 없앤다 (기획 2)
        FillEmptyTablesFromQueue();

        // 대기열에서 채운 조합이 곧바로 매칭될 수 있으니 확인
        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            if (_model.IsTableMatched(t))
            {
                StartCoroutine(ProcessMatch(t));
                return;
            }
        }

        CheckDeadlock();
    }

    private void RestoreUnit(int table, int slot)
    {
        int unit = _model.GetUnit(table, slot);

        _model.PlaceUnit(table, slot, unit);
    }

    // 드롭한 슬롯이 차있으면 같은 테이블 빈 슬롯 찾기
    // 우측부터 순환 탐색
    private int ResolveDropSlot(int tableIdx, int targetSlot)
    {
        if (_model.GetUnit(tableIdx, targetSlot) < 0)
            return targetSlot;

       for (int offset = 1; offset < SortModel.SLOTS_PER_TABLE; offset++)
        {
            int idx = (targetSlot + offset) % SortModel.SLOTS_PER_TABLE;
            if (_model.GetUnit(tableIdx, idx) < 0)
                return idx;
        }

        return -1;  // 테이블 꽉 참
    }

    // ---------- 정렬 성공 ----------
    private IEnumerator ProcessMatch(int tableIdx)
    {
        _isProcessing = true;

        int matchedUnit = _model.GetUnit(tableIdx, 0);
        UnitType matchedType = (UnitType)matchedUnit;

        yield return new WaitForSeconds(_clearDuration);

        for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
        {
            _model.ClearSlot(tableIdx, s);
        }

        _model.ClearTable(tableIdx);

        // 전투 시스템한테 알리기
        OnSortCompleted?.Invoke(matchedType);

        // 빈 테이블 채우기
        yield return new WaitForSeconds(0.1f);
        FillEmptyTablesFromQueue();

        // 연쇄 체크: 대기열에서 채운 게 바로 매칭될 수 있음 (예: AAA 조합)
        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            if (_model.IsTableMatched(t))
            {
                yield return StartCoroutine(ProcessMatch(t));
                yield break;  // 재귀로 처리됨
            }
        }

        CheckDeadlock();
        _isProcessing = false;
    }

    // ---------- 대기열 -> 퍼즐 ----------
    private void FillEmptyTablesFromQueue()
    {
        // private bool _isAutoFillEnabled = true;랑 같이 후에 삭제
        if (!_isAutoFillEnabled) return;

        for (int t = 0; t < SortModel.TABLE_COUNT; t++)
        {
            if (_model.IsTableEmpty(t))
                FillTableFromQueue(t);
        }
    }

    private void FillTableFromQueue(int tableIdx)
    {
        WaitingCombo combo = _model.GetWaiting(0);

        // 조합의 유닛을 퍼즐 테이블에 배치
        for (int s = 0; s < SortModel.SLOTS_PER_TABLE; s++)
        {
            if (combo.unitTypes[s] >= 0)
                _model.PlaceUnit(tableIdx, s, combo.unitTypes[s]);
        }

        // 대기열 한 칸씩 당기고 맨 뒤에 새 조합 생성
        _model.ShiftWaitingQueue();
        _model.SetWaiting(SortModel.WAITING_COUNT - 1, GenerateWaitingCombo());
    }

    // ---------- 대기열 조합 생성 (난이도 알고리즘) ----------
    private WaitingCombo GenerateWaitingCombo()
    {
        UpdateDifficultyWeights();
        WaitingDifficulty diff = PickDifficulty();
        return CreateCombo(diff);
    }

    private void UpdateDifficultyWeights()
    {
        int emptySlots = _model.CountEmptySlots();

        // 기본 확률로 리셋
        _lowProb = 0.40f;
        _warningProb = 0.35f;
        _highProb = 0.25f;

        // 빈 칸 적으면 (위험하면) low 비중 올림
        if (emptySlots <= 4)
        {
            _lowProb += 0.15f;
            _warningProb -= 0.075f;
            _highProb -= 0.075f;
        }
        else if (emptySlots >= 12)
        {
            // 빈 칸 많으면 (안전하면) high 비중 올림
            _highProb += 0.10f;
            _lowProb -= 0.05f;
            _warningProb -= 0.05f;
        }

        // HP 보정
        if (_playerModel != null)
        {
            float hpRatio = (float)_playerModel.CurrentHP / Mathf.Max(1, _playerModel.MaxHP);

            if (hpRatio <= 0.30f)
            {
                _lowProb += 0.20f;
                _warningProb -= 0.10f;
                _highProb -= 0.10f;
            }
            else if (hpRatio <= 0.50f)
            {
                _lowProb += 0.10f;
                _warningProb -= 0.05f;
                _highProb -= 0.05f;
            }
        }

        ClampProbabilities();
    }

    private WaitingDifficulty PickDifficulty()
    {
        float roll = (float)_rng.NextDouble();
        if (roll < _lowProb) return WaitingDifficulty.Low;
        if (roll < _lowProb + _warningProb) return WaitingDifficulty.Warning;
        return WaitingDifficulty.High;
    }

    private WaitingCombo CreateCombo(WaitingDifficulty diff)
    {
        var combo = new WaitingCombo
        {
            unitTypes = new int[] { -1, -1, -1 },
            difficulty = diff
        };

        switch (diff)
        {
            case WaitingDifficulty.Low:
                int a = RandomUnit();
                if (_rng.NextDouble() < 0.5)
                {
                    combo.unitTypes[0] = a;
                }
                else
                {
                    combo.unitTypes[0] = a;
                    combo.unitTypes[1] = a;
                }
                break;

            case WaitingDifficulty.Warning:
                int wa = RandomUnit();
                int wb = RandomUnitExcept(wa);
                if (_rng.NextDouble() < 0.5)
                {
                    combo.unitTypes[0] = wa;
                    combo.unitTypes[1] = wa;
                    combo.unitTypes[2] = wb;
                }
                else
                {
                    combo.unitTypes[0] = wa;
                    combo.unitTypes[1] = wb;
                }
                break;

            case WaitingDifficulty.High:
                int ha = RandomUnit();
                int hb = RandomUnitExcept(ha);
                int hc = RandomUnitExcept(ha, hb);
                combo.unitTypes[0] = ha;
                combo.unitTypes[1] = hb;
                combo.unitTypes[2] = hc;
                break;
        }

        return combo;
    }

    // ---------- 유틸 ----------
    private int RandomUnit() => _rng.Next(0, SortModel.UNIT_TYPE_COUNT);

    private int RandomUnitExcept(params int[] except)
    {
        int result;
        int safety = 0;
        do
        {
            result = RandomUnit();
            safety++;
        }
        while (Array.IndexOf(except, result) >= 0 && safety < 50);
        return result;
    }

    private void ClampProbabilities()
    {
        _lowProb = Mathf.Max(0.05f, _lowProb);
        _warningProb = Mathf.Max(0.05f, _warningProb);
        _highProb = Mathf.Max(0.05f, _highProb);

        float total = _lowProb + _warningProb + _highProb;
        _lowProb /= total;
        _warningProb /= total;
        _highProb /= total;
    }

    // ---------- 데드락 ----------
    private void CheckDeadlock()
    {
        if (_deadlockDetector.IsDeadlock(_model))
            StartCoroutine(HandleDeadlock());
    }

    private IEnumerator HandleDeadlock()
    {
        _isProcessing = true;
        OnDeadlockDetected?.Invoke();

        yield return new WaitForSeconds(1.5f);

        _model.ResetBoard();
        FillEmptyTablesFromQueue();

        _isProcessing = false;
    }
}