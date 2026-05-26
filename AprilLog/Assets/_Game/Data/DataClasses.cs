// 담당자 : 정승우
// 설명   : 데이터 테이블 구조체 모음 -- 기획서 v1.03 기준 전면 수정 (2026.05.26)

using System;
using System.Collections.Generic;

/// <summary>
/// 모든 데이터 테이블의 행 구조를 정의한다.
/// 필드명은 기획자 Excel의 행 1(영문 컬럼명)과 정확히 일치해야 함.
/// </summary>

// JSON 파싱용 래퍼
[Serializable]
public class DataArray<T>
{
    public T[] data;
}

// 캐릭터 관련

[Serializable]
public class CharacterMasterData
{
    public int Character_ID;
    public string CharacterType;    // Main, Guide, Monster
    public int CharacterName;       // FK -> CharacterNameData
}

[Serializable]
public class CharacterNameData
{
    public int CharacterName;       // PK
    public string Name_KR;
    public string Name_EN;
}

// 주인공/몬스터 공용 스탯

[Serializable]
public class CommonStatusData
{
    public int Character_ID;
    public int MaxHP;
    public int Attack;
}

// 주인공 전용 스탯

[Serializable]
public class CharacterStatusData
{
    public int Character_ID;
    public float CriticalRate;
    public float PercentagePierce;
    public int StunPower;
    public int SlowPower;
    public int HitCount;
    public int AoE;
    public int MaxTargets;
}

// 몬스터 전용 스탯

[Serializable]
public class MonsterStatusData
{
    public int Character_ID;
    public float BaseAttackSpeed;
    public int Defense;
    public float MoveSpeed;
    public int Range;
    public int EXP;
}

// 스킬 관련

[Serializable]
public class SkillMasterData
{
    public int StandardID;
    public string Name;
    public string AttackType;
    public string HitRange;
    public int StatusEffect;
    public int StatusEffect2;
    public int StatusEffect3;
    public int StatusEffect4;
}

[Serializable]
public class SkillData
{
    public int StandardID;
    public int SkillID;
    public int Level;
    public int Speed;
    public int Dmg;
    public int DetectRange;
    public int HitSize;
    public int PelletCount;
    public int NumberOfCycle;
    public int Interval;
    public float PercentagePierce;
    public float CriticalRate;
    public int Image;
}

[Serializable]
public class EffectData
{
    public int EffectID;
    public string Name;
    public string StatusType;
    public int Value;
    public int Duration;
    public int Interval;
}

// 인챈트 관련

[Serializable]
public class EnchantMasterData
{
    public int EnchantID;
    public string EnchantType;
    public string Name;
    public int MaxLevel;
    public int LinkedSkillID;
    public int LinkedStatType;
    public string DescriptionKey;
    public string ImageKey;
}

[Serializable]
public class EnchantLevelData
{
    public int EnchantID;
    public int Level;
    public float Value;
}

[Serializable]
public class EnchantWeightData
{
    public int OwnedCountMin;
    public int OwnedCountMax;
    public float OwnedWeight;
    public float UnownedWeight;
}

[Serializable]
public class ChapterData
{
    public int Chapter_ID;
    public int ChapterOrder;
    public int UnlockOutLevel;
    public int StageCount;
    public int StoryStart;
    public int StoryEnd;
    public int Name;            // FK -> MapLanguageData
    public int Explanation;     // FK -> MapLanguageData
}

[Serializable]
public class MapLanguageData
{
    public string Language;     // PK (N_1, D_1 등)
    public string KR;
    public string EN;
}

[Serializable]
public class StageData
{
    public int Chapter_ID;      // FK
    public int Stage_ID;        // PK
    public int StageOrder;
    public int TimeLimit;
    public int WaveGroup_ID;    // 현재 미사용, 향후 확장용
}

