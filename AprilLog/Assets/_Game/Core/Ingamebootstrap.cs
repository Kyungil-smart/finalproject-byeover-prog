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

// 8차 수정자 : 김영찬
// 데이터 로드 개선

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

    [Header("플레이어")]
    [Tooltip("플레이어 캐릭터의 Character_ID. CharacterRepo에서 이 ID로 공통/캐릭터 스탯을 로드한다. 현재 데이터의 플레이어 캐릭터 = 5001")]
    [SerializeField] private int _playerCharacterId = 5001;

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
    private bool _settlementRewardGranted;   // 단계④: 정산 보상 중복 지급(재정산 중복가산) 방지 가드

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
        var commonStatus = DataManager.Instance.CharacterRepo.GetCommonStatus(_playerCharacterId);
        var characterStatus = DataManager.Instance.CharacterRepo.GetCharacterStatus(_playerCharacterId);
        // 신규 CharacterRepo는 데이터 없으면 예외 대신 null 반환 → PlayerModel.Initialize가 null 역참조로 NRE,
        // try/catch 없는 InitializeAll 전체가 중단되어 소트·웨이브까지 안 뜸. 한 단계 실패를 명시 로그+중단으로 격리.
        if (commonStatus == null || characterStatus == null)
        {
            Debug.LogError($"[InGameBootstrap] 캐릭터(ID={_playerCharacterId}) 스탯 데이터 없음 — CharacterRepo SO 배선/시트(Character_ID={_playerCharacterId}) 확인. 초기화 중단.");
            return;
        }
        _playerModel.Initialize(commonStatus, characterStatus);

        // 아웃게임 성장 보너스 적용 (홍정옥)
        int characterLevel = GetCharacterLevel();
        DataManager.Instance.ConfigRepo.GetOutGrowthBonusUntilLevel(characterLevel,
            out int hpBonus, out int attackBonus, out int effectPower, out int flatPierce);
        _playerModel.ApplyStatBonus_OutGameBonus(hpBonus, attackBonus, effectPower, flatPierce);

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
            _enchantModel.RestoreFromSave(saveData.acquiredEnchants);
            // 이어하기: 세이브된 인챈트 효과를 최종 레벨 누적값으로 재적용
            // (RestoreFromSave는 이벤트를 발행하지 않으므로 직접 재적용 필요)
            _enchantApplicationSystem.ReapplyFromSave(saveData.acquiredEnchants);

            // ★플레이어 HP 복원은 인챈트 HP 재적용 '뒤'에. (앞에 두면 재적용이 CurrentHP에 보너스를
            //  한 번 더 더해 과회복됨. 뒤에 두면 저장된 현재 HP로 덮어써 정확. MaxHP는 재적용으로 보정됨.)
            _playerModel.RestoreFromSave(saveData);

            if (_growthSystem != null)
                _growthSystem.RestoreFromSave(saveData.inGameLevel, saveData.currentEXP);
            
            RunStats.RestoreFromSave(saveData.totalDamage, saveData.highestDamage, saveData.MaxBySkill);
            
            if (_comboModel != null)
            {
                _comboModel.RestoreFromSave(saveData.maxCombo);
            }
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
        
        if (isResume && saveData != null)
        {
            // 이어하기 : 퍼즐의 상태와 조커의 상태를 복구한다.
            // 퍼즐 보드 및 대기열 복구
            _sortSystem.RestoreFromSave(seed, saveData.puzzleSlots, saveData.waitingSlots);

            // 조커 상태 복구
            var jokerSystem = FindFirstObjectByType<JokerSystem>();
            if (jokerSystem != null)
            {
                jokerSystem.RestoreFromSave(saveData.jokerCount, saveData.jokerRemainingCooldown);
            }
        }
        else
        {
            _sortSystem.Initialize(seed);
        }

        // CombatSystem은 OnEnable(씬 로드) 시점엔 아직 없던 SortSystem 구독을 놓쳤을 수 있으므로,
        // SortSystem 생성/초기화 직후 명시적으로 바인딩한다 (→ 정렬 성공 시 공격 발동).
        var combatSystem = FindFirstObjectByType<CombatSystem>();
        if (combatSystem != null)
            combatSystem.BindSortSystem(_sortSystem);

        // 튜토리얼 GameAction 훅: 정렬 완성 시 현재 단계가 GameAction이면 TutorialManager가 다음 단계로 진행.
        // (SortSystem : ISortNotifier. 씬 배선 없이 런타임 연결.)
        if (_sortSystem != null)
        {
            var tutorialHook = gameObject.AddComponent<TutorialGameActionHook>();
            tutorialHook.Bind(_sortSystem);
        }

        // [5] 플레이어 비주얼 + 전투 발사 셋업
        SetupPlayerCombat();

        // [5.5] 불 속성 스킬 실행 설정(장판/소환) 등록 + 스킬 인챈트 시스템 연결.
        // 스킬 자체는 레벨업 인챈트 선택으로 획득해야 발동된다 (자동공격도 일반 스킬 인챈트 획득 시 활성화).
        RegisterFireSkills(combatSystem, isResume, saveData);

        // [6] 실제 웨이브 시스템 시작 (데이터 기반). 더미 테스터는 비활성화.
        int chapterId = ResolveStartChapterId(isResume, saveData);
        int startStageIndex = ResolveStartStageIndex(isResume, saveData);
        DisableDummyTester();
        StartWaveSystem(chapterId, startStageIndex, seed);

        Debug.Log("[InGameBootstrap] === InGame 초기화 완료 ===");
    }

    private int ResolveStartChapterId(bool isResume, InGameSaveData saveData)
    {
        if (isResume && saveData != null)
        {
            return Mathf.Max(1, saveData.chapterId);
        }

        // 추가: 조규민 - 챕터 포기 후 재진입 또는 로비 선택 진입 시 저장 없이도 선택 챕터를 반영한다.
        int selectedChapterId = GameManager.Instance != null ? GameManager.Instance.SelectedChapterId : 0;
        if (selectedChapterId >= 100)
        {
            return Mathf.Max(1, selectedChapterId / 100);
        }

        if (selectedChapterId > 0)
        {
            return selectedChapterId;
        }

        return _defaultChapterId;
    }

    private int ResolveStartStageIndex(bool isResume, InGameSaveData saveData)
    {
        if (isResume && saveData != null)
        {
            return Mathf.Max(0, saveData.clearedStage);
        }

        int selectedStageId = GameManager.Instance != null ? GameManager.Instance.SelectedChapterId : 0;
        if (selectedStageId < 100)
        {
            return 0;
        }

        int selectedStageNumber = selectedStageId % 100;
        return Mathf.Max(0, selectedStageNumber - 1);
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
        
        // 스킬(인챈트)별 단일타격 최고뎀 상위 3개. RunStats가 MonsterAI.TakeDamage(dmg, skillId)로 스킬ID별 기록.
        // (.Key=StandardID로 어떤 인챈트인지도 알 수 있음 — 정산창에 이름 표시하려면 ResultPopup.Show에 ID 추가)
        var topEnchants = RunStats.TopSkillsByDamage(3);
        int enchantDamage1 = topEnchants.Count > 0 ? topEnchants[0].Value : 0;
        int enchantDamage2 = topEnchants.Count > 1 ? topEnchants[1].Value : 0;
        int enchantDamage3 = topEnchants.Count > 2 ? topEnchants[2].Value : 0;

        // 보상(임시값): 챕터 클리어 시 재화·양피지. 정확값은 ConfigRepo 연동 시 교체 (기획 보상 수치 미확정)
        int gold = isVictory ? 100 : 0;
        int parchment = isVictory ? 10 : 0;

        var loop = FindFirstObjectByType<StageLoopManager>();
        int chapterId = loop != null ? loop.CurrentChapterId : _defaultChapterId;
        int completedStageCount = loop != null ? loop.CompletedStageCount : 0;

        // 단계④: 같은 정산이 중복 발동돼도 보상은 한 번만 지급(재정산 중복가산 방지).
        if (GameManager.Instance != null && !_settlementRewardGranted)
        {
            GameManager.Instance.SaveChapterResult(isVictory, chapterId, completedStageCount, gold, parchment);
            _settlementRewardGranted = true;
        }

        view.Show(isVictory, maxCombo, maxDamage, enchantDamage1, enchantDamage2, enchantDamage3, gold, parchment);

        // 기획 1-3-1: 승/패 확정 즉시 플레이어 조작 비활성화.
        // 정산 팝업(UI)은 월드 좌표 기반 퍼즐 드래그를 막지 못하므로 입력 핸들러를 직접 끈다.
        DisablePlayerInputOnGameEnd();
        
        // 정산창이 뜨면 게임 진행 상황을 모두 지우고 새롭게 재시작해야된다. (기획팀 이형진 확인)
        if (GameManager.Instance != null)
        {
            bool hasSaveData = GameManager.Instance.HasLocalSave();
            if (hasSaveData) GameManager.Instance.DeleteLocalSave();
        }
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
        // 파이어브레스 1011~13: 최단거리 타겟 위치 고정 장판, 3회 타격 (Lv3는 범위 확대).
        // style=FireBreath라 HazardRoutine 펄스루프를 우회 → 발사 간격은 pulseInterval이 아니라 SO의 fireBreathFlameInterval(0.5s)을 사용한다(아래 pulseInterval은 FireBreath에선 미사용).
        var fireBreath = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.FireBreath, widthPx = 500, heightPx = 700, pulseInterval = 0.4f, flashColor = new Color(1f, 0.3f, 0.05f, 0.35f) };
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

        // ===== 바람·번개 속성 장판 (골격 — placeholder VFX=SpawnHazardFlash 색 사각형) =====
        // 데미지/판정/발동만 동작. 상태이상(슬로우/스턴/넉백)·체인·버프·관통은 미구현(별도 시스템 필요).
        // 투사체형(바람칼날 3021·템페스트 3051·헤이스트 3011)은 기본 투사체 경로라 여기 등록 안 함. (사슬번개 4021은 아래 LightningChain 하자드로 등록 → 투사체 경로 안 탐)
        // 돌풍 3031~33: 최단거리 장판, 3+1히트(PelletCount 4). 넉백 미구현.
        var gust = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 350, heightPx = 350, pulseInterval = 0.15f, flashColor = new Color(0.4f, 1f, 0.7f, 0.35f) };
        skillSystem.RegisterHazardSkill(3031, gust);
        skillSystem.RegisterHazardSkill(3032, gust);
        gust.widthPx = 550; gust.heightPx = 550;   // 돌풍 Lv3 범위 증가 (기획 3-1-5-3)
        skillSystem.RegisterHazardSkill(3033, gust);

        // 허리케인 3041~43: 지속 장판 4초·0.5초틱(PelletCount 8). 슬로우 미구현. (기획상 화면중앙이나 우선 최단거리)
        var hurricane = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.WindVortex, widthPx = 500, heightPx = 500, pulseInterval = 0.5f, flashColor = new Color(0.3f, 0.9f, 0.9f, 0.3f) };
        skillSystem.RegisterHazardSkill(3041, hurricane);
        skillSystem.RegisterHazardSkill(3042, hurricane);
        skillSystem.RegisterHazardSkill(3043, hurricane);

        // 구형 번개 4011~13: 최단거리 정사각 장판 3초·0.25초틱(PelletCount 12). 실제 VFX(Projectile_Lightning_Ball)·피격 350×350 (기획 v2.02 4-1).
        var orbLightning = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.LightningHeld, widthPx = 350, heightPx = 350, pulseInterval = 0.25f, flashColor = new Color(1f, 0.95f, 0.3f, 0.4f) };
        skillSystem.RegisterHazardSkill(4011, orbLightning);
        skillSystem.RegisterHazardSkill(4012, orbLightning);
        orbLightning.widthPx = 450; orbLightning.heightPx = 450;   // 구형번개 Lv3 범위 증가 (기획 1-1-5-3)
        skillSystem.RegisterHazardSkill(4013, orbLightning);

        // 방전 4031~33: 최단거리 장판 5초·0.5초틱(PelletCount 10)·피격 1440×200 (기획 4-3). LineRenderer 번개줄. 첫 피격 몬스터 슬로우(Lv3 지속↑) 구현.
        var discharge = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.LightningDischarge, widthPx = 1440, heightPx = 200, pulseInterval = 0.5f, flashColor = new Color(0.8f, 0.7f, 1f, 0.35f) };
        skillSystem.RegisterHazardSkill(4031, discharge);
        skillSystem.RegisterHazardSkill(4032, discharge);
        skillSystem.RegisterHazardSkill(4033, discharge);

        // 사슬 번개 4021~23 (조합): 랜덤 시작→가까운 적 순차 연결 최대 5마리, 4회 반복 타격(PelletCount 4)·LineRenderer 번개줄 (기획 4-2). 5마리×4회 구현.
        var chainLightning = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.LightningChain, widthPx = 700, heightPx = 200, pulseInterval = 0.375f, flashColor = new Color(0.9f, 0.85f, 1f, 0.4f) };
        skillSystem.RegisterHazardSkill(4021, chainLightning);
        skillSystem.RegisterHazardSkill(4022, chainLightning);
        skillSystem.RegisterHazardSkill(4023, chainLightning);

        // 벼락 4041~43: 엘리트/보스 우선 타겟(없으면 최단거리) 정사각 낙뢰 4히트(PelletCount 4). 실제 VFX(Lightning_Big)·피격 675×675 (기획 450×450 +50% 요청). Lv3 스턴(1.5초) 구현.
        var thunderbolt = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 675, heightPx = 675, pulseInterval = 0.1f, flashColor = new Color(1f, 0.9f, 0.4f, 0.45f) };
        skillSystem.RegisterHazardSkill(4041, thunderbolt);
        skillSystem.RegisterHazardSkill(4042, thunderbolt);
        skillSystem.RegisterHazardSkill(4043, thunderbolt);

        // 뇌격 4051~53: 플레이어 기준 세로 레이저 빔 — 플레이어 X에서 위(몬스터 스폰 방향)로 쭉 뻗음(PlayerColumn). 피격 300×1600 (기획 v2.02 4-5).
        var thunderStrike = new HazardConfig { placement = HazardPlacement.PlayerColumn, widthPx = 300, heightPx = 1600, pulseInterval = 0.15f, flashColor = new Color(0.9f, 0.8f, 1f, 0.4f) };
        skillSystem.RegisterHazardSkill(4051, thunderStrike);
        skillSystem.RegisterHazardSkill(4052, thunderStrike);
        skillSystem.RegisterHazardSkill(4053, thunderStrike);

        // ===== 물 속성 장판 (VFX=WaterSkillVfxLibrary 연결, 슬로우/넉백=DealHazardDamage CC, 물폭탄/파도/하이드로=전용 루틴) =====
        Color waterFlash = new Color(0.2f, 0.5f, 1f, 0.4f);
        // 물 폭탄 2011~13 (일반): 장벽서 물 공이 최단거리 타겟으로 날아가 착탄 폭발 250×250 + 50% 슬로우 (WaterBombRoutine).
        var waterBomb = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.WaterBombImpact, widthPx = 250, heightPx = 250, pulseInterval = 0.2f, flashColor = waterFlash };
        skillSystem.RegisterHazardSkill(2011, waterBomb);
        skillSystem.RegisterHazardSkill(2012, waterBomb);
        skillSystem.RegisterHazardSkill(2013, waterBomb);

        // 탄환 세례 2021~23 (조합): 최단거리 광역 장판 600×700, 1/1/2히트(PelletCount) + 넉백 + 10% 슬로우. Water_Splash_B VFX.
        var bulletShower = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 600, heightPx = 700, pulseInterval = 0.15f, flashColor = waterFlash };
        skillSystem.RegisterHazardSkill(2021, bulletShower);
        skillSystem.RegisterHazardSkill(2022, bulletShower);
        skillSystem.RegisterHazardSkill(2023, bulletShower);

        // 급류 2031~33 (조합): 최단거리 타겟 Y에 전체 폭 띠 장판 1440×250, 같은 자리 4히트(PelletCount). 폭포 띠 VFX. (기획 4-3: 고정 X·타겟 Y)
        var torrent = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 1440, heightPx = 250, pulseInterval = 0.3f, flashColor = waterFlash };
        skillSystem.RegisterHazardSkill(2031, torrent);
        skillSystem.RegisterHazardSkill(2032, torrent);
        skillSystem.RegisterHazardSkill(2033, torrent);

        // 파도 소환 2041~43 (콤보): 타겟 X·장벽 Y에서 위로 솟구치며 전진하는 파도(Speed 600) + 넉백 (WaveSummonRoutine).
        var waveSummon = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.WaveRise, widthPx = 400, heightPx = 250, pulseInterval = 0.2f, flashColor = waterFlash };
        skillSystem.RegisterHazardSkill(2041, waveSummon);
        skillSystem.RegisterHazardSkill(2042, waveSummon);
        skillSystem.RegisterHazardSkill(2043, waveSummon);

        // 하이드로 펌프 2051~53 (콤보): 전투구역 중앙 고정 세로 컬럼 250×1700, 2초간 0.2초마다 틱 (HydroPumpRoutine). Water_Beam VFX.
        var hydroPump = new HazardConfig { placement = HazardPlacement.PlayerColumn, style = HazardStyle.WaterBeamSustain, widthPx = 250, heightPx = 1700, pulseInterval = 0.2f, flashColor = waterFlash };
        skillSystem.RegisterHazardSkill(2051, hydroPump);
        skillSystem.RegisterHazardSkill(2052, hydroPump);
        skillSystem.RegisterHazardSkill(2053, hydroPump);

        // ===== 얼음 속성 장판 (골격 — placeholder VFX=색 사각형(하늘). 데미지/판정/발동만. 빙결/슬로우 CC·이동장판은 폴리싱) =====
        // 글레이셜 피어스 5021~23(조합)은 투사체(piercing)라 기본 투사체 경로 — 여기 등록 안 함.
        Color iceFlash = new Color(0.6f, 0.9f, 1f, 0.4f);
        // 마칭 아이스 5011~13 (일반): 장판형. 에이프릴 → 최단거리 타겟 방향으로 100×100 정사각형 PelletCount(6/7/8)칸을 pulseInterval마다 순차 발동(마칭). (style=MarchingIce → MarchingIceRoutine, placement 미사용. 2026-06-24 QA 개편: 제자리→타겟 전진)
        var marchingIce = new HazardConfig { placement = HazardPlacement.PlayerFront, style = HazardStyle.MarchingIce, widthPx = 100, heightPx = 100, pulseInterval = 0.15f, flashColor = iceFlash };
        skillSystem.RegisterHazardSkill(5011, marchingIce);
        skillSystem.RegisterHazardSkill(5012, marchingIce);
        skillSystem.RegisterHazardSkill(5013, marchingIce);

        // 빙결 지대 5031~33 (조합): 최단거리 장판 400×400(Lv3 550×550), 1히트. 빙결 CC 미구현.
        var freezeZone = new HazardConfig { placement = HazardPlacement.NearestTarget, widthPx = 400, heightPx = 400, pulseInterval = 0.2f, flashColor = iceFlash };
        skillSystem.RegisterHazardSkill(5031, freezeZone);
        skillSystem.RegisterHazardSkill(5032, freezeZone);
        freezeZone.widthPx = 550; freezeZone.heightPx = 550;
        skillSystem.RegisterHazardSkill(5033, freezeZone);

        // 얼음 결정 5041~43 (콤보): 랜덤 타겟 단발 장판 350/400/450. 이동장판(Speed 300)·슬로우는 폴리싱.
        var iceCrystal = new HazardConfig { placement = HazardPlacement.RandomTarget, style = HazardStyle.IceCrystalMoving, widthPx = 350, heightPx = 350, pulseInterval = 0.2f, flashColor = iceFlash };
        skillSystem.RegisterHazardSkill(5041, iceCrystal);
        iceCrystal.widthPx = 400; iceCrystal.heightPx = 400;
        skillSystem.RegisterHazardSkill(5042, iceCrystal);
        iceCrystal.widthPx = 450; iceCrystal.heightPx = 450;
        skillSystem.RegisterHazardSkill(5043, iceCrystal);

        // 절대영도 5051~53 (콤보): 최단거리 장판 650×450 / 750×500 / 850×550 (기획 테이블 HitSize 동기화, QA #191). Iceshower VFX + 2초간 0.2초마다 틱(AbsoluteZeroRoutine).
        var absoluteZero = new HazardConfig { placement = HazardPlacement.NearestTarget, style = HazardStyle.AbsoluteZero, widthPx = 650, heightPx = 450, pulseInterval = 0.2f, flashColor = iceFlash };
        skillSystem.RegisterHazardSkill(5051, absoluteZero);
        absoluteZero.widthPx = 750; absoluteZero.heightPx = 500;
        skillSystem.RegisterHazardSkill(5052, absoluteZero);
        absoluteZero.widthPx = 850; absoluteZero.heightPx = 550;
        skillSystem.RegisterHazardSkill(5053, absoluteZero);

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
        {
            Debug.LogWarning("[InGameBootstrap] 클라우드 데이터를 찾을 수 없음. 아웃 게임 레벨 1");
            return 1;
        }
        Debug.Log($"[InGameBootstrap] 클라우드 데이터를 찾음. 아웃 게임 레벨 {GameManager.Instance.CloudData.characterLevel}");
        return GameManager.Instance.CloudData.characterLevel;
    }
}
