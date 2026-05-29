// 담당자 : 정승우
// 설명   : 밸런스/성장/업적/보상 데이터 저장소

// 1차 수정자 : 홍정옥
// 수정내용 : GetOutGrowthBonusUntilLevel 메서드 추가 (아웃게임 성장 누적 보너스 계산)

// 2차 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨 곡선, 아웃게임 성장, 업적, 보상 등 설정 데이터를 관리한다.
/// </summary>
public class Legacy_ConfigRepo : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private InLevelTable _inLevelTable;
    [SerializeField] private OutLevelTable _outLevelTable;
    [SerializeField] private AchievementDataTable _achievementTable;
    [SerializeField] private ChangeRewardTable _changeRewardTable;

    private Dictionary<int, InLevelData> _inLevel;
    private Dictionary<int, OutLevelData> _outLevel;
    private Dictionary<int, AchievementData> _achievements;
    private List<ChangeRewardData> _changeRewards;
    private bool _isInitialized;

    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.Log("[ConfigRepo] Already initialized. Skip.");
            return;
        }

        _inLevel = BuildDictionary(_inLevelTable, nameof(_inLevelTable), r => r.InLevel);
        _outLevel = BuildDictionary(_outLevelTable, nameof(_outLevelTable), r => r.OutLevel);
        _achievements = BuildDictionary(_achievementTable, nameof(_achievementTable), r => r.AchievementID);
        _changeRewards = BuildList(_changeRewardTable, nameof(_changeRewardTable));
        _isInitialized = true;
        Debug.Log($"[ConfigRepo] 초기화 완료. InLevel: {_inLevel.Count}, OutLevel: {_outLevel.Count}, Achievements: {_achievements.Count}, ChangeRewards: {_changeRewards.Count}");
    }

    public InLevelData GetInLevel(int level) => GetData(_inLevel, level, nameof(GetInLevel));
    public OutLevelData GetOutLevel(int level) => GetData(_outLevel, level, nameof(GetOutLevel));
    public AchievementData GetAchievement(int id) => GetData(_achievements, id, nameof(GetAchievement));

    public IReadOnlyDictionary<int, AchievementData> GetAllAchievements()
    {
        if (_achievements == null)
        {
            Debug.LogWarning("[ConfigRepo] Achievement cache is not initialized. Empty dictionary will be used.");
            _achievements = new Dictionary<int, AchievementData>();
        }

        return _achievements;
    }

    public IReadOnlyList<ChangeRewardData> GetAllChangeRewards()
    {
        if (_changeRewards == null)
        {
            Debug.LogWarning("[ConfigRepo] ChangeReward cache is not initialized. Empty list will be used.");
            _changeRewards = new List<ChangeRewardData>();
        }

        return _changeRewards;
    }

    // 홍정옥 요청: 1레벨부터 targetLevel까지의 성장 보너스 누적합 계산
    // 예: 캐릭터가 10레벨이면 1~10레벨까지의 MaxHP, Attack 등 증가량을 전부 더함
    public void GetOutGrowthBonusUntilLevel(int targetLevel,
        out int hpBonus, out int attackBonus, out int stunBonus, out int slowBonus)
    {
        hpBonus = 0;
        attackBonus = 0;
        stunBonus = 0;
        slowBonus = 0;

        for (int lv = 1; lv <= targetLevel; lv++)
        {
            var data = GetOutLevel(lv);
            if (data == null) continue;

            hpBonus += data.MaxHP;
            attackBonus += data.Attack;
            stunBonus += data.StunPower;
            slowBonus += data.SlowPower;
        }
    }

    private Dictionary<TKey, TData> BuildDictionary<TData, TKey>(
        DataTable<TData> table,
        string tableName,
        Func<TData, TKey> keySelector)
        where TData : class
    {
        var result = new Dictionary<TKey, TData>();

        if (table == null)
        {
            Debug.LogWarning($"[ConfigRepo] {tableName} is not assigned. Empty dictionary will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[ConfigRepo] {tableName}.rows is null. Empty dictionary will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[ConfigRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            TKey key = keySelector(row);
            if (result.ContainsKey(key))
            {
                Debug.LogWarning($"[ConfigRepo] {tableName} has duplicate key '{key}'. Keep first row and skip index {i}.");
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
            Debug.LogWarning($"[ConfigRepo] {tableName} is not assigned. Empty list will be used.");
            return result;
        }

        if (table.rows == null)
        {
            Debug.LogWarning($"[ConfigRepo] {tableName}.rows is null. Empty list will be used.");
            return result;
        }

        for (int i = 0; i < table.rows.Count; i++)
        {
            TData row = table.rows[i];
            if (row == null)
            {
                Debug.LogWarning($"[ConfigRepo] {tableName}.rows[{i}] is null. Skip.");
                continue;
            }

            result.Add(row);
        }

        return result;
    }

    private TData GetData<TData>(Dictionary<int, TData> dictionary, int key, string methodName)
        where TData : class
    {
        if (dictionary == null)
        {
            Debug.LogWarning($"[ConfigRepo] {methodName} cache is not initialized. Key: {key}");
            return null;
        }

        if (dictionary.TryGetValue(key, out TData data))
            return data;

        Debug.LogWarning($"[ConfigRepo] {methodName} data not found. Key: {key}");
        return null;
    }
}
