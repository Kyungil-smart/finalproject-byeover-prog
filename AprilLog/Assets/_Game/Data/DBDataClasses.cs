// 담당자 : 김영찬
// 설명   : DB에 사용하는 클래스 및 구조체 모음 (신규)
// 주의 사항 : 알파벳 순으로 정렬 할 것

using System;

#region JSON 파싱용 래퍼

/// <summary>
/// 모든 데이터 테이블의 행 구조를 정의한다.
/// 필드명은 기획자 Excel의 행 1(영문 컬럼명)과 정확히 일치해야 함.
/// </summary>
[Serializable]
public class DataArray<T>
{
    public T[] data;
}

#endregion

#region ----- B -----

/// <summary>
/// 전투 중 특정 행위에 대한 보상<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class BattleRewardData
{
    public int Target_ID;
    public string RewardTrigger; // WaveClear, EliteKill, BossKill
    public string RewardScope; // InGame, OutGame
    public int FirstRewardItem;
    public int FirstRewardAmount;
    public int SecondRewardItem;
    public int SecondRewardAmount;
}

#endregion

#region ----- C -----

/// <summary>
/// 변동 보상 ID와 설정 매칭<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 26.06.11
/// </summary>
[Serializable]
public class ChangeRewardData
{
    public string RewardRepeat; // FirstClear, RepeatClear
    public int ChangeReward_ID;
    public int Start_ID;
    public int End_ID;
    public string RewardType;       // Gold, Parchment, Diamond
    public int BaseAmount;
    public string GrowthType;       // None, Add, Rate
    public float GrowthValue;
}

/// <summary>
/// 쳅터 ID와 설정 매칭<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
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

/// <summary>
/// 캐릭터 ID와 타입/이름 매칭<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class CharacterMasterData
{
    public int Character_ID;
    public string CharacterType;    // Main, Guide, Monster
    public int CharacterName;       // FK -> CharacterNameData
}

/// <summary>
/// 주인공 전용 스텟<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class CharacterStatusData
{
    public int Character_ID;
    public float CriticalRate;
    public int CriticalDamage;
    public int FlatPierce;
    public float PercentagePierce;
    public int EffectPower;
    public int HitCount;
    public int AoE;
    public int MaxTargets;
}

/// <summary>
/// 주인공/몬스터 공용 스텟<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class CommonStatusData
{
    public int Character_ID;
    public int MaxHP;
    public int Attack;
    public float BaseAttackSpeed;
}

#endregion

#region ----- E -----

/// <summary>
/// 스킬의 부가 효과 일람<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class EffectTableData
{
    public int Effect_ID;
    public int EffectName;
    public string EffectType; // DotDamage, Stun, KnockBack, Heal, Slow
    public string OverLap; // 중첩 여부 : Refresh, Ignore, Stack
    public string GrowthType; // EffectType의 계산 방식 : Add, Rate
    public float Value;
    public float Duration;
    public float Interval;
}

#endregion

#region ----- F -----

/// <summary>
/// 무료 가챠 상자 상세<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class FreeGachaBoxData
{
    public int Gacha_ID;
    public string FreeDrawType; // Cooldown, AdDaily
    public int Count;
    public int FreeCooldownHour;
    public int DailyLimit;
    public bool AdRequired;
}

#endregion

#region ----- G -----

/// <summary>
/// 아티펙트 가챠 확률<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.16
/// </summary>
[Serializable]
public class GachaBoxData
{
    public int Gacha_ID;
    public int CostAmount;
    public float RareRate;
    public float EpicRate;
    public float LegendaryRate;
    public string PityType; // None, RandomLegendary
    public int PityCount;
    public int Name;
    public int Explanation;
}

/// <summary>
/// 아티펙트 가챠 누적 보상<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GachaRewardData
{
    public int Gacha_ID;
    public int MileageCount;
    public int FirstRewardItem;
    public int FirstRewardAmount;
    public int SecondRewardItem;
    public int SecondRewardAmount;
    public bool Reset;
}

