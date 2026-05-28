using TMPro;
using UnityEngine;

// 작성자 : 홍정옥
// 설명 : 상단바 정보 연동

public class LobbyTopBarUI : MonoBehaviour
{
    [Header("프로필 UI")]
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text levelText;

    [Header("재화 UI")]
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text goldText;

    public void SetNickname(string nickname)
    {
        if (nicknameText == null)
            return;

        nicknameText.text = string.IsNullOrWhiteSpace(nickname) ? "Guest" : nickname;
    }

    public void SetLevel(int level)
    {
        if (levelText == null)
            return;

        levelText.text = $"Lv.{Mathf.Max(1, level)}";
    }

    public void SetCurrency(int stamina, int gold)
    {
        if (staminaText != null)
            staminaText.text = Mathf.Max(0, stamina).ToString();

        if (goldText != null)
            goldText.text = Mathf.Max(0, gold).ToString("N0");
    }
}