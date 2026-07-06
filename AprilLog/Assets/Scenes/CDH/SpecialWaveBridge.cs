using UnityEngine;

public class SpecialWaveBridge : MonoBehaviour
{
    [SerializeField] private EliteBattleResult _eliteBattleResult;

    public void TriggerReward()
    {
        if (_eliteBattleResult != null)
        {
            _eliteBattleResult.gameObject.SetActive(true);
            _eliteBattleResult.StartRewardProcess();
        }
    }
}
