using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum SortType
{
    Default,
    GradeHigh,
    GradeLow,
    Name,
    Attack,
    HP
}

public static class ArtifactSorter
{
    public static List<ArtifactInstance> Sort(List<ArtifactInstance> list, SortType type)
    {
        switch (type)
        {
            case SortType.GradeHigh:
                return list.OrderByDescending(a => GetGradePriority(a.MasterData.GearGrade)).ToList();

            case SortType.GradeLow:
                return list.OrderBy(a => GetGradePriority(a.MasterData.GearGrade)).ToList();

            case SortType.Name:
                return list.OrderBy(a => a.MasterData.GearName).ToList();

            case SortType.Attack:
                return list.OrderByDescending(a => a.MasterData.AttackBaseAmount).ToList();

            case SortType.HP:
                return list.OrderByDescending(a => a.MasterData.MaxHPBaseAmount).ToList();

            default:
                return list;
        }
        
    }

    private static int GetGradePriority(string grade)
    {
        if (grade == "Legendary") return 3;
        if (grade == "Epic") return 2;
        return 1;
    }
}
