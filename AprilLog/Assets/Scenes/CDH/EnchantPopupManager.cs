using UnityEngine;

public class EnchantPopupManager : MonoBehaviour
{
    [SerializeField] private EliteMonsterReward _rewardSystem;
    private int _remainingPopups = 0;

    private void Awake()
    {
        if (_rewardSystem != null)
            _rewardSystem.Manager = this;
    }

    public void StartRewardSequence()
    {
        float rand = Random.value;
        if (rand < 0.1f) _remainingPopups = 3;
        else if (rand < 0.4f) _remainingPopups = 2;
        else _remainingPopups = 1;

        Debug.Log($"[EnchantPopup] 확률 결과: {rand:F2} (0~0.1: 10%, 0.1~0.4: 30%, 0.4~1.0: 60%) | 생성된 팝업 횟수: {_remainingPopups}");

        ShowNextPopup();
    }

    public void ShowNextPopup()
    {
        if (_remainingPopups > 0)
        {
            _remainingPopups--;
            _rewardSystem.OpenEnchantPopup();
        }
    }
}
