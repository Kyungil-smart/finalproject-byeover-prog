// 담당자 : 정승우
// 설명   : Firestore에 저장되는 유저 데이터 구조

// 2차 수정자 : 조규민
// 수정 내용 : 하우징 자동재화 마지막 수령 시간 저장 필드 추가,하우징 구매 보유 가구 ID 저장 필드 추가

using System;
using System.Collections.Generic;

[Serializable]
public class UserCloudData
{
    // ---------- 프로필 ----------
    public string uid;
    public string playerId;
    public string email;
    public string displayName;
    public string provider;

    // ---------- 진행 상황 ----------
    public int characterLevel = 1;
    public int currentChapter = 1;
    public int currentStage = 1;
    public List<int> unlockedStages = new List<int>();

    // ---------- 재화 ----------
    public int gold;
    public int parchment;
    public int diamond;

    // ---------- 하우징 ----------
    public string housingAutoCurrencyLastClaimAt;
    // 추가: 조규민 - 계정별 하우징 배치 가구 ID를 저장한다.
    public List<int> housingPlacedFurnitureIds = new List<int>();
    // 추가: 조규민 - 구매 완료된 하우징 가구 ID를 계정 저장 데이터에 보관한다.
    public List<int> housingOwnedFurnitureIds = new List<int>();

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
            diamond = 0,
            housingAutoCurrencyLastClaimAt = DateTime.UtcNow.ToString("o"),
            housingPlacedFurnitureIds = new List<int>(),
            housingOwnedFurnitureIds = new List<int>(),
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
