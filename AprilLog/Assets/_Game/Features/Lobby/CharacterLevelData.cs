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
    public static CharacterStatData GetStat(int level)
    {
        level = Mathf.Clamp(level, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel);

        return new CharacterStatData(
            100 + (level - 1) * 10,
            20 + (level - 1) * 2,
            5 + (level - 1),
            5 + (level - 1));
    }

    public static CharacterLevelCostData GetLevelUpCost(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, PlayerProgressModel.StartLevel, PlayerProgressModel.MaxLevel);

        return new CharacterLevelCostData(
            100 + (currentLevel - 1) * 50,
            1 + (currentLevel - 1));
    }
}
