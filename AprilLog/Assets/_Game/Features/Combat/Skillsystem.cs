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
    [Tooltip("새 인챈트 테이블 기반 데미지/투사체/범위/효과 계산기 (없거나 매핑 데미지 0이면 레거시 공식으로 폴백)")]
    [SerializeField] private EnchantCalculator _enchantCalculator;
    private bool _hasTriedResolveEnchant;

    [Header("발사 위치")]
    [Tooltip("투사체가 생성되는 기준 위치")]
    [SerializeField] private Transform _firePoint;

    [Tooltip("살아있는 몬스터 목록과 공격 타겟 선택을 담당")]
    [SerializeField] private MonsterSpawner _monsterSpawner;

    [Header("직선 탄 설정")]
    [Tooltip("기본 공격 투사체 속도")]
    [SerializeField] private float _basicProjectileSpeed = 10f;

    [Header("기본공격 VFX (인스펙터에서 프리팹 드래그)")]
    [Tooltip("자동공격 투사체 VFX — AutoSkill Variant 프리팹을 여기에 드래그")]
    [SerializeField] private GameObject _autoAttackVfx;
    [Tooltip("소트(정렬) 공격 투사체 VFX — SortSkill Variant 프리팹을 여기에 드래그")]
    [SerializeField] private GameObject _sortAttackVfx;
    [Tooltip("기본공격 VFX 스케일")]
    [SerializeField] private float _basicAttackVfxScale = 1f;
    [Tooltip("기본공격 VFX 진행방향 회전 보정(도). 뒤집히면 180")]
    [SerializeField] private float _basicAttackVfxTrimDeg = 0f;

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

    // 불 속성 VFX 라이브러리 (Resources에서 1회 로드. 없어도 사각형 폴백으로 동작)
    private FireSkillVfxLibrary _vfx;
    private bool _vfxLoadTried;

    private FireSkillVfxLibrary Vfx
    {
        get
        {
            if (!_vfxLoadTried)
            {
                _vfxLoadTried = true;
                _vfx = Resources.Load<FireSkillVfxLibrary>("FireSkillVfxLibrary");
                if (_vfx == null)
                    Debug.LogWarning("[SkillSystem] Resources/FireSkillVfxLibrary.asset 을 찾지 못해 VFX 없이(사각형 폴백) 동작합니다.");
            }
            return _vfx;
        }
    }
    // 번개 속성 VFX 라이브러리 (기획 v2.02. 없어도 사각형 폴백)
    private LightningSkillVfxLibrary _lightningVfx;
    private bool _lightningVfxLoadTried;
    private LightningSkillVfxLibrary LightningVfx
    {
        get
        {
            if (!_lightningVfxLoadTried)
            {
                _lightningVfxLoadTried = true;
                _lightningVfx = Resources.Load<LightningSkillVfxLibrary>("LightningSkillVfxLibrary");
            }
            return _lightningVfx;
        }
    }

    private WindSkillVfxLibrary _windVfx;
    private bool _windVfxLoadTried;
    private WindSkillVfxLibrary WindVfx
    {
        get
        {
            if (!_windVfxLoadTried)
            {
                _windVfxLoadTried = true;
                _windVfx = Resources.Load<WindSkillVfxLibrary>("WindSkillVfxLibrary");
            }
            return _windVfx;
        }
    }

    private WaterSkillVfxLibrary _waterVfx;
    private bool _waterVfxLoadTried;
    private WaterSkillVfxLibrary WaterVfx
    {
        get
        {
            if (!_waterVfxLoadTried)
            {
                _waterVfxLoadTried = true;
                _waterVfx = Resources.Load<WaterSkillVfxLibrary>("WaterSkillVfxLibrary");
            }
            return _waterVfx;
        }
    }

    /// <summary>번개 스킬 StandardID로 VFX 프리팹+스케일 해결. 라이브러리/프리팹 없으면 false.</summary>
    private bool TryGetLightningVfx(Legacy_SkillData data, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        var lib = LightningVfx;
        if (lib == null || data == null) return false;
        switch (data.StandardID)
        {
            case 401: prefab = lib.orbPrefab; scale = lib.orbScale; break;                 // 구형 번개
            case 404: prefab = lib.thunderboltPrefab; scale = lib.thunderboltScale; break;  // 벼락
            case 405: prefab = lib.laserPrefab; scale = lib.laserScale; break;              // 뇌격
            // 402 사슬·403 방전은 전용 루틴(LightningChain/DischargeRoutine)에서 아크 파티클로 직접 그림 → 여기서 CFXR 막 안 씀.
        }
        return prefab != null;
    }

    /// <summary>바람 하자드 StandardID로 VFX 프리팹+스케일 해결 (장판형: 돌풍 303·허리케인 304·부메랑 306). 투사체형(301/302/305)은 스킨 게이트에서 직접 처리.</summary>
    private bool TryGetWindVfx(Legacy_SkillData data, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        var lib = WindVfx;
        if (lib == null || data == null) return false;
        switch (data.StandardID)
        {
            case 303: prefab = lib.gustHazard; scale = lib.gustHazardScale; break;            // 돌풍 (전방 단발)
            case 304: prefab = lib.hurricaneHazard; scale = lib.hurricaneHazardScale; break;  // 허리케인 (지속 소용돌이)
            case 306: prefab = lib.boomerangHazard; scale = lib.boomerangHazardScale; break;  // 부메랑 (백업 — skill_data에 306 행 없어 현재 미발동)
        }
        return prefab != null;
    }

    /// <summary>물 하자드 StandardID로 VFX 프리팹+스케일 해결 (장판형: 탄환세례 202·급류 203). 201/204/205는 전용 루틴에서 직접 소환.</summary>
    private bool TryGetWaterVfx(Legacy_SkillData data, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        var lib = WaterVfx;
        if (lib == null || data == null) return false;
        switch (data.StandardID)
        {
            case 202: prefab = lib.bulletShowerVfx; scale = lib.bulletShowerScale; break;  // 탄환 세례
            case 203: prefab = lib.torrentVfx;      scale = lib.torrentScale;      break;  // 급류
        }
        return prefab != null;
    }

    private bool TryGetIceVfx(Legacy_SkillData data, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        var lib = Vfx;
        if (lib == null || data == null) return false;
        switch (data.StandardID)
        {
            case 501: prefab = lib.iceCurtainVfx; scale = lib.iceCurtainScale; break;   // 마칭 아이스
            case 503: prefab = lib.snowFreezeVfx; scale = lib.snowFreezeScale; break;    // 빙결 지대
            case 504: prefab = lib.iceStormVfx;   scale = lib.iceStormScale;   break;    // 얼음 결정
            case 505: prefab = lib.absoluteZeroVfx; scale = lib.absoluteZeroScale; break; // 절대영도 (Iceshower 전용)
        }
        return prefab != null;
    }

    private bool _hasLoggedMissingFirePoint;
    private bool _hasLoggedMissingSpawner;
    private bool _hasTriedResolveSpawner;

    // 다발 스킬(PelletCount>1)의 발 간 간격(초). "빠르게 연속 발사" 연출용.
    private const float BurstShotInterval = 0.08f;
    // 화염 작렬/화염 정령(StandardID 102)의 발 간 간격(초). QA 요청: 0.25초로 느리게.
    private const float FlameBurstShotInterval = 0.25f;

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
        // 투사체 증가 인챈트: 추가 발사 수(보유 없으면 0 → 무변화). 추가탄 간격(pelletGap)·데미지감소(subPelletDmgReduce)는 후속 정교화.
        ResolveReferences();
        if (_enchantCalculator != null && data != null)
        {
            int addShots = _enchantCalculator.ProjectileAddCalculate(MapToNewDamageId(data.SkillID), out _, out _);
            if (addShots > 0) shots += addShots;
        }
        if (shots <= 1)
        {
            FireOneProjectile(data, type);
            return;
        }

        StartCoroutine(FireBurstRoutine(data, shots, type));
    }

    // ---------- 장판 (hazard) ----------
    // 인챈트 테이블 v1.03 Hit_Scope=hazard: 지정 위치에 PelletCount회 광역 타격.
    private System.Collections.IEnumerator HazardRoutine(Legacy_SkillData data, HazardConfig cfg)
    {
        ResolveReferences();
        if (_monsterSpawner == null || _firePoint == null) yield break;

        Vector2 sizeWorld = new Vector2(PxToWorld(cfg.widthPx), PxToWorld(cfg.heightPx));
        // 인챈트 범위 확장(HitSize_X/Y). 보유 인챈트 없으면 1f라 무변화. DealHazardDamage·VFX 둘 다 sizeWorld 기반이라 동시 적용됨.
        if (_enchantCalculator != null)
        {
            _enchantCalculator.SkillAreaExtensionCalculate(MapToNewDamageId(data.SkillID), out float xRate, out float yRate);
            sizeWorld = new Vector2(sizeWorld.x * xRate, sizeWorld.y * yRate);
        }
        int pulses = Mathf.Max(1, data.PelletCount);

        // 파이어브레스: 시전 시점의 최단거리 타겟 위치에 고정(수정 소환 연출) — 펄스마다 재탐색하지 않는다.
        Vector2 fixedCenter = default;
        if (cfg.placement == HazardPlacement.NearestTarget)
        {
            // 벼락(404)은 엘리트/보스 우선 타겟, 그 외는 최단거리 (기획 4-4)
            MonsterAI t;
            bool found = data.StandardID == 404
                ? _monsterSpawner.TryFindPriorityTarget(_firePoint.position, out t)
                : _monsterSpawner.TryFindAttackTarget(_firePoint.position, out t);
            if (!found)
                yield break;
            fixedCenter = t.transform.position;
        }

        // 랜덤 타겟: 한 발동(볼리) 안에서는 같은 몬스터를 두 번 안 뽑는다 (기획 3-2-3 '타겟 수만큼 랜덤 타겟').
        // 같은 자리에 메테오 2발이 떨어져 '연속 2차 폭발'로 보이는 문제도 함께 방지.
        HashSet<MonsterAI> volleyPicked = cfg.placement == HazardPlacement.RandomTarget
            ? new HashSet<MonsterAI>() : null;

        // 파이어브레스는 수정구 1개가 N발을 쏘는 시퀀스 — 펄스 루프를 우회하고 전용 루틴이 0.5초×N 타이밍을 소유한다.
        if (cfg.style == HazardStyle.FireBreath)
        {
            StartCoroutine(FireBreathRoutine(data, fixedCenter, sizeWorld, pulses));
            yield break;
        }

        // 구형 번개: 지속 VFX 1개를 깔고 펄스마다 데미지. 펄스 루프 우회(전용 루틴이 타이밍 소유).
        if (cfg.style == HazardStyle.LightningHeld)
        {
            StartCoroutine(LightningHeldRoutine(data, fixedCenter, sizeWorld, pulses, cfg.pulseInterval));
            yield break;
        }

        // 방전: 가운데 번개막 + 양옆 구슬 2개를 깔고 지속 데미지. 펄스 루프 우회.
        if (cfg.style == HazardStyle.LightningDischarge)
        {
            StartCoroutine(LightningDischargeRoutine(data, fixedCenter, sizeWorld, pulses, cfg.pulseInterval));
            yield break;
        }

        // 사슬 번개: 에이프릴→타겟 전기선 + 몬스터 타격 이펙트. 펄스 루프 우회.
        if (cfg.style == HazardStyle.LightningChain)   // 402 에너지 볼(구가 적들 사이를 튕겨다니는 이동 투사체)
        {
            StartCoroutine(EnergyBallRoutine(data, fixedCenter, sizeWorld, pulses, cfg.pulseInterval));
            yield break;
        }

        // 허리케인: center에 소용돌이 VFX를 지속 생성하고 펄스마다 데미지. 펄스 루프 우회.
        if (cfg.style == HazardStyle.WindVortex)
        {
            StartCoroutine(WindHeldRoutine(data, fixedCenter, sizeWorld, pulses, cfg.pulseInterval));
            yield break;
        }

        // 절대영도: 최단거리 타겟 중심에 Iceshower 1회 + 2초간 0.2초마다 데미지(지속 빙벽). 펄스 루프 우회.
        if (cfg.style == HazardStyle.AbsoluteZero)
        {
            StartCoroutine(AbsoluteZeroRoutine(data, fixedCenter, sizeWorld));
            yield break;
        }

        // 마칭 아이스: 플레이어 X에서 위로 전진하는 좁은 정사각 마칭(PelletCount칸). 펄스 루프 우회.
        if (cfg.style == HazardStyle.MarchingIce)
        {
            StartCoroutine(MarchingIceRoutine(data, sizeWorld, pulses, cfg.pulseInterval));
            yield break;
        }

        // 얼음 결정: 플레이어에서 생성→타겟 방향으로 천천히 전진하며 5초 다단히트. 펄스 루프 우회.
        if (cfg.style == HazardStyle.IceCrystalMoving)
        {
            StartCoroutine(IceCrystalRoutine(data, sizeWorld));
            yield break;
        }

        // 물 폭탄: 장벽 윗변에서 물 공이 타겟으로 날아가 착탄 시 폭발(범위)+슬로우. 펄스 루프 우회.
        if (cfg.style == HazardStyle.WaterBombImpact)
        {
            StartCoroutine(WaterBombRoutine(data, fixedCenter, sizeWorld));
            yield break;
        }

        // 파도 소환: 타겟 X·장벽 Y에서 위로 솟구치며 전진하는 파도 장판. 펄스 루프 우회.
        if (cfg.style == HazardStyle.WaveRise)
        {
            StartCoroutine(WaveSummonRoutine(data, fixedCenter, sizeWorld));
            yield break;
        }

        // 하이드로 펌프: 전투구역 중앙 고정 세로 컬럼에 2초간 0.2초마다 틱. 펄스 루프 우회.
        if (cfg.style == HazardStyle.WaterBeamSustain)
        {
            StartCoroutine(HydroPumpRoutine(data, sizeWorld));
            yield break;
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
                    if (!TryPickRandomAliveMonster(out MonsterAI randomTarget, volleyPicked))
                        yield break;
                    volleyPicked.Add(randomTarget);
                    center = randomTarget.transform.position;
                    break;

                case HazardPlacement.PlayerColumn:
                    // 뇌격: 플레이어 X 고정, 위(몬스터 스폰 방향)로 세로 컬럼이 뻗음. 박스 하단이 플레이어, 위로 sizeWorld.y.
                    center = new Vector2(
                        _firePoint != null ? _firePoint.position.x : CamCenterX(),
                        (_firePoint != null ? _firePoint.position.y : 0f) + sizeWorld.y * 0.5f);
                    break;

                default: // NearestTarget
                    center = fixedCenter;
                    break;
            }

            if (cfg.style == HazardStyle.MeteorStrike)
            {
                // 메테오: 타격마다 독립 시퀀스(마커→낙하→폭발+판정)를 병행 가동. 시작만 pulseInterval 간격.
                StartCoroutine(MeteorStrikeRoutine(data, center, sizeWorld));
            }
            else
            {
                DealHazardDamage(data, center, sizeWorld, i == pulses - 1); // 돌풍(303): 마지막 펄스=폭발+넉백
                // 대지 균열(StandardID 104)만 7열 크랙 행 / 번개 단발(벼락·뇌격)은 실제 VFX 단발 / 그 외는 기존 플래시.
                // (PlayerFront를 쓰는 물·얼음 골격 스킬 — 급류 203·파도소환 204·마칭아이스 501 — 은 불 크랙 대신 자기 색 플래시로 폴백)
                if (cfg.placement == HazardPlacement.PlayerFront && data.StandardID == 104)
                    SpawnEarthCrackRow(center, sizeWorld);
                else if (TryGetLightningVfx(data, out GameObject lvfx, out float lscale))
                {
                    var v = SpawnVfx(lvfx, center, lscale, 52);
                    if (v != null)
                    {
                        // 뇌격(405): 세로 레이저 빔 — looping 유지(지속 빔)하고 laserSustainSec만큼 길게 띄운다. startRotation 180 보정(위아래 정렬).
                        //   그 외 번개(벼락 등)는 기존대로 1회 재생.
                        if (data.StandardID == 405 && LightningVfx != null)
                        {
                            ApplyParticleRotation(v, LightningVfx.laserRotationDeg);
                            float sustain = LightningVfx.laserSustainSec > 0f ? LightningVfx.laserSustainSec : 2f;
                            Destroy(v, sustain);   // StopLoopingOneShot 안 함 → 루프 유지되어 빔이 지속 재생됨
                        }
                        else
                        {
                            StopLoopingOneShot(v); Destroy(v, ComputeOneShotLifetime(v));
                        }
                    }
                    else SpawnHazardFlash(center, sizeWorld, cfg.flashColor);
                }
                else if (TryGetWindVfx(data, out GameObject wvfx, out float wscale))
                {
                    // 바람 단발 하자드(돌풍): 펄스마다 center에 VFX 1개 소환.
                    var wv = SpawnVfx(wvfx, center, wscale, 52);
                    if (wv != null)
                    {
                        if (WindVfx != null) ApplyParticleRotation(wv, WindVfx.gustRotationDeg);
                        StopLoopingOneShot(wv); Destroy(wv, ComputeOneShotLifetime(wv));
                    }
                    else SpawnHazardFlash(center, sizeWorld, cfg.flashColor);
                }
                else if (TryGetIceVfx(data, out GameObject ivfx, out float iscale))
                {
                    // 얼음 하자드(마칭 501/빙결 503/얼음결정 504/절대영도 505): center에 VFX 1회 소환·재생.
                    var iv = SpawnVfx(ivfx, center, iscale, 52);
                    if (iv != null)
                    {
                        StopLoopingOneShot(iv);
                        foreach (var ps in iv.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
                        Destroy(iv, ComputeOneShotLifetime(iv));
                    }
                    else SpawnHazardFlash(center, sizeWorld, cfg.flashColor);
                }
                else if (TryGetWaterVfx(data, out GameObject wvfx2, out float wscale2))
                {
                    // 물 하자드(탄환세례 202·급류 203): center에 VFX 1회 소환·재생.
                    var wv2 = SpawnVfx(wvfx2, center, wscale2, 52);
                    if (wv2 != null)
                    {
                        if (WaterVfx != null) ApplyParticleRotation(wv2, data.StandardID == 203 ? WaterVfx.torrentRotationDeg : WaterVfx.bulletShowerRotationDeg);
                        StopLoopingOneShot(wv2);
                        foreach (var ps in wv2.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
                        Destroy(wv2, ComputeOneShotLifetime(wv2));
                    }
                    else SpawnHazardFlash(center, sizeWorld, cfg.flashColor);
                }
                else
                    SpawnHazardFlash(center, sizeWorld, cfg.flashColor);
            }

            if (i < pulses - 1)
                yield return new WaitForSeconds(cfg.pulseInterval);
        }
    }

    // 물 폭탄 (기획 1-1): 장벽 윗변 중앙에서 물 공이 최단거리 타겟으로 날아가 → 착탄 시 폭발 VFX + 범위 데미지 + 50% 슬로우(DealHazardDamage CC 201).
    private System.Collections.IEnumerator WaterBombRoutine(Legacy_SkillData data, Vector2 target, Vector2 sizeWorld)
    {
        var lib = WaterVfx;
        Vector2 start = _firePoint != null ? (Vector2)_firePoint.position : new Vector2(CamCenterX(), 0f);  // 장벽 윗변 중앙(=파이어포인트)
        Vector2 dir = target - start;

        // 물 공 투사체 VFX (start→target 직선 비행)
        GameObject ball = null;
        if (lib != null && lib.waterBallProjectile != null)
        {
            ball = SpawnVfx(lib.waterBallProjectile, start, lib.waterBallScale, 53);
            if (ball != null)
            {
                if (dir.sqrMagnitude > 0.0001f)
                    ApplyParticleRotation(ball, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + lib.waterBallRotationTrimDeg);
                // 인스턴스화 파티클은 Play On Awake가 안 잡혀 안 보일 수 있음 → 명시적 재생(폭발과 동일).
                foreach (var ps in ball.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
            }
        }
        const float flightTime = 0.25f;
        float t = 0f;
        while (t < flightTime)
        {
            t += Time.deltaTime;
            if (ball != null) ball.transform.position = (Vector3)Vector2.Lerp(start, target, t / flightTime);
            yield return null;
        }
        if (ball != null) Destroy(ball);

        // 착탄 폭발 VFX + 범위 데미지(슬로우는 DealHazardDamage CC 201)
        if (lib != null && lib.waterBombImpact != null)
        {
            var impact = SpawnVfx(lib.waterBombImpact, target, lib.waterBombImpactScale, 52);
            if (impact != null)
            {
                ApplyParticleRotation(impact, lib.waterBombImpactRotationDeg);
                StopLoopingOneShot(impact);
                foreach (var ps in impact.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
                Destroy(impact, ComputeOneShotLifetime(impact));
            }
        }
        else SpawnHazardFlash(target, sizeWorld, new Color(0.4f, 0.7f, 1f, 0.35f));
        DealHazardDamage(data, target, sizeWorld, true);   // 폭발 1회 판정(250x250) + 50% 슬로우
    }

    // 파도 소환 (기획 3-1, 나미 R식): 타겟 X · 장벽 Y에서 시작해 위로 솟구치며 전진, 지나는 적에게 다단히트 + 마지막 넉백.
    private System.Collections.IEnumerator WaveSummonRoutine(Legacy_SkillData data, Vector2 target, Vector2 sizeWorld)
    {
        var lib = WaterVfx;
        float baseY = _firePoint != null ? _firePoint.position.y : 0f;
        Vector2 cur = new Vector2(target.x, baseY + sizeWorld.y * 0.5f);   // 타겟 X · 장벽 Y에서 시작
        float speed = PxToWorld(250f);     // 나미 R식: 천천히 전진하며 적을 밀기 (느린 속도)
        const float duration = 2.0f;       // 느리게 더 멀리 전진(튜닝 가능)

        GameObject vfx = null;
        if (lib != null && lib.waveVfx != null)
        {
            vfx = SpawnVfx(lib.waveVfx, cur, lib.waveScale, 52);
            if (vfx != null)
            {
                // 기획 4-4: 프리팹 루트에 Euler(-10,0,-90) 베이크 — SpawnVfx가 identity로 생성하므로 코드에서 복원.
                vfx.transform.rotation = Quaternion.Euler(lib.waveRotationXDeg, 0f, lib.waveRotationDeg);
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
            }
        }
        else SpawnHazardFlash(cur, sizeWorld, new Color(0.4f, 0.7f, 1f, 0.35f));

        const float tickGap = 0.2f;
        float tickT = tickGap, elapsed = 0f;
        DealHazardDamage(data, cur, sizeWorld);
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt; tickT += dt;
            cur.y += speed * dt;                                  // 수직 상승
            if (vfx != null) vfx.transform.position = (Vector3)cur;
            if (tickT >= tickGap) { tickT = 0f; DealHazardDamage(data, cur, sizeWorld, elapsed + tickGap >= duration); }
            yield return null;
        }
        if (vfx != null) Destroy(vfx);
    }

    // 하이드로 펌프/아쿠아 스트림 (기획 3-2): 전투구역 중앙 고정 세로 컬럼에 Water_Beam 2초 지속 + 0.2초마다 틱.
    private System.Collections.IEnumerator HydroPumpRoutine(Legacy_SkillData data, Vector2 sizeWorld)
    {
        var lib = WaterVfx;
        Vector2 center = new Vector2(CamCenterX(), (_firePoint != null ? _firePoint.position.y : 0f) + sizeWorld.y * 0.5f);

        GameObject beam = null;
        if (lib != null && lib.hydroBeamVfx != null)
        {
            beam = SpawnVfx(lib.hydroBeamVfx, center, lib.hydroBeamScale, 52);
            if (beam != null)
            {
                ApplyParticleRotation(beam, lib.hydroBeamRotationDeg);
                // 지속 빔: looping 유지(StopLoopingOneShot 안 함) → 2초 동안 재생.
                foreach (var ps in beam.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
            }
        }
        else SpawnHazardFlash(center, sizeWorld, new Color(0.4f, 0.7f, 1f, 0.35f));

        const float duration = 2f;     // 기획 3-2: 2초 지속
        const float tickGap = 0.2f;    // 0.2초 간격
        float elapsed = 0f, tickT = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt; tickT += dt;
            if (tickT >= tickGap) { tickT = 0f; DealHazardDamage(data, center, sizeWorld); }
            yield return null;
        }
        if (beam != null) Destroy(beam);
    }

    // 메테오 시퀀스 (기획 4-4-3): 착탄 지점에 조준원이 먼저 깔리고, 하늘에서 운석이 그 위로 낙하 → 착탄 폭발
    //   ① 착탄 지점(땅)에 조준원(Flame_ellipse) 생성 → 낙하 끝까지 루프 (0.5초 예고 = 땅 위 위험 표시)
    //   ② 하늘에서 몸체(Fireball_loop_2)가 조준원 중심으로 사선 낙하
    //   ③ 충돌 순간 조준원·몸체 동시 삭제 (기획 4-4-3 4번)
    //   ④ 착탄 폭발(explosion_5) + 고정 좌표에 데미지 판정 (기획 3-2-3)
    private System.Collections.IEnumerator MeteorStrikeRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld)
    {
        var lib = Vfx;
        float telegraph = lib != null ? lib.meteorTelegraph : 0.5f;
        float fallTime = lib != null ? lib.meteorFallTime : 0.35f;
        float fallHeight = lib != null ? lib.meteorFallHeight : 3.5f;
        float fallOffsetX = lib != null ? lib.meteorFallOffsetX : 1.2f;

        // 투사체는 착탄 지점의 오른쪽 위 '하늘'에서 출발 → 왼쪽 사선으로 낙하 (기획 4-4-1 Fireball_loop_2 스폰 좌표)
        Vector2 skyPos = center + new Vector2(fallOffsetX, fallHeight);

        // ① 착탄 지점(땅)에 조준원(Flame_ellipse) 생성. 낙하 끝까지 살려두며 루프시켜 '위험 예고'로 쓴다 (기획 4-4-3)
        GameObject marker = SpawnVfx(lib != null ? lib.meteorMarker : null, center, lib != null ? lib.meteorMarkerScale : 1f, 54);
        yield return new WaitForSeconds(telegraph);

        // ② 하늘에서 몸체(Fireball_loop_2)가 조준원 중심점을 향해 사선 낙하
        GameObject ball = SpawnVfx(lib != null ? lib.meteorBall : null, skyPos, lib != null ? lib.meteorBallScale : 1f, 55);
        if (ball != null)
        {
            // 몸체 이펙트가 '머리 왼쪽' 방향으로 제작돼 있어, 실제 낙하 방향으로 텍스처를 자동 회전.
            // (뷰 정렬 빌보드 파티클이라 transform 회전은 안 먹고 startRotation을 돌려야 함)
            // 머리(-1,0)를 낙하 방향 d로 보내는 시계방향 회전: θ = atan2(d.y, -d.x). 수직 낙하면 -90°.
            Vector2 dir = (center - skyPos).normalized;
            float autoDeg = Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg;
            float trimDeg = lib != null ? lib.meteorBallRotationTrimDeg : 0f;
            ApplyParticleRotation(ball, autoDeg + trimDeg);

            Vector3 from = skyPos;
            Vector3 to = center;
            float t = 0f;
            while (t < fallTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fallTime);
                ball.transform.position = Vector3.Lerp(from, to, k * k); // 가속 낙하
                yield return null;
            }
            Destroy(ball);
        }
        else
        {
            yield return new WaitForSeconds(fallTime); // VFX 미연결이어도 타이밍은 유지
        }

        // ③ 투사체가 조준원 중심에 충돌하는 순간 조준원·몸체 동시 삭제 (기획 4-4-3 4번)
        if (marker != null) Destroy(marker);

        // ④ 착탄: 몸체가 사라진 뒤 한 박자 쉬고 폭발 (폭발이 몸체를 가리는 것 방지) + 데미지 판정
        float explosionDelay = lib != null ? lib.meteorExplosionDelay : 0.05f;
        if (explosionDelay > 0f)
            yield return new WaitForSeconds(explosionDelay);

        // 폭발 비주얼 크기를 '실제 공격 범위'에 연동: 공격 범위가 (인챈트 등으로) 커지면 폭발도 비례 확대.
        // meteorExplosionScale은 기준 범위(테이블 350px)에서 폭발이 공격 범위와 겹쳐 보이도록 보정하는 값.
        float areaFactor = sizeWorld.x / Mathf.Max(0.01f, PxToWorld(MeteorRefAreaPx));
        float explScale = (lib != null ? lib.meteorExplosionScale : 1f) * areaFactor;

        // explosion_5는 착탄 지점에서 위로 Position y(기획 4-4-1: 130px)만큼 올려 생성한다.
        // 범위가 커지면 이 오프셋도 폭발 크기와 같은 비율(areaFactor)로 커져 정렬이 유지된다(기획 4-4-2: 1.1배 → 130→143px).
        float explOffsetY = PxToWorld(lib != null ? lib.meteorExplosionOffsetYPx : 130f) * areaFactor;
        Vector2 explPos = center + new Vector2(0f, explOffsetY);

        GameObject expl = SpawnVfx(lib != null ? lib.meteorExplosion : null, explPos, explScale, 56);
        if (expl != null)
        {
            StopLoopingOneShot(expl);   // explosion_5는 루프 파티클 — 1회 재생으로 전환
            // 제거 시점을 '파티클 수명 + 여유'로 계산해 2번째 사이클(duration 경과) 시작 전에 반드시 제거.
            // (예전 고정 1.2초는 사이클 1.0초보다 길어서 2차 폭발이 잠깐 비치는 원인이 됐음)
            Destroy(expl, ComputeOneShotLifetime(expl));
        }
        else
        {
            SpawnHazardFlash(center, sizeWorld, new Color(1f, 0.15f, 0.05f, 0.45f)); // VFX 미연결 폴백
        }

        // 보정용: 실제 공격 범위 표시 (폭발 크기를 범위에 맞춘 뒤 SO에서 끌 것)
        if (lib != null && lib.debugShowHitArea)
            SpawnHazardFlash(center, sizeWorld, new Color(1f, 1f, 1f, 0.18f));

        DealHazardDamage(data, center, sizeWorld);
    }

    // 파이어브레스 시퀀스 (기획 4-1): 타겟 발밑에 수정구를 깔아 루프 → 0.5초마다 최단 적 방향으로 화염 단발 분사 N발 → N발 후 수정구 삭제.
    private System.Collections.IEnumerator FireBreathRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, int flameCount)
    {
        var lib = Vfx;
        float crystalScale = lib != null ? lib.fireBreathCrystalScale : 0.2f;  // 구체: 범위 스탯과 무관한 고정 크기
        // 브레스(화염)만 '스킬 범위 스탯'에 연동: 실제 공격은 화염이 하므로, 피격 범위(sizeWorld)가 버프/인챈트로 커지면
        // 화염 크기도 같은 비율(flameAreaFactor)로 확대된다(기획 4-1-3). 구체는 여기 안 곱해 고정.
        float flameAreaFactor = sizeWorld.x / Mathf.Max(0.01f, PxToWorld(FireBreathRefAreaPx));
        float flameScale = (lib != null ? lib.fireBreathFlameScale : 0.4f) * flameAreaFactor;
        float flameInterval = lib != null ? lib.fireBreathFlameInterval : 0.5f;
        float flameOffsetY = PxToWorld(lib != null ? lib.fireBreathFlameYOffsetPx : 400f);
        float flameTrim = lib != null ? lib.fireBreathFlameRotationTrimDeg : 0f;

        // ① 타겟 발밑에 수정구 생성 → 시퀀스 끝까지 루프 유지 (기획 4-1-4 #1)
        GameObject crystal = SpawnVfx(lib != null ? lib.fireBreathCrystal : null, center, crystalScale, 53);

        for (int n = 0; n < flameCount; n++)
        {
            // ② 발사 시점의 '현재' 최단 적을 수정구 중심 기준으로 재조회해 조준 (적 이동/사망 대응, 기획 4-1-2)
            Vector2 aimNorm = Vector2.up; // 적 전멸 시 위쪽 폴백
            if (_monsterSpawner != null && _monsterSpawner.TryFindAttackTarget(center, out MonsterAI tgt))
            {
                Vector2 d = (Vector2)tgt.transform.position - center;
                if (d.sqrMagnitude > 0.0001f) aimNorm = d.normalized;
            }

            // ③ 화염 단발 분사 — 수정구에서 적 방향으로 flameOffsetY만큼 떨어진 곳에 적을 향해 회전 (피벗 공유, 기획 4-1-2/4-1-4)
            Vector2 flamePos = center + aimNorm * flameOffsetY;
            GameObject flame = SpawnVfx(lib != null ? lib.fireBreathFlame : null, flamePos, flameScale, 56);
            if (flame != null)
            {
                StopLoopingOneShot(flame);   // 루프 프리팹 → 1회 재생
                // Fire_Asset 화염은 오른쪽(+X) 제작 → -atan2(d.y,d.x)로 적 방향 정렬 (화염작렬과 동일). 뒤집히면 trim에 ±90/180.
                float autoDeg = -Mathf.Atan2(aimNorm.y, aimNorm.x) * Mathf.Rad2Deg;
                ApplyParticleRotation(flame, autoDeg + flameTrim);
                Destroy(flame, ComputeOneShotLifetime(flame));
            }
            else
            {
                SpawnHazardFlash(center, sizeWorld, new Color(1f, 0.3f, 0.05f, 0.35f)); // VFX 미연결 폴백
            }

            // ④ 데미지 판정 (발사마다 1회 = 기획 '3회 대미지')
            DealHazardDamage(data, center, sizeWorld);

            if (n < flameCount - 1)
                yield return new WaitForSeconds(flameInterval);
        }

        // ⑤ 정해진 횟수 후 수정구 삭제 (기획 4-1-4 #5)
        if (crystal != null) Destroy(crystal);
    }

    // 지속형 번개 장판(구형 번개): VFX 1개를 center에 생성(루프 유지) → pulses회 데미지(간격 interval) → 한 박자 뒤 삭제.
    private System.Collections.IEnumerator LightningHeldRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, int pulses, float interval)
    {
        GameObject vfx = null;
        if (TryGetLightningVfx(data, out GameObject prefab, out float scale))
            vfx = SpawnVfx(prefab, center, scale, 52);
        else
            SpawnHazardFlash(center, sizeWorld, new Color(1f, 0.95f, 0.3f, 0.4f)); // VFX 미연결 폴백

        for (int i = 0; i < pulses; i++)
        {
            DealHazardDamage(data, center, sizeWorld);
            if (i < pulses - 1)
                yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(0.3f); // 마지막 데미지 후 VFX 애니메이션 여유
        if (vfx != null) Destroy(vfx);
    }

    // 지속형 바람 장판(허리케인 소용돌이): VFX 1개를 center에 생성·유지 → pulses회 데미지(간격 interval) → 한 박자 뒤 삭제.
    private System.Collections.IEnumerator WindHeldRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, int pulses, float interval)
    {
        GameObject vfx = null;
        if (TryGetWindVfx(data, out GameObject prefab, out float scale))
            vfx = SpawnVfx(prefab, center, scale, 52);
        else
            SpawnHazardFlash(center, sizeWorld, new Color(0.3f, 0.9f, 0.9f, 0.3f)); // VFX 미연결 폴백(청록)

        for (int i = 0; i < pulses; i++)
        {
            DealHazardDamage(data, center, sizeWorld);
            if (i < pulses - 1)
                yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(0.3f);
        if (vfx != null) Destroy(vfx);
    }

    // 절대영도 (기획 얼음 3-2 / 4-5): 최단거리 타겟 중심에 Iceshower VFX 1회 + 2초간 0.2초마다 범위 데미지(지속 빙벽) → 소멸.
    private System.Collections.IEnumerator AbsoluteZeroRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld)
    {
        GameObject vfx = null;
        if (TryGetIceVfx(data, out GameObject prefab, out float scale))
        {
            vfx = SpawnVfx(prefab, center, scale, 52);
            if (vfx != null)
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
        }
        else SpawnHazardFlash(center, sizeWorld, new Color(0.6f, 0.85f, 1f, 0.35f)); // VFX 미연결 폴백

        const float duration = 2f;   // 기획 4-5: 2초 지속
        const float tick = 0.2f;     // 테이블 Count 0.2 = 틱 간격
        int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / tick));
        for (int i = 0; i < ticks; i++)
        {
            DealHazardDamage(data, center, sizeWorld);
            if (i < ticks - 1) yield return new WaitForSeconds(tick);
        }

        yield return new WaitForSeconds(0.2f);
        if (vfx != null) Destroy(vfx);
    }

    // 마칭 아이스: 투사체/이동 아님. 에이프릴 정수리 '위'에서 제자리 발동(흐웨이 QW식 세로 분출 장판).
    // 단일 VFX를 정수리 위에 고정 생성(이동X=투사체 아님, 스택X=타일링 직사각형 없음) + 정수리에서 위로 뻗는 좁은 세로 컬럼에 지속 범위 데미지.
    private System.Collections.IEnumerator MarchingIceRoutine(Legacy_SkillData data, Vector2 sizeWorld, int pulses, float interval)
    {
        Vector2 head = _firePoint != null ? (Vector2)_firePoint.position : new Vector2(CamCenterX(), 0f);
        float laneLen = Mathf.Max(1, pulses) * sizeWorld.y;                 // 위로 뻗는 길이 = PelletCount칸
        Vector2 vfxPos = head + Vector2.up * (sizeWorld.y * 0.5f + 0.6f);   // 정수리 '위' 분출 지점(고정)
        Vector2 hitCenter = head + Vector2.up * (laneLen * 0.5f);           // 정수리→위 좁은 세로 컬럼 판정
        Vector2 hitSize = new Vector2(sizeWorld.x, laneLen);

        GameObject vfx = null;
        if (TryGetIceVfx(data, out GameObject prefab, out float scale))
        {
            vfx = SpawnVfx(prefab, vfxPos, scale, 52);                      // 단일·제자리(이동/스택 없음)
            if (vfx != null)
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
        }
        else SpawnHazardFlash(hitCenter, hitSize, new Color(0.6f, 0.85f, 1f, 0.35f));

        float duration = Mathf.Max(0.35f, pulses * (interval > 0f ? interval : 0.15f));
        float tickGap = interval > 0f ? interval : 0.15f;
        float elapsed = 0f, tickT = tickGap;
        DealHazardDamage(data, hitCenter, hitSize);                         // 발동 즉시 1틱
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt; tickT += dt;
            if (tickT >= tickGap) { tickT = 0f; DealHazardDamage(data, hitCenter, hitSize); }
            yield return null;
        }
        if (vfx != null) Destroy(vfx);
    }

    // 얼음 결정 (기획 얼음 3-1, 루나라 W식): 플레이어에서 생성 → 최장거리 타겟 좌표로 초당 500px 전진하며 5초간 0.25초마다 다단히트.
    private System.Collections.IEnumerator IceCrystalRoutine(Legacy_SkillData data, Vector2 sizeWorld)
    {
        const float duration = 5f;          // 기획 3-1: 5초 지속
        const float tickGap = 0.25f;        // 기획 3-1-5-1: 0.25초 간격 다단히트
        float speed = PxToWorld(500f);      // QA 재현: 초당 500px 전진(자리 고정 → 최장거리 타겟 추적)

        Vector2 start = _firePoint != null ? (Vector2)_firePoint.position : new Vector2(CamCenterX(), 0f);
        Vector2 cur = start;

        // 최장거리(가장 먼) 살아있는 몬스터 좌표를 1회 잡아 그 좌표로 이동. 없으면 위(스폰 방향)로 끝까지.
        Vector2 destCoord = start + Vector2.up * (speed * duration);
        if (TryPickFarthestAliveMonster(start, out MonsterAI t))
            destCoord = t.transform.position;
        Vector2 dir = destCoord - start;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;

        GameObject vfx = null;
        if (TryGetIceVfx(data, out GameObject prefab, out float scale))
        {
            vfx = SpawnVfx(prefab, cur, scale, 52);
            if (vfx != null)
            {
                // IceStorm 프리팹 콘은 로컬 -Y(아래)로 방출되고 SpawnVfx가 identity 회전으로 생성해 아래로 흘렀다.
                // startRotation(ApplyParticleRotation)은 스프라이트만 돌리고 방출방향은 못 바꾸므로 transform 회전으로 진행방향 dir에 정렬한다.
                // 로컬 -Y를 dir로 보내는 Z각 = atan2(dir.x, -dir.y) (위=180도, 오른쪽=90도).
                float zDeg = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
                vfx.transform.rotation = Quaternion.Euler(0f, 0f, zDeg);
                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }
            }
        }
        else SpawnHazardFlash(cur, sizeWorld, new Color(0.6f, 0.85f, 1f, 0.35f));

        float elapsed = 0f, tickT = tickGap; // 첫 틱 즉시
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt; tickT += dt;
            cur = Vector2.MoveTowards(cur, destCoord, speed * dt);   // 최장거리 좌표로 전진(도착하면 그 자리 유지)
            if (vfx != null) vfx.transform.position = (Vector3)cur;
            if (tickT >= tickGap) { tickT = 0f; DealHazardDamage(data, cur, sizeWorld); }   // 504 슬로우는 DealHazardDamage 내장
            yield return null;
        }
        if (vfx != null) Destroy(vfx);
    }

    // 방전 (기획 v2.02 4-3): 타겟 위치에 가로로 넓은 전기 장판(피격 1440×200).
    // 방전 (기획 4-3): 타겟 위치에 CFXR 번개막(가운데) + 양옆 구슬 2개를 깔고 지속 → 0.5초마다 데미지, 첫 피격 몬스터 슬로우.
    private System.Collections.IEnumerator LightningDischargeRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, int pulses, float interval)
    {
        var lib = LightningVfx;

        // 기획: 몬스터 위치(높이)만 읽어 벽의 Y를 정하고, 화면 좌·우 '끝'에 구슬을 고정 생성한 뒤
        // 두 구슬 사이를 잇는 전기 벽을 1회 깔아 지속시킨다(적을 따라가지 않음 = 구슬·벽 위치 고정).
        MonsterAI refM = FindNearestAliveMonster(_firePoint != null ? (Vector2)_firePoint.position : center, null);
        float wallY = refM != null ? refM.transform.position.y : center.y;

        float cx = CamCenterX();
        float halfW = CamHalfWidth();
        float inset = PxToWorld(lib != null ? lib.dischargeEdgeInsetPx : 60f);
        Vector2 leftPos = new Vector2(cx - halfW + inset, wallY);
        Vector2 rightPos = new Vector2(cx + halfW - inset, wallY);

        // 양끝 구슬(CFXR 점 이펙트) + 그 사이 전기 벽(아크 파티클 타일)을 1회 고정 생성(이후 안 움직임, 적 안 따라감).
        GameObject orbL = null, orbR = null;
        var wallGos = new List<GameObject>();
        if (lib != null)
        {
            orbL = SpawnVfx(lib.dischargeOrb, leftPos, lib.dischargeOrbScale, 53);
            orbR = SpawnVfx(lib.dischargeOrb, rightPos, lib.dischargeOrbScale, 53);
            SpawnArcLine(wallGos, lib.arcEffect, leftPos, rightPos, lib.arcScale, lib.arcSpacing, 52);
        }

        // 데미지 판정 = 화면 전체 폭 × 벽 두께 밴드. 적이 이 벽(고정 위치)을 지나면 맞음.
        Vector2 wallCenter = new Vector2(cx, wallY);
        Vector2 wallHitSize = new Vector2(halfW * 2f, sizeWorld.y);

        // 기획 2-2: 이 스킬에 '처음 피해를 입은' 몬스터만 슬로우(Lv3일수록 길게).
        var slowed = new HashSet<MonsterAI>();
        float slowDur = 1.0f + 0.5f * Mathf.Clamp(data.Level, 1, 3); // Lv1 1.5 / Lv2 2.0 / Lv3 2.5초

        for (int i = 0; i < pulses; i++)
        {
            DealHazardDamage(data, wallCenter, wallHitSize);

            for (int b = 0; b < _hazardHitBuffer.Count; b++)
            {
                MonsterAI hm = _hazardHitBuffer[b];
                if (hm != null && slowed.Add(hm))
                    hm.ApplySlow(0.5f, slowDur);
            }

            if (i < pulses - 1)
                yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(0.3f);
        if (orbL != null) Destroy(orbL);
        if (orbR != null) Destroy(orbR);
        for (int g = 0; g < wallGos.Count; g++)
            if (wallGos[g] != null) Destroy(wallGos[g]);
    }

    // 에너지 볼 (StandardID 402, 기획 번개 2-1): 랜덤 적으로 전기 구가 '튕겨' 이동 → 도착(피격) 시 또 랜덤 적 탐색·이동 →
    // 정해진 횟수(Lv1·2: 3회 / Lv3: 4회) 반복 후 소멸. 선을 긋는 게 아니라 구 1개가 적들 사이를 이동하며 직격.
    private System.Collections.IEnumerator EnergyBallRoutine(Legacy_SkillData data, Vector2 firstTarget, Vector2 sizeWorld, int pulses, float interval)
    {
        var lib = LightningVfx;
        int count = Mathf.Max(1, pulses);                               // 탐색 횟수 = 데이터 Count(PelletCount). 테이블 v1.04: Lv1·2=3, Lv3=4
        float speed = (lib != null && lib.energyBallSpeed > 0f) ? lib.energyBallSpeed : 12f;

        // 전기 구 1개를 플레이어 위치에서 생성(Plazma_Ball 파티클이 구를 따라다니며 재생).
        Vector2 cur = _firePoint != null ? (Vector2)_firePoint.position : firstTarget;
        GameObject ball = (lib != null && lib.plazmaBall != null) ? SpawnVfx(lib.plazmaBall, cur, lib.plazmaBallScale, 54) : null;

        var avoid = new HashSet<MonsterAI>();                           // 직전 타겟 회피 → 매번 다른 적으로 튕김
        for (int hop = 0; hop < count; hop++)
        {
            // 랜덤 살아있는 적 탐색(직전 적 제외, 그것마저 없으면 아무 적이나)
            if (!TryPickRandomAliveMonster(out MonsterAI target, avoid) && !TryPickRandomAliveMonster(out target, null))
                break;

            // 타겟까지 이동(움직이면 추적). 안전 타임아웃 2초.
            float t = 0f;
            while (target != null && target.gameObject.activeInHierarchy)
            {
                Vector2 dest = target.transform.position;
                cur = Vector2.MoveTowards(cur, dest, speed * Time.deltaTime);
                if (ball != null) ball.transform.position = (Vector3)cur;
                if ((cur - dest).sqrMagnitude <= 0.04f) break;          // 도착
                t += Time.deltaTime;
                if (t > 2f) break;
                yield return null;
            }

            // 도착(피격): 데미지 + 타격 스파크
            if (target != null && target.gameObject.activeInHierarchy)
            {
                cur = target.transform.position;
                if (ball != null) ball.transform.position = (Vector3)cur;

                int damage = ComputeSkillDamage(data);
                target.TakeDamage(damage, data.StandardID);            // 정산 인챈트별 최고뎀 기록
                if (lib != null && lib.chainHitEffect != null)
                {
                    var hit = SpawnVfx(lib.chainHitEffect, cur, lib.chainHitScale, 55);
                    if (hit != null) Destroy(hit, 0.6f);
                }
                avoid.Clear();
                avoid.Add(target);                                     // 다음엔 이 적 회피
            }
        }

        yield return new WaitForSeconds(0.1f);                          // 마지막 피격 후 잠깐 뒤 소멸
        if (ball != null) Destroy(ball);
    }

    // 살아있는 몬스터 중 from에서 가장 가까운 1마리 (exclude 제외). 체인 라이트닝 연결용.
    private MonsterAI FindNearestAliveMonster(Vector2 from, HashSet<MonsterAI> exclude)
    {
        if (_monsterSpawner == null) return null;
        var alive = _monsterSpawner.AliveMonsters;
        MonsterAI best = null; float bestSq = float.MaxValue;
        for (int i = 0; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m == null || !m.gameObject.activeInHierarchy) continue;
            if (exclude != null && exclude.Contains(m)) continue;
            float sq = ((Vector2)m.transform.position - from).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = m; }
        }
        return best;
    }

    // 메테오 기준 공격 범위(px, 테이블 v1.03). 폭발 비주얼 스케일의 기준점으로 사용.
    private const float MeteorRefAreaPx = 350f;
    // 파이어브레스 기준 피격 범위 폭(px, 기획 4-1 Size 500). 화염 범위연동(flameAreaFactor)의 기준점.
    private const float FireBreathRefAreaPx = 500f;

    /// <summary>루프 파티클을 1회 재생으로 전환 (폭발처럼 단발이어야 하는 이펙트용).</summary>
    private static void StopLoopingOneShot(GameObject go)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.loop = false;
        }
    }

    /// <summary>
    /// 단발 이펙트의 제거 시점(초) = 모든 자식 파티클의 (지연+수명) 최대값 + 여유.
    /// 시스템 duration(사이클 길이)보다 짧게 잡혀 2번째 사이클이 시작될 틈을 주지 않는다.
    /// </summary>
    private static float ComputeOneShotLifetime(GameObject go)
    {
        float maxEnd = 0.3f; // 최소 보장
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            float delay = main.startDelay.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startDelay.constantMax : main.startDelay.constant;
            float life = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax : main.startLifetime.constant;
            // 마지막 입자/버스트는 방출 기간(duration)이 끝날 때 나올 수 있으므로 그 수명까지 더해야 폭발이 중간에 잘리지 않는다.
            // (explosion_5는 duration=1s·버스트가 사이클 양 끝(t=0,1)에 있어, duration 무시 시 0.6초에 파괴돼 폭발이 거의 안 보였음.
            //  loop는 StopLoopingOneShot로 이미 꺼져 있어 길게 잡아도 2번째 사이클=2차 폭발은 시작되지 않는다.)
            maxEnd = Mathf.Max(maxEnd, delay + main.duration + life);
        }
        return maxEnd + 0.1f;
    }

    /// <summary>
    /// 뷰 정렬 빌보드 파티클의 텍스처 회전(startRotation)에 오프셋을 더한다 (도 단위, 양수=시계방향).
    /// 옆 방향으로 제작된 이펙트를 진행 방향에 맞춰 돌릴 때 사용.
    /// </summary>
    private static void ApplyParticleRotation(GameObject go, float degrees)
    {
        if (Mathf.Approximately(degrees, 0f)) return;

        float rad = degrees * Mathf.Deg2Rad;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            var sr = main.startRotation;
            switch (sr.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    main.startRotation = new ParticleSystem.MinMaxCurve(sr.constant + rad);
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    main.startRotation = new ParticleSystem.MinMaxCurve(sr.constantMin + rad, sr.constantMax + rad);
                    break;
                // 커브 모드는 거의 안 쓰여서 생략 (필요 시 추가)
            }
        }
    }

    /// <summary>투사체(풀 공유 'Projectile_Basic')에 화염 작렬 등 VFX 스킨을 입히고 진행 방향으로 회전시킨다.
    /// 스킨은 ProjectileController가 디스폰 시 정리하므로 기본공격/다른 탄엔 영향 없다.</summary>
    private void AttachProjectileSkin(ProjectileController controller, GameObject projObj, GameObject skinPrefab,
        float scale, Vector2 origin, Vector2 targetPos, float trimDeg)
    {
        if (controller == null || projObj == null || skinPrefab == null) return;

        var skin = Instantiate(skinPrefab, projObj.transform);
        skin.transform.localPosition = Vector3.zero;
        skin.transform.localRotation = Quaternion.identity;
        if (!Mathf.Approximately(scale, 1f))
            skin.transform.localScale = skin.transform.localScale * scale;

        foreach (var r in skin.GetComponentsInChildren<ParticleSystemRenderer>(true))
            r.sortingOrder = 55;

        // Fireball_2_normal은 오른쪽(+X)을 향하게 제작된 빌보드 파티클(기획 R177/R206: 에셋이 오른쪽을 봐 -90 보정)이라
        // transform 회전이 안 먹는다. 진행 방향 d로 '오른쪽'을 보내는 시계방향 각도 = -atan2(d.y, d.x). startRotation을 돌려 정렬(메테오와 동일).
        Vector2 dir = targetPos - origin;
        if (dir.sqrMagnitude > 0.0001f)
        {
            float autoDeg = -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            ApplyParticleRotation(skin, autoDeg + trimDeg);
        }

        // Play On Awake에 의존하지 않고 파티클 재생을 명시적으로 보장한다.
        // (불/바람 프리팹은 우연히 자동재생됐지만, 다중 파티클 variant는 자식으로 붙이면 재생이 안 잡혀 안 보이던 원인)
        foreach (var ps in skin.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }

        controller.SetSkin(skin);
    }

    /// <summary>VFX 프리팹 소환 + 파티클 정렬 순서 지정(게임 스프라이트 위에 보이도록). prefab이 null이면 null 반환.</summary>
    private GameObject SpawnVfx(GameObject prefab, Vector2 pos, float scale, int sortingOrder)
    {
        if (prefab == null) return null;

        var go = Instantiate(prefab, (Vector3)pos, Quaternion.identity);
        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale = go.transform.localScale * scale;

        foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            r.sortingOrder = sortingOrder;

        return go;
    }

    /// <summary>from→to를 따라 전기 아크 파티클(빌보드)을 일정 간격으로 타일 배치해 '전기선'을 만든다.
    /// 빌보드는 transform으로 못 늘리므로 점을 따라 여러 개를 깔아 선처럼 보이게 한다. 생성된 인스턴스는 sink에 담아 호출부가 정리.</summary>
    private void SpawnArcLine(List<GameObject> sink, GameObject prefab, Vector2 from, Vector2 to, float scale, float spacing, int sortingOrder)
    {
        if (prefab == null) return;

        Vector2 d = to - from;
        float dist = d.magnitude;
        float angleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;        // 세그먼트 방향(가로=0, 세로=90)
        int count = Mathf.Max(1, Mathf.RoundToInt(dist / Mathf.Max(0.05f, spacing)));

        for (int i = 0; i <= count; i++)
        {
            Vector2 pos = Vector2.Lerp(from, to, count > 0 ? i / (float)count : 0.5f);
            var go = Instantiate(prefab, (Vector3)pos, Quaternion.identity);
            if (scale > 0f && !Mathf.Approximately(scale, 1f))
                go.transform.localScale = go.transform.localScale * scale;

            // 이 프리팹은 Billboard(View 정렬)이라 transform 회전이 안 먹는다 → 파티클 startRotation을 돌려
            // 가로 스프라이트를 세그먼트 방향으로 정렬(세로 구간이면 세로로). 회전 적용분으로 즉시 재방출.
            ApplyParticleRotation(go, -angleDeg);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) { ps.Clear(); ps.Play(); }

            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
                r.sortingOrder = sortingOrder;
            if (sink != null) sink.Add(go);
        }
    }

    private void DealHazardDamage(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, bool finalPulse = false)
    {
        int damage = ComputeSkillDamage(data);

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
        {
            MonsterAI m = _hazardHitBuffer[i];
            m.TakeDamage(damage, data.StandardID);   // 데미지 + 정산 인챈트별 최고뎀 기록

            // 바람 CC: 허리케인(304)=매 타격 슬로우(50%) / 돌풍(303)=마지막(폭발) 펄스에만 넉백(위로).
            // 번개 CC: 벼락(404) Lv3=스턴(1.5초). 방전(403) 첫피격 슬로우는 LightningDischargeRoutine에서 처리.
            if (data.StandardID == 304)
                m.ApplySlow(0.5f, 1.0f);
            else if (data.StandardID == 303 && finalPulse)
                m.ApplyKnockback(Vector2.up * PxToWorld(600f), 0.2f);
            else if (data.StandardID == 404 && data.Level >= 3)
                m.ApplyStun(1.5f);
            else if (data.StandardID == 504)
                m.ApplySlow(0.5f, data.Level >= 3 ? 2.0f : 1.0f);   // 얼음 결정: 슬로우(50%), Lv3 지속↑ (기획 3-1-5-3)
            // 물 CC: 물폭탄(201)=착탄 50% 슬로우 / 탄환세례(202)=10% 슬로우 + 마지막 펄스 넉백 / 파도소환(204)=마지막 틱 넉백.
            else if (data.StandardID == 201)
                m.ApplySlow(0.5f, 1.0f);                                       // 물 폭탄: 50% 슬로우 (기획 1-1)
            else if (data.StandardID == 202)
            {
                m.ApplySlow(0.9f, 1.0f);                                       // 탄환 세례: 10% 슬로우 (factor 0.9)
                if (finalPulse) m.ApplyKnockback(Vector2.up * PxToWorld(400f), 0.15f);  // 넉백 (기획 2-1)
            }
            else if (data.StandardID == 204)
                m.ApplyKnockback(Vector2.up * PxToWorld(120f), 0.2f);         // 파도 소환: 매 틱 위로 밀기(나미 R식 푸시)
        }
    }

    /// <summary>살아있는 몬스터 중 무작위 1마리. exclude에 든 몬스터는 피하되, 전부 제외되면(몬스터 부족) 중복 허용 폴백.</summary>
    private bool TryPickRandomAliveMonster(out MonsterAI picked, HashSet<MonsterAI> exclude = null)
    {
        picked = null;
        var alive = _monsterSpawner.AliveMonsters;

        _hazardHitBuffer.Clear();
        for (int i = 0; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m == null || !m.gameObject.activeInHierarchy) continue;
            if (exclude != null && exclude.Contains(m)) continue;
            _hazardHitBuffer.Add(m);
        }

        // 제외하고 나니 후보가 없으면(타겟 수 > 몬스터 수) 제외 없이 다시 수집
        if (_hazardHitBuffer.Count == 0 && exclude != null && exclude.Count > 0)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                MonsterAI m = alive[i];
                if (m != null && m.gameObject.activeInHierarchy)
                    _hazardHitBuffer.Add(m);
            }
        }

        if (_hazardHitBuffer.Count == 0) return false;
        picked = _hazardHitBuffer[Random.Range(0, _hazardHitBuffer.Count)];
        return true;
    }

    // 살아있는 몬스터 중 from에서 가장 먼 1마리 (얼음 결정: 최장거리 타겟). 없으면 false.
    private bool TryPickFarthestAliveMonster(Vector2 from, out MonsterAI picked)
    {
        picked = null;
        if (_monsterSpawner == null) return false;
        var alive = _monsterSpawner.AliveMonsters;
        float best = -1f;
        for (int i = 0; i < alive.Count; i++)
        {
            MonsterAI m = alive[i];
            if (m == null || !m.gameObject.activeInHierarchy) continue;
            float d = ((Vector2)m.transform.position - from).sqrMagnitude;
            if (d > best) { best = d; picked = m; }
        }
        return picked != null;
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

    // 대지 균열 행 배치 (기획 4-3-1)
    private const int EarthCrackColumns = 7;        // 한 줄 당 크랙 개수
    private const float EarthCrackXStepPx = 200f;   // 크랙 간 가로 간격 px (하나당 x+200, 맨왼쪽 -600 ~ 맨오른쪽 +600)

    /// <summary>대지 균열: 위험영역을 가로지르는 7개 크랙(Ground_explosion)을 단발 재생한다.
    /// center.y는 HazardRoutine이 펄스마다 전진(+sizeWorld.y)시킨 값을 그대로 받아 행 전체가 함께 전진한다(기획 4-3-2).</summary>
    private void SpawnEarthCrackRow(Vector2 center, Vector2 sizeWorld)
    {
        var lib = Vfx;
        if (lib == null || lib.earthCrackExplosion == null)
        {
            SpawnHazardFlash(center, sizeWorld, new Color(1f, 0.55f, 0.1f, 0.35f)); // VFX 미연결 폴백
            return;
        }

        float step = PxToWorld(EarthCrackXStepPx);   // 200px → world
        float scale = lib.earthCrackScale;
        for (int n = 0; n < EarthCrackColumns; n++)
        {
            // n=0..6 → (n-3)= -3..+3 → x -600..+600px (기획 4-3-1)
            float cellX = center.x + (n - (EarthCrackColumns - 1) * 0.5f) * step;
            var crack = SpawnVfx(lib.earthCrackExplosion, new Vector2(cellX, center.y), scale, 50);
            if (crack != null)
            {
                StopLoopingOneShot(crack);   // 루프 파티클 → 1회 재생 (메테오 explosion_5와 동일)
                Destroy(crack, ComputeOneShotLifetime(crack));
            }
        }
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

        int damage = ComputeSkillDamage(data);

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", origin, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float speed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;
        controller.SetupStraight(damage, origin, target.transform.position, speed, skillId: data.StandardID);

        // 정령이 시전하는 화염 작렬(StandardID 102)도 동일 스킨 적용 (기획 4-5: 정령 투사체는 화염 작렬 에셋 공유).
        if (data.StandardID == 102)
        {
            var lib = Vfx;
            if (lib != null && lib.fireballProjectile != null)
                AttachProjectileSkin(controller, obj, lib.fireballProjectile, lib.fireballProjectileScale,
                    origin, target.transform.position, lib.fireballRotationTrimDeg);
        }
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

    // 화면 절반 폭(월드). 방전 전기벽을 화면 좌·우 끝까지 깔 때 사용.
    private float CamHalfWidth()
    {
        EnsureCamera();
        return _cam != null ? _cam.orthographicSize * _cam.aspect : 5f;
    }

    private void EnsureCamera()
    {
        if (_cam == null) _cam = Camera.main;
    }

    private System.Collections.IEnumerator FireBurstRoutine(Legacy_SkillData data, int shots, AttackType type)
    {
        // 화염 작렬(StandardID 102)만 0.25초 간격, 그 외 다발 스킬은 기본(0.08초) 유지.
        float interval = data.StandardID == 102 ? FlameBurstShotInterval : BurstShotInterval;
        for (int i = 0; i < shots; i++)
        {
            FireOneProjectile(data, type);
            yield return new WaitForSeconds(interval);
        }
    }

    // 한 발 발사 : 데미지 계산 → 가장 가까운 적 탐색 → 직선 탄.
    private void FireOneProjectile(Legacy_SkillData data, AttackType type)
    {
        // 데미지 계산 (새 인챈트 경로 → DamageCalculate, 폴백 시 레거시)
        int damage = ComputeSkillDamage(data);

        ResolveReferences();

        // 템페스트(305)는 랜덤 타겟, 그 외는 최단거리 타겟 (기획: 템페스트=랜덤/관통/8히트)
        Vector2 targetPos;
        if (data.StandardID == 305)
        {
            if (_monsterSpawner == null || !TryPickRandomAliveMonster(out MonsterAI rt)) return;
            targetPos = rt.transform.position;
        }
        else if (!TryFindAttackTargetPosition(out targetPos))
        {
            return;
        }

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", _firePoint.position, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float projectileSpeed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;

        // 관통 투사체: 바람 칼날(302, 이즈 궁식 느린 관통)·템페스트(305, 랜덤+관통+멀티히트).
        // 관통 횟수=레벨별, 멀티히트(피격당 N회 대미지)=템페스트의 NumberOfCycle(8). data.Speed로 속도 제어.
        bool pierce = data.StandardID == 302 || data.StandardID == 305 || data.StandardID == 502;
        int maxPierce = int.MaxValue;
        int hitMul = 1;
        int lv = Mathf.Clamp(data.Level, 1, 3);
        if (data.StandardID == 302) maxPierce = 10 + 5 * lv;              // 바람칼날 관통 15/20/25
        else if (data.StandardID == 305)
        {
            maxPierce = 6 + 2 * lv;                                       // 템페스트 관통 8/10/12
            hitMul = data.NumberOfCycle > 0 ? data.NumberOfCycle : 1;     // 피격 시 8회 대미지
        }
        else if (data.StandardID == 502) maxPierce = 15 + 5 * lv;         // 글레이셜 피어스 관통 20/25/30
        controller.SetupStraight(damage, _firePoint.position, targetPos, projectileSpeed, pierce, maxPierce, hitMul, data.StandardID);

        // 화염 작렬 계열(StandardID 102)만 Fireball_2_normal VFX 스킨 + 진행방향 회전. 기본공격/타 투사체는 사각형 유지.
        if (data.StandardID == 102)
        {
            var lib = Vfx;
            if (lib != null && lib.fireballProjectile != null)
                AttachProjectileSkin(controller, obj, lib.fireballProjectile, lib.fireballProjectileScale,
                    _firePoint.position, targetPos, lib.fireballRotationTrimDeg);
        }
        // 바람 투사체 스킨: 헤이스트(301)·바람칼날(302)·템페스트(305). 동일 AttachProjectileSkin 경로.
        else if (data.StandardID == 301)
        {
            var wlib = WindVfx;
            if (wlib != null && wlib.hasteProjectile != null)
                AttachProjectileSkin(controller, obj, wlib.hasteProjectile, wlib.hasteProjectileScale,
                    _firePoint.position, targetPos, wlib.hasteRotationTrimDeg);
        }
        else if (data.StandardID == 302)
        {
            var wlib = WindVfx;
            if (wlib != null && wlib.windBladeProjectile != null)
                AttachProjectileSkin(controller, obj, wlib.windBladeProjectile, wlib.windBladeProjectileScale,
                    _firePoint.position, targetPos, wlib.windBladeRotationTrimDeg);
        }
        else if (data.StandardID == 305)
        {
            var wlib = WindVfx;
            if (wlib != null && wlib.tempestProjectile != null)
                AttachProjectileSkin(controller, obj, wlib.tempestProjectile, wlib.tempestProjectileScale,
                    _firePoint.position, targetPos, wlib.tempestRotationTrimDeg);
        }
        else if (data.StandardID == 502)
        {
            // 글레이셜 피어스(얼음 관통 투사체): 거대 고드름 VFX 스킨.
            var lib = Vfx;
            if (lib != null && lib.harshJudgmentVfx != null)
                AttachProjectileSkin(controller, obj, lib.harshJudgmentVfx, lib.harshJudgmentScale,
                    _firePoint.position, targetPos, lib.harshJudgmentRotationTrimDeg);
        }
        else
        {
            // 원소 전용 스킨이 없는 투사체(소트 기본공격·더미 소트 스킬 등) → 공격 종류별 기본 VFX(소트=SortSkill/자동=AutoSkill).
            GameObject def = GetBasicAttackSkin(type);
            if (def != null)
                AttachProjectileSkin(controller, obj, def, GetBasicAttackScale(type), _firePoint.position, targetPos, GetBasicAttackTrim(type));
        }
    }

    // 공격 종류별 기본 VFX 선택: 인스펙터(SkillSystem) 연결분 우선, 비면 FireSkillVfxLibrary 연결분.
    private GameObject GetBasicAttackSkin(AttackType type)
    {
        if (type == AttackType.Sort) return _sortAttackVfx != null ? _sortAttackVfx : (Vfx != null ? Vfx.sortAttackVfx : null);
        if (type == AttackType.Auto) return _autoAttackVfx != null ? _autoAttackVfx : (Vfx != null ? Vfx.autoAttackVfx : null);
        return null;
    }

    // 공격 종류별 기본공격 VFX 스케일: 라이브러리의 sort/auto 스케일 × SkillSystem 전역 배수(_basicAttackVfxScale, 기본 1).
    private float GetBasicAttackScale(AttackType type)
    {
        float libScale = 1f;
        if (Vfx != null) libScale = (type == AttackType.Sort) ? Vfx.sortAttackScale : Vfx.autoAttackScale;
        return _basicAttackVfxScale * libScale;
    }

    // 공격 종류별 기본공격 VFX 회전 보정: 진행방향 자동정렬에 더해지는 오프셋. 라이브러리 sort/auto 트림 + 전역(_basicAttackVfxTrimDeg).
    private float GetBasicAttackTrim(AttackType type)
    {
        float libTrim = 0f;
        if (Vfx != null) libTrim = (type == AttackType.Sort) ? Vfx.sortAttackRotationTrimDeg : Vfx.autoAttackRotationTrimDeg;
        return _basicAttackVfxTrimDeg + libTrim;
    }

    /// <returns>실제로 발사했으면 true. 타겟 부재 등으로 스킵하면 false (자동공격 카운트는 발사 성공만 센다).</returns>
    public bool FireBasicAttack(AttackType type = AttackType.Auto)
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

        // 공격 종류별 VFX를 투사체에 입힌다 (소트=SortSkill, 자동=AutoSkill).
        GameObject skin = GetBasicAttackSkin(type);
        if (skin != null)
            AttachProjectileSkin(controller, obj, skin, GetBasicAttackScale(type), _firePoint.position, targetPos, GetBasicAttackTrim(type));
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
        if (_enchantCalculator == null && !_hasTriedResolveEnchant)
        {
            _hasTriedResolveEnchant = true;
            _enchantCalculator = FindFirstObjectByType<EnchantCalculator>();
        }
        if (_monsterSpawner != null) return;
        if (_hasTriedResolveSpawner) return;

        _hasTriedResolveSpawner = true;
        _monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
    }

    /// <summary>부트스트랩이 명시적으로 EnchantCalculator를 주입할 때 사용(미주입 시 ResolveReferences가 Find로 폴백).</summary>
    public void SetEnchantCalculator(EnchantCalculator calc) { if (calc != null) _enchantCalculator = calc; }

    // 레거시 SkillID(4자리) → 신규 SkillEnchantTable Skill_ID(5자리). 기본 = 원소digit 뒤 0 삽입(2011→20011, 같은 스킬의 ID 재포맷).
    // 분할 스킬(투사체 본체 Dmg=0, 데미지는 폭발 엔트리)만 폭발 ID로 오버라이드. 물폭탄: 기획 확인(20111~20113).
    private static readonly Dictionary<int, int> NewDamageIdOverride = new Dictionary<int, int>
    {
        { 2011, 20111 }, { 2012, 20112 }, { 2013, 20113 },
    };

    private static int MapToNewDamageId(int legacySkillId)
    {
        if (NewDamageIdOverride.TryGetValue(legacySkillId, out int o)) return o;
        return (legacySkillId / 1000) * 10000 + (legacySkillId % 1000);   // insert-0: 2011→20011, 5053→50053
    }

    // 새 인챈트 경로 데미지: DamageCalculate(ATK/크리/스킬·그룹보너스) × 콤보(보존). 매핑 데미지 0(분할 본체/미정의)·계산기 부재 시 레거시 공식 폴백 → 0뎀으로 안 깨짐.
    private int ComputeSkillDamage(Legacy_SkillData data)
    {
        ResolveReferences();
        if (_enchantCalculator != null && data != null)
        {
            // 계산기 미초기화/참조 누락(예: _playerModel 미연결)으로 던져도 전투(특히 뒤이은 VFX 스폰)가 안 죽도록 폴백.
            try
            {
                int baseDmg = _enchantCalculator.DamageCalculate(MapToNewDamageId(data.SkillID));
                if (baseDmg > 0)
                {
                    float comboBonus = _combatSystem != null ? _combatSystem.GetComboBonusRate() : 1f;
                    return Mathf.FloorToInt(baseDmg * comboBonus);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SkillSystem] EnchantCalculator.DamageCalculate 실패 → 레거시 폴백: {e.Message}");
            }
        }
        return CalGroupDamageBonus(_combatSystem.CalculateDamage(data.DmgRate), GetDamageGroupType(data));
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
    PlayerColumn,   // 플레이어 X 고정, 위(몬스터 스폰 방향)로 세로 컬럼 (뇌격 레이저)
}

/// <summary>장판 실행 스타일</summary>
public enum HazardStyle
{
    InstantPulse,   // 즉시 판정 + 플래시 (대지 균열 등)
    MeteorStrike,   // 마커 → 낙하 → 폭발 3단 시퀀스 (메테오)
    FireBreath,     // 수정구 소환 → 0.5초 간격 N발 화염 분사 → 수정구 삭제 (파이어브레스)
    LightningHeld,  // VFX 1회 생성·지속 → 펄스마다 데미지 → 종료 후 삭제 (구형 번개)
    LightningDischarge, // 가운데 번개막 + 양옆 구슬 2개 지속 → 펄스마다 데미지 (방전)
    LightningChain, // 에이프릴→타겟 전기선 + 몬스터 몸체 타격 이펙트 (사슬 번개)
    WindVortex,     // VFX 1개를 center에 지속 생성 → 펄스마다 데미지 (허리케인 소용돌이)
    AbsoluteZero,   // 최단거리 타겟 중심에 VFX 1회 + 2초간 0.2초마다 데미지 (절대영도 지속 빙벽)
    MarchingIce,    // 플레이어 X에서 위로 한 칸씩 전진하는 좁은 정사각 얼음 마칭 (흐웨이 QW식)
    IceCrystalMoving, // 플레이어에서 생성→타겟 방향으로 천천히 전진하며 5초 다단히트 (얼음 결정, 루나라 W식)
    WaterBombImpact,  // 201: 물 공이 타겟으로 날아가 착탄 시 폭발(범위)+50% 슬로우 (물 폭탄)
    WaveRise,         // 204: 타겟 X·장벽 Y에서 위로 솟구치며 전진하는 파도 장판 + 넉백 (파도 소환)
    WaterBeamSustain, // 205: 중앙 고정 세로 컬럼에 2초간 0.2초마다 틱 (하이드로 펌프)
}

public struct HazardConfig
{
    public HazardPlacement placement;
    public HazardStyle style;
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
