using UnityEngine;

public class EliteBattleResult : MonoBehaviour
{
    public EliteRewardEffect rewardEffect;
    public EliteMonsterReward rewardSystem;
    public EnchantPopupManager popupManager;
    public JokerSystem jokerSystem;

    public void StartRewardProcess()
    {
        if (popupManager != null)
        {
            popupManager.StartRewardSequence();
        }

        gameObject.SetActive(false);
    }
}
