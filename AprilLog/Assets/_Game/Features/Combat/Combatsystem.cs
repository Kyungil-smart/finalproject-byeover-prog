// 담당자 : 정승우
// 설명   : 전투 발동 판정 + 데미지 계산

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : Codex
// 수정내용 : CharacterRepo가 Inspector에 연결되지 않아도 DataManager에서 자동 참조

using System.Collections.Generic;
using UnityEngine;
 
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
 
    [Header("자동공격")]
    [SerializeField] private bool _autoAttackEnabled;
 
    [Tooltip("자동공격 간격(초). 인챈트로 변경 가능.")]
    [SerializeField] private float _autoAttackInterval = 1.0f;
 
    // ---------- Private ----------
    private ISortNotifier _sortNotifier;
    private float _autoAttackTimer;
 
    private void Awake()
    {
        _sortNotifier = _sortSystemObj as ISortNotifier;
        ResolveRepository();
    }
 
    private void OnEnable()
    {
        ResolveRepository();
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
            var combiSkill = _characterRepo.GetSkill(skillId);
            _skillSystem.FireSkill(combiSkill, AttackType.Combi);
            _combinationModel.ConsumeRecipe(idx);
        }
 
        var comboSkills = _skillSystem.GetTriggeredComboSkills(_comboModel.CurrentCombo);
        for (int i = 0; i < comboSkills.Count; i++)
            _skillSystem.FireSkill(comboSkills[i], AttackType.Combo);
    }
 
    // 데미지 공식 -- 기획서 v1.03 기준 (FlatPierce, CriticalDamageBonus 삭제됨)
    public int CalculateDamage(int baseDmg)
    {
        ResolveRepository();
        if (_characterRepo == null)
        {
            Debug.LogError("[CombatSystem] CharacterRepo를 찾을 수 없어 기본 대미지만 계산합니다.");
            return Mathf.Max(1, baseDmg);
        }

        int comboBonus = _comboModel.GetComboBonus();

        // 추가 : 홍정옥
        // 내용 : CommonStatus와 OutLevel에서 PlayerModel에 반영된 Attack을 전투 데미지에 연결
        float playerAttack = _playerModel != null ? _playerModel.Attack : 0f;
        float dmg = playerAttack + baseDmg + comboBonus;
 
        var stats = _characterRepo.GetCharacterStatus(1);
 
        // 치명타 (기본 25% 추가 데미지)
        if (Random.value < stats.CriticalRate)
            dmg *= 1.25f;
 
        // 비율 관통은 몬스터 Defense 적용 시 사용 (MonsterAI.TakeDamage에서 처리)
 
        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }
 
    public void EnableAutoAttack()
    {
        _autoAttackEnabled = true;
    }

    private void ResolveRepository()
    {
        if (_characterRepo != null) return;
        if (DataManager.Instance == null) return;

        _characterRepo = DataManager.Instance.CharacterRepo;
    }
}
