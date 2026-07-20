using UnityEngine;

public class EliteMonsterReward : MonoBehaviour
{
    [SerializeField] private EnchantSelectView _enchantView;

    public EnchantPopupManager Manager { get; set; }

    public void OpenEnchantPopup()
    {
        if (Manager != null && Manager.IsChangePopupActive())
        {
            return;
        }

        _enchantView.gameObject.SetActive(false);
        StartCoroutine(ResetAndShow());
    }


    private System.Collections.IEnumerator ResetAndShow()
    {
        yield return null;

        _enchantView.gameObject.SetActive(true);

        _enchantView.Show();
    }
}
