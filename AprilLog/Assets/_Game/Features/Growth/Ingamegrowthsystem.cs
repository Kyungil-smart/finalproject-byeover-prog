// 담당자 : 정승우
// 설명   : 인게임 레벨/EXP 관리

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

// 수정자 : 정승우
// 수정내용 : ConfigRepo가 Inspector에 연결되지 않아도 DataManager에서 자동 참조

// 수정자 : 김영찬
// DataManager 최신화 중 기존 연결을 Legacy로 변경 및 최신화 완료

// 수정자 : 김영찬
// 몬스터 사망 시 획득 Exp와 연결 완료

// 3차 수정자 : 조규민
// 수정 내용 : 데드락 경험치 패널티 즉시 적용 제거 및 안내 팝업 예 선택 적용 분리

using System;
using UnityEngine;
 
/// <summary>
/// 몬스터 처치 -> EXP 획득 -> 레벨업 흐름을 관리한다.
/// </summary>
public class InGameGrowthSystem : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnEXPChanged;
 
    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private ScreenNavigator _navigator;
    [SerializeField] private ConfigRepo _configRepo;
    [SerializeField] private PlayerModel _playerModel;
 
    [Header("설정")]
    [SerializeField] private int _maxLevel = 30;

    // 데드락 통지(SortSystem.OnDeadlockDetected) 구독용. 비면 런타임 자동 탐색.
    [SerializeField] private SortSystem _sortSystem;
 
    // ---------- 데이터 ----------
    public int CurrentLevel { get; private set; }
    public int CurrentEXP { get; private set; }
 
    public void Initialize()
    {
        ResolveRepository();

        CurrentLevel = 1;
        CurrentEXP = 0;
    }
 
    public void RestoreFromSave(int level, int exp)
    {
        ResolveRepository();

        CurrentLevel = level;
        CurrentEXP = exp;
    }

    /// <summary>
    /// 현재 레벨/EXP 상태를 이벤트로 한 번 발행한다.
    /// HUD Presenter가 모델 초기화 이후에 생성되어 구독만으론 현재 값을 못 받는 경우,
    /// 이걸 호출해 초기 동기화(placeholder 덮어쓰기)를 한다.
    /// </summary>
    public void EmitCurrentState()
    {
        ResolveRepository();

        OnLevelUp?.Invoke(CurrentLevel);

        int required = 0;
        if (_configRepo != null)
        {
            var levelData = _configRepo.GetInLevel(CurrentLevel);
            if (levelData != null) required = levelData.RequiredEXP;
        }
        OnEXPChanged?.Invoke(CurrentEXP, required);
    }
 
    private void OnEnable()
    {
        ResolveRepository();   // _spawner/_navigator/_sortSystem 등 자가 연결

        if (_spawner != null)
            _spawner.OnMonsterDied += HandleMonsterDied;

        // 데드락 패널티 수동 적용 전환
        // 추가: 조규민 - 데드락 패널티 안내 팝업 예 선택 시 적용
    }

    private void OnDisable()
    {
        if (_spawner != null)
            _spawner.OnMonsterDied -= HandleMonsterDied;

        // 데드락 패널티 자동 구독 없음.
    }
 
    private void HandleMonsterDied(MonsterAI monster, bool isKamikaze = false)
    {
        if (!isKamikaze)
        {
            int exp = monster.Exp;
            if (TutorialInGameDirector.TryGetTutorialMonsterExpOverride(out int tutorialExp))
            {
                exp = tutorialExp;
            }

            AddEXP(exp);
        }
    }
 
    public void AddEXP(int amount)
    {
        ResolveRepository();
        if (_configRepo == null)
        {
            Debug.LogError("[InGameGrowthSystem] ConfigRepo를 찾을 수 없어 EXP 처리를 중단합니다.");
            return;
        }

        if (CurrentLevel >= _maxLevel) return;
 
        CurrentEXP += amount;
 
        var levelData = _configRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;
 
        int required = levelData.RequiredEXP;
 
        while (CurrentEXP >= required && CurrentLevel < _maxLevel)
        {
            CurrentEXP -= required;
            CurrentLevel++;

            // HPRecovery는 최대 체력 대비 비율(예: 0.08 = 8%)이다.
            // 기존엔 RoundToInt(0.08)=0 이라 회복이 전혀 적용되지 않았다.
            if (_playerModel != null)
                _playerModel.Heal(Mathf.RoundToInt(_playerModel.MaxHP * levelData.HPRecovery));
            OnLevelUp?.Invoke(CurrentLevel);
 
            if (_navigator != null)
                _navigator.ShowEnchantSelection();
 
            levelData = _configRepo.GetInLevel(CurrentLevel);
            if (levelData == null) break;
            required = levelData.RequiredEXP;
        }
 
        OnEXPChanged?.Invoke(CurrentEXP, required);
    }
 
    public void ApplyDeadlockPenalty()
    {
        ResolveRepository();
        if (_configRepo == null)
        {
            Debug.LogError("[InGameGrowthSystem] ConfigRepo를 찾을 수 없어 데드락 패널티를 처리할 수 없습니다.");
            return;
        }

        var levelData = _configRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;
 
        int penalty = Mathf.RoundToInt(levelData.RequiredEXP * 0.1f);
        CurrentEXP = Mathf.Max(0, CurrentEXP - penalty);
        OnEXPChanged?.Invoke(CurrentEXP, levelData.RequiredEXP);
    }

    // 씬에 직렬화 참조가 비어 있어도 동작하도록 모든 의존성을 자가 연결한다 (다른 시스템과 동일 패턴).
    // 특히 _navigator(레벨업 시 인챈트 팝업 호출)와 _spawner(몬스터 처치 EXP 수신)가 핵심.
    private void ResolveRepository()
    {
        if (_configRepo == null && DataManager.Instance != null)
            _configRepo = DataManager.Instance.ConfigRepo;

        if (_spawner == null) _spawner = FindFirstObjectByType<MonsterSpawner>();
        if (_navigator == null) _navigator = FindFirstObjectByType<ScreenNavigator>();
        if (_playerModel == null) _playerModel = FindFirstObjectByType<PlayerModel>();
        if (_sortSystem == null) _sortSystem = FindFirstObjectByType<SortSystem>();
    }
}
