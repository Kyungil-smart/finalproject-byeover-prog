// 담당자 : 정승우
// 설명   : 재화 View -- 골드, 양피지
// 수정자 : 홍정옥
// 수정내용 : 행동력(스태미나)을 StaminaModel로 분리, CurrencyModel은 재화만 관리
// 수정자 : 정승우 (재화 관리 통일 단계 ③)
// 수정내용 : 영속 원본은 GameManager(CloudData). 이 모델은 GameManager 단일 재화 API에 '위임'하는 로비 View로 전환.
//           GameManager가 없는 단독 씬 실행(테스트)에서만 로컬 폴백 값을 쓴다.

using System;
using UnityEngine;

/// <summary>
/// 골드·양피지 View. 영속 단일 원본은 GameManager.CloudData이며, 모든 조작은 GameManager 단일 API에 위임한다.
/// (GameManager가 없을 때 = 로비 단독 실행 테스트에서만 로컬 폴백 값으로 동작)
/// 값이 바뀌면 OnCurrencyChanged 발행(GameManager 이벤트를 그대로 전달).
/// </summary>
public class CurrencyModel : MonoBehaviour
{
    public const int TestStartGold      = 99900;
    public const int TestStartParchment = 99900;

    // ---------- 이벤트 ----------
    public event Action<int, int> OnCurrencyChanged;    // gold, parchment

    // ---------- 데이터 (GameManager 있으면 그 값을, 없으면 로컬 폴백을 반환) ----------
    public int Gold      => GameManager.Instance != null ? GameManager.Instance.Gold      : _localGold;
    public int Parchment => GameManager.Instance != null ? GameManager.Instance.Parchment : _localParchment;

    [Header("단독 씬 테스트 기본값 (GameManager 없을 때만 사용)")]
    [SerializeField] private bool initializeWithTestValues = true;
    [SerializeField] private int testStartGold      = TestStartGold;
    [SerializeField] private int testStartParchment = TestStartParchment;

    private int _localGold;       // GameManager 없을 때 폴백 보유량
    private int _localParchment;

    private void Awake()
    {
        // GameManager(영속 원본)가 있으면 재화는 거기서 온다 → 테스트 기본값으로 덮지 않는다(클라우드 재화 보호).
        if (initializeWithTestValues && GameManager.Instance == null)
            Initialize(testStartGold, testStartParchment);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCurrencyChanged += HandleAuthorityChanged;
            RaiseChanged(); // 현재 권위 값으로 UI 동기화
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCurrencyChanged -= HandleAuthorityChanged;
    }

    private void HandleAuthorityChanged(int gold, int parchment) => OnCurrencyChanged?.Invoke(gold, parchment);

    // ---------- 초기화/리셋 ----------
    public void Initialize(int gold, int parchment)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCurrency(gold, parchment);
        }
        else
        {
            _localGold = Mathf.Max(0, gold);
            _localParchment = Mathf.Max(0, parchment);
            RaiseChanged();
        }
    }

    // ---------- 조작 (GameManager 단일 API에 위임, 없으면 로컬 폴백) ----------
    public void AddGold(int amount) => Add(amount, 0);
    public void AddParchment(int amount) => Add(0, amount);

    private void Add(int gold, int parchment)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCurrency(gold, parchment, "lobby");
        }
        else
        {
            _localGold = Mathf.Max(0, _localGold + gold);
            _localParchment = Mathf.Max(0, _localParchment + parchment);
            RaiseChanged();
        }
    }

    public bool SpendGold(int amount) => TrySpend(amount, 0);
    public bool SpendParchment(int amount) => TrySpend(0, amount);

    public bool CanAfford(int gold, int parchment)
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.CanAffordCurrency(gold, parchment);
        return _localGold >= Mathf.Max(0, gold) && _localParchment >= Mathf.Max(0, parchment);
    }

    public bool TrySpend(int gold, int parchment)
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.TrySpendCurrency(gold, parchment);

        gold = Mathf.Max(0, gold);
        parchment = Mathf.Max(0, parchment);
        if (_localGold < gold || _localParchment < parchment) return false;
        _localGold -= gold;
        _localParchment -= parchment;
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
