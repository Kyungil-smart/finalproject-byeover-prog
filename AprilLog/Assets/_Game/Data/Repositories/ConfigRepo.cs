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

        Debug.Log($"[ConfigRepo] 초기화 완료. 레벨 {_inLevel.Count}, 업적 {_achievements.Count}");
    }

    public InLevelData GetInLevel(int level) => _inLevel.TryGetValue(level, out var d) ? d : null;
    public OutLevelData GetOutLevel(int level) => _outLevel.TryGetValue(level, out var d) ? d : null;
    public AchievementData GetAchievement(int id) => _achievements[id];
    public IReadOnlyDictionary<int, AchievementData> GetAllAchievements() => _achievements;
    public IReadOnlyList<ChangeRewardData> GetAllChangeRewards() => _changeRewardTable.rows;

    // 추가 : 홍정옥
    // 내용 : 확정된 OutLevel 데이터 규칙에 따라 현재 캐릭터 레벨까지의 MaxHP, Attack 성장 보너스를 누적 조회
    public void GetOutLevelBonusUntilLevel(int characterLevel, out int maxHPBonus, out int attackBonus)
    {
        maxHPBonus = 0;
        attackBonus = 0;

        for (int level = 1; level < characterLevel; level++)
        {
            var data = GetOutLevel(level);
            if (data == null) continue;

            maxHPBonus += data.MaxHP;
            attackBonus += data.Attack;
        }
    }
}
