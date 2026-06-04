// 담당자 : 최동훈
// 설명   : Sort 퍼즐 Model -- 슬롯 상태 데이터 + 이벤트
// 수정 사항 : 보드 내 모든 슬롯을 무작위 유닛으로 채우는 랜덤 배치(ShuffleBoard) 로직 구현
// 최종 변경 일자 : 26.05.27

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 퍼즐 테이블 9개(27칸) + 대기 테이블 4개의 상태를 관리한다.
/// 슬롯이 변하면 이벤트를 쏴서 View가 갱신됨.
/// </summary>
public class SortModel : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int, int, int> OnSlotChanged;       // tableIdx, slotIdx, unitType (-1이면 빈칸)
    public event Action<int> OnTableCleared;                 // 정렬 성공한 tableIdx
    public event Action<int, WaitingCombo> OnWaitingUpdated; // waitingIdx, 새 조합
    public event Action OnBoardReset;                        // 데드락 초기화

    // ---------- 상수 ----------
    public const int TABLE_COUNT = 9;
    public const int SLOTS_PER_TABLE = 3;
    public const int TOTAL_SLOTS = TABLE_COUNT * SLOTS_PER_TABLE;
    public const int WAITING_COUNT = 4;
    public const int UNIT_TYPE_COUNT = 5;

    // ---------- 데이터 ----------
    private int[][] _puzzleTables;
    private WaitingCombo[] _waitingQueue;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        _puzzleTables = new int[TABLE_COUNT][];
        for (int i = 0; i < TABLE_COUNT; i++)
        {
            _puzzleTables[i] = new int[SLOTS_PER_TABLE];
            for (int j = 0; j < SLOTS_PER_TABLE; j++)
                _puzzleTables[i][j] = -1;
        }

        _waitingQueue = new WaitingCombo[WAITING_COUNT];
    }

    // ---------- 퍼즐 테이블 ----------
    public int GetUnit(int tableIdx, int slotIdx) => _puzzleTables[tableIdx][slotIdx];

    public void PlaceUnit(int tableIdx, int slotIdx, int unitType)
    {
        _puzzleTables[tableIdx][slotIdx] = unitType;
        OnSlotChanged?.Invoke(tableIdx, slotIdx, unitType);
    }

    public void ClearSlot(int tableIdx, int slotIdx)
    {
        _puzzleTables[tableIdx][slotIdx] = -1;
        OnSlotChanged?.Invoke(tableIdx, slotIdx, -1);
    }

    public void ClearTable(int tableIdx)
    {
        for (int i = 0; i < SLOTS_PER_TABLE; i++)
            _puzzleTables[tableIdx][i] = -1;

        OnTableCleared?.Invoke(tableIdx);
    }

    // ---------- 대기열 ----------
    public WaitingCombo GetWaiting(int index) => _waitingQueue[index];

    public void SetWaiting(int index, WaitingCombo combo)
    {
        _waitingQueue[index] = combo;
        // Debug.Log($"[모델] 대기열 {index}번 갱신됨! 유닛 타입: {combo.unitTypes[0]}");
        OnWaitingUpdated?.Invoke(index, combo);
    }

    public void ShiftWaitingQueue()
    {
        for (int i = 0; i < WAITING_COUNT - 1; i++)
        {
            _waitingQueue[i] = _waitingQueue[i + 1];
            OnWaitingUpdated?.Invoke(i, _waitingQueue[i]);
        }
    }

    // ---------- 보드 초기화 ----------
    public void ResetBoard()
    {
        for (int t = 0; t < TABLE_COUNT; t++)
            for (int s = 0; s < SLOTS_PER_TABLE; s++)
                _puzzleTables[t][s] = -1;

        OnBoardReset?.Invoke();
    }

    // ---------- 랜덤 배치 및 셔플 ----------
    public void ShuffleBoard()
    {
        if (_puzzleTables == null)
        {
            Initialize();
        }

        for (int t = 0; t < TABLE_COUNT; t++)
        {
            for (int s = 0; s < SLOTS_PER_TABLE; s++)
            {
                int randomUnitType = UnityEngine.Random.Range(0, UNIT_TYPE_COUNT);

                _puzzleTables[t][s] = randomUnitType;

                OnSlotChanged?.Invoke(t, s, randomUnitType);
            }
        }
    }

    // ---------- 조회 ----------
    public bool IsTableEmpty(int tableIdx)
    {
        for (int i = 0; i < SLOTS_PER_TABLE; i++)
            if (_puzzleTables[tableIdx][i] >= 0) return false;
        return true;
    }

    public int FindEmptySlot(int tableIdx)
    {
        for (int i = 0; i < SLOTS_PER_TABLE; i++)
            if (_puzzleTables[tableIdx][i] < 0) return i;
        return -1;
    }

    // 3칸 전부 같은 유닛인지
    public bool IsTableMatched(int tableIdx)
    {
        int first = _puzzleTables[tableIdx][0];
        if (first < 0) return false;

        for (int i = 1; i < SLOTS_PER_TABLE; i++)
            if (_puzzleTables[tableIdx][i] != first) return false;

        return true;
    }

    public int CountEmptySlots()
    {
        int count = 0;
        for (int t = 0; t < TABLE_COUNT; t++)
            for (int s = 0; s < SLOTS_PER_TABLE; s++)
                if (_puzzleTables[t][s] < 0) count++;
        return count;
    }

    // 유닛 종류별 개수 (힌트용). Dictionary 재사용해서 GC 안 생김.
    private Dictionary<int, int> _cachedCounts = new Dictionary<int, int>();
    public Dictionary<int, int> CountUnitTypes()
    {
        _cachedCounts.Clear();
        for (int t = 0; t < TABLE_COUNT; t++)
        {
            for (int s = 0; s < SLOTS_PER_TABLE; s++)
            {
                int unit = _puzzleTables[t][s];
                if (unit < 0) continue;

                if (!_cachedCounts.ContainsKey(unit))
                    _cachedCounts[unit] = 0;
                _cachedCounts[unit]++;
            }
        }
        return _cachedCounts;
    }

    // 기획 3-2: 퍼즐 테이블에 '3개 이상' 배치된 유닛 종류 중 하나를 랜덤 선택해
    //           그 종류의 유닛들을 흔든다. 3개 이상인 종류가 없으면 빈 리스트 반환
    //           → HintSystem이 대기 테이블을 흔든다 (3-2-4).
    public List<(int t, int s)> GetHintTargets()
    {
        var counts = CountUnitTypes();
        var candidates = new List<int>();
        foreach (var pair in counts)
            if (pair.Value >= 3) candidates.Add(pair.Key);

        var result = new List<(int t, int s)>();
        if (candidates.Count == 0)
            return result;

        int chosenType = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        for (int t = 0; t < TABLE_COUNT; t++)
            for (int s = 0; s < SLOTS_PER_TABLE; s++)
                if (_puzzleTables[t][s] == chosenType)
                    result.Add((t, s));

        return result;
    }
}
