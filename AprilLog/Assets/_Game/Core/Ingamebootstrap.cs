// 담당자 : 정승우
// 설명   : InGame 씬 초기화 -- Repository -> Model -> System 순서

// 1차 수정자 : 김영찬
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, _boot 씬에서만 초기화 하면 되도록 수정

// 2차 수정자 : 홍정옥
// 수정내용 : 아웃게임 성장 보너스를 PlayerModel에 반영하는 로직 추가

// 3차 수정자 : 정승우
// 수정내용 : 기획서 v1.03 반영. Shield 삭제, Attack/StunPower/SlowPower 보너스 추가.

// 4차 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경

// 5차 수정자 : 김영찬
// 플레이어 스텟 초기화 시 CharacterStatus 반영하도록 수정

// 6차 수정자 : 김영찬
// 정산 창을 정옥님이 작성하신 코드로 변경

// 7차 수정자 : 김영찬
// SaveData 관련 Class Legacy 처리한 내용들을 SaveDataClasses.cs 신설 하면서 클래스명이 변화 한것 반영함

using UnityEngine;
// 추가: 조규민 - 챕터 정산 보상과 진행도를 로그인 계정 CloudData에 즉시 반영한다.

/// <summary>
/// InGame 씬 로드 후 모든 시스템을 의존성 순서대로 초기화한다.
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private PlayerModel _playerModel;
    [SerializeField] private SortModel _sortModel;
    [SerializeField] private ComboModel _comboModel;
    [SerializeField] private CombinationModel _combinationModel;
    [SerializeField] private EnchantModel _enchantModel;
    [Tooltip("비워두면 런타임에 자동 생성됨")]
    [SerializeField] private Legacy_EnchantApplicationSystem _enchantApplicationSystem;
    [Tooltip("비워두면 런타임에 씬에서 탐색")]
    [SerializeField] private InGameGrowthSystem _growthSystem;

    [Header("System")]
    [SerializeField] private SortSystem _sortSystem;

    [Header("플레이어/방벽 위치 (자동 생성 시 · 씬에 PlayerView 두면 무시)")]
    [Tooltip("캐릭터(플레이어) 세로 위치. 0=화면 하단, 1=상단. 노드 바로 위로 두려면 0.40~0.45")]
    [SerializeField, Range(0f, 1f)] private float _characterViewportY = 0.42f;

    [Tooltip("방벽(DefenseLine)이 캐릭터보다 위에 있는 거리(월드 단위). 양수 = 위")]
    [SerializeField] private float _barrierAboveCharacter = 0.5f;

    [Tooltip("카메라가 없을 때 사용할 캐릭터 기본 Y")]
    [SerializeField] private float _fallbackCharacterY = -0.8f;

    [Tooltip("투사체 풀 사전 생성 수량")]
    [SerializeField] private int _projectilePoolSize = 20;

    [Header("웨이브")]
    [Tooltip("새 게임 시작 시 진입할 챕터 ID (이어하기는 세이브의 chapterId 사용)")]
    [SerializeField] private int _defaultChapterId = 1;

    private GameObject _projectileTemplate;

    private void Start()
    {
        InitializeAll();
    }

    private void InitializeAll()
    {
        Debug.Log("[InGameBootstrap] === InGame 초기화 시작 ===");

        RunStats.Reset(); // 한 판 통계(총 데미지 등) 초기화

        // [1] Repository 초기화는 DataManager 싱글톤에서 처리됨 (김영찬)

        // [2] 이어하기 체크
        bool isResume = GameManager.Instance != null && GameManager.Instance.HasLocalSave();
        InGameSaveData saveData = null;

        if (isResume)
        {
            saveData = GameManager.Instance.LoadLocalSaveData();
            Debug.Log("[InGameBootstrap] 이어하기 데이터 로드됨");
        }

        // [3] Model 초기화
        var commonStatus = DataManager.Instance.CharacterRepo.GetCommonStatus(1);
        var characterStatus = DataManager.Instance.CharacterRepo.GetCharacterStatus(1);
        _playerModel.Initialize(commonStatus, characterStatus);

        // 아웃게임 성장 보너스 적용 (홍정옥)
        int characterLevel = GetCharacterLevel();
        DataManager.Instance.ConfigRepo.GetOutGrowthBonusUntilLevel(characterLevel,
            out int hpBonus, out int attackBonus, out int stunBonus, out int slowBonus);
        _playerModel.ApplyStatBonus_OutGameBonus(hpBonus, attackBonus, stunBonus, slowBonus);

        _combinationModel.Initialize();

        // 불 속성 스킬(화염 작렬/정령 등)의 레시피·트리거는 더 이상 시작부터 켜지 않는다.
        // 레벨업 → 인챈트 선택에서 해당 '스킬 인챈트'를 획득해야 SkillEnchantSystem이 등록한다 (테이블 v1.03 정식 흐름).

        _enchantModel.Initialize();

        // 인챈트 효과 적용 시스템: EnchantModel 이벤트 구독 (없으면 자동 생성 → 씬 배선 불필요)
        if (_enchantApplicationSystem == null)
            _enchantApplicationSystem = gameObject.AddComponent<Legacy_EnchantApplicationSystem>();
        _enchantApplicationSystem.Initialize(_playerModel, _enchantModel);

        // 인게임 성장 시스템(레벨/EXP) 초기화. 비어 있으면 씬에서 탐색.
        // (기존엔 어디서도 Initialize/RestoreFromSave를 호출하지 않아 레벨/EXP가 미초기화 상태였음)
        if (_growthSystem == null) _growthSystem = FindFirstObjectByType<InGameGrowthSystem>();

        if (isResume && saveData != null)
        {
            _playerModel.RestoreFromSave(saveData);
            _enchantModel.RestoreFromSave(saveData.acquiredEnchants);
            // 이어하기: 세이브된 인챈트 효과를 최종 레벨 누적값으로 재적용
            // (RestoreFromSave는 이벤트를 발행하지 않으므로 직접 재적용 필요)
            _enchantApplicationSystem.ReapplyFromSave(saveData.acquiredEnchants);

            if (_growthSystem != null)
                _growthSystem.RestoreFromSave(saveData.inGameLevel, saveData.currentEXP);
        }
        else if (_growthSystem != null)
        {
            _growthSystem.Initialize();
        }

        // [4] Sort 초기화
        int seed;
        if (isResume && saveData != null)
            seed = saveData.nextStageSeed;
        else
            seed = Random.Range(0, int.MaxValue);

        // Sort 컨트롤러(SortSystem/SortInputHandler)가 씬에 없으면 런타임에 생성한다.
        // → 씬 배선 없이도 동작. 각 컴포넌트는 ResolveRefs()로 의존성을 자가 연결한다.
        if (FindFirstObjectByType<SortInputHandler>() == null)
        {
            var sortModel = FindFirstObjectByType<SortModel>();
            GameObject host = sortModel != null ? sortModel.gameObject : gameObject;
            host.AddComponent<SortInputHandler>();
            Debug.LogWarning("[InGameBootstrap] SortInputHandler가 씬에 없어 런타임에 생성했습니다.");
        }

        if (_sortSystem == null) _sortSystem = FindFirstObjectByType<SortSystem>();
        if (_sortSystem == null)
        {
            _sortSystem = gameObject.AddComponent<SortSystem>();
            Debug.LogWarning("[InGameBootstrap] SortSystem이 씬에 없어 런타임에 생성했습니다.");
        }
        _sortSystem.Initialize(seed);

        // CombatSystem은 OnEnable(씬 로드) 시점엔 아직 없던 SortSystem 구독을 놓쳤을 수 있으므로,
        // SortSystem 생성/초기화 직후 명시적으로 바인딩한다 (→ 정렬 성공 시 공격 발동).
        var combatSystem = FindFirstObjectByType<CombatSystem>();
        if (combatSystem != null)
            combatSystem.BindSortSystem(_sortSystem);

        // [5] 플레이어 비주얼 + 전투 발사 셋업
        SetupPlayerCombat();

        // [5.5] 불 속성 스킬 실행 설정(장판/소환) 등록 + 스킬 인챈트 시스템 연결.
        // 스킬 자체는 레벨업 인챈트 선택으로 획득해야 발동된다 (자동공격도 일반 스킬 인챈트 획득 시 활성화).
        RegisterFireSkills(combatSystem, isResume, saveData);

        // [6] 실제 웨이브 시스템 시작 (데이터 기반). 더미 테스터는 비활성화.
        int chapterId = (isResume && saveData != null) ? saveData.chapterId : _defaultChapterId;
        int startStageIndex = (isResume && saveData != null) ? saveData.clearedStage : 0;
        DisableDummyTester();
        StartWaveSystem(chapterId, startStageIndex, seed);

        Debug.Log("[InGameBootstrap] === InGame 초기화 완료 ===");
    }

    // 플레이어 사각형 비주얼 생성 + 투사체 풀 보장 + SkillSystem 발사점 연결.
    // (DummyCombatTester가 리플렉션으로 하던 셋업을 정식 경로로 옮김)
    private void SetupPlayerCombat()
    {
        // 오브젝트 풀 보장 (단독 _InGame 실행 시 Boot의 PoolManager가 없을 수 있음)
        var pool = PoolManager.Instance;
        if (pool == null)
            pool = new GameObject("PoolManager").AddComponent<PoolManager>();

        // 투사체 풀 보장 (런타임 사각형 투사체 템플릿)
        if (_projectileTemplate == null)
            _projectileTemplate = BuildProjectileTemplate();
        pool.EnsurePool("Projectile_Basic", _projectileTemplate, _projectilePoolSize);

        // 몬스터 풀 보장: Boot 풀이 유실돼도(머지로 DontDestroyOnLoad가 사라지는 사고 포함) 빌드/에디터 모두
        // Resources/Monsters 프리팹으로 풀을 채운다. (RegisterPool 멱등이라 Boot가 이미 채웠으면 no-op)
        EnsureMonsterPools(pool);

        // 플레이어/방벽 배치.
        // 씬에 PlayerView를 직접 두면 그 위치를 존중하고 자동 배치하지 않는다(완전 수동 제어).
        // 없으면 인스펙터 값(_characterViewportY, _barrierAboveCharacter)으로 자동 배치한다.
        var playerView = FindFirstObjectByType<PlayerView>();
        if (playerView == null)
        {
            float characterY = ResolveCharacterWorldY();

            // 캐릭터(플레이어)는 노드 바로 위에 배치 (기획 1)
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0f, characterY, 0f);
            playerView = go.AddComponent<PlayerView>();

            // 방벽(DefenseLine)은 캐릭터보다 살짝 위 → 몬스터가 캐릭터 위에서 멈춤 (기획 3)
            var defenseLine = GameObject.Find("DefenseLine");
            if (defenseLine != null)
                defenseLine.transform.position = new Vector3(0f, characterY + _barrierAboveCharacter, 0f);
        }

        // SkillSystem에 발사점 연결 (이제 정렬 완성 시 플레이어 위치에서 발사)
        var skillSystem = FindFirstObjectByType<SkillSystem>();
        if (skillSystem != null && playerView.FirePoint != null)
            skillSystem.SetFirePoint(playerView.FirePoint);
        else if (skillSystem == null)
            Debug.LogWarning("[InGameBootstrap] SkillSystem을 찾지 못해 발사점을 연결하지 못했습니다.");

        // 몬스터 스폰 라인을 화면 최상단으로 맞춤 → 위에서 생성되어 방벽으로 하강.
        // (플레이어/방벽과 좌표를 일관되게 코드에서 함께 설정)
        var spawner = FindFirstObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                float z = Mathf.Abs(cam.transform.position.z);
                float topY = cam.ViewportToWorldPoint(new Vector3(0.5f, 1.05f, z)).y;   // 화면 위 살짝 밖
                float leftX = cam.ViewportToWorldPoint(new Vector3(0.12f, 0.5f, z)).x;
                float rightX = cam.ViewportToWorldPoint(new Vector3(0.88f, 0.5f, z)).x;
                spawner.SetNormalSpawnLine(topY, leftX, rightX);
            }
        }
    }

    // 웨이브 시스템(StageLoopManager+StageBootstrapper)을 보장하고 챕터를 시작한다.
    // 씬에 없으면 런타임 생성 — 두 컴포넌트가 서로/스포너/플레이어를 자동 탐색해 연결된다.
    private void StartWaveSystem(int chapterId, int startStageIndex, int seed)
    {
        var loop = FindFirstObjectByType<StageLoopManager>();
        if (loop == null)
        {
            var go = new GameObject("WaveSystem");
            go.AddComponent<StageBootstrapper>();
            loop = go.AddComponent<StageLoopManager>();
        }

        // 챕터 종료(승/패) → 정산 팝업
        loop.OnChapterEnd -= ShowSettlement; // 중복 구독 방지
        loop.OnChapterEnd += ShowSettlement;

        loop.StartChapter(chapterId, startStageIndex, seed);
    }

    // 챕터 종료 시 정산 팝업 표시 + 데이터 주입 (기획: 승패/콤보/총뎀/보상)
    private void ShowSettlement(bool isVictory)
    {
        var view = FindFirstObjectByType<ResultPopup>(FindObjectsInactive.Include);
        if (view == null)
        {
            Debug.LogWarning("[InGameBootstrap] SettlementView를 찾지 못해 정산 팝업을 띄우지 못했습니다.");
            return;
        }

        int maxCombo = _comboModel != null ? _comboModel.MaxComboThisRun : 0;
        int maxDamage = RunStats.HighestDamage;
        
        int enchantDamage1 = RunStats.HighestEnchantDamage1;
        int enchantDamage2 = RunStats.HighestEnchantDamage2;
        int enchantDamage3 = RunStats.HighestEnchantDamage1;

        // 보상(임시값): 챕터 클리어 시 재화·양피지. 정확값은 ConfigRepo 연동 시 교체 (기획 보상 수치 미확정)
        int gold = isVictory ? 100 : 0;
        int parchment = isVictory ? 10 : 0;

        var loop = FindFirstObjectByType<StageLoopManager>();
        int chapterId = loop != null ? loop.CurrentChapterId : _defaultChapterId;
        int completedStageCount = loop != null ? loop.CompletedStageCount : 0;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveChapterResult(isVictory, chapterId, completedStageCount, gold, parchment);
        }
        
        view.Show(isVictory, maxCombo, maxDamage, enchantDamage1, enchantDamage2, enchantDamage3, gold, parchment);

        // 기획 1-3-1: 승/패 확정 즉시 플레이어 조작 비활성화.
        // 정산 팝업(UI)은 월드 좌표 기반 퍼즐 드래그를 막지 못하므로 입력 핸들러를 직접 끈다.
        DisablePlayerInputOnGameEnd();
    }

    // 게임 종료(승/패) 시 퍼즐 입력을 차단해 더 이상 조작/공격이 일어나지 않게 한다. (기획 1-3-1)
    private void DisablePlayerInputOnGameEnd()
    {
        var sortInput = FindFirstObjectByType<SortInputHandler>();
        if (sortInput != null)
            sortInput.enabled = false;
    }

    // 실제 웨이브로 진행하므로 에디터 테스트용 더미 스포너를 끈다.
    private void DisableDummyTester()
    {
#if UNITY_EDITOR
        var dummy = FindFirstObjectByType<DummyCombatTester>();
        if (dummy != null)
        {
            dummy.gameObject.SetActive(false);
            Debug.Log("[InGameBootstrap] 실제 웨이브 시스템 사용 — DummyCombatTester 비활성화.");
        }
#endif
    }

    // 캐릭터(플레이어) 월드 Y = 카메라 뷰포트 기준 _characterViewportY. 카메라 없으면 폴백.
    private float ResolveCharacterWorldY()
    {
        var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            Vector3 wp = cam.ViewportToWorldPoint(
                new Vector3(0.5f, _characterViewportY, Mathf.Abs(cam.transform.position.z)));
            return wp.y;
        }
        return _fallbackCharacterY;
    }

    // 불 속성 스킬의 '실행 설정'(장판 모양/소환 구성 — SkillID→실행 방식 매핑)을 등록하고,
    // 스킬 인챈트 시스템을 연결한다. 트리거/레시피는 여기서 등록하지 않는다 —
    // 레벨업 인챈트 선택에서 스킬 인챈트를 획득하면 SkillEnchantSystem이 등록한다 (테이블 v1.03 정식 흐름).
    private void RegisterFireSkills(CombatSystem combatSystem, bool isResume, InGameSaveData saveData)
    {
        var skillSystem = FindFirstObjectByType<SkillSystem>();
        var spellRepo = DataManager.Instance != null ? DataManager.Instance.SpellRepo : null;
        if (skillSystem == null || spellRepo == null)
        {
            Debug.LogWarning("[InGameBootstrap] SkillSystem/SpellRepo가 없어 불 속성 스킬 설정을 등록하지 못했습니다.");
            return;
        }

        // -- 장판 설정 (테이블 px 좌표계: 화면 폭 = 1440px) --
        // 파이어브레스 1011~13: 최단거리 타겟 위치 고정 장판, 3회 타격 (Lv3는 범위 확대)
        var fireBreath = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 500, heightPx = 700, pulseInterval = 0.4f, flashColor = new Color(1f, 0.3f, 0.05f, 0.35f) };
        skillSystem.RegisterHazardSkill(1011, fireBreath);
        skillSystem.RegisterHazardSkill(1012, fireBreath);
        fireBreath.widthPx = 600; fireBreath.heightPx = 770;
        skillSystem.RegisterHazardSkill(1013, fireBreath);

        // 대지 균열 1041~43: 전방 전체 폭 장판, 순차 다회 타격
        var earthCrack = new HazardConfig { placement = HazardPlacement.PlayerFront, widthPx = 1440, heightPx = 300, pulseInterval = 0.35f, flashColor = new Color(1f, 0.55f, 0.1f, 0.35f) };
        skillSystem.RegisterHazardSkill(1041, earthCrack);
        skillSystem.RegisterHazardSkill(1042, earthCrack);
        skillSystem.RegisterHazardSkill(1043, earthCrack);

        // 메테오 1051~53: 매 펄스 랜덤 타겟 — 마커(Flame_ellipse)→낙하(Fireball_loop_2)→폭발(explosion_5) 3단 시퀀스
        var meteor = new HazardConfig { placement = HazardPlacement.RandomTarget, style = HazardStyle.MeteorStrike, widthPx = 350, heightPx = 350, pulseInterval = 0.15f, flashColor = new Color(1f, 0.15f, 0.05f, 0.45f) };
        skillSystem.RegisterHazardSkill(1051, meteor);
        skillSystem.RegisterHazardSkill(1052, meteor);
        skillSystem.RegisterHazardSkill(1053, meteor);

        // 화염 정령 1031~33: 정령 2마리가 같은 레벨의 화염 작렬을 1초마다 5초간 시전
        for (int lv = 1; lv <= 3; lv++)
        {
            var burst = spellRepo.GetSkill(1020 + lv); // 화염 작렬 1021~23
            skillSystem.RegisterSummonSkill(1030 + lv, new SummonConfig { castSkill = burst, lifetime = 5f, castInterval = 1f });
        }

        // -- 스킬 인챈트 시스템: 인챈트 획득/레벨업 → 스킬 해금·교체 --
        var skillEnchantSystem = GetComponent<SkillEnchantSystem>();
        if (skillEnchantSystem == null)
            skillEnchantSystem = gameObject.AddComponent<SkillEnchantSystem>();
        skillEnchantSystem.Initialize(_enchantModel, skillSystem, _combinationModel, combatSystem);

        // 이어하기: 세이브된 스킬 인챈트를 최종 레벨로 재등록 (RestoreFromSave는 이벤트를 안 쏘므로 직접)
        if (isResume && saveData != null)
            skillEnchantSystem.ReapplyFromSave(saveData.acquiredEnchants);

        Debug.Log("[InGameBootstrap] 불 속성 스킬 설정 등록 완료 — 스킬은 레벨업 인챈트 선택으로 획득 시 발동");
    }

    // Boot의 PoolManager 풀이 (단독 _InGame 실행이거나 머지로 DontDestroyOnLoad가 유실돼) 비어 있어도
    // 몬스터가 스폰되도록, Resources/Monsters 폴더의 프리팹을 풀에 등록한다. 에디터·빌드 공통(AssetDatabase 비의존).
    // RegisterPool이 멱등이라 Boot가 이미 등록한 키는 무시되므로 중복 등록은 안전하다.
    private void EnsureMonsterPools(PoolManager pool)
    {
        var prefabs = Resources.LoadAll<GameObject>("Monsters");
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[InGameBootstrap] Resources/Monsters 에서 몬스터 프리팹을 못 찾음. 프리팹이 Assets/Resources/Monsters/ 에 있는지 확인.");
            return;
        }

        foreach (var prefab in prefabs)
        {
            if (prefab != null)
                pool.EnsurePool(prefab.name, prefab, 5); // key = 프리팹명(Monster_11 …) = 풀키 규칙
        }
    }

    // 코드로 만든 투사체 템플릿(사각형). DummyCombatTester의 런타임 생성 로직을 정식화.
    private GameObject BuildProjectileTemplate()
    {
        var t = new GameObject("Projectile_Basic_Template");
        t.SetActive(false);
        t.transform.localScale = Vector3.one * 0.3f;

        var sr = t.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Square();
        sr.color = Color.yellow;
        sr.sortingOrder = 50;

        // 트리거 충돌 콜백을 받으려면 한쪽에 Rigidbody2D가 필요하다.
        var rb = t.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = t.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        t.AddComponent<ProjectileController>();
        return t;
    }

    // 클라우드 데이터에서 캐릭터 레벨 가져오기 (홍정옥)
    private int GetCharacterLevel()
    {
        if (GameManager.Instance == null || GameManager.Instance.CloudData == null)
            return 1;
        return GameManager.Instance.CloudData.characterLevel;
    }
}
