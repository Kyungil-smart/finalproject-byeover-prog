// 담당자 : 정승우
// 설명   : 스킬 데이터 조회 + 투사체 생성

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System.Collections.Generic;
using UnityEngine;

// 수정자 : Codex
// 수정내용 : MonsterSpawner의 살아있는 몬스터 목록을 기준으로 실제 공격 타겟을 선택하도록 변경.
// 수정내용 : 모든 플레이어 공격을 직선 탄 전용 경로로 발사하여 발사 시 객체 생성을 줄임.

/// <summary>
/// 스킬 발사, 투사체 생성, 스킬 슬롯 관리를 담당한다.
/// CombatSystem이 "이 스킬 쏴라"고 하면 여기서 실제로 투사체를 만듦.
/// </summary>
public class SkillSystem : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private CombatSystem _combatSystem;

    [Header("발사 위치")]
    [Tooltip("투사체가 나가는 기준 위치 (캐릭터)")]
    [SerializeField] private Transform _firePoint;

    [Tooltip("살아있는 몬스터 목록과 공격 타겟 선택을 담당")]
    [SerializeField] private MonsterSpawner _monsterSpawner;

    [Header("직선 탄 설정")]
    [Tooltip("기본 공격 투사체 속도")]
    [SerializeField] private float _basicProjectileSpeed = 10f;

    // ---------- Private ----------
    // 인챈트로 획득한 스킬 슬롯. UnitType -> SkillData 매핑.
    private Dictionary<UnitType, SkillData> _sortSkills = new Dictionary<UnitType, SkillData>();

    // 콤보 스킬: 콤보 배수에 도달하면 자동 발동
    private List<ComboSkillEntry> _comboSkills = new List<ComboSkillEntry>();

    // 콤보 스킬 판정용 재사용 리스트 (GC 방지)
    private List<SkillData> _triggeredComboCache = new List<SkillData>(4);
    private bool _hasLoggedMissingFirePoint;
    private bool _hasLoggedMissingSpawner;
    private bool _hasTriedResolveSpawner;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

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

        int damage = _combatSystem.CalculateDamage(data.Dmg);
        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return;

        // 투사체 생성
        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        // 타격 방식에 따라 행동 결정
        float projectileSpeed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;

        controller.SetupStraight(damage, _firePoint.position, targetPos, projectileSpeed);
    }

    public void FireBasicAttack()
    {
        // 기본공격: 가장 가까운 몬스터한테 직선 투사체
        int baseDmg = _combatSystem.CalculateDamage(10);  // 기본 데미지
        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return;

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        controller.SetupStraight(baseDmg, _firePoint.position, targetPos, _basicProjectileSpeed);
    }

    // ---------- 투사체 행동 팩토리 ----------
    private IProjectileBehavior CreateBehavior(string hitRange, int speed)
    {
        // hitRange 문자열에 따라 다른 행동 생성 (OCP)
        switch (hitRange)
        {
            default:            return null;
        }
    }
    
    private bool TryFindAttackTargetPosition(out Vector2 targetPos)
    {
        targetPos = default;
        ResolveReferences();

        if (_firePoint == null)
        {
            if (!_hasLoggedMissingFirePoint)
            {
                Debug.LogWarning("[SkillSystem] FirePoint 참조가 비어 있어 공격을 건너뜁니다.", this);
                _hasLoggedMissingFirePoint = true;
            }

            return false;
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

        if (!_monsterSpawner.TryFindAttackTarget(_firePoint.position, out MonsterAI target))
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
}

[System.Serializable]
public struct ComboSkillEntry
{
    public int comboMultiple;   // 이 배수마다 발동 (예: 5면 5, 10, 15... 콤보에 발동)
    public SkillData data;
}