/// <summary>
/// 아티팩트 능력 및 효과 연결을 위한 연결 데이터<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.15
/// </summary>
[Serializable]
public class GearMasterData
{
    public int Gear_ID;
    public int Special_ID; // 특수 능력치
    public int OwnedSpecial_ID; // 보유 특수 능력치
    public string GearGrade; // Rare, Epic, Legendary
    public string IconSprite;
    public int MaxHPBaseAmount;
    public int AttackBaseAmount;
    public int GearName; // 기어이름 : 번역 연결용
    public int Explanation; // 기어 설명 : 번역 연결용
}

/// <summary>
/// 기어 등급에 따른 공통 데이터<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.16
/// </summary>
[Serializable]
public class GearGradeData
{
    public string GearGrade;
    public int MaxLevel;
    public int MaxAscension; // 최대 돌파
    public int MaxOwned;
    public int UpgradeStone; // 분해 시 나오는 강화석 개수
    public int BaseLevel; // 기본 레벨 상한
    public int LevelCapIncrease; // 돌파 당 레벨 상한 증가량
}

/// <summary>
/// 각 기어 별 레벨에 따른 능력치 증가 데이터<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearLevelData
{
    public int Gear_ID;
    public int StartLevel; // 최초 획득 시 시작 레벨
    public int EndLevel; // 모든 돌파 적용 후 최종 레벨
    public int MaxHPValue; // 레벨업 당 Hp 증가량 : 최종 MaxHP = MaxHPBaseAmount + (MaxHPBaseAmount × MaxHPValue) × (현재 레벨 - 1)
    public int AttackValue; // 레벨업 당 Attack 증가량 : 최종 Attack = AttackBaseAmount + (AttackBaseAmount × AttackValue) × (현재 레벨 - 1)
}

/// <summary>
/// 기어 등급 별 돌파에 필요한 재화 데이터<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearAscensionCostData
{
    public string GearGrade;
    public string MaterialType;
    public int CostItem;
    public int CostAmount;
}

/// <summary>
/// 각 기어 별 레벨업에 필요한 재화 데이터<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.16
/// </summary>
[Serializable]
public class GearUpgradeCostData
{
    public int Gear_ID;
    public int StartLevel; // 최초 획득 시 시작 레벨
    public int EndLevel; // 모든 돌파 적용 후 최종 레벨
    public int Type; // 업그레이드에 들어가는 재화 종류
    public int BaseAmount; // 업그레이드에 들어가는 재화의 기본 수치
    public float GrowthValue; // 업그레이드에 들어가는 재화의 성장 보정치 : 최종 필요 재화량 = BaseAmount × GrowthValue ^ (현재 레벨 - StartLevel), 소수점이 발생할 경우, 소수점 이하 올림.
}

/// <summary>
/// 기어에 적용되는 특수 효과 정의 데이터<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearSpecialEffectData
{
    public int Special_ID;
    public string Special; // 특수능력치 종류 일람, 자세한 내용은 확인 할 것
    public string EffectType; // 발동 타입 : Equipped - 장착 시 발동, Owned - 보유 시 효과
    public int Start; // 최초 획득 시 특수 효과 첫 레벨 - 레벨 0일때 처리 어떻게 하는지 확인 할 것
    public int End; // 특수 효과 최대 레벨, 레벨 증가 조건 확인 할 것.
    public float BaseAmount; // 해당 특수 능력치의 기본 값, 65001(ATK), 65002(HP)는 %가 아닌 정수
    public string GrowthType; // None, Add, Rate > 없음, 합연산, 곱연산
    public float Value; // 1레벨 당 성장치, 65001(ATK), 65002(HP)는 %가 아닌 정수
    public string TriggerType; // 특수 능력치의 발동 조건
    public string LimitType; // 특수 능력치의 발동 조건의 제한(언제 발동되는지), 확인 필요
}

