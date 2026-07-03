// 작성자 : 김영찬
// 내용 : 재화와 스태미나 데이터 조회 및 관리

using System;
using System.Collections;
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
        
        InitializeContainer(nameof(_itemContainer), _itemContainer, () => _itemContainer.Initialize(_items));
        InitializeContainer(nameof(_staminaContainer), _staminaContainer, () => _staminaContainer.Initialize(_stamina));
        
        _isInitialized = true;
        Debug.Log($"[ResourceRepo] 초기화 완료. Item : {_items.Count}, Stamina: {_stamina.Count}");
    }

    private void InitializeContainer(string containerName, object container, Action initializeAction)
    {
        if (container == null)
        {
            Debug.LogError($"[ResourceRepo] {containerName} is not assigned. Repository initialization skipped.");
            return;
        }

        try
        {
            initializeAction.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceRepo] {containerName} initialization failed.\n{exception}");
        }
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
            // 전투/정산 중(InGame)엔 자동 저장하지 않는다. '전투 중 종료=포기'(기획 #300) 정책상
            // 런 진행 중 CloudData(재화 등)를 강제 저장하면 포기해도 런 결과가 남아 정책이 깨진다.
            // (정산 보상은 SaveChapterResult가 통제된 시점에 이미 저장. InGame 밖에서만 자동 저장.)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InGame) return;
            SaveResourceData();
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
    
    private void OnApplicationQuit()
    {
        if (!_isInitialized || _staminaContainer == null) return;

        // 전투/정산 중(InGame) 종료는 '포기'라 저장하지 않는다(기획 #300, OnApplicationPause와 동일 정책).
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InGame) return;
        SaveResourceData();
    }
    
    private void SaveResourceData()
    {
        if (GameManager.Instance != null && GameManager.Instance.CloudData != null)
        {
            GameManager.Instance.SyncAndSaveResourceCloudData();
            Debug.Log("[ResourceRepo] 재화 및 스태미나 타임스탬프 저장 실행!");
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
        
        if (GameManager.Instance != null && GameManager.Instance.CloudData != null)
        {
            var data = GameManager.Instance.CloudData;
            SetItemCount(70001, data.gold);      // 골드
            SetItemCount(70002, data.parchment); // 양피지
            SetItemCount(70003, data.diamond);   // 다이아
            
            if (data.inventory != null)
            {
                foreach (var item in data.inventory)
                {
                    SetItemCount(item.itemId, item.amount);
                }
            }
            
            if (data.staminaData != null)
            {
                foreach (var savedStamina in data.staminaData)
                {
                    if (!_stamina.TryGetValue(savedStamina.staminaId, out var dbData)) continue;
                    
                    // 저장된 시간(문자열)을 DateTime으로 변환
                    if (!DateTime.TryParse(savedStamina.lastUpdateTime, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out DateTime savedTime)) continue;
                    
                    _staminaContainer.AddOrInitSlot(dbData, savedStamina.currentAmount, savedTime);
                    GetStaminaSlot(dbData.Stamina_ID).CalculateOfflineRecovery(DateTime.Now);
                    OnStaminaRecovered?.Invoke(dbData.Stamina_ID);
                }
            }
            
            Debug.Log("[ResourceRepo] 클라우드 재화 및 스태미나 연동 완료!");
        }
    }
    
    // ---------- Data Export ----------
    public List<ItemSaveEntry> ExportInventory()
    {
        var list = new List<ItemSaveEntry>();
        if (_itemContainer == null) return list;

        foreach (var kvp in _itemContainer.GetAllItems())
        {
            // 골드, 양피지, 다이아는 CloudData에 전용 변수가 따로 있으므로(데이터 절약) 리스트에서 제외
            if (kvp.Key == 70001 || kvp.Key == 70002 || kvp.Key == 70003) 
                continue; 
            
            // 보유량이 0인 아이템은 제외 (용량 최적화)
            if (kvp.Value <= 0) 
                continue;

            list.Add(new ItemSaveEntry { itemId = kvp.Key, amount = kvp.Value });
        }
        return list;
    }
    
    public List<StaminaSaveEntry> ExportStaminaData()
    {
        var list = new List<StaminaSaveEntry>();
        if (_staminaContainer == null) return list;

        string currentTimeString = DateTime.UtcNow.ToString("o"); // 현재 시간을 ISO 규격 문자로 변환

        foreach (var slot in _staminaContainer.Slots.Values)
        {
            list.Add(new StaminaSaveEntry
            {
                staminaId = slot.StaminaID,
                currentAmount = slot.CurrentAmount,
                lastUpdateTime = currentTimeString 
            });
        }
        return list;
    }
    
    // ---------- 조회 API ----------
    // Item
    public ItemData GetItemInfo(int id) => GetData(_items, id, nameof(GetItemInfo));
    public int GetItemCount(int itemId) => _itemContainer.GetItemCount(itemId);
    
    // Stamina
    public StaminaSlot GetStaminaSlot(int id) => _staminaContainer.GetSlot(id);
    public int GetStaminaAmount(int id) => GetStaminaSlot(id).CurrentAmount;
    public float GetStaminaTimer(int id) => GetStaminaSlot(id).RemainTimer;
    
    // ---------- 관리 API ----------
    // Item
    public void AddItem(int itemId, int amount) => _itemContainer.AddItem(itemId, amount);
    public bool UseItem(int itemId, int amount) => _itemContainer.UseItem(itemId, amount);
    public void SetItemCount(int itemId, int amount) => _itemContainer.SetItemCount(itemId, amount);
    
    // Stamina
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
}
