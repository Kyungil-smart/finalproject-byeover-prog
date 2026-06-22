// 담당자 : 정승우
// 설명   : 전투 발동 판정 + 데미지 계산

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : 정승우
// 수정내용 : CharacterRepo가 Inspector에 연결되지 않아도 DataManager에서 자동 참조

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경 및 new DataManager와 연결

// 수정자 : 김영찬
// 능력치 시트 1.04와 데미지 공식 1.01 바탕으로 플레이어 공격력 계산 최신화

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Sort 정렬 성공 시 전투를 발동하고 데미지를 계산한다.
/// </summary>
public class CombatSystem : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonoBehaviour _sortSystemObj;
    [SerializeField] private SkillSystem _skillSystem;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private CombinationModel _combinationModel;
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private SpellRepo _spellRepo;
 
    [Header("자동공격")]
    [SerializeField] private bool _autoAttackEnabled;
 
    [Tooltip("자동공격 간격(초). 인챈트로 변경 가능.")]
    [SerializeField] private float _autoAttackInterval = 1.0f;
 
    // ---------- Private ----------
    private ISortNotifier _sortNotifier;
    private float _autoAttackTimer;
    private int _autoAttackCount; // 누적 자동공격 횟수 (N회마다 일반 스킬 인챈트 발동용)

    private void Awake()
    {
        _sortNotifier = _sortSystemObj as ISortNotifier;
        ResolveCharacterRepository();
    }
 
    private void OnEnable()
    {
        ResolveCharacterRepository();
        ResolveSystemReferences();

        if (_sortNotifier == null)
            _sortNotifier = _sortSystemObj as ISortNotifier;

        if (_sortNotifier == null)
        {
            Debug.LogWarning("[전투진단] CombatSystem: SortSystem(ISortNotifier)을 못 찾아 정렬 완성을 수신하지 못합니다 → 공격 안 나감.", this);
            return;
        }

        _sortNotifier.OnSortCompleted += HandleSortCompleted;
        Debug.Log("[전투진단] CombatSystem: OnSortCompleted 구독 완료 (정렬 성공 시 공격 발동).");
    }

    /// <summary>
    /// SortSystem이 런타임에 생성되는 경우, CombatSystem.OnEnable 시점엔 아직 없어서
    /// OnSortCompleted 구독을 놓친다(=공격 안 나감). 생성 직후 InGameBootstrap이 이걸 호출해
    /// 명시적으로 바인딩한다. 중복 구독은 방지한다.
    /// </summary>
    public void BindSortSystem(SortSystem sortSystem)
    {
        if (sortSystem == null) return;

        if (_sortNotifier != null)
            _sortNotifier.OnSortCompleted -= HandleSortCompleted;

        _sortSystemObj = sortSystem;
        _sortNotifier = sortSystem;   // SortSystem : ISortNotifier
        _sortNotifier.OnSortCompleted += HandleSortCompleted;

        Debug.Log("[전투진단] CombatSystem: SortSystem 런타임 바인딩 완료 (정렬 성공 시 공격 발동).");
    }

    // 씬에서 참조가 안 꽂혀 있어도 동작하도록 자동 탐색 (다른 시스템과 동일 패턴).
    private void ResolveSystemReferences()
    {
        if (_sortSystemObj == null) _sortSystemObj = FindFirstObjectByType<SortSystem>();
        if (_skillSystem == null) _skillSystem = FindFirstObjectByType<SkillSystem>();
        if (_comboModel == null) _comboModel = FindFirstObjectByType<ComboModel>();
        if (_combinationModel == null) _combinationModel = FindFirstObjectByType<CombinationModel>();
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
    }
 
    private void OnDisable()
    {
        if (_sortNotifier == null) return;
        _sortNotifier.OnSortCompleted -= HandleSortCompleted;
    }
 
    private void Update()
    {
        if (!_autoAttackEnabled || _skillSystem == null) return;

        ExpireHasteIfDue();

        _autoAttackTimer += Time.deltaTime;
        if (_autoAttackTimer >= _autoAttackInterval * _hasteIntervalMul) // 헤이스트 시 간격 단축
        {
            _autoAttackTimer = 0f;

            // 자동공격 N회마다 발동하는 일반 스킬 인챈트 (인챈트 테이블 v1.03 — 파이어브레스 등).
            // 카운트는 '발사 성공'만 센다 — 몬스터 공백(웨이브 전환 등) 중 틱에 N회째가 걸려
            // 파이어브레스 발동이 무음 소실되는 것을 방지.
            if (_skillSystem.FireBasicAttack(AttackType.Auto))
            {
                _autoAttackCount++;
                var autoSkills = _skillSystem.GetTriggeredAutoAttackSkills(_autoAttackCount);
                for (int i = 0; i < autoSkills.Count; i++)
                {
                    _skillSystem.FireSkill(autoSkills[i], AttackType.Auto);
                    // 헤이스트(301) 발동 → 공격력·공속 버프 활성 (해당 발동 레벨 기준)
                    if (autoSkills[i] != null && autoSkills[i].StandardID == 301)
                        ActivateHaste(autoSkills[i].Level);
                }
            }
        }
    }
 
    private void HandleSortCompleted(UnitType type)
    {
        Debug.Log($"[전투진단] 정렬 완성 수신: type={type} → 공격 발동 시도");

        ResolveSpellRepository();
        ResolveSystemReferences();

        if (_skillSystem == null)
        {
            Debug.LogWarning("[전투진단] SkillSystem이 없어 공격을 발사할 수 없습니다.", this);
            return;
        }

        if (_spellRepo == null)
        {
            Debug.LogError("[CombatSystem] SpellRepo를 찾을 수 없어 인첸트가 발동되지 않습니다.");
        }

        if (_comboModel != null)
            _comboModel.IncrementCombo();
 
        var sortSkill = _skillSystem.GetSortSkill(type);
        if (sortSkill != null)
            _skillSystem.FireSkill(sortSkill, AttackType.Sort);
        else
            _skillSystem.FireBasicAttack(AttackType.Sort);
 
        if (_combinationModel != null)
        {
            _combinationModel.CheckIngredient(type);
            // 한 번의 정렬로 여러 조합식이 동시에 완성될 수 있으므로 전부 발동 (최대 레시피 수만큼, 무한루프 가드)
            for (int guard = 0; guard < 3 && _combinationModel.HasCompletedRecipe(); guard++)
            {
                int idx = _combinationModel.GetCompletedRecipeIndex();
                int skillId = _combinationModel.GetRecipeSkillId(idx);

                if (_spellRepo != null)
                {
                    var combiSkill = _spellRepo.GetSkill(skillId);
                    _skillSystem.FireSkill(combiSkill, AttackType.Combi);
                }

                _combinationModel.ConsumeRecipe(idx);
            }
        }

        if (_comboModel != null)
        {
            var comboSkills = _skillSystem.GetTriggeredComboSkills(_comboModel.CurrentCombo);
            for (int i = 0; i < comboSkills.Count; i++)
                _skillSystem.FireSkill(comboSkills[i], AttackType.Combo);
        }
    }
 
    // 데미지 공식 -- 능력치 시트 1.04와 데미지 공식 1.01 기준
    public float CalculateDamage(float skillDmgRate)
    {
        // 데미지 공식에 필요한 필드 정리
        float attack = _playerModel != null ? _playerModel.Attack : 1; // ATK (Stat_ATK_Enchant는 PlayerModel.Attack에 반영됨)
        float criRate = _playerModel != null ? _playerModel.CriticalRate : 0; // 치명타 확률 0~1
        float critDamageStat = _playerModel != null ? _playerModel.CriticalDamage : 0f; // Stat_Crit_Damage_Enchant(%)
        float comboBonus = (float)_comboModel.GetComboBonusRate(); // ComboModifier

        // CriticalModifier (데미지 공식 1.01 기준)
        //   미발생 = 1.0 / 발생 = 1 + 0.2 × ((100 + Stat_Crit_Damage_Enchant) / 100)
        float critModifier = GetIsHitCritical(criRate)
            ? 1f + 0.2f * ((100f + critDamageStat) / 100f)
            : 1f;

        // Base_Damage = ATK × skillDmgRate(=1 + Skill_Enchant/100) × ComboModifier × CriticalModifier
        //   (1 + Group_Bonus/100) 항은 데미지 그룹 데이터 미정의로 SkillSystem에서 후보정 ToDo로 남김
        float baseDmg = attack * skillDmgRate * comboBonus * critModifier;
        return baseDmg;
    }

    /// <summary>EnchantCalculator 경로용: 콤보 보너스만 노출(콤보 보존). DamageCalculate가 ATK/크리/그룹은 처리하므로 콤보만 곱한다.</summary>
    public float GetComboBonusRate() => _comboModel != null ? (float)_comboModel.GetComboBonusRate() : 1f;

    public void EnableAutoAttack()
    {
        _autoAttackEnabled = true;
    }

    // 헤이스트(바람 301): 발동 시 공격력↑ + 자동공격 간격↓. 수치 placeholder(기획 확정 시 조정).
    // Develop 머지 후 교체 필수: ApplyAttackBonus_Add/_RemoveA(삭제됨) → StatusEnhance(PlayerStatus.Attack, CalFormula.Add, x, false/true).
    private float _hasteEndTime;
    private int _hasteAtkBonus;          // 적용 중 공격력 보너스(해제용)
    private float _hasteIntervalMul = 1f; // 자동공격 간격 배율(작을수록 빠름)

    private void ActivateHaste(int level)
    {
        int lv = Mathf.Clamp(level, 1, 3);
        int[] atk = { 8, 10, 12 };            // 공격력 보너스
        float[] mul = { 0.8f, 0.75f, 0.7f };  // 자동공격 간격 배율
        float[] dur = { 5f, 5f, 6f };         // 지속(초)

        if (_hasteAtkBonus > 0 && _playerModel != null)   // 갱신: 기존 보너스 먼저 해제
            _playerModel.StatusEnhance(PlayerStatus.AttackSpeed, CalFormula.Add, _hasteAtkBonus, true);

        _hasteAtkBonus = atk[lv - 1];
        _hasteIntervalMul = mul[lv - 1];
        _hasteEndTime = Time.time + dur[lv - 1];
        if (_playerModel != null) 
            _playerModel.StatusEnhance(PlayerStatus.AttackSpeed, CalFormula.Add, _hasteAtkBonus, false);
    }

    private void ExpireHasteIfDue()
    {
        if (_hasteAtkBonus == 0) return;
        if (Time.time >= _hasteEndTime)
        {
            if (_playerModel != null) 
                _playerModel.StatusEnhance(PlayerStatus.AttackSpeed, CalFormula.Add, _hasteAtkBonus, true);
            _hasteAtkBonus = 0;
            _hasteIntervalMul = 1f;
        }
    }

    private void ResolveCharacterRepository()
    {
        if (_characterRepo != null) return;
        if (DataManager.Instance == null) return;

        _characterRepo = DataManager.Instance.CharacterRepo;
    }
    
    private void ResolveSpellRepository()
    {
        if (_spellRepo != null) return;
        if (DataManager.Instance == null) return;

        _spellRepo = DataManager.Instance.SpellRepo;
    }

    private bool GetIsHitCritical(float criRate)
    {
        float criChance = Random.Range(0, 1f);
        if (criChance <= criRate)
        {
            return true;
        }
        return false;
    }
}
