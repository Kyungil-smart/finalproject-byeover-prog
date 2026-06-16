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
    private Dictionary<int, FreeGachaBoxData> _freeGachas;
    private Dictionary<int, PaidGachaBoxData> _paidGachas;
    private Dictionary<int, GachaRewardData> _rewards;
    
    private Dictionary<int, GearMasterData> _gears;
    private Dictionary<int, GearLevelData> _levels;
    private Dictionary<string, GearGradeData> _grades;
    private Dictionary<int, GearUpgradeCostData> _upgradeCosts;
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
        _freeGachas = BuildDictionary(_freeGachaBoxTable, nameof(_freeGachaBoxTable), r => r.Gacha_ID);
        _paidGachas = BuildDictionary(_paidGachaBoxTable, nameof(_paidGachaBoxTable), r => r.Gacha_ID);
        _rewards = BuildDictionary(_gachaRewardTable, nameof(_gachaRewardTable), r => r.Gacha_ID);
    }

    private void GearDataInit()
    {
        _gears = BuildDictionary(_gearTable, nameof(_gearTable), r => r.Gear_ID);
        _levels = BuildDictionary(_levelTable, nameof(_levelTable), r => r.Gear_ID);
        _grades = BuildDictionary(_gradeTable, nameof(_gradeTable), r => r.GearGrade);
        _upgradeCosts = BuildDictionary(_upgradeCostTable, nameof(_upgradeCostTable), r => r.Gear_ID);
        _ascensionCosts = BuildAscensionCostDictionary();
        _dismantles = BuildDictionary(_dismantleTable, nameof(_dismantleTable), r => r.GearGrade);
        _shardExchange = BuildDictionary(_shardExchangeTable, nameof(_shardExchangeTable), r => r.Exchange_ID);
    }

    private void GearEffectDataInit()
    {
        _specialEffects = BuildDictionary(_specialEffectTable, nameof(_specialEffectTable), r => r.Special_ID);
    }
    
    // ---------- Get Data ----------
    public GachaBoxData GetGachaBox(int id) => GetData(_gachas, id, nameof(GetGachaBox));
    public FreeGachaBoxData GetFreeGachaBox(int id) => GetData(_freeGachas, id, nameof(GetFreeGachaBox));
    public PaidGachaBoxData GetPaidGachaBox(int id) => GetData(_paidGachas, id, nameof(GetPaidGachaBox));
    public GachaRewardData GetGachaReward(int id) => GetData(_rewards, id, nameof(GetGachaReward));
    
    public GearMasterData GetGearData(int id) => GetData(_gears, id, nameof(GetGearData));
    public GearLevelData GetGearLevel(int id) => GetData(_levels, id, nameof(GetGearLevel));
    public GearGradeData GetGearGrade(string gearGrade) => GetData(_grades, gearGrade, nameof(GetGearGrade));
    public GearUpgradeCostData GetGearUpgradeCost(int id) => GetData(_upgradeCosts, id, nameof(GetGearUpgradeCost));
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
            Debug.LogWarning($"[GearRepo] AscensionCostTable is not assigned. Empty dictionary will be used.");
            return result;
        }
        
        if (table.rows == null)
        {
            Debug.LogWarning($"[GearRepo] AscensionCostTable.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            var row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[GearRepo] AscensionCostTable.rows[{i}] is null. Skip.");
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
