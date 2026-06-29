using UnityEngine;

public class EnchantPopupConnector : MonoBehaviour
{
    [SerializeField] private EnchantSelectView _view;
    [SerializeField] private EliteMonsterReward _rewardSystem;

    private void OnEnable()
    {
        if (_view != null)
        {
            _view.OnChoiceSelected += HandleChoiceSelected;
        }
    }

    private void OnDisable()
    {
        if (_view != null)
        {
            _view.OnChoiceSelected -= HandleChoiceSelected;
        }
    }

    private void HandleChoiceSelected(int index)
    {
        if (_rewardSystem != null)
        {
            _rewardSystem.CloseEnchantPopup();
        }
    }
}
