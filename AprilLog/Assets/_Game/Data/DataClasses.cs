// 담당자 : 정승우
// 설명   : 데이터 테이블 구조체 모음 -- JSON 파싱 + SO 저장 겸용

using System;
using System.Collections.Generic;

/// <summary>
/// 모든 데이터 테이블의 행 구조를 정의한다.
/// [Serializable]이라서 SO 안에 들어가고, JSON 파싱도 되고, Inspector에도 보인다.
/// 필드명은 기획자 Excel의 영문 컬럼명과 정확히 일치해야 함.
/// </summary>

// JSON 파싱용 래퍼 (JsonUtility는 최상위 배열 못 읽어서 이걸로 감쌈)
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
    public string CharacterName;
}

// 주인공/몬스터 공용 스탯 (HP, Shield, Attack)
[Serializable]
public class CommonStatusData
{
    public int Character_ID;
    public int MaxHP;
    public int Shield;
    public int Attack;
}

// 주인공 전용 스탯 (관통, 치명타 등 12개)
[Serializable]
public class CharacterStatusData
{
    public int Character_ID;
    public int FlatPierce;              // 고정 관통력
    public float PercentagePierce;      // % 관통력 (0.0 ~ 1.0)
    public float CriticalRate;          // 치명타 확률 (0.0 ~ 1.0)
    public float CriticalDamageBonus;   // 치명타 추가 데미지 배율
    public int CCPower;                 // CC기 위력
    public int HitCount;                // 타격 횟수
    public int AoE;                     // 범위 공격 크기
    public int MaxTargets;              // 최대 타겟 수
    public float BaseAttackSpeed;       // 기본 공격 속도 (0.001 ~ 1.0)
    public float AttackSpeedBonus;      // 공격 속도 보너스
    public int DetectRange;             // 탐지 범위
}

// 몬스터 전용 스탯
[Serializable]
public class MonsterStatusData
{
    public int Character_ID;
    public float MoveSpeed;
    public int AttackRange;             // 사거리
    public string MovementPattern;      // Straight, Zigzag
    public float ZigzagAmplitude;       // 좌우반복 거리 (Zigzag일 때만)
}

// 스킬 관련

// 스킬 마스터 -- 스킬의 기본 정보
[Serializable]
public class SkillMasterData
{
    public int StandardID;
    public string Name;
    public string AttackType;       // Sort, Combi, Combo, Auto
    public string HitRange;         // Single, Circle, Fan 등
    public int StatusEffect;        // EffectTable FK (0이면 없음)
    public int StatusEffect2;
    public int StatusEffect3;
    public int StatusEffect4;
}

// 스킬 레벨별 수치
[Serializable]
public class SkillData
{
    public int StandardID;          // SkillMaster FK
    public int SkillID;             // StandardID * 100 + Level
    public int Level;
    public int Speed;               // 투사체 속도
    public int Dmg;                 // 기본 데미지
    public int DetectRange;
    public int HitSize;             // 피격 범위 (0~1)
    public int PelletCount;         // 발사체 수
    public int NumberOfCycle;       // 반복 횟수
    public int Interval;            // 반복 간격
    public float PercentagePierce;
    public float CriticalRate;
    public int Image;               // 스프라이트 ID
}

// 스킬 이펙트 (넉백, 도트뎀 등)
[Serializable]
public class EffectData
{
    public int EffectID;
    public string Name;
    public string StatusType;       // Knockback, DotDamage, Bounce, Penetration, Targeting
    public int Value;               // 효과 수치
    public int Duration;            // 지속 시간 (프레임 or 밀리초)
    public int Interval;            // 도트뎀 간격
}

// 인챈트 관련

// 인챈트 마스터 -- 인챈트 기본 정보
[Serializable]
public class EnchantMasterData
{
    public int EnchantID;
    public string EnchantType;      // SkillNormal, SkillCombi, SkillCombo, Stat
    public string Name;
    public int MaxLevel;
    public int LinkedSkillID;       // 스킬 인챈트일 때 SkillMaster FK
    public int LinkedStatType;      // 스탯 인챈트일 때 어떤 스탯인지
    public string DescriptionKey;   // 로컬라이제이션 키
    public string ImageKey;
}

