// 작성자 : 김영찬
// 설명 : 하우징 DB의 Dictionary 캐쉬 생성 및 조회

// 1차 수정자 : 조규민
// 수정 내용 : Type/Category 캐시 기준 정상화 및 현재 챕터 이하 최신 하우징 보상 조회 추가

using System;
using System.Collections.Generic;
using UnityEngine;

public class HousingRepo : MonoBehaviour
{
    // ---------- SerializeField ----------
    [SerializeField] private HousingFurnitureTable _furnitureTable;
    [SerializeField] private HousingRewardTable _rewardTable;
    
    // ---------- Dictionary ----------
    private Dictionary<int, HousingFurnitureData> _furnitureById;
    private Dictionary<string, List<HousingFurnitureData>> _furnitureByType;
    private Dictionary<string, List<HousingFurnitureData>> _furnitureByCategory;
    private Dictionary<int, HousingRewardData> _rewards;
    
    // ---------- private ----------
    private bool _isInitialized;
    
    // ---------- Initialize ----------
    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[HousingRepo] Already initialized. Skip.");
            return;
        }

        _furnitureById = BuildDictionary(_furnitureTable, nameof(_furnitureTable), r => r.Furniture_ID);
        _furnitureByType = BuildGroupedDictionary(_furnitureTable, nameof(_furnitureTable), r => r.Type);
        _furnitureByCategory = BuildGroupedDictionary(_furnitureTable, nameof(_furnitureTable), r => r.Category);
        _rewards = BuildDictionary(_rewardTable, nameof(_rewardTable), r => r.ClearChapter);
        
        _isInitialized = true;
        Debug.Log($"[HousingRepo] Initialize Complete. Loaded {_furnitureById.Count} furniture, {_rewards.Count} rewards.");
    }
    
    // ---------- 조회 API ----------
    public HousingFurnitureData GetFurnitureById(int id) => GetData(_furnitureById, id, nameof(GetFurnitureById));
    public List<HousingFurnitureData> GetFurnitureListByType(string type) => GetData(_furnitureByType, type, nameof(GetFurnitureListByType));
    public List<HousingFurnitureData> GetFurnitureListByCategory(string category) => GetData(_furnitureByCategory, category, nameof(GetFurnitureListByCategory));
    public HousingRewardData GetReward(int id) => GetData(_rewards, id, nameof(GetReward));

    public HousingRewardData GetRewardAtOrBelow(int _clearChapter)
    {
        if (_rewards == null)
        {
            Debug.LogWarning("[HousingRepo] GetRewardAtOrBelow cache is not initialized.");
            return null;
        }

        int _bestChapter = int.MinValue;
        HousingRewardData _bestReward = null;

        foreach (KeyValuePair<int, HousingRewardData> _entry in _rewards)
        {
            if (_entry.Key > _clearChapter || _entry.Key <= _bestChapter)
            {
                continue;
            }

            _bestChapter = _entry.Key;
            _bestReward = _entry.Value;
        }

        return _bestReward;
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
            Debug.LogWarning($"[HousingRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[HousingRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[HousingRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[HousingRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
                continue;
            }

            result.Add(key, row);
        }

        return result;
    }
    
    private List<TData> BuildList<TData>(DataTable<TData> table, string tableName)
        where TData : class
    {
        var result = new List<TData>();

        if (table == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName} is not assigned. Empty list will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[StageRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[StageRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
    }

    private Dictionary<TKey, List<TData>> BuildGroupedDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, List<TData>>();

        if (table == null)
        {
            Debug.LogWarning($"[HousingRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[HousingRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[HousingRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
        
            // Key가 null이거나 빈 문자열인 경우 스킵 (기획 데이터 누락 방어)
            if (key == null || (key is string strKey && string.IsNullOrEmpty(strKey)))
            {
                continue;
            }

            // 해당 Key로 만들어진 리스트가 없다면 최초 1회 생성
            if (!result.ContainsKey(key))
            {
                result.Add(key, new List<TData>());
            }

            // 리스트에 데이터 추가
            result[key].Add(row);
        }

        return result;
    }

    private TData GetData<TKey, TData>(Dictionary<TKey, TData> dictionary, TKey key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[HousingRepo] {methodName} cache is not initialized.");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[HousingRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}
