// 담당자 : 정승우
// 설명   : 인게임 레벨/EXP 관리

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

using System;
using UnityEngine;

/// <summary>
/// 몬스터 처치 -> EXP 획득 -> 레벨업 -> 인챈트 선택 흐름을 관리한다.
/// 경험치 초과분은 다음 레벨로 이월됨.
/// </summary>
public class InGameGrowthSystem : MonoBehaviour
{
    // ---------- 이벤트 ----------
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnEXPChanged;     // current, required

    // ---------- SerializeField ----------
    [Header("참조")]
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private ScreenNavigator _navigator;
    [SerializeField] private PlayerModel _playerModel;

    [Header("설정")]
    [Tooltip("인게임 최대 레벨")]
    [SerializeField] private int _maxLevel = 30;

    [Tooltip("몬스터 처치당 기본 경험치")]
    [SerializeField] private int _baseExpPerKill = 50;

    // ---------- 데이터 ----------
    public int CurrentLevel { get; private set; }
    public int CurrentEXP { get; private set; }

    // ---------- 초기화 ----------
    public void Initialize()
    {
        CurrentLevel = 1;
        CurrentEXP = 0;
    }

    public void RestoreFromSave(int level, int exp)
    {
        CurrentLevel = level;
        CurrentEXP = exp;
    }

    // ---------- 생명주기 ----------
    private void OnEnable()
    {
        if (_spawner != null)
            _spawner.OnMonsterDied += HandleMonsterDied;
    }

    private void OnDisable()
    {
        if (_spawner != null)
            _spawner.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(MonsterAI monster)
    {
        AddEXP(_baseExpPerKill);
    }

    // ---------- EXP ----------
    public void AddEXP(int amount)
    {
        if (CurrentLevel >= _maxLevel) return;

        CurrentEXP += amount;

        var levelData = DataManager.Instance.ConfigRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;

        int required = levelData.RequiredEXP;

        // 초과분 이월: 필요 1000인데 1200 들어오면 레벨업 + 잔여 200
        while (CurrentEXP >= required && CurrentLevel < _maxLevel)
        {
            CurrentEXP -= required;
            CurrentLevel++;

            // 레벨업 시 HP 회복
            _playerModel.Heal(levelData.HPRecovery);

            OnLevelUp?.Invoke(CurrentLevel);

            // 인챈트 선택 팝업
            if (_navigator != null)
                _navigator.ShowEnchantSelection();

            // 다음 레벨 데이터
            levelData = DataManager.Instance.ConfigRepo.GetInLevel(CurrentLevel);
            if (levelData == null) break;
            required = levelData.RequiredEXP;
        }

        OnEXPChanged?.Invoke(CurrentEXP, required);
    }

    // 데드락 페널티: 경험치 10% 감소 (0 이하로는 안 내려감)
    public void ApplyDeadlockPenalty()
    {
        var levelData = DataManager.Instance.ConfigRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;

        int penalty = Mathf.RoundToInt(levelData.RequiredEXP * 0.1f);
        CurrentEXP = Mathf.Max(0, CurrentEXP - penalty);
        OnEXPChanged?.Invoke(CurrentEXP, levelData.RequiredEXP);
    }
}