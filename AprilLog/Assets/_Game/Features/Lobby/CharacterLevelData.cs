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

public static class CharacterLevelData
{
    // 수치 기준
    //   HP     : 레벨당 +100  (Lv1: 100, Lv2: 200, ...)
    //   ATK    : 레벨당 +50   (Lv1: 50,  Lv2: 100, ...)
    //   Stun   : 레벨당 +1
    //   Slow   : 레벨당 +1
    //   골드   : 레벨당 +100  (Lv1→2: 100, Lv2→3: 200, ...)
    //   양피지 : 레벨당 +1

    public static CharacterStatData GetStat(int level)
    {
        level = Mathf.Clamp(level, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel);
        return new CharacterStatData(
            hp:    100 * level,
            atk:    50 * level,
            stern:       level,
            slow:        level);
    }

    public static CharacterLevelCostData GetLevelUpCost(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel - 1);
        return new CharacterLevelCostData(
            gold:      1000 * currentLevel,
            parchment:  1000 * currentLevel);
    }
}
