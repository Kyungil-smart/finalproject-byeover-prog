// 작성자 : 홍정옥
// 설명   : 다이아 / 뽑기 티켓 재화 Model

using System;
using UnityEngine;

public class ExtraCurrencyModel : MonoBehaviour
{
    public const int TestStartDiamond = 0;
    public const int TestStartTicket  = 0;

    // ---------- 이벤트 ----------
    public event Action OnChanged;   // 다이아/티켓 중 하나라도 변경 시 발행

    // ---------- 데이터 ----------
    public int Diamond     { get; private set; }
    public int GachaTicket { get; private set; }

    [Header("테스트 기본값")]
    [SerializeField] private bool initializeWithTestValues = true;
    [SerializeField] private int testStartDiamond = TestStartDiamond;
    [SerializeField] private int testStartTicket  = TestStartTicket;

    private bool _initialized;

    private void Awake()
    {
        if (initializeWithTestValues && !_initialized)
            Initialize(testStartDiamond, testStartTicket);
    }

    // ---------- 초기화/리셋 ----------
    public void Initialize(int diamond, int ticket)
    {
        Diamond     = Mathf.Max(0, diamond);
        GachaTicket = Mathf.Max(0, ticket);
        _initialized = true;
        RaiseChanged();
    }

    // ---------- 다이아 ----------
    public void AddDiamond(int amount)
    {
        Diamond = Mathf.Max(0, Diamond + amount);
        RaiseChanged();
    }

    public bool CanAffordDiamond(int amount) => Diamond >= Mathf.Max(0, amount);

    public bool SpendDiamond(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Diamond < amount) return false;
        Diamond -= amount;
        RaiseChanged();
        return true;
    }

    // ---------- 뽑기 티켓 ----------
    public void AddTicket(int amount)
    {
        GachaTicket = Mathf.Max(0, GachaTicket + amount);
        RaiseChanged();
    }

    public bool CanAffordTicket(int amount) => GachaTicket >= Mathf.Max(0, amount);

    public bool SpendTicket(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (GachaTicket < amount) return false;
        GachaTicket -= amount;
        RaiseChanged();
        return true;
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}
