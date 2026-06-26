using UnityEngine;

public class EliteBattleResult : MonoBehaviour
{
    public EliteMonsterReward rewardSystem;
    public EnchantPopupManager popupManager;
    public JokerSystem jokerSystem;

    private void OnDisable()
    {
        if (popupManager != null)
        {
            popupManager.StartRewardSequence();
        }

        if (jokerSystem != null)
        {
            jokerSystem.RestoreJokerImages();
        }
    }

    public void OnEliteMonsterDefeated()
    {
        rewardSystem.OpenEnchantPopup();
    }
}
