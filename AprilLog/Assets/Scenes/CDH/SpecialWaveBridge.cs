using UnityEngine;

public class SpecialWaveBridge : MonoBehaviour
{
    [SerializeField] private EliteBattleResult _eliteBattleResult;
    [SerializeField] private MonsterSpawner _spawner;

    private void OnEnable()
    {
        if (_spawner != null)
        {
            _spawner.OnEliteDeath += TriggerReward;
        }
    }

    private void OnDisable()
    {
        if (_spawner != null)
        {
            _spawner.OnEliteDeath -= TriggerReward;
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
}
