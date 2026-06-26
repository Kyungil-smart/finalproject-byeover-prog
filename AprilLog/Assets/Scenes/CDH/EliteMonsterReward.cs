using UnityEngine;

public class EliteMonsterReward : MonoBehaviour
{
    [SerializeField] private EnchantSelectView _enchantView;

    public EnchantPopupManager Manager { get; set; }

    public void OpenEnchantPopup()
    {
        if (_enchantView != null)
        {
            _enchantView.Show();
        }
    }

    public void CloseEnchantPopup()
    {
        if (Manager != null)
        {
            Manager.ShowNextPopup();
        }
    }
}
