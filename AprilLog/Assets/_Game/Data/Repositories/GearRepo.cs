using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GearRepo : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("Gacha Box Data")] 
    [SerializeField] private GachaBoxTable _gachaBoxTable;
    [SerializeField] private FreeGachaBoxTable _freeGachaBoxTable;
    [SerializeField] private PaidGachaBoxTable _paidGachaBoxTable;
    [SerializeField] private GachaRewardTable _gachaRewardTable;
    
    [Header("Gear Data")]
    [SerializeField] private GearMasterTable _gearTable;
    [SerializeField] private GearGradeTable _gradeTable;
    [SerializeField] private GearLevelTable _levelTable;
    [SerializeField] private GearUpgradeCostTable _upgradeCostTable;
    [SerializeField] private GearAscensionCostTable _ascensionCostTable;
    [SerializeField] private GearDismantleTable _dismantleTable;
    [SerializeField] private LegendaryShardExchangeTable _shardExchangeTable;
    
    [Header("Special Effect Data")]
    [SerializeField] private GearSpecialEffectTable _specialEffectTable;
    
    // ---------- Dictionary ----------
    private Dictionary<int, GachaBoxData> _gachas;
    private Dictionary<(int, string), FreeGachaBoxData> _freeGachas;
    private Dictionary<(int, int), PaidGachaBoxData> _paidGachas;
    private Dictionary<int, GachaRewardData> _rewards;
    
    private Dictionary<int, GearMasterData> _gears;
    private Dictionary<int, GearLevelData> _levels;
    private Dictionary<string, GearGradeData> _grades;
    private Dictionary<int, GearUpgradeSupporter> _upgradeCosts;
    private Dictionary<string, Dictionary<string, GearAscensionCostData>> _ascensionCosts;
    private Dictionary<string, GearDismantleData> _dismantles;
    private Dictionary<int, LegendaryShardExchangeData> _shardExchange;
    
    private Dictionary<int, GearSpecialEffectData> _specialEffects;
    
    // ---------- private ----------
    private bool _isInitialized;
    
    // ---------- Initialize ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[GearRepo] Already initialized. Skip.");
            return;
        }
        
        GachaDataInit();
        GearDataInit();
        GearEffectDataInit();
        
        _isInitialized = true;
        Debug.Log($"[GearRepo] Initialize Complete.\n GachaBox: {_gachas.Count}, Gears: {_gears.Count}, GearLevels: {_levels.Count}, GearGrades: {_grades.Count}, UpgradeCosts: {_upgradeCosts.Count}, SpecialEffects: {_specialEffects.Count}");
    }

    private void GachaDataInit()
    {
        _gachas = BuildDictionary(_gachaBoxTable, nameof(_gachaBoxTable), r => r.Gacha_ID);
        _freeGachas = BuildDictionary(_freeGachaBoxTable, nameof(_freeGachaBoxTable), r => (r.Gacha_ID,r.FreeDrawType));
        _paidGachas = BuildDictionary(_paidGachaBoxTable, nameof(_paidGachaBoxTable), r => (r.Gacha_ID,r.Count));
        _rewards = BuildDictionary(_gachaRewardTable, nameof(_gachaRewardTable), r => r.Gacha_ID);
    }

    private void GearDataInit()
    {
        _gears = BuildDictionary(_gearTable, nameof(_gearTable), r => r.Gear_ID);
        _levels = BuildDictionary(_levelTable, nameof(_levelTable), r => r.Gear_ID);
        _grades = BuildDictionary(_gradeTable, nameof(_gradeTable), r => r.GearGrade);
        _upgradeCosts = BuildGearUpgradeCostDictionary();
        _ascensionCosts = BuildAscensionCostDictionary();
        _dismantles = BuildDictionary(_dismantleTable, nameof(_dismantleTable), r => r.GearGrade);
        _shardExchange = BuildDictionary(_shardExchangeTable, nameof(_shardExchangeTable), r => r.Exchange_ID);
    }

    private void GearEffectDataInit()
    {
        _specialEffects = BuildDictionary(_specialEffectTable, nameof(_specialEffectTable), r => r.Special_ID);
    }
    
    // ---------- 조회 API ----------
    public GachaBoxData GetGachaBox(int id) => GetData(_gachas, id, nameof(GetGachaBox));
    public FreeGachaBoxData GetFreeGachaBox(int id, string freeDrawType) => GetData(_freeGachas, (id , freeDrawType), nameof(GetFreeGachaBox));
    public PaidGachaBoxData GetPaidGachaBox(int id, int count) => GetData(_paidGachas, (id, count), nameof(GetPaidGachaBox));
    public GachaRewardData GetGachaReward(int id) => GetData(_rewards, id, nameof(GetGachaReward));
    
    public GearMasterData GetGearData(int id) => GetData(_gears, id, nameof(GetGearData));
    public GearLevelData GetGearLevel(int id) => GetData(_levels, id, nameof(GetGearLevel));
    public GearGradeData GetGearGrade(string gearGrade) => GetData(_grades, gearGrade, nameof(GetGearGrade));
    public GearDismantleData GetGearDismantleData(string gearGrade) => GetData(_dismantles, gearGrade, nameof(GetGearDismantleData));
    public LegendaryShardExchangeData GetLegendaryShardExchangeData(int id) => GetData(_shardExchange, id, nameof(GetLegendaryShardExchangeData));
    
    public GearSpecialEffectData GetGearSpecialEffect(int id) => GetData(_specialEffects, id, nameof(GetGearSpecialEffect));

    /// <summary>
    /// 장비 돌파 데이터 추출
    /// </summary>
    /// <param name="gearGrade">DB의 아티팩트 등급</param>
    /// <param name="materialType">DB의 재료 종류 - "SameGear" 기본값</param>
    /// <returns></returns>
    public GearAscensionCostData GetAscensionCosts(string gearGrade, string materialType = "SameGear")
    {
        if (_ascensionCosts == null)
        {
            Debug.LogWarning($"[GearRepo] GetAscensionCosts cache is not initialized.");
            return null;
        }

        if (_ascensionCosts.TryGetValue(gearGrade, out var temp))
        {
            if (temp.TryGetValue(materialType, out var data))
                return data;
        }
        
        Debug.LogWarning($"[GearRepo] GetAscensionCosts data not found. GearGrade: {gearGrade}, MaterialType: {materialType}");
        return null;
    }

    /// <summary>
    /// 기어 업그레이드 코스트 계산
    /// </summary>
    /// <param name="gearId">DB의 기어 ID</param>
    /// <param name="curLevel">기어의 현재 레벨</param>
    /// <param name="costType">기어 업그레이드에 들어가는 재료 ID (ItemMaster 시트 참고)</param>
    /// <returns></returns>
    public int GetGearUpgradeCost(int gearId, int curLevel, int costType)
    {
        if (_upgradeCosts == null)
        {
            Debug.LogWarning($"[GearRepo] GetAscensionCosts cache is not initialized. Get 0.");
            return 0;
        }

        if (_upgradeCosts.TryGetValue(gearId, out var temp))
        {
            int? cost = temp.CalculateUpgradeCosts(curLevel, costType);
            
            if (cost != null) return cost.Value;
        }
        
        Debug.LogWarning($"[GearRepo] GetGearUpgradeCost data not found. Get 0.\nGearID: {gearId}, CurLevel: {curLevel}, CostType: {costType}");
        return 0;
    }
    
    
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
            Debug.LogWarning($"[GearRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[GearRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[GearRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[GearRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }

    private Dictionary<string, Dictionary<string, GearAscensionCostData>> BuildAscensionCostDictionary()
    {
        var result = new Dictionary<string, Dictionary<string, GearAscensionCostData>>();
        var table = _ascensionCostTable;
        
        if (table == null)
        {
            Debug.LogWarning($"[GearRepo] GearAscensionCostTable is not assigned. Empty dictionary will be used.");
            return result;
        }
        
        if (table.rows == null)
        {
            Debug.LogWarning($"[GearRepo] GearAscensionCostTable.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            var row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[GearRepo] GearAscensionCostTable.rows[{i}] is null. Skip.");
                continue;
            }
            
            var key = row.GearGrade;
            var key2 = row.MaterialType;
            
            if (!result.TryGetValue(key, out var pool))
            {
                pool = new Dictionary<string, GearAscensionCostData>();
                result.Add(key, pool);
            }

            pool[key2] = row;
        }
        
        return result;
    }

    private Dictionary<int, GearUpgradeSupporter> BuildGearUpgradeCostDictionary()
    {
        var result = new Dictionary<int, GearUpgradeSupporter>();
        var table = _upgradeCostTable;
        
        if (table == null)
        {
            Debug.LogWarning($"[GearRepo] GearUpgradeCostTable is not assigned. Empty dictionary will be used.");
            return result;
        }
        
        if (table.rows == null)
        {
            Debug.LogWarning($"[GearRepo] GearUpgradeCostTable.rows is null. Empty dictionary will be used.");
            return result;
        }
        
        for (int i = 0; i < table.rows.Count; i++)
        {
            var row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[GearRepo] GearUpgradeCostTable.rows[{i}] is null. Skip.");
                continue;
            }
            
            var key = row.Gear_ID;
            
            if (!result.TryGetValue(key, out var pool))
            {
                pool = new GearUpgradeSupporter(row.Gear_ID, row.StartLevel, row.EndLevel);
                pool.AddData(row);
                result.Add(key, pool);
            }
            
            pool.AddData(row);
        }
        
        return result;
    }
    
    private TData GetData<TKey, TData>(Dictionary<TKey, TData> dictionary, TKey key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[GearRepo] {methodName} cache is not initialized.");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[GearRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}
