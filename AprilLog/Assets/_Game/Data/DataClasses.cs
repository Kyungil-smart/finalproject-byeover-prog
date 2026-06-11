// 담당자 : 김영찬
// 설명   : 데이터 테이블 구조체 모음 (신규)
// 주의 사항 : 알파벳 순으로 정렬 할 것

using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

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

#region ----- C -----

/// <summary>
/// 변동 보상 ID와 설정 매칭<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 26.05.29
/// </summary>
[Serializable]
public class ChangeRewardData
{
    public int ChangeReward_ID;
    public int Start_ID;
    public int End_ID;
    public string RewardType;       // Gold, Parchment
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
/// 최종 수정일 : 
/// </summary>
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

#region G

/// <summary>
/// 아티펙트 가챠 확률
/// 생성일 : 26.06.11
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GachaBoxData
{
    public int Gacha_ID;
    public string GachaName; // RareBox, EpicBox, LegendaryBox
    public string CostType; // Gold, Diamond
    public int CostAmount;
    public float RareRate;
    public float EpicRate;
    public float LegendaryRate;
    public string FreeDrawType; // Cooldown, AdDaily, None
    public int FreeCooldownHour;
    public bool AdRequired;
    public string PityType; // None, SelectPity
    public int PityCount;
}

/// <summary>
/// 아티팩트 능력 및 효과 연결을 위한 연결 데이터
/// 생성일 : 26.06.11
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearMasterData
{
    public int Gear_ID;
    public int Special_ID; // 특수 능력치
    public int OwnedSpecial_ID; // 보유 특수 능력치
    public string GearGrade; // Rare, Epic, Legendary
    public int MaxHPBaseAmount;
    public int AttackBaseAmount;
    public int GearName; // 기어이름 : 번역 연결용
    public int Explanation; // 기어 설명 : 번역 연결용
}

/// <summary>
/// 기어 등급에 따른 공통 데이터
/// 생성일 : 26.06.11
/// 최종 수정일 : 
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
    public string AscensionType; // 돌파 재료 타입 : SameGear, None -> 추후 변동 가능성 있음
    public int AscensionAmount; // 돌파에 필요한 재료 수
}

/// <summary>
/// 각 기어 별 레벨에 따른 능력치 증가 데이터
/// 생성일 : 26.06.11
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
/// 각 기어 별 레벨업에 필요한 재화 데이터
/// 생성일 : 26.06.11
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class GearUpgradeCostData
{
    public int Gear_ID;
    public int StartLevel; // 최초 획득 시 시작 레벨
    public int EndLevel; // 모든 돌파 적용 후 최종 레벨
    public string Type; // 업그레이드에 들어가는 재화 종류 : Gold, UpgradeStone
    public int BaseAmount; // 업그레이드에 들어가는 재화의 기본 수치
    public float GrowthValue; // 업그레이드에 들어가는 재화의 성장 보정치 : 최종 필요 재화량 = BaseAmount × GrowthValue ^ (현재 레벨 - StartLevel), 소수점이 발생할 경우, 소수점 이하 올림.
}

/// <summary>
/// 기어에 적용되는 특수 효과 정의 데이터
/// 생성일 : 26.06.11
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

#region ----- O -----

/// <summary>
/// 아웃게임 레벨<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class OutLevelData
{
    public int OutLevel;
    public int RequiredGold;
    public int RequiredParchment;
    public int MaxHP;
    public int Attack;
    public int StunPower;
    public int SlowPower;
    public int NewEnchant;
}


#endregion

#region ----- S -----

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
    public int SpecialSpawnInterval;
    public int SpecialSpawnAmount;
}

/// <summary>
/// 스테이지 구성 데이터<br/>
/// Legacy에서 이관<br/>
/// 최종 수정일 : 
/// </summary>
[Serializable]
public class StageData
{
    public int Chapter_ID;      // FK
    public int Stage_ID;        // PK
    public int StageOrder;
    public int TimeLimit;
    public int WaveGroup_ID;    // 현재 미사용, 향후 확장용
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

#endregion

#region Sort 보조 구조체

/// <summary>
/// Sort 보조 구조체
/// </summary>
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

    #endregion

