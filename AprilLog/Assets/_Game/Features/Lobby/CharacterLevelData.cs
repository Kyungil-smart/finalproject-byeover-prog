using UnityEngine;

public readonly struct CharacterStatData
{
    public readonly int HP;
    public readonly int ATK;
    public readonly int Stern;
    public readonly int Slow;

    public CharacterStatData(int hp, int atk, int stern, int slow)
    {
        HP = hp;
        ATK = atk;
        Stern = stern;
        Slow = slow;
    }
}

public readonly struct CharacterLevelCostData
{
    public readonly int Gold;
    public readonly int Parchment;

    public CharacterLevelCostData(int gold, int parchment)
    {
        Gold = gold;
        Parchment = parchment;
    }
}

/// <summary>
/// 로비에 표시할 캐릭터 레벨별 능력치/레벨업 비용을 계산한다.
///
/// 수치는 전부 데이터(ConfigRepo / CharacterRepo)에서 읽는다.
///   - 기본 스탯(Lv 시작값)      : CharacterRepo.GetCommonStatus(1) / GetCharacterStatus(1)  (v.1.04 테이블)
///   - 레벨당 누적 성장 보너스   : ConfigRepo.GetOutGrowthBonusUntilLevel(level)  (OutLevelTable)
///   - 레벨업 비용               : ConfigRepo.GetOutLevel(level).RequiredGold / RequiredParchment
///
/// 이렇게 하면 로비에 보이는 스탯이 인게임 진입 시 PlayerModel에 실제로 적용되는 값
/// (InGameBootstrap: base + GetOutGrowthBonusUntilLevel)과 정확히 일치한다.
/// DataManager/Repo가 아직 준비되지 않은 테스트 씬에서는 안전한 폴백 공식을 사용한다.
/// </summary>
public static class CharacterLevelData
{
    // v.1.04 CharacterMaster 기준 주인공(Main) ID
    private const int MainCharacterId = 1;

    public static CharacterStatData GetStat(int level)
    {
        level = Mathf.Clamp(level, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel);

        DataManager dataManager = DataManager.Instance;
        if (dataManager != null && dataManager.CharacterRepo != null && dataManager.ConfigRepo != null)
        {
            CommonStatusData common = dataManager.CharacterRepo.GetCommonStatus(MainCharacterId);
            CharacterStatusData characterStatus = dataManager.CharacterRepo.GetCharacterStatus(MainCharacterId);

            if (common != null)
            {
                dataManager.ConfigRepo.GetOutGrowthBonusUntilLevel(level,
                    out int hpBonus, out int attackBonus, out int stunBonus, out int slowBonus);

                int baseStun = characterStatus != null ? characterStatus.StunPower : 0;
                int baseSlow = characterStatus != null ? characterStatus.SlowPower : 0;

                return new CharacterStatData(
                    hp:    common.MaxHP + hpBonus,
                    atk:   common.Attack + attackBonus,
                    stern: baseStun + stunBonus,
                    slow:  baseSlow + slowBonus);
            }
        }

        Debug.LogWarning("[CharacterLevelData] 스탯 데이터를 찾을 수 없어 폴백 공식을 사용합니다. (DataManager/Repo 미초기화)");
        return FallbackStat(level);
    }

    public static CharacterLevelCostData GetLevelUpCost(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel - 1);

        DataManager dataManager = DataManager.Instance;
        if (dataManager != null && dataManager.ConfigRepo != null)
        {
            OutLevelData data = dataManager.ConfigRepo.GetOutLevel(currentLevel);
            if (data != null)
                return new CharacterLevelCostData(data.RequiredGold, data.RequiredParchment);
        }

        Debug.LogWarning("[CharacterLevelData] 레벨업 비용 데이터를 찾을 수 없어 폴백 공식을 사용합니다. (DataManager/ConfigRepo 미초기화)");
        return FallbackCost(currentLevel);
    }

    // ---------- 폴백 (데이터 미준비 시) ----------
    private static CharacterStatData FallbackStat(int level)
    {
        return new CharacterStatData(
            hp:    100 * level,
            atk:    50 * level,
            stern:       level,
            slow:        level);
    }

    private static CharacterLevelCostData FallbackCost(int currentLevel)
    {
        return new CharacterLevelCostData(
            gold:      1000 * currentLevel,
            parchment: 1000 * currentLevel);
    }
}
