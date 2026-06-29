using UnityEngine;

public class EliteBattleResult : MonoBehaviour
{
    public EliteMonsterReward rewardSystem;
    public EnchantPopupManager popupManager;
    public JokerSystem jokerSystem;

    public void StartRewardProcess()
    {
        gameObject.SetActive(false);

        if (jokerSystem != null)
        {
            jokerSystem.RestoreJokerImages();
        }

        if (popupManager != null)
        {
            popupManager.StartRewardSequence();
        }
    }
}
