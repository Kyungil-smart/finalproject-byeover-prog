// 담당자 : 정승우
// 설명   : 스킬 데이터 조회 + 투사체 생성

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 발사, 투사체 생성, 스킬 슬롯 관리를 담당한다.
/// CombatSystem이 "이 스킬 쏴라"고 하면 여기서 실제로 투사체를 만듦.
/// </summary>
public class SkillSystem : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private CharacterRepo _characterRepo;
    [SerializeField] private CombatSystem _combatSystem;

    [Header("발사 위치")]
    [Tooltip("투사체가 나가는 기준 위치 (캐릭터)")]
    [SerializeField] private Transform _firePoint;

    // ---------- Private ----------
    // 인챈트로 획득한 스킬 슬롯. UnitType -> SkillData 매핑.
    private Dictionary<UnitType, SkillData> _sortSkills = new Dictionary<UnitType, SkillData>();

    // 콤보 스킬: 콤보 배수에 도달하면 자동 발동
    private List<ComboSkillEntry> _comboSkills = new List<ComboSkillEntry>();

    // 콤보 스킬 판정용 재사용 리스트 (GC 방지)
    private List<SkillData> _triggeredComboCache = new List<SkillData>(4);

    // ---------- 스킬 등록 (인챈트 시스템에서 호출) ----------
    public void RegisterSortSkill(UnitType type, SkillData data)
    {
        _sortSkills[type] = data;
    }

    public void RegisterComboSkill(int comboMultiple, SkillData data)
    {
        _comboSkills.Add(new ComboSkillEntry { comboMultiple = comboMultiple, data = data });
    }

    public void UnregisterSortSkill(UnitType type)
    {
        _sortSkills.Remove(type);
    }

    // ---------- 스킬 조회 ----------
    public SkillData GetSortSkill(UnitType type)
    {
        return _sortSkills.TryGetValue(type, out var data) ? data : null;
    }

    public List<SkillData> GetTriggeredComboSkills(int currentCombo)
    {
        _triggeredComboCache.Clear();

        for (int i = 0; i < _comboSkills.Count; i++)
        {
            int multiple = _comboSkills[i].comboMultiple;
            // 콤보가 배수에 정확히 도달한 순간만 발동
            if (multiple > 0 && currentCombo > 0 && currentCombo % multiple == 0)
                _triggeredComboCache.Add(_comboSkills[i].data);
        }

        return _triggeredComboCache;
    }

    // ---------- 발사 ----------
    public void FireSkill(SkillData data, AttackType type)
    {
        if (data == null) return;

        var master = _characterRepo.GetSkillMaster(data.StandardID);
        int damage = _combatSystem.CalculateDamage(data.Dmg);

        // 투사체 생성
        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        // 타격 방식에 따라 행동 결정
        IProjectileBehavior behavior = CreateBehavior(master.HitRange, data.Speed);
        Vector2 targetPos = FindNearestMonsterPos();

        controller.Setup(behavior, damage, _firePoint.position, targetPos);
    }

    public void FireBasicAttack()
    {
        // 기본공격: 가장 가까운 몬스터한테 직선 투사체
        int baseDmg = _combatSystem.CalculateDamage(10);  // 기본 데미지

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        IProjectileBehavior behavior = new StraightProjectile();
        Vector2 targetPos = FindNearestMonsterPos();

        controller.Setup(behavior, baseDmg, _firePoint.position, targetPos);
    }

    // ---------- 투사체 행동 팩토리 ----------
    private IProjectileBehavior CreateBehavior(string hitRange, int speed)
    {
        // hitRange 문자열에 따라 다른 행동 생성 (OCP)
        switch (hitRange)
        {
            case "Piercing":    return new PiercingProjectile();
            case "Homing":      return new HomingProjectile();
            case "Bouncing":    return new BouncingProjectile();
            default:            return new StraightProjectile();
        }
    }
    
    private Vector2 FindNearestMonsterPos()
    {
        return (Vector2)_firePoint.position + Vector2.up * 10f;
    }
}

[System.Serializable]
public struct ComboSkillEntry
{
    public int comboMultiple;   // 이 배수마다 발동 (예: 5면 5, 10, 15... 콤보에 발동)
    public SkillData data;
}