// 담당자 : 김영찬
// 설명   : 데이터 테이블 구조체 모음 (신규)
// 주의 사항 : 알파벳 순으로 정렬 할 것

using System;
using System.Collections.Generic;

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
    public string MonsterPoolType;  // Normal, Agile, Tank, Ranged, Infested, Gimmick, Elite, Boss
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

