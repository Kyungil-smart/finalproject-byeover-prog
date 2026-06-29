// 작성자 : 김영찬
// 내용 : 재화와 스태미나 데이터 조회 및 관리

using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceRepo : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("아이템 데이터")]
    [SerializeField] private ItemTable _itemTable;
    
    [Header("행동력 데이터")]
    [SerializeField] private StaminaTable _staminaTable;
    
    // ---------- Dictionary ----------
    private Dictionary<int, ItemData> _items;
    private Dictionary<int, StaminaData> _stamina;
    
    // ---------- Cache Container ----------
    private ItemContainer _itemContainer;
    private StaminaContainer _staminaContainer;
    
    // ---------- private ----------
    private bool _isInitialized;
    
    // ---------- Events ----------
    public event Action<int> OnStaminaRecovered; // StaminaID
    
    // ---------- Initialize ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[ResourceRepo] Already initialized. Skip.");
            return;
        }

        _items = BuildDictionary(_itemTable, nameof(_itemTable), r => r.Item_ID);
        _stamina = BuildDictionary(_staminaTable, nameof(_staminaTable), r => r.Stamina_ID);
        
        _itemContainer = new ItemContainer();
        _staminaContainer = new StaminaContainer();
        
        DateTime now = DateTime.Now;
        foreach (var dbData in _stamina.Values)
        {
            // 최초 접속이라 가정하고 InitialAmount 세팅. 초기화 후 로드데이터 있으면 로드 함.
            int savedAmount = dbData.InitialAmount; 
            DateTime savedTime = now; 

            _staminaContainer.AddOrInitSlot(dbData, savedAmount, savedTime);
            
            // 최초 기동 시 오프라인 회복 1회 계산
            GetStaminaSlot(dbData.Stamina_ID).CalculateOfflineRecovery(now);
        }
        
        _isInitialized = true;
        Debug.Log($"[ResourceRepo] 초기화 완료. Item : {_items.Count}, Stamina: {_stamina.Count}");
    }
    // ---------- Unity Life Cycle ----------
    private void Update()
    {
        if (!_isInitialized || _staminaContainer == null) return;

        // 게임이 켜져 있는 동안의 스태미나 실시간 회복 처리
        foreach (var slot in _staminaContainer.Slots.Values)
        {
            if (slot.TickOnline(Time.deltaTime))
            {
                OnStaminaRecovered?.Invoke(slot.StaminaID);
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!_isInitialized || _staminaContainer == null) return;

        if (pauseStatus)
        {
            // 앱이 백그라운드로 내려감 -> 데이터 저장
            // TODO: _itemContainer와 _staminaContainer의 내용을 JSON 등으로 기기/서버에 저장. <저장 부분은 팀장님과 협의 후 결정>
            Debug.Log("[ResourceRepo] 앱 일시정지 - 재화 및 스태미나 타임스탬프 저장 필요");
        }
        else
        {
            // 앱으로 복귀 -> 오프라인 시간만큼 스태미나 일괄 회복 계산
            DateTime now = DateTime.Now;
            foreach (var slot in _staminaContainer.Slots.Values)
            {
                slot.CalculateOfflineRecovery(now);
                OnStaminaRecovered?.Invoke(slot.StaminaID); // 복귀 시 UI 즉시 갱신
            }
            Debug.Log("[ResourceRepo] 앱 복귀 - 오프라인 스태미나 회복 계산 완료");
        }
    }
    // ---------- Data Load ----------
    public void LoadResourceData()
    {
        if (!_isInitialized)
        {
            Debug.LogWarning($"[ResourceRepo] {nameof(ResourceRepo)} is not initialized. Skip.");
            return;
        }
        // ToDo : 아이템 정보 로드 해야됨.  <로드 부분은 팀장님과 협의 후 수정>
    }
    
    // ---------- 조회 API ----------
    public ItemData GetItemInfo(int id) => GetData(_items, id, nameof(GetItemInfo));
    public int GetItemCount(int itemId) => _itemContainer.GetItemCount(itemId);
    public int GetStaminaAmount(int id) => GetStaminaSlot(id).CurrentAmount;
    public float GetStaminaTimer(int id) => GetStaminaSlot(id).RemainTimer;
    
    // ---------- 관리 API ----------
    public void AddItem(int itemId, int amount) => _itemContainer.AddItem(itemId, amount);
    public bool UseItem(int itemId, int amount) => _itemContainer.UseItem(itemId, amount);
    public void AddStamina(int id, int amount, out int lossAmount) => GetStaminaSlot(id).Add(amount, out lossAmount);
    public bool UseStamina(int id, int amount) => GetStaminaSlot(id).UseStamina(amount);
    
    // ---------- 보조 함수 ----------
    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            Debug.LogWarning($"[ResourceRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[ResourceRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[ResourceRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[ResourceRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }
    
    private TData GetData<TData>(Dictionary<int, TData> dictionary, int key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[ResourceRepo] {methodName} cache is not initialized. Key: {key}");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[ResourceRepo] {methodName} data not found. Key: {key}");
        return null;
    }
    
    private StaminaSlot GetStaminaSlot(int id) => _staminaContainer.GetSlot(id);
}
