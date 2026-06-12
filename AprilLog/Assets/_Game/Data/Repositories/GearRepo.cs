using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GearRepo : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("Gacha Box Data")] 
    [SerializeField] private GachaBoxTable _gachaBoxTable;
    
    [Header("Gear Data")]
    [SerializeField] private GearMasterTable _gearTable;
    [SerializeField] private GearGradeTable _gradeTable;
    [SerializeField] private GearLevelTable _levelTable;
    [SerializeField] private GearUpgradeCostTable _upgradeCostTable;
    
    [Header("Special Effect Data")]
    [SerializeField] private GearSpecialEffectTable _specialEffectTable;
    
    // ---------- Dictionary ----------
    private Dictionary<int, GachaBoxData> _gachas;
    private Dictionary<int, GearMasterData> _gears;
    private Dictionary<int, GearLevelData> _levels;
    private Dictionary<string, GearGradeData> _grades;
    private Dictionary<int, GearUpgradeCostData> _upgradeCosts;
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
        
        _gachas = BuildDictionary(_gachaBoxTable, nameof(_gachaBoxTable), r => r.Gacha_ID);
        _gears = BuildDictionary(_gearTable, nameof(_gearTable), r => r.Gear_ID);
        _levels = BuildDictionary(_levelTable, nameof(_levelTable), r => r.Gear_ID);
        _grades = BuildDictionary(_gradeTable, nameof(_gradeTable), r => r.GearGrade);
        _upgradeCosts = BuildDictionary(_upgradeCostTable, nameof(_upgradeCostTable), r => r.Gear_ID);
        _specialEffects = BuildDictionary(_specialEffectTable, nameof(_specialEffectTable), r => r.Special_ID);
        
        _isInitialized = true;
        Debug.Log($"[GearRepo] Initialize Complete.\n GachaBox: {_gachas.Count}, Gears: {_gears.Count}, GearLevels: {_levels.Count}, GearGrades: {_grades.Count}, UpgradeCosts: {_upgradeCosts.Count}, SpecialEffects: {_specialEffects.Count}");
    }
    
    // ---------- Get Data ----------
    public GachaBoxData GetGachaBox(int id)
    {
        if (_gachas == null)
        {
            Debug.LogWarning("[GearRepo] GachaBox cache is not initialized. Empty dictionary will be used.");
            _gachas = new Dictionary<int, GachaBoxData>();
        }

        if (_gachas.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GachaBox not found. Id: {id}");
        return null;
    }
    
    public GearMasterData GetGearData(int id)
    {
        if (_gears == null)
        {
            Debug.LogWarning("[GearRepo] GearMasterData cache is not initialized. Empty dictionary will be used.");
            _gears = new Dictionary<int, GearMasterData>();
        }

        if (_gears.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GearData not found. Id: {id}");
        return null;
    }
    
    public GearLevelData GetGearLevel(int id)
    {
        if (_levels == null)
        {
            Debug.LogWarning("[GearRepo] GearLevelData cache is not initialized. Empty dictionary will be used.");
            _levels = new Dictionary<int, GearLevelData>();
        }

        if (_levels.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GearLevel not found. Id: {id}");
        return null;
    }
    
    public GearGradeData GetGearGrade(string gearGradeName)
    {
        if (_grades == null)
        {
            Debug.LogWarning("[GearRepo] GearGradeData cache is not initialized. Empty dictionary will be used.");
            _grades = new Dictionary<string, GearGradeData>();
        }

        if (_grades.TryGetValue(gearGradeName, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GearGradeData not found. GearGradeName: {gearGradeName}");
        return null;
    }
    
    public GearUpgradeCostData GetGearUpgradeCost(int id)
    {
        if (_upgradeCosts == null)
        {
            Debug.LogWarning("[GearRepo] GearUpgradeCostData cache is not initialized. Empty dictionary will be used.");
            _upgradeCosts = new Dictionary<int, GearUpgradeCostData>();
        }

        if (_upgradeCosts.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GearUpgradeCost not found. Id: {id}");
        return null;
    }
    
    public GearSpecialEffectData GetGearSpecialEffect(int id)
    {
        if (_specialEffects == null)
        {
            Debug.LogWarning("[GearRepo] GearSpecialEffectData cache is not initialized. Empty dictionary will be used.");
            _specialEffects = new Dictionary<int, GearSpecialEffectData>();
        }

        if (_specialEffects.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning($"[GearRepo] GearSpecialEffect not found. Id: {id}");
        return null;
    }
    
    // ---------- Build Dictionary ----------
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
}
