// 담당자 : 정승우
// 설명   : 스킬 데이터 조회 + 투사체 생성

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : 정승우
// 수정내용 : MonsterSpawner의 살아있는 몬스터 목록을 기준으로 실제 공격 타겟을 선택하도록 변경.
// 수정내용 : 모든 플레이어 공격을 직선 탄 전용 경로로 발사하여 발사 시 객체 생성을 줄임.
// 수정내용 : ProjectileController.Setup과 SetupStraight의 사용 기준을 호출부 주석으로 명확화.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 발사, 투사체 생성, 스킬 트리거 목록 관리를 담당한다.
/// CombatSystem은 공격 타이밍만 결정하고, 실제 발사 처리는 이 클래스가 맡는다.
/// </summary>
public class SkillSystem : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private CombatSystem _combatSystem;

    [Header("발사 위치")]
    [Tooltip("투사체가 생성되는 기준 위치")]
    [SerializeField] private Transform _firePoint;

    [Tooltip("살아있는 몬스터 목록과 공격 타겟 선택을 담당")]
    [SerializeField] private MonsterSpawner _monsterSpawner;

    [Header("직선 탄 설정")]
    [Tooltip("기본 공격 투사체 속도")]
    [SerializeField] private float _basicProjectileSpeed = 10f;

    // ---------- Private ----------
    private Dictionary<UnitType, Legacy_SkillData> _sortSkills = new Dictionary<UnitType, Legacy_SkillData>();
    private List<ComboSkillEntry> _comboSkills = new List<ComboSkillEntry>();
    private List<Legacy_SkillData> _triggeredComboCache = new List<Legacy_SkillData>(4);
    private bool _hasLoggedMissingFirePoint;
    private bool _hasLoggedMissingSpawner;
    private bool _hasTriedResolveSpawner;

    // 발사 기준 위치. firePoint가 비어 있으면 캐릭터(자기) 위치에서 발사한다.
    private Vector3 FireOrigin => _firePoint != null ? _firePoint.position : transform.position;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    // ---------- 스킬 등록 ----------
    public void RegisterSortSkill(UnitType type, Legacy_SkillData data)
    {
        _sortSkills[type] = data;
    }

    public void RegisterComboSkill(int comboMultiple, Legacy_SkillData data)
    {
        _comboSkills.Add(new ComboSkillEntry { comboMultiple = comboMultiple, data = data });
    }

    public void UnregisterSortSkill(UnitType type)
    {
        _sortSkills.Remove(type);
    }

    // ---------- 스킬 조회 ----------
    public Legacy_SkillData GetSortSkill(UnitType type)
    {
        return _sortSkills.TryGetValue(type, out var data) ? data : null;
    }

    public List<Legacy_SkillData> GetTriggeredComboSkills(int currentCombo)
    {
        _triggeredComboCache.Clear();

        for (int i = 0; i < _comboSkills.Count; i++)
        {
            int multiple = _comboSkills[i].comboMultiple;
            if (multiple > 0 && currentCombo > 0 && currentCombo % multiple == 0)
                _triggeredComboCache.Add(_comboSkills[i].data);
        }

        return _triggeredComboCache;
    }

    // ---------- 발사 ----------
    public void FireSkill(Legacy_SkillData data, AttackType type)
    {
        if (data == null) return;

        // 데미지 계산
        float temp = _combatSystem.CalculateDamage(data.DmgRate);
        int damage = CalGroupDamageBonus(temp, GetDamageGroupType(data));
        
        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return;

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", FireOrigin, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float projectileSpeed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;

        // 현재 플레이어 공격은 전부 직선 탄이다. 유도/관통탄이 필요할 때만 ProjectileController.Setup을 사용한다.
        controller.SetupStraight(damage, FireOrigin, targetPos, projectileSpeed);
    }

    public void FireBasicAttack()
    {
        float temp = _combatSystem.CalculateDamage(1.0f);
        int baseDmg = CalGroupDamageBonus(temp, DamageGroupType.None);
        
        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return;

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", FireOrigin, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        // 기본 공격도 직선 탄 전용 경로를 사용해 발사 시 객체 생성을 줄인다.
        controller.SetupStraight(baseDmg, FireOrigin, targetPos, _basicProjectileSpeed);
    }

    private bool TryFindAttackTargetPosition(out Vector2 targetPos)
    {
        targetPos = default;
        ResolveReferences();

        if (_firePoint == null && !_hasLoggedMissingFirePoint)
        {
            // firePoint 미할당 시 캐릭터(자기) 위치에서 발사하도록 폴백한다(공격을 건너뛰지 않음).
            Debug.LogWarning("[SkillSystem] FirePoint가 비어 있어 캐릭터 위치(자기 Transform)에서 발사합니다.", this);
            _hasLoggedMissingFirePoint = true;
        }

        if (_monsterSpawner == null)
        {
            if (!_hasLoggedMissingSpawner)
            {
                Debug.LogWarning("[SkillSystem] MonsterSpawner 참조를 찾지 못해 공격을 건너뜁니다.", this);
                _hasLoggedMissingSpawner = true;
            }

            return false;
        }

        if (!_monsterSpawner.TryFindAttackTarget(FireOrigin, out MonsterAI target))
            return false;

        targetPos = target.transform.position;
        return true;
    }

    private void ResolveReferences()
    {
        if (_monsterSpawner != null) return;
        if (_hasTriedResolveSpawner) return;

        _hasTriedResolveSpawner = true;
        _monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
    }

    private int CalGroupDamageBonus(float damage, DamageGroupType damageGroupType)
    {
        float baseDmg = damage;

        switch (damageGroupType)
        {
            // ToDo : 데미지 그룹 정해지면 그룹별 보정할것
            default:
                break;
        }
        
        return Mathf.FloorToInt(baseDmg);
    }

    private DamageGroupType GetDamageGroupType(Legacy_SkillData data)
    {
        // ToDo : 차후 데미지 그룹 데이터 정리 되면 로직 추가
        return DamageGroupType.None;
    }
}

[System.Serializable]
public struct ComboSkillEntry
{
    public int comboMultiple;
    public Legacy_SkillData data;
}
