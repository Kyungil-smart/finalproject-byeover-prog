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
 
    private void OnEnable()
    {
        ResolveRepository();

        if (_spawner != null)
            _spawner.OnMonsterDied += HandleMonsterDied;

        // 데드락 발생 시 EXP 10% 감소(기획 4-2-6) 연결
        if (_sortSystem == null) _sortSystem = FindFirstObjectByType<SortSystem>();
        if (_sortSystem != null)
            _sortSystem.OnDeadlockDetected += ApplyDeadlockPenalty;
    }

    private void OnDisable()
    {
        if (_spawner != null)
            _spawner.OnMonsterDied -= HandleMonsterDied;

        if (_sortSystem != null)
            _sortSystem.OnDeadlockDetected -= ApplyDeadlockPenalty;
    }
 
    private void HandleMonsterDied(MonsterAI monster, bool isKamikaze = false)
    {
        if (!isKamikaze)
        {
            AddEXP(monster.Exp);
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

    private void ResolveRepository()
    {
        if (_configRepo != null) return;
        if (DataManager.Instance == null) return;

        _configRepo = DataManager.Instance.ConfigRepo;
    }
}
