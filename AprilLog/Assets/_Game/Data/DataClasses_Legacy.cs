// 담당자 : 정승우
// 설명   : 데이터 테이블 구조체 모음

// 수정자 : 김영찬
// 수정 내용 : 파일 이름 변경
// 참고 사항 : 이 파일은 더 이상 사용하지 않음.
//           이 파일에 연결된 데이터 클래스는 변경 가능 시점에 조속히 신규 데이터 클래스로 이관 할 것.

using System;
using System.Collections.Generic;

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

// 몬스터 풀 정보
[Serializable]
public class MonsterPoolMasterData
{
    public int MonsterPool_ID;      // PK
    public string MonsterPoolType;  // Normal, Agile, Tank, Ranged, Infested, Gimmick, Elite, Boss
}

[Serializable]
public class MapLanguageData
{
    public string Language;     // PK (N_1, D_1 등)
    public string KR;
    public string EN;
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