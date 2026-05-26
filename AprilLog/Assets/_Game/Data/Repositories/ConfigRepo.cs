// 담당자 : 정승우
// 설명   : 밸런스/성장/업적/보상 데이터 저장소

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
    [SerializeField] private OutGrowthDataTable _outGrowthTable;
    [SerializeField] private AchievementDataTable _achievementTable;
    [SerializeField] private ChangeRewardTable _changeRewardTable;

    private Dictionary<int, InLevelData> _inLevel;
    private Dictionary<int, OutGrowthData> _outGrowth;
    private Dictionary<int, AchievementData> _achievements;
    private Dictionary<int, ChangeRewardData> _rewards;

    public void Initialize()
    {
        _inLevel = _inLevelTable.rows.ToDictionary(r => r.Level);
        _outGrowth = _outGrowthTable.rows.ToDictionary(r => r.CharacterLevel);
        _achievements = _achievementTable.rows.ToDictionary(r => r.AchievementID);
        _rewards = _changeRewardTable.rows.ToDictionary(r => r.RewardID);

        Debug.Log($"[ConfigRepo] 초기화 완료. 레벨 {_inLevel.Count}, 업적 {_achievements.Count}");
    }

    public InLevelData GetInLevel(int level) => _inLevel.TryGetValue(level, out var d) ? d : null;
    public OutGrowthData GetOutGrowth(int charLevel) => _outGrowth.TryGetValue(charLevel, out var d) ? d : null;
    public AchievementData GetAchievement(int id) => _achievements[id];
    public IReadOnlyDictionary<int, AchievementData> GetAllAchievements() => _achievements;

    // 추가 : 홍정옥
    // 내용 : 확정된 공식 없이 OutGrowth JSON/SO 행을 누적해 현재 캐릭터 레벨의 성장 보너스를 계산
    public void GetOutGrowthBonusUntilLevel(int characterLevel, out int hpBonus, out int shieldBonus, out int attackBonus)
    {
        hpBonus = 0;
        shieldBonus = 0;
        attackBonus = 0;

        for (int level = 1; level < characterLevel; level++)
        {
            var data = GetOutGrowth(level);
            if (data == null) continue;

            hpBonus += data.HPIncrease;
            shieldBonus += data.ShieldIncrease;
            attackBonus += data.AttackIncrease;
        }
    }
}
