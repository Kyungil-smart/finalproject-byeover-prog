using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 홍정옥
// 설명   : 분해 팝업의 예상 보상 슬롯(RewardPreviewSlot 프리팹).
//          분해 시 받게 될 재화 하나(아이콘 + 수량)를 표시한다.
//          보상이 없는 종류는 슬롯 자체를 생성하지 않으므로, 이 뷰는 "값이 있는 보상"만 그린다.
public class RewardPreviewSlotView : MonoBehaviour
{
    [Header("RewardPreviewSlot 구성")]
    [SerializeField] private Image _icon;          // Slot/RewardPreviewIcon
    [SerializeField] private TMP_Text _amountText; // RewardAmount/Text_RewardAmount

    // 보상 한 종류를 채운다. icon 이 없으면 아이콘은 그대로 둔다(아이콘 소스 미연동 대비).
    public void SetReward(Sprite icon, int amount)
    {
        if (_icon != null && icon != null)
            _icon.sprite = icon;

        if (_amountText != null)
            _amountText.text = FormatAmount(amount);
    }

    // 큰 수량은 K 단위로 축약(프리팹 예시 "99.9K" 와 동일한 표기).
    private static string FormatAmount(int amount)
    {
        if (amount >= 1000)
            return $"{amount / 1000f:0.#}K";
        return amount.ToString();
    }
}