/// <summary>
/// 각 기어 별 분해 시 지급하는 재화 데이터<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearDismantleData
{
    public string GearGrade; // Rare, Epic, Legendary
    public int RewardItem;
    public int RewardAmount;
}

#endregion

#region ----- I -----

/// <summary>
/// 인게임 레벨<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class InLevelData
{
    public int InLevel;
    public int RequiredEXP;
    public float HPRecovery;
}

[Serializable]
public class ItemData
{
    public int Item_ID;
    public string ItemType; // Currency, GachaTicket, InGameItem, Material, RandomGearReward
    public int Name;
    public int Explanation;
}

#endregion

#region ----- M -----

/// <summary>
/// 몬스터 풀 ID와 몬스터 캐릭터 간 연결과 가중치 설정<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class MonsterPoolData
{
    public int MonsterPool_ID;  // FK -> MonsterPoolMaster (복합키)
    public int Character_ID;    // FK -> CharacterMaster (복합키)
    public int Weight;          // 가중치
}

/// <summary>
/// 스테이지 별 몬스터 퓰의 보정치<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
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

/// <summary>
/// 몬스터 전용 스텟<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class MonsterStatusData
{
    public int Character_ID;
    public int Defense;
    public float MoveSpeed;
    public int Range;
    public int EXP;
    public string MovementPattern;  // Straight, Zigzag
    public int ZigzagAmplitude;
    public string PrefabKey;        // 오브젝트 풀 키. 비어 있으면 "Monster_{Character_ID}" 관례로 폴백
}

/// <summary>
/// 몬스터 풀 ID와 몬스터 풀의 타입 연결(구 MonsterPoolMasterData)<br/>
/// 생성일 : 26.05.29<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class MonsterWavePoolData
{
    public int MonsterWavePool_ID;
    public string WavePoolType;  // Normal, Agile, Tank, Ranged, Infested, Gimmick, Elite, Boss
    public int MonsterPool_ID;      // PK
}


#endregion

#region ----- L -----

/// <summary>
/// 레전더리 조각 교환 비율<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class LegendaryShardExchangeData
{
    public int Exchange_ID;
    public int CostItem;
    public int CostAmount;
    public string MaterialType; // 교환 결과 - SelectGear, Item
    public int RewardItem;
    public int RewardAmount;
    public int RewardGrade; // 교환 아이템 등급 - Legendary, None
    public int RequirementType; // 교환 조건 - UnownedOnly, None
}

#endregion

#region ----- O -----

/// <summary>
/// 아웃게임 레벨<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class OutLevelData
{
    public int OutLevel;
    public int RequiredGold;
    public int RequiredParchment;
    public int MaxHP;
    public int Attack;
    public int EffectPower;
    public int FlatPierce;
    public int NewEnchant;
}


#endregion

#region ----- P -----

/// <summary>
/// 유료 가챠 상자 상세<br/>
/// 생성일 : 26.06.16<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class PaidGachaBoxData
{
    public int Gacha_ID;
    public int Count;
    public int CostType;
    public int CostAmount;
}

#endregion

#region ----- S -----

