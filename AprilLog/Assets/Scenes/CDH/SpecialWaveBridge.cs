using UnityEngine;

public class SpecialWaveBridge : MonoBehaviour
{
    [SerializeField] private EliteBattleResult _eliteBattleResult;
    [SerializeField] private MonsterSpawner _spawner;
    [SerializeField] private JokerSystem _jokerSystem;

    private void OnEnable()
    {
        if (_spawner != null)
        {
            _spawner.OnEliteDeath += TriggerReward;
            _spawner.IsBossDeath += ForceStopAllEffects;
        }
    }

    private void OnDisable()
    {
        if (_spawner != null)
        {
            _spawner.OnEliteDeath -= TriggerReward;
            _spawner.IsBossDeath -= ForceStopAllEffects;
        }
    }

    public void TriggerReward()
    {
        if (_eliteBattleResult != null)
        {
            _eliteBattleResult.gameObject.SetActive(true);
            _eliteBattleResult.StartRewardProcess();
        }
    }

    public void ForceStopAllEffects()
    {
        if (_jokerSystem != null)
        {
            _jokerSystem.ForceStopJokerEffect();
        }
    }
}
