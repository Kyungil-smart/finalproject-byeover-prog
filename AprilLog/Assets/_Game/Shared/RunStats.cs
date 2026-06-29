// 담당자 : 정승우
// 설명   : 한 판(챕터) 동안의 통계 누적 — 정산 화면용. 챕터 시작 시 Reset() 호출.

// 수정자 : 김영찬
// 설명 : 수정된 정산창 내용에 맞춰 임시 변수 추가
// 수정자 : 정승우
// 설명 : 스킬(인챈트)별 단일타격 최고뎀을 스킬ID(StandardID) 키로 기록. MonsterAI.TakeDamage(dmg, skillId)에서 누적.
//        정산창 '인챈트별 데미지 최고치'용. 기존 HighestEnchantDamage1~3(미사용 死변수) 제거 → TopSkillsByDamage로 대체.
// 수정자 : 김영찬
// 수정 내용 : 세이브/로드 시 인첸트별 최고치 정보 또한 갱신 하도록 수정

using System.Collections.Generic;

/// <summary>
/// 인게임 한 판의 통계(총 데미지, 스킬별 최고뎀)를 누적한다. 정산에서 읽어 표시.
/// 챕터 시작 시 InGameBootstrap이 Reset()을 호출한다.
/// </summary>
public static class RunStats
{
    public static int TotalDamage { get; private set; }
    public static int HighestDamage { get; private set; }   // 전체 단일타격 최고뎀

    // 스킬(StandardID)별 단일타격 최고뎀. 정산창 '인챈트별 최고치'용.
    private static Dictionary<int, int> _maxBySkill = new ();
    public static Dictionary<int, int> MaxBySkill => _maxBySkill;

    public static void Reset()
    {
        TotalDamage = 0;
        HighestDamage = 0;
        _maxBySkill.Clear();
    }

    public static void RestoreFromSave(int totalDamage, int highestDamage, Dictionary<int, int> maxBySkill)
    {
        TotalDamage = totalDamage;
        HighestDamage = highestDamage;
        _maxBySkill = maxBySkill;
    }

    public static void AddDamage(int amount) => AddDamage(amount, 0);

    /// <summary>데미지 누적 + 스킬별 최고뎀 갱신. skillId=0이면 스킬별 기록은 생략(기본공격 등).</summary>
    public static void AddDamage(int amount, int skillId)
    {
        if (amount <= 0) return;

        TotalDamage += amount;
        if (amount > HighestDamage) HighestDamage = amount;

        if (skillId != 0)
        {
            if (!_maxBySkill.TryGetValue(skillId, out int cur) || amount > cur)
                _maxBySkill[skillId] = amount;
        }
    }

    /// <summary>해당 스킬(StandardID)의 단일타격 최고뎀. 기록 없으면 0.</summary>
    public static int GetMaxDamageForSkill(int skillId) =>
        _maxBySkill.TryGetValue(skillId, out int v) ? v : 0;

    /// <summary>최고뎀 상위 count개를 (skillId, maxDamage) 내림차순으로 반환. 정산 인챈트별 표시용.</summary>
    public static List<KeyValuePair<int, int>> TopSkillsByDamage(int count)
    {
        var list = new List<KeyValuePair<int, int>>(_maxBySkill);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (count >= 0 && list.Count > count)
            list.RemoveRange(count, list.Count - count);
        return list;
    }
}
