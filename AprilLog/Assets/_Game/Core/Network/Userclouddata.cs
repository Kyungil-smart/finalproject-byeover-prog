// 담당자 : 정승우
// 설명   : Firestore에 저장되는 유저 데이터 구조

// 2차 수정자 : 조규민
// 수정 내용 : 하우징 자동재화 마지막 수령 시간 저장 필드 추가,하우징 구매 보유 가구 ID 저장 필드 추가

using System;
using System.Collections.Generic;

// 2차 수정자 : 조규민
// 수정 내용 : 하우징 자동재화 마지막 수령 시간 저장 필드 추가

// 3차 수정자 : 김영찬
// 수정 내용 : 재화 및 스태미너와 아티펙트 관련 데이터 저장 필드 추가
//
// 4차 수정자 : 조규민
// 수정 내용 : 계정별 최초 스토리 시작과 튜토리얼 완료 상태 및 기존 계정 마이그레이션 상태 추가

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
    public List<int> unlockedStages = new ();
    // 최초 클리어 보상을 이미 지급한 스테이지의 실제 Stage_ID 집합(1회성 보상 중복지급 방지 + 최초클리어 판정).
    // 키는 데이터의 실제 Stage_ID(10101, 980101 등)를 쓸 것 — StageData.Stage_ID / StageRepo.GetStageId 값 그대로.
    public List<int> firstClearRewardedStages = new ();
    public List<int> firstClearRewardedChapters = new ();
    public List<int> firstReadScenarios = new();

    // ---------- 최초 진입 상태 (조규민 4차: 최초 스토리/튜토리얼 노출 제어 + 기존 계정 마이그레이션) ----------
    // 참조 코드(GameManager/FirestoreService)엔 있는데 본문에서 빠져 컴파일 에러였음 → 복구.
    public bool _hasInitialFlowState;   // 이 스키마 필드를 가진 계정인지. false=업데이트 이전 계정 → 1회 마이그레이션 대상.
    public bool _initialStoryStarted;   // 최초 스토리를 이미 시작했는지(재노출 방지).
    public bool _tutorialCompleted;     // 튜토리얼 완료 여부.

    // ---------- 재화 및 스태미너 ----------
    public int gold;
    public int parchment;
    public int diamond;
    public List<ItemSaveEntry> inventory = new ();
    public List<StaminaSaveEntry> staminaData = new ();
    
    // ---------- 아티펙트 ----------
    public List<ArtifactInstance> myArtifacts = new ();
    
    // ---------- 하우징 ----------
    public string housingAutoCurrencyLastClaimAt;
    // 추가: 조규민 - 계정별 하우징 배치 가구 ID를 저장한다.
    public List<int> housingPlacedFurnitureIds = new List<int>();
    // 추가: 조규민 - 구매 완료된 하우징 가구 ID를 계정 저장 데이터에 보관한다.
    public List<int> housingOwnedFurnitureIds = new List<int>();

    // ---------- 아웃게임 성장 (김영찬 3차) ----------
    // 참조 코드(FirestoreService 직렬화/복원)엔 있는데 본문에서 빠져 컴파일 에러였음 → 복구.
    public int hpBonus;
    public int attackBonus;
    public int shieldBonus;

    // ---------- 업적 ----------
    public List<AchievementSaveEntry> achievements = new ();

    // ---------- 인챈트 도감 ----------
    public List<int> enchantBookOwned = new ();

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
            // 진행 상황
            characterLevel = 1,
            currentChapter = 1,
            currentStage = 1,
            unlockedStages = new List<int> { 10101 },   // 첫 챕터(101) 1스테이지의 실 Stage_ID
            firstClearRewardedStages = new (),
            firstClearRewardedChapters = new (),
            firstReadScenarios = new (),
            
            // 신규 계정은 스키마 보유 상태 → 기존계정 마이그레이션(최초콘텐츠 스킵) 대상 아님. story/tutorial는 false로 시작해 신규 유저가 최초 스토리·튜토리얼을 보게 한다.
            _hasInitialFlowState = true,
            _initialStoryStarted = false,
            _tutorialCompleted = false,
            
            // 재화 및 스태미너
            gold = 0,
            parchment = 0,
            diamond = 0,
            inventory = new List<ItemSaveEntry>(),
            staminaData = new List<StaminaSaveEntry>(),
            
            // 아티펙트
            myArtifacts = new List<ArtifactInstance>(),
            
            // 하우징
            housingAutoCurrencyLastClaimAt = DateTime.UtcNow.ToString("o"),
            housingPlacedFurnitureIds = new List<int>(),
            housingOwnedFurnitureIds = new List<int>(),
            
            // 설정
            language = "ko",
            sfxVolume = 1f,
            bgmVolume = 1f,
            
            // 메타
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

[Serializable]
public class ItemSaveEntry
{
    public int itemId;
    public int amount;
}

[Serializable]
public class StaminaSaveEntry
{
    public int staminaId;
    public int currentAmount;
    public string lastUpdateTime; // 오프라인 회복 계산을 위한 마지막 접속(저장) 시간
}
