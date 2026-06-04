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

    [Header("플레이어/전투 셋업")]
    [Tooltip("플레이어 사각형 비주얼이 생성될 월드 위치")]
    [SerializeField] private Vector3 _playerSpawnPosition = new Vector3(0f, -2.5f, 0f);

    [Tooltip("투사체 풀 사전 생성 수량")]
    [SerializeField] private int _projectilePoolSize = 20;

    [Tooltip("방벽(DefenseLine) 세로 위치. 0=화면 하단, 1=상단. 퍼즐 위로 두려면 0.4~0.5 권장")]
    [SerializeField, Range(0f, 1f)] private float _defenseLineViewportY = 0.5f;

    private GameObject _projectileTemplate;

    private void Start()
    {
        InitializeAll();
    }

    private void InitializeAll()
    {
        Debug.Log("[InGameBootstrap] === InGame 초기화 시작 ===");

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

        // 방벽(DefenseLine)을 퍼즐 위(카메라 뷰포트 기준)로 재배치한다.
        // DefenseLine을 옮기면 플레이어(자식)와 몬스터 정지선(이 Y를 읽음)이 함께 따라온다.
        var defenseLine = GameObject.Find("DefenseLine");
        if (defenseLine != null)
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                Vector3 wp = cam.ViewportToWorldPoint(
                    new Vector3(0.5f, _defenseLineViewportY, Mathf.Abs(cam.transform.position.z)));
                defenseLine.transform.position = new Vector3(wp.x, wp.y, 0f);
            }
        }

        // 플레이어 비주얼(사각형) + 발사점. 씬에 PlayerView가 있으면 우선 사용, 없으면 방벽 자식으로 생성.
        var playerView = FindFirstObjectByType<PlayerView>();
        if (playerView == null)
        {
            var go = new GameObject("Player");
            if (defenseLine != null)
            {
                go.transform.SetParent(defenseLine.transform, false);
                go.transform.localPosition = Vector3.zero; // 방벽 위에 정렬, 방벽 이동 시 따라감
            }
            else
            {
                Debug.LogWarning("[InGameBootstrap] 'DefenseLine'(방벽)을 찾지 못해 기본 위치에 플레이어를 둡니다.");
                go.transform.position = _playerSpawnPosition;
            }
            playerView = go.AddComponent<PlayerView>();
        }

        // SkillSystem에 발사점 연결 (이제 정렬 완성 시 플레이어 위치에서 발사)
        var skillSystem = FindFirstObjectByType<SkillSystem>();
        if (skillSystem != null && playerView.FirePoint != null)
            skillSystem.SetFirePoint(playerView.FirePoint);
        else if (skillSystem == null)
            Debug.LogWarning("[InGameBootstrap] SkillSystem을 찾지 못해 발사점을 연결하지 못했습니다.");
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