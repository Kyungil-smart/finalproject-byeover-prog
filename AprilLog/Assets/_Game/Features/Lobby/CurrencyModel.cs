// 담당자 : 정승우
// 설명   : 재화 Model -- 골드, 양피지
// 수정자 : 홍정옥
// 수정내용 : 행동력(스태미나)을 StaminaModel로 분리, CurrencyModel은 재화만 관리

using System;
using UnityEngine;

/// <summary>
/// 골드와 양피지 보유량을 관리한다. 값 바뀌면 이벤트 발행.
/// (행동력은 StaminaModel에서 별도 관리)
/// </summary>
public class CurrencyModel : MonoBehaviour
{
    public const int TestStartGold      = 99900;
    public const int TestStartParchment = 99900;

    // ---------- 이벤트 ----------
    public event Action<int, int> OnCurrencyChanged;    // gold, parchment

    // ---------- 데이터 ----------
    public int Gold      { get; private set; }
    public int Parchment { get; private set; }

    [Header("테스트 기본값")]
    [SerializeField] private bool initializeWithTestValues = true;
    [SerializeField] private int testStartGold      = TestStartGold;
    [SerializeField] private int testStartParchment = TestStartParchment;

    private bool _initialized;

    private void Awake()
    {
        if (initializeWithTestValues && !_initialized)
            Initialize(testStartGold, testStartParchment);
    }

    // ---------- 초기화 ----------
    public void Initialize(int gold, int parchment)
    {
        Gold      = Mathf.Max(0, gold);
        Parchment = Mathf.Max(0, parchment);
        _initialized = true;
        RaiseChanged();
    }

    // ---------- 조작 ----------
    public void AddGold(int amount)
    {
        Gold = Mathf.Max(0, Gold + amount);
        RaiseChanged();
    }

    public void AddParchment(int amount)
    {
        Parchment = Mathf.Max(0, Parchment + amount);
        RaiseChanged();
    }

    public bool SpendGold(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Gold < amount) return false;
        Gold -= amount;
        RaiseChanged();
        return true;
    }

    public bool SpendParchment(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Parchment < amount) return false;
        Parchment -= amount;
        RaiseChanged();
        return true;
    }

    public bool CanAfford(int gold, int parchment)
    {
        return Gold >= Mathf.Max(0, gold) && Parchment >= Mathf.Max(0, parchment);
    }

    public bool TrySpend(int gold, int parchment)
    {
        gold = Mathf.Max(0, gold);
        parchment = Mathf.Max(0, parchment);
        if (!CanAfford(gold, parchment))
            return false;

        Gold -= gold;
        Parchment -= parchment;
        RaiseChanged();
        return true;
    }

    public static string FormatAmount(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount < 1000)
            return amount.ToString();

        float value = amount / 1000f;
        return value.ToString(value % 1f == 0f ? "0" : "0.#") + "k";
    }

    private void RaiseChanged()
    {
        OnCurrencyChanged?.Invoke(Gold, Parchment);
    }
}
