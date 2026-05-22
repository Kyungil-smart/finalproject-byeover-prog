// 담당자 : 정승우
// 설명   : 캐릭터/스킬/인챈트 데이터 저장소

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 캐릭터, 스킬, 인챈트 관련 SO를 Inspector에서 받아서
/// Dictionary로 캐싱한다. 런타임에서는 Dictionary 조회만.
/// Initialize()에서만 LINQ 씀 (한 번이라 GC 괜찮음).
/// </summary>
public class CharacterRepo : MonoBehaviour
{
    // ---------- SO 참조 (Inspector에서 드래그) ----------
    [Header("캐릭터")]
    [SerializeField] private CharacterMasterTable _characterMasterTable;
    [SerializeField] private CommonStatusTable _commonStatusTable;
    [SerializeField] private CharacterStatusTable _characterStatusTable;
    [SerializeField] private MonsterStatusTable _monsterStatusTable;

    [Header("스킬")]
    [SerializeField] private SkillMasterTable _skillMasterTable;
    [SerializeField] private SkillDataTable _skillDataTable;
    [SerializeField] private EffectDataTable _effectTable;

    [Header("인챈트")]
    [SerializeField] private EnchantMasterTable _enchantMasterTable;
    [SerializeField] private EnchantLevelTable _enchantLevelTable;
    [SerializeField] private EnchantWeightTable _enchantWeightTable;

    // ---------- Dictionary 캐시 ----------
    private Dictionary<int, CharacterMasterData> _characterMaster;
    private Dictionary<int, CommonStatusData> _commonStatus;
    private Dictionary<int, CharacterStatusData> _characterStatus;
    private Dictionary<int, MonsterStatusData> _monsterStatus;
    private Dictionary<int, SkillMasterData> _skillMaster;
    private Dictionary<int, SkillData> _skills;
    private Dictionary<int, EffectData> _effects;
    private Dictionary<int, EnchantMasterData> _enchantMaster;
    private Dictionary<string, EnchantLevelData> _enchantLevels;

    // ---------- 초기화 ----------
    public void Initialize()
    {
        // SO List -> Dictionary 변환 (파싱 아님, 이미 메모리에 있는 데이터 옮기기)
        _characterMaster = _characterMasterTable.rows.ToDictionary(r => r.Character_ID);
        _commonStatus = _commonStatusTable.rows.ToDictionary(r => r.Character_ID);
        _characterStatus = _characterStatusTable.rows.ToDictionary(r => r.Character_ID);
        _monsterStatus = _monsterStatusTable.rows.ToDictionary(r => r.Character_ID);
        _skillMaster = _skillMasterTable.rows.ToDictionary(r => r.StandardID);
        _skills = _skillDataTable.rows.ToDictionary(r => r.SkillID);
        _effects = _effectTable.rows.ToDictionary(r => r.EffectID);
        _enchantMaster = _enchantMasterTable.rows.ToDictionary(r => r.EnchantID);

        // 인챈트 레벨은 복합 키 (EnchantID_Level)
        _enchantLevels = new Dictionary<string, EnchantLevelData>();
        for (int i = 0; i < _enchantLevelTable.rows.Count; i++)
        {
            var row = _enchantLevelTable.rows[i];
            _enchantLevels[$"{row.EnchantID}_{row.Level}"] = row;
        }

        Debug.Log($"[CharacterRepo] 초기화 완료. " +
            $"캐릭터 {_commonStatus.Count}, 스킬 {_skills.Count}, 인챈트 {_enchantMaster.Count}");
    }

    // ---------- 조회 API ----------
    public CommonStatusData GetCommonStatus(int id) => _commonStatus[id];
    public CharacterStatusData GetCharacterStatus(int id) => _characterStatus[id];
    public MonsterStatusData GetMonsterStatus(int id) => _monsterStatus[id];
    public SkillMasterData GetSkillMaster(int standardId) => _skillMaster[standardId];
    public SkillData GetSkill(int skillId) => _skills[skillId];
    public EffectData GetEffect(int effectId) => _effects[effectId];
    public EnchantMasterData GetEnchantMaster(int enchantId) => _enchantMaster[enchantId];
    public EnchantLevelData GetEnchantLevel(int enchantId, int level) => _enchantLevels[$"{enchantId}_{level}"];

    // 전체 조회 (인챈트 선택 로직에서 필요)
    public IReadOnlyDictionary<int, EnchantMasterData> GetAllEnchantMasters() => _enchantMaster;
    public IReadOnlyList<EnchantWeightData> GetEnchantWeights() => _enchantWeightTable.rows;

    // 안전 조회 (키가 없을 수 있는 경우)
    public bool TryGetCommonStatus(int id, out CommonStatusData data) => _commonStatus.TryGetValue(id, out data);
    public bool TryGetEffect(int id, out EffectData data) => _effects.TryGetValue(id, out data);
}
