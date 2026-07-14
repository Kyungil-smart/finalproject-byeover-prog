// 담당자 : 정승우
// 설명   : 밸런스/성장/업적/보상 데이터 저장소

// 1차 수정자 : 홍정옥
// 수정내용 : GetOutGrowthBonusUntilLevel 메서드 추가 (아웃게임 성장 누적 보너스 계산)

// 2차 수정자 : 김영찬
// 수정 내용 : 행동력 데이터 삽입

// 3차 수정자 : 김영찬
// 수정 내용 : 26.06.12 DB 컬럼 변경 사항 반영하여 기절강화 둔화강화를 효과 강화로 연결

// 4차 수정자 : 김영찬
// 수정 내용 : 재화와 스태미나 부분을 SupplyRepo로 이관

// 5차 수정자 : 김영찬
// 수정 내용 : 보상 부분을 RewardRepo로 이관

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨 곡선, 아웃게임 성장, 업적 등 설정 데이터를 관리한다.
/// </summary>
public class ConfigRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("성장 데이터")]
    [SerializeField] private InLevelTable _inLevelTable;
    [SerializeField] private OutLevelTable _outLevelTable;
    
    [Header("업적 데이터")]
    [SerializeField] private Legacy_AchievementDataTable _achievementTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, InLevelData> _inLevel;
    private Dictionary<int, OutLevelData> _outLevel;
    private Dictionary<int, Legacy_AchievementData> _achievements;
    
    private bool _isInitialized;

    // ---------- 초기화 ----------
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
        
        _isInitialized = true;
        Debug.Log($"[ConfigRepo] 초기화 완료. InLevel: {_inLevel.Count}, OutLevel: {_outLevel.Count}, Achievements: {_achievements.Count}");
    }

    // ---------- 조회 API ----------
    public InLevelData GetInLevel(int level) => GetData(_inLevel, level, nameof(GetInLevel));
    public OutLevelData GetOutLevel(int level) => GetData(_outLevel, level, nameof(GetOutLevel));

    // OutLevel 테이블에 존재하는 가장 높은 레벨(= 아웃게임 캐릭터 레벨 상한)
    public int GetMaxOutLevel()
    {
        if (_outLevel == null || _outLevel.Count == 0)
        {
            Debug.LogWarning("[ConfigRepo] OutLevel 캐시가 비어 있어 최대 레벨을 확인할 수 없습니다.");
            return 0;
        }

        int max = 0;
        foreach (int level in _outLevel.Keys)
            if (level > max) max = level;
        return max;
    }
    public Legacy_AchievementData GetAchievement(int id) => GetData(_achievements, id, nameof(GetAchievement));
    

    public IReadOnlyDictionary<int, Legacy_AchievementData> GetAllAchievements()
    {
        if (_achievements == null)
        {
            Debug.LogWarning("[ConfigRepo] Achievement cache is not initialized. Empty dictionary will be used.");
            _achievements = new Dictionary<int, Legacy_AchievementData>();
        }

        return _achievements;
    }

    // 홍정옥 요청: 1레벨부터 targetLevel까지의 성장 보너스 누적합 계산
    // 예: 캐릭터가 10레벨이면 1~10레벨까지의 MaxHP, Attack 등 증가량을 전부 더함
    public void GetOutGrowthBonusUntilLevel(int targetLevel,
        out int hpBonus, out int attackBonus, out int effectPower, out int flatPierce)
    {
        hpBonus = 0;
        attackBonus = 0;
        effectPower = 0;
        flatPierce = 0;

        for (int lv = 1; lv <= targetLevel; lv++)
        {
            var data = GetOutLevel(lv);
            if (data == null) continue;

            hpBonus += data.MaxHP;
            attackBonus += data.Attack;
            effectPower += data.EffectPower;
            flatPierce += data.FlatPierce;
        }
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