/// <summary>
/// 스킬 인첸트 정보 일람<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class SkillTableData
{
    public int SkillGroup_ID;
    public int Skill_ID;
    public int Name;
    public int Skill_Descrip;
    public string Hit_Scope; // hazard, projectile, cone, movinghazard
    public int Level;
    public int Dmg;
    public float CriticalRate; // 추가 치명타 확률
    public float HitSize_X; // 범위 - 사각형
    public float HitSize_Y;
    public float Hit_Duration;
    public float Speed;
    public string PelletType; // 투사체 효과 : none, piercing, bounce
    public float Count; // piercing / 튕김 횟수 / 장판의 interval(float값으로 변경)
    public int Effect_ID_1; // 효과
    public string E_ValueType_1; // 증감 항목
    public float E_Variation_1; // 증감율
    public int Effect_ID_2; // 효과
    public string E_ValueType_2; // 증감 항목
    public float E_Variation_2; // 증감율
    public int ActiveCount; // 기본 발동 횟수
    public int ActivePlusCount; // 시전 횟수
    public float PelletGap; // projectile 간격
    public float SubPelletDmg; // 보조 projectile 대미지 증감치
    public float RequiredValue_1; // 메인 조건
    public float RequiredValue_2; // 서브 조건
    public float RequiredValue_3; // 서브 조건
    public string RequiredValue_4; // 스킬의 타게팅 및 스폰 방법 구분 : normal, spawn, boss, random
    public int Tag_ID_1; // 스킬에 부착되는 테그 - UI표시 및 테그 증뎀을 위함
    public int Tag_ID_2;
    public int Tag_ID_3;
    public int Tag_ID_4;
    public int SkillIEffect_Id; // 이펙트 이미지
    public int SkillIcon_ID; // 아이콘 이미지
    public int Sfx_ID; // 사운드
}

/// <summary>
/// 웨이브 진행 중 특수 웨이브 삽입 데이터<br/>
/// 생성일 : 26.05.29<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class SpecialWaveRuleData
{
    public int SpecialWave_ID;
    public int MonsterWavePool_ID;
    public string WaveType; // Rush, Gimmick, Elite, Boss
    public int TriggerTime;
    public string EndType;  // Duration, WaveEnd, Instant
    public int ActiveDuration;
    public float SpecialSpawnInterval;
    public int SpecialSpawnAmount;
}

/// <summary>
/// 스테이지 구성 데이터<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class StageData
{
    public int Chapter_ID;      // FK
    public int Stage_ID;        // PK
    public int StageOrder;
}

/// <summary>
/// 스테이지 별 웨이브 구성 데이터(구 StageSpawnRuleData)<br/>
/// 생성일 : 26.05.29<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class StageWaveRuleData
{
    public int Stage_ID;
    public int WaveOrder;
    public int WaveDuration;
    public string WaveEndType;  // TimeOver, TimeOverOrBossKill
    public string WaveEndAction;    // KeepAlive, DespawnRemaining
    public float SpawnInterval;
    public int SpawnAmount;
    public int MonsterWavePool_ID;
    public float NormalChance;  // 티입별 소환 확률 : 전체 확률의 백분율
    public float AgileChance;
    public float TankChance;
    public float RangedChance;
    public float InfestedChance;
    public int SpecialWave_ID;  // 0인 경우 발동하지않음
}

/// <summary>
/// 행동력 보유량, 회복시간 등을 정의<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class StaminaData
{
    public int Stamina_ID;
    public int MaxOwned; // 자동 회복으로 회복 가능한 상한선
    public int RecoveryTime; // 단위 : sec
    public int RecoveryCount;
    public int InitialAmount; // 최초 계정 생성 시 적용 값
    public int OverCapMax; // 초과 보유 상한
}

/// <summary>
/// 스텟 인첸트 정보 일람<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 26.06.12
/// </summary>
[Serializable]
public class StatTableData
{
    public int StatGroup_ID;
    public int StatEnchant_ID;
    public int Stat_Name;
    public int Stat_Descrip;
    public int Target_1; // 대상 효과 ID
    public int Target_2; // 스킬 조건
    public int StatLevel; // 
    public string ValueType_1;
    public int Variation_1;
    public string ValueType_2;
    public float Variation_2;
    public string ValueType_3;
    public float Variation_3;
    public int Image_ID;
}

#endregion

#region ----- U -----

/// <summary>
/// 유닛(블록)의 ID와 타입을 정의<br/>
/// 생성일 : 26.06.11<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class UnitTableData
{
    public int UnitID;
    public string Name;
    public string Type; // normal, special - 테이블 내 동일한 유닛 두개와 같이 있으면 동일 유닛 취급
}

#endregion