// 몬스터 풀 마스터 (신규)
[Serializable]
public class MonsterPoolMasterData
{
    public int MonsterPool_ID;      // PK
    public string MonsterPoolType;  // Normal, Agile, Tank, Ranged, Infested, Gimmick, Elite, Boss
}

// 몬스터 풀 구성원 (신규)
[Serializable]
public class MonsterPoolData
{
    public int MonsterPool_ID;  // FK -> MonsterPoolMaster (복합키)
    public int Character_ID;    // FK -> CharacterMaster (복합키)
    public int Weight;          // 가중치
}

// 스테이지 스폰 규칙 (신규 -- 기존 StageMonsterData 대체)
[Serializable]
public class StageSpawnRuleData
{
    public int StartStage_ID;       // FK
    public int EndStage_ID;         // FK
    public int MonsterPool_ID;      // FK -> MonsterPoolMaster
    public int MaxAlive;
    public float SpawnInterval;
    public int SpawnAmount;
    public string GrowthType;       // None, Add, Rate
    public float GrowthValue;
    public string SpawnPositionType; // RandomAll, SP_1 ~ SP_7
}

// 몬스터 스테이지 스케일링 (신규 -- 기존 MonsterScalingData 대체)
[Serializable]
public class MonsterStageScalingData
{
    public int StartStage_ID;
    public int EndStage_ID;
    public int MonsterPool_ID;
    public string MaxHPGrowthType;      // None, Add, Rate
    public float MaxHPGrowthValue;
    public string AttackGrowthType;
    public float AttackGrowthValue;
    public string DefenseGrowthType;
    public float DefenseGrowthValue;
}


// 인게임 레벨
[Serializable]
public class InLevelData
{
    public int InLevel;
    public int RequiredExp;
    public int HPRecovery;
}

// 아웃게임 레벨업 비용 + 스탯 증가량

[Serializable]
public class OutLevelData
{
    public int OutLevel;
    public int ConsumeGold;
    public int ConsumeParchment;
    public int MaxHP;
    public int Attack;
    public int StunPower;
    public int SlowPower;
    public int NewEnchant;
}

// 보상

[Serializable]
public class ChangeRewardData
{
    public string ChangeReward_ID;  // PK (SCR_1 등)
    public string Start_ID;         // C_ 또는 S_ 시작
    public string End_ID;
    public string RewardType;       // Gold, Parchment
    public int BaseAmount;
    public string GrowthType;       // None, Add, Rate
    public float GrowthValue;
}

// 업적
[Serializable]
public class AchievementData
{
    public int AchievementID;
    public string NameKey;
    public string ConditionType;
    public int ConditionValue;
    public int RewardGold;
    public int RewardParchment;
}

// 로컬라이제이션

[Serializable]
public class LanguageEntry
{
    public string Key;
    public string Ko;
    public string En;
}

// View 표시용 구조체

[Serializable]
public class EnchantDisplayData
{
    public int EnchantId;
    public string Name;
    public string Description;
    public int Level;
    public string ImageKey;
}

[Serializable]
public class StageDisplayData
{
    public int StageId;
    public string ChapterName;
    public int StageOrder;
    public bool IsUnlocked;
    public bool IsCleared;
}

// 인게임 세이브

[Serializable]
public class InGameSaveData
{
    public int chapterId;
    public int clearedStage;
    public int playerHP;
    public int currentEXP;
    public int inGameLevel;
    public int[] puzzleSlots;
    public int[] waitingSlots;
    public List<AcquiredEnchant> acquiredEnchants;
    public int totalDamage;
    public int maxCombo;
    public int nextStageSeed;
}

[Serializable]
public class AcquiredEnchant
{
    public int enchantId;
    public int level;
}

// Sort 보조 구조체

[Serializable]
public struct WaitingCombo
{
    public int[] unitTypes;
    public WaitingDifficulty difficulty;

    public int FilledCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < unitTypes.Length; i++)
                if (unitTypes[i] >= 0) c++;
            return c;
        }
    }
}