// 작성자 : 홍정옥
// 설명   : 뽑기 티켓 재화 Model.
//          다이아는 CurrencyModel/GameManager 로 통합되어 더 이상 여기서 관리하지 않는다.
//          뽑기 티켓은 아직 공용 아이템/인벤토리 저장소가 없어 이 모델이 임시로 보유한다
//          (StaminaModel 과 동일한 로컬 보관 패턴). 추후 인벤토리 시스템으로 이관 예정.

using System;
using UnityEngine;

public class ExtraCurrencyModel : MonoBehaviour
{
    public const int TestStartTicket = 0;
    private const int GachaTicketId = 70006;

    // ---------- 이벤트 ----------
    public event Action OnChanged;   // 티켓 변경 시 발행

    // ---------- 데이터 ----------
    private int _localTicket;
    public int GachaTicket => GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null
        ? DataManager.Instance.ResourceRepo.GetItemCount(GachaTicketId) 
        : _localTicket;

    [Header("테스트 기본값")]
    [SerializeField] private bool initializeWithTestValues = true;
    [SerializeField] private int testStartTicket = TestStartTicket;

    private void Awake()
    {
        if (initializeWithTestValues && GameManager.Instance == null)
            Initialize(testStartTicket);
    }
    
    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnCurrencyChanged += HandleInventoryChanged;
        RaiseChanged();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnCurrencyChanged -= HandleInventoryChanged;
    }

    // ---------- 초기화/리셋 ----------
    public void Initialize(int ticket)
    {
        if (GameManager.Instance != null)
        {
            DataManager.Instance.ResourceRepo.SetItemCount(GachaTicketId, Mathf.Max(0, ticket));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localTicket = Mathf.Max(0, ticket);
            RaiseChanged();
        }
    }

    // ---------- 뽑기 티켓 ----------
    public void AddTicket(int amount)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            DataManager.Instance.ResourceRepo.AddItem(GachaTicketId, Mathf.Max(0, amount));
            GameManager.Instance.SyncAndSaveResourceCloudData();
        }
        else
        {
            _localTicket += Mathf.Max(0, amount);
            RaiseChanged();
        }
    }

    public bool CanAffordTicket(int amount) => GachaTicket >= Mathf.Max(0, amount);

    public bool SpendTicket(int amount)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            if (GachaTicket < amount) return false;
            if (!DataManager.Instance.ResourceRepo.UseItem(GachaTicketId, Mathf.Max(0, amount))) return false;
            GameManager.Instance.SyncAndSaveResourceCloudData();
            return true;
        }
        
        if (_localTicket < amount) return false;
        _localTicket -= amount;
        RaiseChanged();
        return true;
    }

    private void RaiseChanged() => OnChanged?.Invoke();
    private void HandleInventoryChanged(int gold, int parchment) => RaiseChanged();
}
