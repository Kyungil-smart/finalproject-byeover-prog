// 담당자 : 홍정옥
// 설명   : 행동력(스태미나) Model -- 현재/최대 관리, 회복형 자원
//          재화(골드/양피지)와 별개로 관리한다.

using System;
using UnityEngine;

/// <summary>
/// 행동력(스태미나)을 관리한다. 현재값/최대값을 가지며 값이 바뀌면 이벤트 발행.
/// </summary>
public class StaminaModel : MonoBehaviour
{
    public const int TestStartStamina = 999;
    public const int TestMaxStamina   = 999;

    // ---------- 이벤트 ----------
    public event Action<int, int> OnStaminaChanged;   // current, max

    // ---------- 데이터 ----------
    public int Current { get; private set; }
    public int Max     { get; private set; }

    [Header("테스트 기본값")]
    [SerializeField] private bool initializeWithTestValues = true;
    [SerializeField] private int testStartStamina = TestStartStamina;
    [SerializeField] private int testMaxStamina   = TestMaxStamina;

    private bool _initialized;

    private void Awake()
    {
        if (initializeWithTestValues && !_initialized)
            Initialize(testStartStamina, testMaxStamina);
    }

    // ---------- 초기화 ----------
    public void Initialize(int current, int max)
    {
        Max     = Mathf.Max(1, max);
        Current = Mathf.Clamp(current, 0, Max);
        _initialized = true;
        RaiseChanged();
    }

    // ---------- 조작 ----------
    public void Recover(int amount)
    {
        Current = Mathf.Clamp(Current + Mathf.Max(0, amount), 0, Max);
        RaiseChanged();
    }

    public bool Spend(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Current < amount) return false;
        Current -= amount;
        RaiseChanged();
        return true;
    }

    public bool CanAfford(int amount) => Current >= Mathf.Max(0, amount);

    public void SetMax(int max)
    {
        Max     = Mathf.Max(1, max);
        Current = Mathf.Clamp(Current, 0, Max);
        RaiseChanged();
    }

    private void RaiseChanged() => OnStaminaChanged?.Invoke(Current, Max);
}
