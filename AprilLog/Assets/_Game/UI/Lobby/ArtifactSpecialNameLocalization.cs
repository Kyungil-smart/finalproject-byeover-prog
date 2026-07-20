using UnityEngine;

// 아티팩트 특수능력 코드에 대응하는 한/영 표시명을 반환한다.
public static class ArtifactSpecialNameLocalization
{
    public static string Resolve(string code, string fallbackKr = "특수능력", string fallbackEn = "Special Ability")
    {
        bool isKorean = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguage == "ko"
            : Application.systemLanguage == SystemLanguage.Korean;

        return code switch
        {
            "ATKPercent"       => isKorean ? "공격력 증가" : "Attack Power",
            "HPPercent"        => isKorean ? "체력 증가" : "Max HP",
            "CriticalRate"     => isKorean ? "치명타 확률" : "Critical Rate",
            "GoldBonus"        => isKorean ? "골드 획득" : "Gold Gain",
            "PlainDMG"         => isKorean ? "추가 피해" : "Bonus Damage",
            "FireDMG"          => isKorean ? "화염 피해" : "Fire Damage",
            "IceDMG"           => isKorean ? "냉기 피해" : "Ice Damage",
            "LightingDMG"      => isKorean ? "전격 피해" : "Lightning Damage",
            "WindDMG"          => isKorean ? "바람 피해" : "Wind Damage",
            "WaterDMG"         => isKorean ? "물 피해" : "Water Damage",
            "ElementDMG"       => isKorean ? "속성 피해" : "Elemental Damage",
            "WaveHealPencent"  => isKorean ? "웨이브 회복" : "Wave Recovery",
            "AutoDMG"          => isKorean ? "자동 포탑 피해" : "Auto Turret Damage",
            "RecipeDMG"        => isKorean ? "조합 피해" : "Combination Damage",
            "ComboDMG"         => isKorean ? "콤보 피해" : "Combo Damage",
            "Execute"          => isKorean ? "처형" : "Execute",
            "Revive"           => isKorean ? "부활" : "Revive",
            "CastPerKillCount" => isKorean ? "처치 시 시전" : "Cast on Kill",
            "Reroll"           => isKorean ? "리롤" : "Reroll",
            _ => string.IsNullOrEmpty(code) ? (isKorean ? fallbackKr : fallbackEn) : code
        };
    }
}
