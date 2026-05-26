// 담당자 : 정승우
// 설명   : 전투 발동 판정 + 데미지 계산

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sort 정렬 성공 시 전투를 발동하고 데미지를 계산한다.
/// 정렬공격, 조합공격, 콤보공격, 자동공격 4가지 경로.
/// </summary>
public class CombatSystem : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [Tooltip("Sort 시스템 -- ISortNotifier로 접근")]
    [SerializeField] private MonoBehaviour _sortSystemObj;

    [SerializeField] private SkillSystem _skillSystem;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private CombinationModel _combinationModel;
    [SerializeField] private PlayerModel _playerModel;

    [Header("자동공격")]
    [Tooltip("자동공격 켜짐 여부. 인챈트 선택 전에는 꺼져있음")]
    [SerializeField] private bool _autoAttackEnabled;

    // ---------- Private ----------
    private ISortNotifier _sortNotifier;
    private float _autoAttackTimer;
    private float _autoAttackInterval = 1.0f;

    // ---------- 생명주기 ----------
    private void Awake()
    {
        _sortNotifier = _sortSystemObj as ISortNotifier;
    }

    private void OnEnable()
    {
        _sortNotifier.OnSortCompleted += HandleSortCompleted;
    }

    private void OnDisable()
    {
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

    // ---------- 정렬 성공 -> 공격 ----------
    private void HandleSortCompleted(UnitType type)
    {
        // 콤보 올리기
        _comboModel.IncrementCombo();

        // 정렬 공격
        var sortSkill = _skillSystem.GetSortSkill(type);
        if (sortSkill != null)
            _skillSystem.FireSkill(sortSkill, AttackType.Sort);
        else
            _skillSystem.FireBasicAttack();

        // 조합식 재료 체크
        _combinationModel.CheckIngredient(type);
        if (_combinationModel.HasCompletedRecipe())
        {
            int idx = _combinationModel.GetCompletedRecipeIndex();
            int skillId = _combinationModel.GetRecipeSkillId(idx);
            var combiSkill = DataManager.Instance.CharacterRepo.GetSkill(skillId);
            _skillSystem.FireSkill(combiSkill, AttackType.Combi);
            _combinationModel.ConsumeRecipe(idx);
        }

        // 콤보 스킬 체크
        var comboSkills = _skillSystem.GetTriggeredComboSkills(_comboModel.CurrentCombo);
        for (int i = 0; i < comboSkills.Count; i++)
            _skillSystem.FireSkill(comboSkills[i], AttackType.Combo);
    }

    // ---------- 데미지 계산 ----------
    public int CalculateDamage(int baseDmg)
    {
        int comboBonus = _comboModel.GetComboBonus();
        
        // 추가 : 홍정옥
        // 내용 : PlayerModel의 공격력을 스킬 기본 데미지에 더해 JSON/SO 캐릭터 공격력이 전투에 반영되도록 연결
        float dmg = _playerModel.Attack + baseDmg + comboBonus;

        // 치명타
        var stats = DataManager.Instance.CharacterRepo.GetCharacterStatus(1);
        if (Random.value < stats.CriticalRate)
            dmg *= (1f + stats.CriticalDamageBonus);

        // 관통
        dmg += stats.FlatPierce;

        return Mathf.Max(1, Mathf.RoundToInt(dmg));
    }

    // 인챈트에서 자동공격 얻으면 호출
    public void EnableAutoAttack()
    {
        _autoAttackEnabled = true;
        var stats = DataManager.Instance.CharacterRepo.GetCharacterStatus(1);
        _autoAttackInterval = stats.BaseAttackSpeed;
    }
}
