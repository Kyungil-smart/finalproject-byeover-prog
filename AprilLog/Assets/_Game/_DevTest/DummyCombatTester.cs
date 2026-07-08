// 담당자 : 정승우
// 설명   : 플레이어 공격 파이프라인을 _InGame 단독 Play로 확인하는 throwaway 하니스 (삭제 예정)

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Animations;
using UnityEngine;

public class DummyCombatTester : MonoBehaviour
{
    // ---------- SerializeField ----------
    [Header("더미 몬스터")]
    [Tooltip("동시에 유지할 더미 몬스터 수")]
    [SerializeField] private int _monsterCount = 3;

    [Tooltip("더미 몬스터 체력")]
    [SerializeField] private int _monsterHP = 60;

    [Tooltip("더미 몬스터 스폰 Y. 위에서 아래로 내려옴")]
    [SerializeField] private float _spawnY = 5f;

    [Tooltip("사망 후 리스폰 딜레이(초)")]
    [SerializeField] private float _respawnDelay = 1f;

    [Header("플레이어 발사 위치")]
    [Tooltip("투사체가 발사되는 기준 좌표")]
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
    private RuntimeAnimatorController _dummyController;

    // ---------- 생명주기 ----------
    private IEnumerator Start()
    {
        // InGameBootstrap이 모델과 Sort를 먼저 초기화하도록 한 프레임 양보한다.
        yield return null;

        _squareSprite = BuildSquareSprite();
        _dummyController = BuildDummyAnimatorController();

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
        // InjectFirePoint();  // 발사점은 InGameBootstrap의 PlayerView가 제공한다(덮어쓰기 방지).
        RegisterDummySkills();
        CacheSpawnerAliveList();

        // 임시 타겟용 더미 몬스터만 공급한다. (정식 웨이브 시스템 배치 전까지 한시적)
        for (int i = 0; i < _monsterCount; i++)
            SpawnDummyMonster();

        // 자동공격은 기획상 '자동공격 인챈트' 선택 시에만 켜진다. 상시 켜지 않는다.
        // _combatSystem.EnableAutoAttack();

        Debug.Log("[DummyCombatTester] 준비 완료. 3-sort 정렬을 완성하면 플레이어가 발사합니다.");
    }

    // ---------- 풀 / 투사체 ----------
    private void EnsurePoolManagerWithProjectile()
    {
        // _InGame 단독 실행이면 PoolManager가 없으므로 런타임에 만든다.
        var pm = PoolManager.Instance;
        if (pm == null)
            pm = new GameObject("[DUMMY] PoolManager").AddComponent<PoolManager>();

        var template = new GameObject("[DUMMY] Projectile_Basic");
        template.SetActive(false);
        template.transform.localScale = Vector3.one * 0.3f;

        var sr = template.AddComponent<SpriteRenderer>();
        sr.sprite = _squareSprite;
        sr.color = Color.yellow;
        sr.sortingOrder = 50;

        // 트리거 충돌 콜백을 받으려면 한쪽에 Rigidbody2D가 필요하다.
        var rb = template.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = template.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;

        template.AddComponent<ProjectileController>();

        // RegisterPool은 private이고, 이미 등록돼 있으면 내부에서 무시된다.
        var register = typeof(PoolManager).GetMethod("RegisterPool",
            BindingFlags.NonPublic | BindingFlags.Instance);
        register.Invoke(pm, new object[] { "Projectile_Basic", template, 20 });
    }

    // ---------- 스킬 등록 ----------
    private void RegisterDummySkills()
    {
        for (int i = 0; i < SortModel.UNIT_TYPE_COUNT; i++)
        {
            // 유닛 타입별로 데미지를 다르게 줘서 발사 차이를 눈으로 구분한다.
            var data = new Legacy_SkillData
            {
                SkillID = 9000 + i,
                DmgRate = _sortSkillDamage + i * 5,
                Speed = 12,
            };
            _skillSystem.RegisterSortSkill((UnitType)i, data);
        }

        foreach (int multiple in _comboMultiples)
        {
            var combo = new Legacy_SkillData { SkillID = 9100 + multiple, DmgRate = _sortSkillDamage * 3, Speed = 14 };
            _skillSystem.RegisterComboSkill(multiple, combo);
        }
    }

    // ---------- 발사 위치 ----------
    private void InjectFirePoint()
    {
        var firePoint = new GameObject("[DUMMY] FirePoint").transform;
        firePoint.position = _firePointPos;

        // 씬의 SkillSystem._firePoint가 비어 있어 공격이 건너뛰어지므로 주입한다.
        var field = typeof(SkillSystem).GetField("_firePoint",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(_skillSystem, firePoint);
    }

    // ---------- 더미 몬스터 ----------
    private void CacheSpawnerAliveList()
    {
        // 스포너는 자기 alive 리스트만 타겟 후보로 보므로 직접 참조를 확보한다.
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

        // AttackSupport가 _animator.SetTrigger를 무조건 호출해서, 비어 있으면 NPE가 난다.
        // 컨트롤러가 없으면 SetBool/SetTrigger마다 "Animator is not playing an AnimatorController"
        // 경고가 나므로, 파라미터만 가진 더미 컨트롤러를 붙여 경고를 막는다.
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = _dummyController;
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
        _spawnerAliveList.Add(ai);
    }

    private void HandleDummyMonsterDeath(MonsterAI monster, bool isKamikaze = false, bool isBoss = false, bool isElite = false) // 엘리트 인자 추가(최동훈)
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
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // MonsterAI가 참조하는 "Move"(Bool) / "Attack"(Trigger) 파라미터만 정의한 빈 컨트롤러.
    // 에셋으로 저장하지 않고 메모리에만 만들어 모든 더미 몬스터가 공유한다.
    private static RuntimeAnimatorController BuildDummyAnimatorController()
    {
        var controller = new AnimatorController();
        controller.name = "[DUMMY] MonsterAnimator";
        controller.AddLayer("Base");
        controller.AddParameter("Move", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        return controller;
    }
}
#endif
