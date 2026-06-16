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
            case 402: prefab = lib.chainBolt; scale = lib.chainScale; break;                // 사슬 번개 (연결 번개막)
            case 403: prefab = lib.dischargeBarrier; scale = lib.dischargeScale; break;     // 방전 (가운데 번개막)
        }
        return prefab != null;
    }

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
        if (cfg.style == HazardStyle.LightningChain)
        {
            StartCoroutine(LightningChainRoutine(data, fixedCenter, sizeWorld, pulses, cfg.pulseInterval));
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
                DealHazardDamage(data, center, sizeWorld);
                // 대지 균열은 7열 크랙 행 / 번개 단발(벼락·뇌격)은 실제 VFX 단발 / 그 외는 기존 플래시.
                if (cfg.placement == HazardPlacement.PlayerFront)
                    SpawnEarthCrackRow(center, sizeWorld);
                else if (TryGetLightningVfx(data, out GameObject lvfx, out float lscale))
                {
                    var v = SpawnVfx(lvfx, center, lscale, 52);
                    if (v != null)
                    {
                        // 뇌격: Lazer_purple prefab의 startRotation이 0이라 위아래가 뒤집혀 보임 → 스펙 4-5-2 'Start Rotation 180' 적용해 세로 방향 정렬. (벼락 등 다른 번개는 회전 안 함)
                        if (data.StandardID == 405 && LightningVfx != null)
                            ApplyParticleRotation(v, LightningVfx.laserRotationDeg);
                        StopLoopingOneShot(v); Destroy(v, ComputeOneShotLifetime(v));
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

    // 방전 시퀀스 (기획 v2.02 4-3): 타겟 위치에 가운데 번개막 + 양옆(±offset) 구슬 2개를 깔고, 지속시간 동안 데미지 → 종료 후 전부 삭제.
    private System.Collections.IEnumerator LightningDischargeRoutine(Legacy_SkillData data, Vector2 center, Vector2 sizeWorld, int pulses, float interval)
    {
        var lib = LightningVfx;
        GameObject barrier = null, orbL = null, orbR = null, connector = null;
        if (lib != null)
        {
            float side = PxToWorld(lib.dischargeOrbOffsetPx);
            barrier = SpawnVfx(lib.dischargeBarrier, center, lib.dischargeScale, 52);
            orbL = SpawnVfx(lib.dischargeOrb, center + new Vector2(-side, 0f), lib.dischargeOrbScale, 53);
            orbR = SpawnVfx(lib.dischargeOrb, center + new Vector2(side, 0f), lib.dischargeOrbScale, 53);
            if (lib.dischargeConnector != null) // 밑 전기선 연결점 (에디터에서 드래그하면 표시)
                connector = SpawnVfx(lib.dischargeConnector, center + new Vector2(0f, PxToWorld(lib.dischargeConnectorYOffsetPx)), lib.dischargeConnectorScale, 51);
        }
        if (barrier == null && orbL == null)
            SpawnHazardFlash(center, sizeWorld, new Color(0.8f, 0.7f, 1f, 0.35f)); // VFX 미연결 폴백

        for (int i = 0; i < pulses; i++)
        {
            DealHazardDamage(data, center, sizeWorld);
            if (i < pulses - 1)
                yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(0.3f);
        if (barrier != null) Destroy(barrier);
        if (orbL != null) Destroy(orbL);
        if (orbR != null) Destroy(orbR);
        if (connector != null) Destroy(connector);
    }

    // 사슬 번개 = 정통 체인 라이트닝 (기획 v2.02 4-2): 에이프릴→가장 가까운 적1→적2→… 로 전기줄이 1→2→3 순차로 '다다닥' 점프하며 각 타겟을 직격.
    private System.Collections.IEnumerator LightningChainRoutine(Legacy_SkillData data, Vector2 firstTarget, Vector2 sizeWorld, int pulses, float interval)
    {
        var lib = LightningVfx;
        int maxTargets = (lib != null && lib.chainMaxTargets > 0) ? lib.chainMaxTargets : 5;
        float hopDelay = (lib != null && lib.chainHopDelay > 0f) ? lib.chainHopDelay : 0.07f;

        // 체인 구성: 첫 타겟에서 시작해 가장 가까운 미방문 몬스터로 순차 연결
        var visited = new HashSet<MonsterAI>();
        var chainTargets = new List<MonsterAI>();
        MonsterAI nextM = FindNearestAliveMonster(firstTarget, null);
        while (nextM != null && chainTargets.Count < maxTargets)
        {
            visited.Add(nextM);
            chainTargets.Add(nextM);
            nextM = FindNearestAliveMonster(nextM.transform.position, visited);
        }
        if (chainTargets.Count == 0)
        {
            SpawnHazardFlash(firstTarget, sizeWorld, new Color(0.9f, 0.85f, 1f, 0.4f)); // 몬스터 없음 폴백
            yield break;
        }

        int damage = CalGroupDamageBonus(_combatSystem.CalculateDamage(data.DmgRate), GetDamageGroupType(data));
        var spawned = new List<GameObject>();
        Vector2 prev = _firePoint != null ? (Vector2)_firePoint.position : firstTarget;

        // 에이프릴→1→2→3… 한 칸씩 전기줄이 이어지며 각 타겟 직격
        for (int i = 0; i < chainTargets.Count; i++)
        {
            MonsterAI m = chainTargets[i];
            if (m == null || !m.gameObject.activeInHierarchy) continue;
            Vector2 pos = m.transform.position;
            Vector2 dir = pos - prev;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (lib != null && lib.chainBolt != null)
            {
                var bolt = SpawnVfx(lib.chainBolt, (prev + pos) * 0.5f, lib.chainScale, 52); // 직전 지점→타겟 전기줄 세그먼트
                if (bolt != null) { ApplyParticleRotation(bolt, angleDeg + lib.chainBoltRotationTrimDeg); spawned.Add(bolt); }
            }
            if (lib != null && lib.chainHitEffect != null)
            {
                var hit = SpawnVfx(lib.chainHitEffect, pos, lib.chainHitScale, 53); // 몬스터 몸체 타격 이펙트
                if (hit != null) spawned.Add(hit);
            }
            m.TakeDamage(damage);   // 타겟 직격 (테이블: 사슬 = 면적 없는 타겟 직격)
            prev = pos;
            yield return new WaitForSeconds(hopDelay);   // 다다닥
        }

        yield return new WaitForSeconds(0.25f);
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
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

        float temp = _combatSystem.CalculateDamage(data.DmgRate);
        int damage = CalGroupDamageBonus(temp, GetDamageGroupType(data));

        var obj = PoolManager.Instance.Spawn("Projectile_Basic", origin, Quaternion.identity);
        if (obj == null) return;

        var controller = obj.GetComponent<ProjectileController>();
        if (controller == null) return;

        float speed = data.Speed > 0 ? data.Speed : _basicProjectileSpeed;
        controller.SetupStraight(damage, origin, target.transform.position, speed);

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

        // 화염 작렬 계열(StandardID 102)만 Fireball_2_normal VFX 스킨 + 진행방향 회전. 기본공격/타 투사체는 사각형 유지.
        if (data.StandardID == 102)
        {
            var lib = Vfx;
            if (lib != null && lib.fireballProjectile != null)
                AttachProjectileSkin(controller, obj, lib.fireballProjectile, lib.fireballProjectileScale,
                    _firePoint.position, targetPos, lib.fireballRotationTrimDeg);
        }
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

/// <summary>장판 실행 스타일</summary>
public enum HazardStyle
{
    InstantPulse,   // 즉시 판정 + 플래시 (대지 균열 등)
    MeteorStrike,   // 마커 → 낙하 → 폭발 3단 시퀀스 (메테오)
    FireBreath,     // 수정구 소환 → 0.5초 간격 N발 화염 분사 → 수정구 삭제 (파이어브레스)
    LightningHeld,  // VFX 1회 생성·지속 → 펄스마다 데미지 → 종료 후 삭제 (구형 번개)
    LightningDischarge, // 가운데 번개막 + 양옆 구슬 2개 지속 → 펄스마다 데미지 (방전)
    LightningChain, // 에이프릴→타겟 전기선 + 몬스터 몸체 타격 이펙트 (사슬 번개)
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