// 인챈트 레벨별 수치
[Serializable]
public class EnchantLevelData
{
    public int EnchantID;
    public int Level;
    public float Value;             // 레벨별 계수
}

// 인챈트 등장 확률 가중치 (보유 개수에 따라 달라짐)
[Serializable]
public class EnchantWeightData
{
    public int OwnedCountMin;
    public int OwnedCountMax;
    public float OwnedWeight;       // 보유 인챈트군 확률
    public float UnownedWeight;     // 미보유 인챈트군 확률
}

// 스테이지 관련

[Serializable]
public class ChapterData
{
    public int ChapterID;
    public string ChapterName;
    public int StageCount;
    public int ClearRewardGold;
    public int ClearRewardParchment;
}

[Serializable]
public class StageData
{
    public int StageID;
    public int ChapterID;
    public int StageOrder;
    public int WaveCount;
    public float TimeLimit;
    public int FirstClearBonusGold;
}

// 스테이지별 몬스터 배치 (어떤 몬스터가 어느 웨이브에서 어디서 나오는지)
[Serializable]
public class StageMonsterData
{
    public int StageID;
    public int WaveIndex;
    public int MonsterID;           // CharacterMaster FK
    public int SpawnPoint;          // 1~7
    public float SpawnDelay;        // 이전 몬스터 대비 딜레이(초)
    public int Count;               // 동시 스폰 수
}

// 스테이지별 몬스터 스탯 스케일링
[Serializable]
public class MonsterScalingData
{
    public int StageID;
    public float HPMultiplier;
    public float AttackMultiplier;
    public float SpeedMultiplier;
}

// 밸런스 / 성장

// 인게임 레벨별 필요 EXP
[Serializable]
public class InLevelData
{
    public int Level;
    public int RequiredEXP;
    public int HPRecovery;          // 레벨업 시 HP 회복량
}

// 아웃게임 캐릭터 레벨업 비용
[Serializable]
public class OutGrowthData
{
    public int CharacterLevel;
    public int RequiredGold;
    public int RequiredParchment;
    public int HPIncrease;
    // 추가 : 홍정옥
    // 내용 : 아웃게임 성장 JSON/SO에서 읽어온 Shield 증가량을 보관하기 위한 필드
    public int ShieldIncrease;
    public int AttackIncrease;
}

// 업적
[Serializable]
public class AchievementData
{
    public int AchievementID;
    public string NameKey;          // 로컬라이제이션 키
    public string ConditionType;
    public int ConditionValue;
    public int RewardGold;
    public int RewardParchment;
}

// 변동 보상
[Serializable]
public class ChangeRewardData
{
    public int RewardID;
    public int BaseGold;
    public float GoldMultiplier;    // 스테이지별 자동 증가율
    public int BaseParchment;
}

// 로컬라이제이션

[Serializable]
public class LanguageEntry
{
    public string Key;
    public string Ko;
    public string En;
}

// View에 전달하는 표시용 구조체

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

// 인게임 세이브 (로컬 JSON)

[Serializable]
public class InGameSaveData
{
    public int chapterId;
    public int clearedStage;

    public int playerHP;
    public int playerShield;
    public int currentEXP;
    public int inGameLevel;

    // Sort 슬롯 상태 (9테이블 * 3슬롯 = 27칸, -1이면 빈칸)
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
    public int[] unitTypes;         // 길이 3, -1이면 빈칸. UnitType 캐스팅해서 쓰면 됨
    public WaitingDifficulty difficulty;

    public int FilledCount
    {
        get
        {
            int c = 0;
            for (int i = 0; i < unitTypes.Length; i++)
            {
                if (unitTypes[i] >= 0) c++;
            }
            return c;
        }
    }
}
