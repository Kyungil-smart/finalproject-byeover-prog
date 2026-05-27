// 담당자 : 정승우
// 설명   : 밸런스/성장/업적/보상 데이터 저장소

// 1차 수정자 : 홍정옥
// 수정내용 : GetOutGrowthBonusUntilLevel 메서드 추가 (아웃게임 성장 누적 보너스 계산)

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 레벨 곡선, 아웃게임 성장, 업적, 보상 등 설정 데이터를 관리한다.
/// </summary>
public class ConfigRepo : MonoBehaviour
{
    [Header("SO 참조")]
    [SerializeField] private InLevelTable _inLevelTable;
    [SerializeField] private OutLevelTable _outLevelTable;
    [SerializeField] private AchievementDataTable _achievementTable;
    [SerializeField] private ChangeRewardTable _changeRewardTable;

    private Dictionary<int, InLevelData> _inLevel;
    private Dictionary<int, OutLevelData> _outLevel;
    private Dictionary<int, AchievementData> _achievements;

    public void Initialize()
    {
        _inLevel = _inLevelTable.rows.ToDictionary(r => r.InLevel);
        _outLevel = _outLevelTable.rows.ToDictionary(r => r.OutLevel);
        _achievements = _achievementTable.rows.ToDictionary(r => r.AchievementID);

        Debug.Log($"[ConfigRepo] 초기화 완료. 인레벨 {_inLevel.Count}, 아웃레벨 {_outLevel.Count}");
    }

    public InLevelData GetInLevel(int level) => _inLevel.TryGetValue(level, out var d) ? d : null;
    public OutLevelData GetOutLevel(int level) => _outLevel.TryGetValue(level, out var d) ? d : null;
    public AchievementData GetAchievement(int id) => _achievements[id];
    public IReadOnlyDictionary<int, AchievementData> GetAllAchievements() => _achievements;
    public IReadOnlyList<ChangeRewardData> GetAllChangeRewards() => _changeRewardTable.rows;

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
}