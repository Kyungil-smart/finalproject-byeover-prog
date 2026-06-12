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

    // 자동공격 N회마다 발동하는 스킬 (인챈트 테이블 v1.03 '일반 스킬 인챈트' — 파이어브레스 등)
    private List<AutoAttackSkillEntry> _autoAttackSkills = new List<AutoAttackSkillEntry>();
    private List<Legacy_SkillData> _triggeredAutoCache = new List<Legacy_SkillData>(4);

    // 장판(hazard) 스킬: SkillID → 장판 설정. FireSkill이 투사체 대신 장판 경로로 분기한다.
    private Dictionary<int, HazardConfig> _hazardConfigs = new Dictionary<int, HazardConfig>();

    // 소환 스킬: SkillID → 소환 설정 (화염 정령). 장판과 마찬가지로 FireSkill에서 분기.
    private Dictionary<int, SummonConfig> _summonConfigs = new Dictionary<int, SummonConfig>();
    private List<FireSpirit> _activeSpirits = new List<FireSpirit>(2);

    // 장판 판정용 임시 버퍼 (TakeDamage→사망→몬스터 목록 변경에 대비한 스냅샷)
    private List<MonsterAI> _hazardHitBuffer = new List<MonsterAI>(16);

    // 기획 테이블 px 좌표계(화면 전체 폭 = 1440px) → 월드 변환용
    private const float TablePxFullWidth = 1440f;
    private Camera _cam;
    private bool _hasLoggedMissingFirePoint;
    private bool _hasLoggedMissingSpawner;
    private bool _hasTriedResolveSpawner;

    // 다발 스킬(PelletCount>1)의 발 간 간격(초). "빠르게 연속 발사" 연출용.
    private const float BurstShotInterval = 0.08f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    // ---------- 외부 주입 ----------
    /// <summary>발사 기준점(보통 PlayerView.FirePoint)을 연결한다.</summary>
    public void SetFirePoint(Transform firePoint)
    {
        _firePoint = firePoint;
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

    /// <summary>자동공격 N회마다 발동하는 스킬 등록 (파이어브레스 등 '일반 스킬 인챈트').</summary>
    public void RegisterAutoAttackSkill(int everyNAttacks, Legacy_SkillData data)
    {
        if (data == null) return;
        _autoAttackSkills.Add(new AutoAttackSkillEntry { everyNAttacks = everyNAttacks, data = data });
    }

    /// <summary>같은 스킬군(StandardID)의 기존 등록을 제거하고 새로 등록 (인챈트 레벨업 시 상위 레벨로 교체).</summary>
    public void ReplaceAutoAttackSkill(int everyNAttacks, Legacy_SkillData data)
    {
        if (data == null) return;
        for (int i = _autoAttackSkills.Count - 1; i >= 0; i--)
            if (_autoAttackSkills[i].data != null && _autoAttackSkills[i].data.StandardID == data.StandardID)
                _autoAttackSkills.RemoveAt(i);
        _autoAttackSkills.Add(new AutoAttackSkillEntry { everyNAttacks = everyNAttacks, data = data });
    }

    /// <summary>같은 스킬군(StandardID)의 기존 등록을 제거하고 새로 등록 (인챈트 레벨업 시 상위 레벨로 교체).</summary>
    public void ReplaceComboSkill(int comboMultiple, Legacy_SkillData data)
    {
        if (data == null) return;
        for (int i = _comboSkills.Count - 1; i >= 0; i--)
            if (_comboSkills[i].data != null && _comboSkills[i].data.StandardID == data.StandardID)
                _comboSkills.RemoveAt(i);
        _comboSkills.Add(new ComboSkillEntry { comboMultiple = comboMultiple, data = data });
    }

    /// <summary>장판형 스킬 등록. 등록된 SkillID는 FireSkill에서 투사체 대신 장판으로 발동된다.</summary>
    public void RegisterHazardSkill(int skillId, HazardConfig config)
    {
        _hazardConfigs[skillId] = config;
    }

    /// <summary>소환형 스킬 등록 (화염 정령). 등록된 SkillID는 FireSkill에서 소환으로 발동된다.</summary>
    public void RegisterSummonSkill(int skillId, SummonConfig config)
    {
        _summonConfigs[skillId] = config;
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

    /// <summary>자동공격 누적 횟수가 등록된 주기(N회)의 배수일 때 발동할 스킬 목록.</summary>
    public List<Legacy_SkillData> GetTriggeredAutoAttackSkills(int autoAttackCount)
    {
        _triggeredAutoCache.Clear();

        for (int i = 0; i < _autoAttackSkills.Count; i++)
        {
            int n = _autoAttackSkills[i].everyNAttacks;
            if (n > 0 && autoAttackCount > 0 && autoAttackCount % n == 0)
                _triggeredAutoCache.Add(_autoAttackSkills[i].data);
        }

        return _triggeredAutoCache;
    }

    // ---------- 발사 ----------
    public void FireSkill(Legacy_SkillData data, AttackType type)
    {
        if (data == null) return;

        // 소환 스킬(화염 정령 등)은 투사체 대신 소환 경로로 분기.
        if (_summonConfigs.TryGetValue(data.SkillID, out var summonCfg))
        {
            SummonSpirits(data, summonCfg);
            return;
        }

        // 장판 스킬(파이어브레스/대지 균열/메테오 등)은 장판 경로로 분기.
        if (_hazardConfigs.TryGetValue(data.SkillID, out var hazardCfg))
        {
            StartCoroutine(HazardRoutine(data, hazardCfg));
            return;
        }

        // PelletCount만큼 다발 발사(예: 화염 작렬 3발). 1발이면 즉시, 2발 이상이면 빠르게 연속 발사하며
        // 매 발마다 가장 가까운 적을 다시 탐색한다(앞 발에 적이 죽으면 다음 적으로). 기획 1-1 화염 작렬.
        int shots = Mathf.Max(1, data.PelletCount);
        if (shots <= 1)
        {
            FireOneProjectile(data);
            return;
        }

        StartCoroutine(FireBurstRoutine(data, shots));
    }

    // ---------- 장판 (hazard) ----------
    // 인챈트 테이블 v1.03 Hit_Scope=hazard: 지정 위치에 PelletCount회 광역 타격.
    private System.Collections.IEnumerator HazardRoutine(Legacy_SkillData data, HazardConfig cfg)
    {
        ResolveReferences();
        if (_monsterSpawner == null || _firePoint == null) yield break;

        Vector2 sizeWorld = new Vector2(PxToWorld(cfg.widthPx), PxToWorld(cfg.heightPx));
        int pulses = Mathf.Max(1, data.PelletCount);

        // 파이어브레스: 시전 시점의 최단거리 타겟 위치에 고정(수정 소환 연출) — 펄스마다 재탐색하지 않는다.
        Vector2 fixedCenter = default;
        if (cfg.placement == HazardPlacement.NearestTarget)
        {
            if (!_monsterSpawner.TryFindAttackTarget(_firePoint.position, out MonsterAI t))
                yield break;
            fixedCenter = t.transform.position;
        }

        for (int i = 0; i < pulses; i++)
        {
            Vector2 center;
            switch (cfg.placement)
            {
                case HazardPlacement.PlayerFront:
                    // 전방 전진 스윕: 에이프릴 앞에서 시작해 펄스마다 띠가 한 칸씩 위(몬스터 쪽)로 전진.
                    // (기획서 3-1-3 '고정 위치에 범위 공격이 순차적으로 발동' + 레퍼런스 [플레임 스윕] 해석)
                    center = new Vector2(CamCenterX(),
                        _firePoint.position.y + sizeWorld.y * 0.5f + 0.3f + i * sizeWorld.y);
                    break;

                case HazardPlacement.RandomTarget:
                    if (!TryPickRandomAliveMonster(out MonsterAI randomTarget))
                        yield break;
                    center = randomTarget.transform.position;
                    break;

                default: // NearestTarget
                    center = fixedCenter;
                    break;
            }

            DealHazardDamage(data, center, sizeWorld);
            SpawnHazardFlash(center, sizeWorld, cfg.flashColor);

            if (i < pulses - 1)
                yield return new WaitForSeconds(cfg.pulseInterval);
        }
    }

    private void DealHazardDamage(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld)
    {
        float temp = _combatSystem.CalculateDamage(data.DmgRate);
        int damage = CalGroupDamageBonus(temp, GetDamageGroupType(data));

        Vector2 half = sizeWorld * 0.5f;
        var alive = _monsterSpawner.AliveMonsters;

        _hazardHitBuffer.Clear();
        for (int i = 0; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m == null || !m.gameObject.activeInHierarchy) continue;

            Vector2 p = m.transform.position;
            if (Mathf.Abs(p.x - center.x) <= half.x && Mathf.Abs(p.y - center.y) <= half.y)
                _hazardHitBuffer.Add(m);
        }

        for (int i = 0; i < _hazardHitBuffer.Count; i++)
            _hazardHitBuffer[i].TakeDamage(damage);
    }

    private bool TryPickRandomAliveMonster(out MonsterAI picked)
    {
        picked = null;
        var alive = _monsterSpawner.AliveMonsters;

        _hazardHitBuffer.Clear();
        for (int i = 0; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m != null && m.gameObject.activeInHierarchy)
                _hazardHitBuffer.Add(m);
        }

        if (_hazardHitBuffer.Count == 0) return false;
        picked = _hazardHitBuffer[Random.Range(0, _hazardHitBuffer.Count)];
        return true;
    }

    // 장판 표시(플레이스홀더): 반투명 사각형을 잠깐 띄운다.
    private void SpawnHazardFlash(Vector2 center, Vector2 sizeWorld, Color color)
    {
        var go = new GameObject("HazardFlash");
        go.transform.position = center;
        go.transform.localScale = new Vector3(sizeWorld.x, sizeWorld.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square();
        sr.color = color;
        sr.sortingOrder = 40;

        Destroy(go, 0.3f);
    }

    // ---------- 소환 (화염 정령) ----------
    private void SummonSpirits(Legacy_SkillData data, SummonConfig cfg)
    {
        if (_firePoint == null) return;

        // 재시전 시 기존 정령은 제거 후 새로 소환 (지속시간 갱신)
        for (int i = 0; i < _activeSpirits.Count; i++)
            if (_activeSpirits[i] != null) Destroy(_activeSpirits[i].gameObject);
        _activeSpirits.Clear();

        int count = Mathf.Max(1, data.PelletCount); // 정령 수 (테이블 ActiveCount=2)
        for (int i = 0; i < count; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            Vector3 pos = _firePoint.position + new Vector3(side * 0.8f, 0.15f, 0f);

            var go = new GameObject("FireSpirit");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Square();
            sr.color = new Color(1f, 0.45f, 0.1f); // 주황 (화염 정령 플레이스홀더)
            sr.sortingOrder = 60;

            var spirit = go.AddComponent<FireSpirit>();
            spirit.Init(this, cfg.castSkill, cfg.lifetime, cfg.castInterval);
            _activeSpirits.Add(spirit);
        }
    }

    /// <summary>지정 위치에서 한 발 발사. 소환수처럼 플레이어 발사점이 아닌 곳에서 쏠 때 사용.</summary>
    public void FireProjectileFrom(Legacy_SkillData data, Vector3 origin)
    {
        if (data == null) return;
        ResolveReferences();
        if (_monsterSpawner == null) return;
        if (!_monsterSpawner.TryFindAttackTarget(origin, out MonsterAI target)) return;

        float temp = _combatSystem.CalculateDamage(data.DmgRate);
        int damage = CalGroupDamageBonus(temp, GetDamageGroupType(data));

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", origin, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float speed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;
        controller.SetupStraight(damage, origin, target.transform.position, speed);
    }

    // ---------- px → world 변환 ----------
    private float PxToWorld(float px)
    {
        EnsureCamera();
        if (_cam == null) return px / 100f; // 카메라를 못 찾으면 대략치

        float worldFullWidth = 2f * _cam.orthographicSize * _cam.aspect;
        return px / TablePxFullWidth * worldFullWidth;
    }

    private float CamCenterX()
    {
        EnsureCamera();
        return _cam != null ? _cam.transform.position.x : 0f;
    }

    private void EnsureCamera()
    {
        if (_cam == null) _cam = Camera.main;
    }

    private System.Collections.IEnumerator FireBurstRoutine(Legacy_SkillData data, int shots)
    {
        for (int i = 0; i < shots; i++)
        {
            FireOneProjectile(data);
            yield return new WaitForSeconds(BurstShotInterval);
        }
    }

    // 한 발 발사 : 데미지 계산 → 가장 가까운 적 탐색 → 직선 탄.
    private void FireOneProjectile(Legacy_SkillData data)
    {
        // 데미지 계산
        float temp = _combatSystem.CalculateDamage(data.DmgRate);
        int damage = CalGroupDamageBonus(temp, GetDamageGroupType(data));

        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return;

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float projectileSpeed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;

        // 현재 플레이어 공격은 전부 직선 탄이다. 유도/관통탄이 필요할 때만 ProjectileController.Setup을 사용한다.
        controller.SetupStraight(damage, _firePoint.position, targetPos, projectileSpeed);
    }

    /// <returns>실제로 발사했으면 true. 타겟 부재 등으로 스킵하면 false (자동공격 카운트는 발사 성공만 센다).</returns>
    public bool FireBasicAttack()
    {
        float temp = _combatSystem.CalculateDamage(1.0f);
        int baseDmg = CalGroupDamageBonus(temp, DamageGroupType.None);

        if (!TryFindAttackTargetPosition(out Vector2 targetPos))
            return false;

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return false;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return false;

        // 기본 공격도 직선 탄 전용 경로를 사용해 발사 시 객체 생성을 줄인다.
        controller.SetupStraight(baseDmg, _firePoint.position, targetPos, _basicProjectileSpeed);
        return true;
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
        {
            Debug.Log("[전투진단] 발사 취소: 살아있는 공격 타겟(몬스터)이 없습니다.");
            return false;
        }

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

[System.Serializable]
public struct AutoAttackSkillEntry
{
    public int everyNAttacks;
    public Legacy_SkillData data;
}

/// <summary>장판 배치 방식 (인챈트 테이블 v1.03 RequiredValue_4 대응)</summary>
public enum HazardPlacement
{
    NearestTarget,  // 최단거리 타겟 위치에 고정 (파이어브레스)
    PlayerFront,    // 플레이어 전방 전체 폭 (대지 균열)
    RandomTarget,   // 매 펄스 랜덤 타겟 (메테오)
}

public struct HazardConfig
{
    public HazardPlacement placement;
    public float widthPx;        // 기획 테이블 px 좌표계 (화면 폭 1440px 기준)
    public float heightPx;
    public float pulseInterval;  // 다회 타격 간격(초)
    public Color flashColor;
}

public struct SummonConfig
{
    public Legacy_SkillData castSkill; // 정령이 시전할 스킬 (화염 작렬)
    public float lifetime;             // 정령 지속시간(초)
    public float castInterval;         // 시전 주기(초)
}
