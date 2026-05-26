// 담당자 : 정승우
// 설명   : SO 테이블 클래스 모음 -- 기획서 v1.03 기준 수정

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 데이터 테이블 SO의 베이스.
/// </summary>
public abstract class DataTable<T> : ScriptableObject
{
    [Header("데이터")]
    public List<T> rows = new List<T>();
}

// 캐릭터
[CreateAssetMenu(fileName = "CharacterMasterTable", menuName = "Data/CharacterMasterTable")]
public class CharacterMasterTable : DataTable<CharacterMasterData> { }

[CreateAssetMenu(fileName = "CharacterNameTable", menuName = "Data/CharacterNameTable")]
public class CharacterNameTable : DataTable<CharacterNameData> { }

[CreateAssetMenu(fileName = "CommonStatusTable", menuName = "Data/CommonStatusTable")]
public class CommonStatusTable : DataTable<CommonStatusData> { }

[CreateAssetMenu(fileName = "CharacterStatusTable", menuName = "Data/CharacterStatusTable")]
public class CharacterStatusTable : DataTable<CharacterStatusData> { }

[CreateAssetMenu(fileName = "MonsterStatusTable", menuName = "Data/MonsterStatusTable")]
public class MonsterStatusTable : DataTable<MonsterStatusData> { }

// 스킬
[CreateAssetMenu(fileName = "SkillMasterTable", menuName = "Data/SkillMasterTable")]
public class SkillMasterTable : DataTable<SkillMasterData> { }

[CreateAssetMenu(fileName = "SkillDataTable", menuName = "Data/SkillDataTable")]
public class SkillDataTable : DataTable<SkillData> { }

[CreateAssetMenu(fileName = "EffectTable", menuName = "Data/EffectTable")]
public class EffectDataTable : DataTable<EffectData> { }

// 인챈트
[CreateAssetMenu(fileName = "EnchantMasterTable", menuName = "Data/EnchantMasterTable")]
public class EnchantMasterTable : DataTable<EnchantMasterData> { }

[CreateAssetMenu(fileName = "EnchantLevelTable", menuName = "Data/EnchantLevelTable")]
public class EnchantLevelTable : DataTable<EnchantLevelData> { }

[CreateAssetMenu(fileName = "EnchantWeightTable", menuName = "Data/EnchantWeightTable")]
public class EnchantWeightTable : DataTable<EnchantWeightData> { }

// 챕터 / 스테이지
[CreateAssetMenu(fileName = "ChapterTable", menuName = "Data/ChapterTable")]
public class ChapterTable : DataTable<ChapterData> { }

[CreateAssetMenu(fileName = "MapLanguageTable", menuName = "Data/MapLanguageTable")]
public class MapLanguageTable : DataTable<MapLanguageData> { }

[CreateAssetMenu(fileName = "StageTable", menuName = "Data/StageTable")]
public class StageDataTable : DataTable<StageData> { }

// 몬스터 풀 (신규)
[CreateAssetMenu(fileName = "MonsterPoolMasterTable", menuName = "Data/MonsterPoolMasterTable")]
public class MonsterPoolMasterTable : DataTable<MonsterPoolMasterData> { }

[CreateAssetMenu(fileName = "MonsterPoolTable", menuName = "Data/MonsterPoolTable")]
public class MonsterPoolTable : DataTable<MonsterPoolData> { }

// 스폰 (신규 -- 기존 StageMonsterTable 대체)
[CreateAssetMenu(fileName = "StageSpawnRuleTable", menuName = "Data/StageSpawnRuleTable")]
public class StageSpawnRuleTable : DataTable<StageSpawnRuleData> { }

[CreateAssetMenu(fileName = "MonsterStageScalingTable", menuName = "Data/MonsterStageScalingTable")]
public class MonsterStageScalingTable : DataTable<MonsterStageScalingData> { }

// 레벨
[CreateAssetMenu(fileName = "InLevelTable", menuName = "Data/InLevelTable")]
public class InLevelTable : DataTable<InLevelData> { }

[CreateAssetMenu(fileName = "OutLevelTable", menuName = "Data/OutLevelTable")]
public class OutLevelTable : DataTable<OutLevelData> { }

// 보상/업적
[CreateAssetMenu(fileName = "ChangeRewardTable", menuName = "Data/ChangeRewardTable")]
public class ChangeRewardTable : DataTable<ChangeRewardData> { }

[CreateAssetMenu(fileName = "AchievementTable", menuName = "Data/AchievementTable")]
public class AchievementDataTable : DataTable<AchievementData> { }

// 언어
[CreateAssetMenu(fileName = "LanguageTable", menuName = "Data/LanguageTable")]
public class LanguageTable : DataTable<LanguageEntry> { }