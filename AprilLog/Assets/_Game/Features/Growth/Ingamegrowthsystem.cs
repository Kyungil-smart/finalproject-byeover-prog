// 담당자 : 정승우
// 설명   : 인게임 레벨/EXP 관리

// 1차 수정자 : 김영찬 ->
// 수정내용 : Repository를 DataManager 싱글톤의 자식으로 편입하여, DataManager의 Instance를 통해 호출하는것으로 수정

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
    [SerializeField] private int _baseExpPerKill = 50;
 
    // ---------- 데이터 ----------
    public int CurrentLevel { get; private set; }
    public int CurrentEXP { get; private set; }
 
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
 
    public void AddEXP(int amount)
    {
        if (CurrentLevel >= _maxLevel) return;
 
        CurrentEXP += amount;
 
        var levelData = _configRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;
 
        int required = levelData.RequiredEXP;
 
        while (CurrentEXP >= required && CurrentLevel < _maxLevel)
        {
            CurrentEXP -= required;
            CurrentLevel++;
 
            _playerModel.Heal(levelData.HPRecovery);
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
        var levelData = _configRepo.GetInLevel(CurrentLevel);
        if (levelData == null) return;
 
        int penalty = Mathf.RoundToInt(levelData.RequiredEXP * 0.1f);
        CurrentEXP = Mathf.Max(0, CurrentEXP - penalty);
        OnEXPChanged?.Invoke(CurrentEXP, levelData.RequiredEXP);
    }
}