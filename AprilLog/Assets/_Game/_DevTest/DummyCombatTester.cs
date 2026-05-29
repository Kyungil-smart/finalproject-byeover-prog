// 담당자 : (임시 테스트)
// 설명   : 플레이어 공격 시스템을 _InGame 단독 Play로 확인하기 위한 throwaway 하니스.
//          기획/데이터 테이블에 없는 더미 데이터만 사용하며, 프로덕션 코드는 건드리지 않는다.
//          (private 필드/메서드는 리플렉션으로 주입)
//
// 사용법 : _InGame 씬의 빈 GameObject에 이 컴포넌트를 붙이고 Play.
//          확인이 끝나면 이 파일과 GameObject를 통째로 삭제하면 된다.
//
// 처리 : ① 런타임 PoolManager + Projectile_Basic 풀(코드 생성 투사체)
//        ② 더미 정렬 스킬 5종 + 콤보 스킬 등록
//        ③ SkillSystem._firePoint 주입
//        ④ 더미 몬스터 스폰 + MonsterSpawner._aliveMonsters 등록 (사망 시 자동 리스폰)
//        ⑤ CombatSystem 자동공격 ON

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DummyCombatTester : MonoBehaviour
{
    [Header("더미 몬스터")]
    [Tooltip("동시에 유지할 더미 몬스터 수")]
    [SerializeField] private int _monsterCount = 3;

    [Tooltip("더미 몬스터 체력")]
    [SerializeField] private int _monsterHP = 60;

    [Tooltip("더미 몬스터 스폰 Y (위에서 아래로 내려옴)")]
    [SerializeField] private float _spawnY = 5f;

    [Tooltip("사망 후 리스폰 딜레이(초)")]
    [SerializeField] private float _respawnDelay = 1f;

    [Header("플레이어 발사 위치")]
    [SerializeField] private Vector3 _firePointPos = new Vector3(0f, -2.5f, 0f);

    [Header("더미 스킬")]
    [Tooltip("정렬 스킬 기본 데미지")]
    [SerializeField] private int _sortSkillDamage = 25;

    [Tooltip("콤보 스킬이 발동할 콤보 배수")]
    [SerializeField] private int[] _comboMultiples = { 5, 10 };

    // ---------- 참조 ----------
    private SkillSystem _skillSystem;
    private CombatSystem _combatSystem;
    private MonsterSpawner _spawner;
    private PlayerModel _playerModel;
    private List<MonsterAI> _spawnerAliveList;

    private Sprite _squareSprite;

    private IEnumerator Start()
    {
        // InGameBootstrap이 모델/Sort를 초기화할 시간을 한 프레임 준다.
        yield return null;

        _squareSprite = BuildSquareSprite();

        _skillSystem = FindFirstObjectByType<SkillSystem>();
        _combatSystem = FindFirstObjectByType<CombatSystem>();
        _spawner = FindFirstObjectByType<MonsterSpawner>();
        _playerModel = FindFirstObjectByType<PlayerModel>();

        if (_skillSystem == null || _combatSystem == null || _spawner == null)
        {
            Debug.LogError("[DummyCombatTester] SkillSystem/CombatSystem/MonsterSpawner를 찾지 못했습니다.");
            yield break;
        }

        EnsurePoolManagerWithProjectile();
        InjectFirePoint();
        RegisterDummySkills();
        CacheSpawnerAliveList();

        for (int i = 0; i < _monsterCount; i++)
            SpawnDummyMonster();

        _combatSystem.EnableAutoAttack();

        Debug.Log("[DummyCombatTester] 준비 완료. 정렬하거나 자동공격으로 투사체가 발사됩니다.");
    }

    // ---------- ① PoolManager + 투사체 풀 ----------
    private void EnsurePoolManagerWithProjectile()
    {
        var pm = PoolManager.Instance;
        if (pm == null)
            pm = new GameObject("[DUMMY] PoolManager").AddComponent<PoolManager>();

        // 코드로 생성한 투사체 템플릿 (노란 사각형 + 트리거 콜라이더 + 키네마틱 RB)
        var template = new GameObject("[DUMMY] Projectile_Basic");
        template.SetActive(false);
        template.transform.localScale = Vector3.one * 0.3f;

        var sr = template.AddComponent<SpriteRenderer>();
        sr.sprite = _squareSprite;
        sr.color = Color.yellow;
        sr.sortingOrder = 50;

        var rb = template.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = template.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        template.AddComponent<ProjectileController>();

        // PoolManager.RegisterPool(private) 호출. 이미 있으면 내부에서 무시됨.
        var mi = typeof(PoolManager).GetMethod("RegisterPool",
            BindingFlags.NonPublic | BindingFlags.Instance);
        mi.Invoke(pm, new object[] { "Projectile_Basic", template, 20 });
    }

    // ---------- ② 더미 스킬 등록 ----------
    private void RegisterDummySkills()
    {
        for (int i = 0; i < SortModel.UNIT_TYPE_COUNT; i++)
        {
            var data = new Legacy_SkillData
            {
                SkillID = 9000 + i,
                Dmg = _sortSkillDamage + i * 5, // 유닛 타입별로 데미지 차등
                Speed = 12,
            };
            _skillSystem.RegisterSortSkill((UnitType)i, data);
        }

        foreach (int multiple in _comboMultiples)
        {
            var combo = new Legacy_SkillData { SkillID = 9100 + multiple, Dmg = _sortSkillDamage * 3, Speed = 14 };
            _skillSystem.RegisterComboSkill(multiple, combo);
        }
    }

    // ---------- ③ FirePoint 주입 ----------
    private void InjectFirePoint()
    {
        var fp = new GameObject("[DUMMY] FirePoint").transform;
        fp.position = _firePointPos;

        var field = typeof(SkillSystem).GetField("_firePoint",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(_skillSystem, fp);
    }

    // ---------- ④ 더미 몬스터 ----------
    private void CacheSpawnerAliveList()
    {
        var field = typeof(MonsterSpawner).GetField("_aliveMonsters",
            BindingFlags.NonPublic | BindingFlags.Instance);
        _spawnerAliveList = field.GetValue(_spawner) as List<MonsterAI>;
    }

    private void SpawnDummyMonster()
    {
        var go = new GameObject("[DUMMY] Monster");
        float x = Random.Range(-2.2f, 2.2f);
        go.transform.position = new Vector3(x, _spawnY, 0f);
        go.transform.localScale = Vector3.one * 0.6f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _squareSprite;
        sr.color = Color.red;
        sr.sortingOrder = 40;

        go.AddComponent<BoxCollider2D>();

        // MonsterAI.AttackSupport가 _animator.SetTrigger를 무조건 호출하므로
        // NPE 방지를 위해 Animator를 붙이고 리플렉션으로 주입한다.
        var animator = go.AddComponent<Animator>();
        var ai = go.AddComponent<MonsterAI>();
        typeof(MonsterAI).GetField("_animator", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(ai, animator);

        var stats = new CommonStatusData { Character_ID = 9001, MaxHP = _monsterHP, Attack = 5, BaseAttackSpeed = 1f };
        var monsterStats = new MonsterStatusData
        {
            Character_ID = 9001,
            Defense = 0,
            MoveSpeed = 1.2f,
            Range = 1,
            EXP = 10,
            MovementPattern = "Straight",
        };
        ai.Initialize(stats, monsterStats, 9001);
        if (_playerModel != null)
            ai.SetPlayerModel(_playerModel);

        ai.OnDeath += HandleDummyMonsterDeath;
        _spawnerAliveList.Add(ai); // 스포너의 타겟 후보로 등록
    }

    private void HandleDummyMonsterDeath(MonsterAI monster)
    {
        monster.OnDeath -= HandleDummyMonsterDeath;
        _spawnerAliveList.Remove(monster);
        monster.gameObject.SetActive(false);
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnDummyMonster();
    }

    // ---------- 유틸 ----------
    private static Sprite BuildSquareSprite()
    {
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}
#endif
