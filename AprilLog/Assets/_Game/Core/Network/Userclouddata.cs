// 담당자 : 정승우
// 설명   : Firestore에 저장되는 유저 데이터 구조

using System;
using System.Collections.Generic;

[Serializable]
public class UserCloudData
{
    // ---------- 진행 상황 ----------
    public int characterLevel = 1;
    public int currentChapter = 1;
    public int currentStage = 1;
    public List<int> unlockedStages = new List<int>();

    // ---------- 재화 ----------
    public int gold;
    public int parchment;

    // ---------- 아웃게임 성장 ----------
    public int hpBonus;
    public int attackBonus;
    public int shieldBonus;

    // ---------- 업적 ----------
    public List<AchievementSaveEntry> achievements = new List<AchievementSaveEntry>();

    // ---------- 인챈트 도감 ----------
    public List<int> enchantBookOwned = new List<int>();

    // ---------- 설정 ----------
    public string language = "ko";
    public float sfxVolume = 1f;
    public float bgmVolume = 1f;

    // ---------- 메타 ----------
    public string lastLoginAt;
    public string createdAt;

    // 기본값으로 신규 유저 데이터 생성
    public static UserCloudData CreateDefault()
    {
        return new UserCloudData
        {
            characterLevel = 1,
            currentChapter = 1,
            currentStage = 1,
            unlockedStages = new List<int> { 10001 },
            gold = 0,
            parchment = 0,
            language = "ko",
            sfxVolume = 1f,
            bgmVolume = 1f,
            lastLoginAt = DateTime.UtcNow.ToString("o"),
            createdAt = DateTime.UtcNow.ToString("o")
        };
    }
}

[Serializable]
public class AchievementSaveEntry
{
    public int achievementId;
    public bool unlocked;
    public int progress;
}