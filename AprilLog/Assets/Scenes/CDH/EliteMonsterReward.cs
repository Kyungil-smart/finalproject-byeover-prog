using UnityEngine;

public class EliteMonsterReward : MonoBehaviour
{
    [SerializeField] private EnchantSelectView _enchantView;

    public EnchantPopupManager Manager { get; set; }

    public void OpenEnchantPopup()
    {
        _enchantView.gameObject.SetActive(false);

        StartCoroutine(ResetAndShow());
    }

    private System.Collections.IEnumerator ResetAndShow()
    {
        yield return null;

        _enchantView.gameObject.SetActive(true);

        _enchantView.Show();

        Debug.Log("[Reward] 팝업 다시 호출 완료");
    }

    public void CloseEnchantPopup()
    {
        Debug.Log($"[Reward] CloseEnchantPopup 호출됨. Manager 존재 여부: {Manager != null}");
        if (Manager != null)
        {
            Manager.ShowNextPopup();
        }

        else
        {
            Debug.LogError("[Reward] Manager가 null입니다! 연결을 확인하세요.");
        }
    }
}
