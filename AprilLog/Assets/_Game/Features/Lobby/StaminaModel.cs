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
    
    private const int StaminaId = 10001;

    // ---------- 이벤트 ----------
    public event Action<int, int> OnStaminaChanged;   // current, max

    // ---------- 데이터 ----------
    public int Current {
        get 
        {
            if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
            {
                var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
                return slot != null ? slot.CurrentAmount : 0;
            }
            return _localStamina;
        }
    }
    public int Max {
        get 
        {
            if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
            {
                var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
                return slot != null ? slot.OverCapMax : 0;
            }
            return _localMaxStamina;
        }
    }
    
    private int _localStamina;
    private int _localMaxStamina;

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
    
    private void OnEnable()
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            DataManager.Instance.ResourceRepo.OnStaminaRecovered += HandleStaminaEvent;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            DataManager.Instance.ResourceRepo.OnStaminaRecovered -= HandleStaminaEvent;
        }
    }

    // ---------- 초기화 ----------
    public void Initialize(int current, int max)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
            slot.SetStamina(Mathf.Max(0, current), Mathf.Max(0, max));
            return;
        }
        
        _localStamina = Mathf.Max(1, max);
        _localMaxStamina = Mathf.Clamp(current, 0, Max);
        _initialized = true;
        HandleStaminaEvent(StaminaId);
    }

    // ---------- 조작 ----------
    public void Recover(int amount, out int lossAmount)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
            if (slot == null)
            {
                lossAmount = 0;
                return;
            }
            DataManager.Instance.ResourceRepo.AddStamina(StaminaId, Mathf.Max(0, amount), out lossAmount);
            GameManager.Instance.SyncAndSaveResourceCloudData();
            HandleStaminaEvent(StaminaId);
            return;
        }

        var temp = Current + Mathf.Max(0, amount);
        _localStamina = Mathf.Clamp(temp, 0, Max);
        lossAmount = Mathf.Max(0, temp - Max);
        HandleStaminaEvent(StaminaId);
    }

    public bool Spend(int amount)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
            if (slot == null) return false;
            if(!DataManager.Instance.ResourceRepo.UseStamina(StaminaId, Mathf.Max(0, amount))) return false;
            GameManager.Instance.SyncAndSaveResourceCloudData();
            HandleStaminaEvent(StaminaId);
            return true;
        }
        
        amount = Mathf.Max(0, amount);
        if (!CanAfford(amount)) return false;
        _localStamina -= amount;
        HandleStaminaEvent(StaminaId);
        return true;
    }

    private bool CanAfford(int amount) => Current >= Mathf.Max(0, amount);

    public void SetMax(int max)
    {
        if (GameManager.Instance != null && DataManager.Instance?.ResourceRepo != null)
        {
            var slot = DataManager.Instance.ResourceRepo.GetStaminaSlot(StaminaId);
            slot.SetStamina(Mathf.Max(Current, Max), Mathf.Max(0, Max));
            GameManager.Instance.SyncAndSaveResourceCloudData();
            HandleStaminaEvent(StaminaId);
            return;
        }
        
        _localMaxStamina = Mathf.Max(1, max);
        _localStamina = Mathf.Clamp(Current, 0, Max);
        HandleStaminaEvent(StaminaId);
    }
    
    private void HandleStaminaEvent(int staminaId)
    {
        if (staminaId == StaminaId) OnStaminaChanged?.Invoke(Current, Max);
    }
}
