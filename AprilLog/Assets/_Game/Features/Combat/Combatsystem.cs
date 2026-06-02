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
    private const double BASE_CRITIAL_DAMAGE = 1.2d;
 
    private void Awake()
    {
        _sortNotifier = _sortSystemObj as ISortNotifier;
        ResolveCharacterRepository();
    }
 
    private void OnEnable()
    {
        ResolveCharacterRepository();
        if (_sortNotifier == null)
            _sortNotifier = _sortSystemObj as ISortNotifier;

        if (_sortNotifier == null) return;
        _sortNotifier.OnSortCompleted += HandleSortCompleted;
    }
 
    private void OnDisable()
    {
        if (_sortNotifier == null) return;
        _sortNotifier.OnSortCompleted -= HandleSortCompleted;
    }
 
    private void Update()
    {
        if (!_autoAttackEnabled) return;
 
        _autoAttackTimer += Time.deltaTime;
        if (_autoAttackTimer >= _autoAttackInterval)
        {
            _autoAttackTimer = 0f;
            _skillSystem.FireBasicAttack();
        }
    }
 
    private void HandleSortCompleted(UnitType type)
    {
        ResolveSpellRepository();
        
        if (_spellRepo == null)
        {
            Debug.LogError("[CombatSystem] SpellRepo를 찾을 수 없어 인첸트가 발동되지 않습니다.");
        }
        
        _comboModel.IncrementCombo();
 
        var sortSkill = _skillSystem.GetSortSkill(type);
        if (sortSkill != null)
            _skillSystem.FireSkill(sortSkill, AttackType.Sort);
        else
            _skillSystem.FireBasicAttack();
 
        _combinationModel.CheckIngredient(type);
        if (_combinationModel.HasCompletedRecipe())
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
 
        var comboSkills = _skillSystem.GetTriggeredComboSkills(_comboModel.CurrentCombo);
        for (int i = 0; i < comboSkills.Count; i++)
            _skillSystem.FireSkill(comboSkills[i], AttackType.Combo);
    }
 
    // 데미지 공식 -- 능력치 시트 1.04와 데미지 공식 1.01 기준
    public float CalculateDamage(float skillDmgRate)
    {
        // 데미지 공식에 필요한 필드 정리
        float attack = _playerModel != null ? _playerModel.Attack : 1; // ATK x ( 1 + Stat_ATK_Enchant / 100)
        float criRate = _playerModel != null ? _playerModel.CriticalRate : 0; // CriticalModifier
        float criDamage = _playerModel != null ? _playerModel.CriticalDamage + (float)BASE_CRITIAL_DAMAGE : (float)BASE_CRITIAL_DAMAGE; // CriticalModifier
        float comboBonus = (float)_comboModel.GetComboBonusRate(); // ComboModifier
        float baseDmg;
        
        // Base_Damage = [ ATK x ( 1 + Stat_ATK_Enchant / 100) x (1 + Skill_Enchant/100) x (1+ Group_Bonus / 100) x CriticalModifier x ComboModifier]
        //                                                         L 매게변수                  L 이부분은 SkillSystem에서 후보정
        if (GetIsHitCritical(criRate))
        {
            baseDmg = attack * skillDmgRate * comboBonus * criDamage;
        }
        else
        {
            baseDmg = attack * skillDmgRate * comboBonus;
        }


        return baseDmg;
    }
 
    public void EnableAutoAttack()
    {
        _autoAttackEnabled = true;
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
