// 담당자 : 정승우
// 설명   : SO 테이블 클래스 모음 -- 제네릭 베이스 + 테이블별 1줄 정의

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 데이터 테이블 SO의 베이스.
/// 자식 클래스는 CreateAssetMenu만 붙이면 끝.
/// </summary>
public abstract class DataTable<T> : ScriptableObject
{
    [Header("데이터")]
    public List<T> rows = new List<T>();
}

[CreateAssetMenu(fileName = "CharacterMasterTable", menuName = "Data/CharacterMasterTable")]
public class CharacterMasterTable : DataTable<CharacterMasterData> { }

[CreateAssetMenu(fileName = "CommonStatusTable", menuName = "Data/CommonStatusTable")]
public class CommonStatusTable : DataTable<CommonStatusData> { }

[CreateAssetMenu(fileName = "CharacterStatusTable", menuName = "Data/CharacterStatusTable")]
public class CharacterStatusTable : DataTable<CharacterStatusData> { }

[CreateAssetMenu(fileName = "MonsterStatusTable", menuName = "Data/MonsterStatusTable")]
public class MonsterStatusTable : DataTable<MonsterStatusData> { }

[CreateAssetMenu(fileName = "SkillMasterTable", menuName = "Data/SkillMasterTable")]
public class SkillMasterTable : DataTable<SkillMasterData> { }

[CreateAssetMenu(fileName = "SkillDataTable", menuName = "Data/SkillDataTable")]
public class SkillDataTable : DataTable<SkillData> { }

[CreateAssetMenu(fileName = "EffectTable", menuName = "Data/EffectTable")]
public class EffectDataTable : DataTable<EffectData> { }

[CreateAssetMenu(fileName = "EnchantMasterTable", menuName = "Data/EnchantMasterTable")]
public class EnchantMasterTable : DataTable<EnchantMasterData> { }

[CreateAssetMenu(fileName = "EnchantLevelTable", menuName = "Data/EnchantLevelTable")]
public class EnchantLevelTable : DataTable<EnchantLevelData> { }

[CreateAssetMenu(fileName = "EnchantWeightTable", menuName = "Data/EnchantWeightTable")]
public class EnchantWeightTable : DataTable<EnchantWeightData> { }

[CreateAssetMenu(fileName = "ChapterTable", menuName = "Data/ChapterTable")]
public class ChapterTable : DataTable<ChapterData> { }

[CreateAssetMenu(fileName = "StageTable", menuName = "Data/StageTable")]
public class StageDataTable : DataTable<StageData> { }

[CreateAssetMenu(fileName = "StageMonsterTable", menuName = "Data/StageMonsterTable")]
public class StageMonsterTable : DataTable<StageMonsterData> { }

[CreateAssetMenu(fileName = "MonsterScalingTable", menuName = "Data/MonsterScalingTable")]
public class MonsterScalingTable : DataTable<MonsterScalingData> { }

[CreateAssetMenu(fileName = "InLevelTable", menuName = "Data/InLevelTable")]
public class InLevelTable : DataTable<InLevelData> { }

[CreateAssetMenu(fileName = "OutGrowthTable", menuName = "Data/OutGrowthTable")]
public class OutGrowthDataTable : DataTable<OutGrowthData> { }

[CreateAssetMenu(fileName = "AchievementTable", menuName = "Data/AchievementTable")]
public class AchievementDataTable : DataTable<AchievementData> { }

[CreateAssetMenu(fileName = "ChangeRewardTable", menuName = "Data/ChangeRewardTable")]
public class ChangeRewardTable : DataTable<ChangeRewardData> { }

[CreateAssetMenu(fileName = "LanguageTable", menuName = "Data/LanguageTable")]
public class LanguageTable : DataTable<LanguageEntry> { }
