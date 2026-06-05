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

using UnityEngine;

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
        Legacy_InGameSaveData saveData = null;

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
        _enchantModel.Initialize();

        if (isResume && saveData != null)
        {
            _playerModel.RestoreFromSave(saveData);
            _enchantModel.RestoreFromSave(saveData.acquiredEnchants);
        }

        // [4] Sort 초기화
        int seed;
        if (isResume && saveData != null)
            seed = saveData.nextStageSeed;
        else
            seed = Random.Range(0, int.MaxValue);

        _sortSystem.Initialize(seed);

        // [5] 플레이어 비주얼 + 전투 발사 셋업
        SetupPlayerCombat();

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

        // 단독 _InGame 실행(Boot 미경유) 시 몬스터 풀이 비어 있어 스폰이 실패한다.
        // 에디터 테스트 편의로 Monsters 폴더 프리팹을 풀에 자동 등록 (빌드는 Boot 경유라 제외).
        EnsureMonsterPoolsInEditor(pool);

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
        var view = FindFirstObjectByType<SettlementView>(FindObjectsInactive.Include);
        if (view == null)
        {
            Debug.LogWarning("[InGameBootstrap] SettlementView를 찾지 못해 정산 팝업을 띄우지 못했습니다.");
            return;
        }

        int maxCombo = _comboModel != null ? _comboModel.MaxComboThisRun : 0;
        int totalDamage = RunStats.TotalDamage;

        // 보상(임시값): 챕터 클리어 시 재화·양피지. 정확값은 ConfigRepo 연동 시 교체 (기획 보상 수치 미확정)
        int gold = isVictory ? 100 : 0;
        int parchment = isVictory ? 10 : 0;

        view.Show();
        view.SetResult(isVictory);
        view.SetStats(maxCombo, totalDamage);
        view.SetRewards(gold, parchment);

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

    // [에디터 전용] 단독 _InGame 실행 시 Monsters 폴더의 프리팹을 풀에 자동 등록.
    // 빌드/정식 플로우는 Boot의 PoolManager configs를 사용하므로 이 블록은 컴파일에서 제외됨.
    private void EnsureMonsterPoolsInEditor(PoolManager pool)
    {
#if UNITY_EDITOR
        const string dir = "Assets/_Game/Prefabs/Monsters";
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { dir });
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                pool.EnsurePool(prefab.name, prefab, 5); // key = 프리팹명(Monster_11 …) = 풀키 규칙
        }
#endif
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